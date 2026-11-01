using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Naknak.Inventory;
using UnityEngine;

/// <summary>Ordered, inspector-editable investigation rules using existing inventory/dialogue/flow assets.</summary>
public sealed class InvestigationPoint : MonoBehaviour, IInteractable
{
    [Serializable]
    public sealed class Rule
    {
        public string memo;
        public List<DialogCondition> conditions = new List<DialogCondition>();
        public InventoryItem[] requiredItems = Array.Empty<InventoryItem>();
        public InventoryItem[] anyItems = Array.Empty<InventoryItem>();
        public InventoryItem[] rewards = Array.Empty<InventoryItem>();
        public List<DialogAction> actions = new List<DialogAction>();
        public StoryData dialogue;
        public bool showUnknownSpace;
        public bool presentEvidence;
        public string evidencePrompt;
        public List<DialogCondition> evidenceConditions = new List<DialogCondition>();
        public StoryData wrongEvidenceDialog;
        public StoryData missingEvidenceDialog;
        public string connectionId;
    }

    [SerializeField] private bool memoryOnly = true;
    [SerializeField] private SceneFlowContainer flow;
    [SerializeField] private Rule[] rules = Array.Empty<Rule>();
    [SerializeField] private SpriteRenderer sparkle;
    [SerializeField] private ConditionID collected;
    [Tooltip("Optional feedback when investigation is still locked on the first memory visit.")]
    [SerializeField] private StoryData firstMemoryDialog;
    private bool busy;
    public bool HasFirstMemoryFeedback => firstMemoryDialog != null;

    private void Update()
    {
        if (sparkle != null)
            sparkle.enabled = !SceneLoadManager.IsFirstMemoryVisit &&
                (collected == null || ConditionManager.Instance == null || ConditionManager.Instance.Get(collected) == 0);
    }

    public void Interact()
    {
        Interact(false);
    }

    public void PresentEvidence()
    {
        if (SceneLoadManager.IsMemoryRevisit) Interact(true);
    }

