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
        public string connectionId;
    }

    [SerializeField] private bool memoryOnly = true;
    [SerializeField] private SceneFlowContainer flow;
    [SerializeField] private Rule[] rules = Array.Empty<Rule>();
    [SerializeField] private SpriteRenderer sparkle;
    [SerializeField] private ConditionID collected;
    private bool busy;

    private void Update()
    {
        if (sparkle != null)
            sparkle.enabled = collected == null || ConditionManager.Instance == null || ConditionManager.Instance.Get(collected) == 0;
    }

    public void Interact()
    {
        if (busy || GameMenuController.Instance == null || ConditionManager.Instance == null ||
            DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog ||
            GameMenuController.IsBlockingGameplay || SceneLoadManager.IsPreparingMemory || SceneLoadManager.IsRestoringSession ||
            SceneLoadManager.IsCutscene || DialogManager.Instance.IsDialogOpen ||
            memoryOnly && !SceneLoadManager.IsMemorySpace ||
            GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;
        var inventory = GameMenuController.Inventory;
        foreach (var rule in rules)
        {
            if (rule == null || !SceneFlowContainer.Passes(rule.conditions) ||
                rule.requiredItems.Any(item => item == null || !inventory.Has(item.Id)) ||
                rule.anyItems.Length > 0 && !rule.anyItems.Any(item => item != null && inventory.Has(item.Id))) continue;
            var rewards = new List<InventoryEntry>();
            foreach (var item in rule.rewards)
            {
                if (item == null) { Debug.LogError("[Investigation] Missing reward asset: " + name, this); return; }
                // Evidence is unique. Also accept older checkpoints containing the item but not its new flag.
                if (!inventory.Has(item.Id)) rewards.Add(new InventoryEntry(item.Id, 1));
            }
            if (!inventory.TryAddAll(rewards)) return;
            ConditionManager.Instance.ExecuteActions(rule.actions);
            if (!string.IsNullOrEmpty(rule.connectionId))
            {
                var connection = flow != null ? flow.connections.Find(row => row.id == rule.connectionId) : null;
                if (connection != null) SceneLoadManager.Instance?.Travel(flow, connection);
                return;
            }
            if (rule.dialogue != null) StartCoroutine(Talk(rule));
            return;
        }
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