    private void Interact(bool evidence)
    {
        if (busy || GameMenuController.Instance == null || ConditionManager.Instance == null ||
            DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog ||
            GameMenuController.IsBlockingGameplay || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsRestoringSession ||
            SceneLoadManager.IsCutscene || DialogManager.Instance.IsDialogOpen ||
            memoryOnly && !SceneLoadManager.IsMemorySpace ||
            GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;
        if (SceneLoadManager.IsFirstMemoryVisit)
        {
            if (!evidence && firstMemoryDialog != null) Explain(firstMemoryDialog);
            return;
        }
        var inventory = GameMenuController.Inventory;
        var reconstruction = GetComponent<EvidenceReconstruction>();
        if (reconstruction != null && SceneLoadManager.IsMemoryRevisit)
        {
            var presentation = rules.FirstOrDefault(r => r != null && r.presentEvidence && SceneFlowContainer.Passes(r.conditions));
            if (presentation != null)
            {
                if (reconstruction.ReadyForConclusion)
                { Explain(reconstruction.CompletionDialog); return; }
                // Inspecting the witness still collects their physical evidence before opening selection.
                foreach (var rule in rules)
                {
                    if (rule == null || rule.presentEvidence || rule.rewards.Length == 0 ||
                        !SceneFlowContainer.Passes(rule.conditions) || !HasRequiredEvidence(rule, inventory)) continue;
                    var rewards = rule.rewards.Where(item => item != null && !item.Retired && !inventory.Has(item.Id))
                        .Select(item => new InventoryEntry(item.Id, 1)).ToArray();
                    if (inventory.TryAddAll(rewards)) ConditionManager.Instance.ExecuteActions(rule.actions);
                    break;
                }
                BeginEvidencePresentation(presentation);
                return;
            }
        }
        // Full conclusions are sequenced only after BOTH witnesses' evidence is resolved.
        foreach (var rule in rules)
        {
            if (rule == null || !SceneFlowContainer.Passes(rule.conditions)) continue;
            if (rule.presentEvidence)
            {
                if (evidence) { BeginEvidencePresentation(rule); return; }
                continue;
            }
            if (evidence) continue;
            if (!HasRequiredEvidence(rule, inventory)) continue;
            Execute(rule);
            return;
        }
    }

    private static bool HasRequiredEvidence(Rule rule, InventoryModel inventory) =>
        rule.requiredItems.All(item => item != null && inventory.Has(item.Id)) &&
        (rule.anyItems.Length == 0 || rule.anyItems.Any(item => item != null && inventory.Has(item.Id)));

    private void BeginEvidencePresentation(Rule rule)
    {
        busy = true;
        if (!GameMenuController.Instance.SelectInventoryItem(rule.evidencePrompt,
                item => SubmitEvidence(rule, item), () => busy = false,
                evidence: GetComponent<EvidenceReconstruction>())) busy = false;
    }

    private void SubmitEvidence(Rule rule, InventoryItem item)
    {
        busy = false;
        if (this == null || !isActiveAndEnabled || SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession ||
            SceneLoadManager.IsCutscene || GameMenuController.IsBlockingGameplay || ConditionManager.Instance == null ||
            GameMenuController.Instance == null || DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog ||
            DialogManager.Instance.IsDialogOpen || memoryOnly && !SceneLoadManager.IsMemorySpace ||
            GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;

        var inventory = GameMenuController.Inventory;
        var reconstruction = GetComponent<EvidenceReconstruction>();
        if (reconstruction != null)
        {
            if (!SceneFlowContainer.Passes(rule.conditions) || item == null || !inventory.Has(item.Id)) return;
            if (!reconstruction.TryPlay(item, rule.missingEvidenceDialog)) Explain(rule.wrongEvidenceDialog);
            return;
        }
        bool related = item != null && inventory.Has(item.Id) &&
            rule.requiredItems.Concat(rule.anyItems).Any(evidence => evidence != null && evidence.Id == item.Id);
        if (!related) { Explain(rule.wrongEvidenceDialog); return; }
        if (!SceneFlowContainer.Passes(rule.conditions) || !SceneFlowContainer.Passes(rule.evidenceConditions) ||
            !HasRequiredEvidence(rule, inventory)) { Explain(rule.missingEvidenceDialog); return; }

        // Presenting proof does not consume it. The same evidence remains available for later deductions and saves.
        Execute(rule);
    }

    private void Explain(StoryData dialogue)
    {
        if (dialogue != null) StartCoroutine(Talk(new Rule { dialogue = dialogue }));
    }

    private void Execute(Rule rule)
    {
        var connection = !string.IsNullOrEmpty(rule.connectionId) && flow != null ?
            flow.connections.Find(row => row.id == rule.connectionId) : null;
        if (!string.IsNullOrEmpty(rule.connectionId) &&
            (connection == null || !SceneFlowContainer.Passes(connection.conditions)))
        { Explain(rule.missingEvidenceDialog); return; }
        var inventory = GameMenuController.Inventory;
        var rewards = new List<InventoryEntry>();
        foreach (var item in rule.rewards)
        {
            if (item == null) { Debug.LogError("[Investigation] Missing reward asset: " + name, this); return; }
            if (item.Retired) continue;
            if (!inventory.Has(item.Id)) rewards.Add(new InventoryEntry(item.Id, 1));
        }
        if (!inventory.TryAddAll(rewards)) return;
        ConditionManager.Instance.ExecuteActions(rule.actions);
        if (connection != null) { SceneLoadManager.Instance?.Travel(flow, connection); return; }
        if (rule.dialogue != null) StartCoroutine(Talk(rule));
    }

    private IEnumerator Talk(Rule rule)
    {
        busy = true;
        var presentation = GetPresentation();
        if (rule.showUnknownSpace && presentation != null) presentation.SetUnknownSpace(true);
        DialogManager.Instance.ShowDialog(rule.dialogue);
        yield return null;
        while (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
        if (rule.showUnknownSpace && presentation != null) presentation.SetUnknownSpace(false);
        busy = false;
    }

    private TutorialScenePresentation GetPresentation()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var value = root.GetComponentInChildren<TutorialScenePresentation>(true);
            if (value != null) return value;
        }
        return null;
    }
}
