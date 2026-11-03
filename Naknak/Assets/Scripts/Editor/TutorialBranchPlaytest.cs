using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Naknak.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class TutorialRevisionPlaytest
{
    public static int OrdersTested { get; private set; }
    public static int MasksTested { get; private set; }
    public static long BranchAssertions { get; private set; }
    public static string BranchPhase { get; private set; }
    private sealed class EvidenceAction
    {
        public EvidenceReconstruction actor;
        public EvidenceReconstruction.Clip clip;
        public InventoryItem item;
    }

    public static void BeginBranches()
    {
        if (!UnityEditor.EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter Play mode first.");
        Passed.Clear(); walkingSamples = 0; OrdersTested = MasksTested = 0; BranchAssertions = 0;
        Result = "Running"; Running = true; Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(BranchSuite()));
    }

    private static void BranchAssert(bool valid, string label)
    {
        if (!valid) throw new InvalidOperationException(BranchPhase + ": " + label);
        BranchAssertions++;
    }

    private static List<EvidenceAction> ActionsInScene()
    {
        var result = new List<EvidenceAction>();
        foreach (string actor in new[] { "A", "B" })
        {
            var owner = Find<EvidenceReconstruction>(actor);
            var clips = (EvidenceReconstruction.Clip[])typeof(EvidenceReconstruction).GetField("clips", Private).GetValue(owner);
            foreach (var clip in clips)
                result.Add(new EvidenceAction { actor = owner, clip = clip,
                    item = clip.evidence.First(e => e != null && !e.Retired) });
        }
        return result;
    }

    private static void ApplyMask(List<EvidenceAction> actions, int mask)
    {
        // The matrix represents the investigation phase, before reality-side
        // truth confirmations replace presentation menus with repeat dialogue.
        ConditionManager.Instance.Set(Load<ConditionID>("Assets/Story/Tutorial/Case/PACompleted.asset"), 0);
        ConditionManager.Instance.Set(Load<ConditionID>("Assets/Story/Tutorial/Case/PBCompleted.asset"), 0);
        for (int i = 0; i < actions.Count; i++) ConditionManager.Instance.Set(actions[i].clip.observed, (mask >> i) & 1);
    }

    private static IEnumerable<int[]> Orders(int[] prefix, int[] remaining)
    {
        if (remaining.Length == 0) { yield return prefix; yield break; }
        foreach (int next in remaining)
            foreach (var order in Orders(prefix.Concat(new[] { next }).ToArray(), remaining.Where(i => i != next).ToArray()))
                yield return order;
    }

    private static IEnumerator DrainBranchDialogue()
    {
        float deadline = Time.realtimeSinceStartup + 8;
        while (Dialog.IsDialogOpen)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Branch dialogue: " + Dialog.CurrentStory?.name);
            Dialog.NextDialog(); yield return null;
        }
        yield return null; // Let the interaction owner's Talk coroutine release its busy flag.
    }

    private static IEnumerator ReplayBranch(EvidenceAction action, string label, bool occupiedDestination = false, bool nativeInteraction = false)
    {
        Physics2D.SyncTransforms();
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var camera = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        Vector3 original = action.actor.transform.position;
        var animator = TutorialActorAnimation.Find(action.actor.transform);
        float facing = TutorialActorAnimation.ReadDirection(animator);
        var colliders = action.actor.GetComponentsInChildren<Collider2D>(true);
        bool[] states = colliders.Select(c => c.enabled).ToArray();
        bool eligible = SceneFlowContainer.Passes(action.clip.conditions) &&
            action.clip.requiredItems.All(i => i != null && GameMenuController.Inventory.Has(i.Id));
        int priorCompletion = ConditionManager.Instance.Get(action.clip.observed);
        if (nativeInteraction)
        {
            player.SetFacingDirection(Vector2.left);
            typeof(PlayerMoveController3).GetMethod("TryInteract", Private).Invoke(player, null);
        }
        else action.actor.GetComponent<InvestigationPoint>().Interact();
        BranchAssert(Selecting, label + " opens normal evidence selection");
        Select(action.item.Id);
        if (!eligible)
        {
            BranchAssert(!action.actor.IsReplaying && Dialog.IsDialogOpen, label + " explains missing prerequisite without replay");
            BranchAssert(ConditionManager.Instance.Get(action.clip.observed) == priorCompletion &&
                Vector3.Distance(action.actor.transform.position, original) < .01f, label + " missing prerequisite preserves progress and position");
            yield return DrainBranchDialogue();
            yield break;
        }
        BranchAssert(action.actor.IsReplaying, label + " begins recognized replay");
        Vector2 expectedStart = action.clip.startPoint != null ? (Vector2)action.clip.startPoint.position : action.clip.startPosition;
        BranchAssert(Vector2.Distance(action.actor.transform.position, expectedStart) < .01f, label + " uses chronological start marker");
        var last = action.clip.beats.Last();
        Vector2 end = last.point != null ? (Vector2)last.point.position : last.position;
        bool arrived = !occupiedDestination && Vector2.Distance(action.actor.transform.position, end) < .04f;
        float deadline = Time.realtimeSinceStartup + 8;
        while (action.actor.IsReplaying)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(label + " replay stuck");
            // Deterministic accelerated ticks: same Update, route and collision checks,
            // but several simulation ticks per rendered frame for the exhaustive matrix.
            for (int tick = 0; tick < 16 && action.actor.IsReplaying; tick++)
            {
                foreach (var track in UnityEngine.Object.FindObjectsByType<TutorialDeductionTrack>(FindObjectsSortMode.None))
                    if (track.isActiveAndEnabled)
                        typeof(TutorialDeductionTrack).GetMethod("Update", Private).Invoke(track, null);
                ValidateWalkingClearance();
                arrived |= occupiedDestination
                    ? !TutorialDeductionTrack.IsMoving && Vector2.Distance(action.actor.transform.position, end) <= 2.01f
                    : Vector2.Distance(action.actor.transform.position, end) < .04f;
                if (Dialog.IsDialogOpen) Dialog.NextDialog();
            }
            yield return null;
        }
        BranchAssert(arrived, label + " reaches authored destination before restoration");
        BranchAssert(ConditionManager.Instance.Get(action.clip.observed) == 1, label + " records completion");
        BranchAssert(Vector3.Distance(action.actor.transform.position, original) < .01f, label + " restores waiting position");
        BranchAssert(Mathf.Approximately(TutorialActorAnimation.ReadDirection(animator), facing), label + " restores facing");
        BranchAssert(colliders.Select(c => c.enabled).SequenceEqual(states), label + " restores collision state");
        BranchAssert(camera.Follow == player.transform && !SceneLoadManager.IsCutscene, label + " restores camera and player control");
        yield return null;
    }

    private static IEnumerator BranchSuite()
    {
        BranchPhase = "Baseline full story";
        yield return Run();
        var objective = UnityEngine.Object.FindFirstObjectByType<TutorialObjectiveController>();
        if (objective != null) objective.enabled = false;
        typeof(GameMenuController).GetMethod("ClearItemSelection", Private).Invoke(Menu, null);
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        ConditionManager.Instance.Set(Load<ConditionID>("Assets/Story/Tutorial/Case/CaseSolved.asset"), 0);
        var originalFlow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var isolatedFlow = UnityEngine.Object.Instantiate(originalFlow);
        isolatedFlow.connections.Clear();
        float originalScale = Time.timeScale;
        SceneLoadManager.Instance.SetContainer(isolatedFlow);
        try
        {
            // Use real collected evidence from the baseline. Only completion flags
            // and scene transitions are isolated for combinatorial branch fixtures.
            foreach (string scene in new[] { "T1", "T2", "Tutorial_T3_VaseBreak", "Tutorial_T4_BroochMissing" })
            {
                BranchPhase = "Audit authored starts: " + scene;
                yield return SceneLoadManager.Instance.LoadContent(scene);
                yield return null; yield return null;
                yield return DrainBranchDialogue();
                if (scene == "T1")
                {
                    var staging = UnityEngine.Object.FindFirstObjectByType<TutorialWitnessStaging>();
                    var so = new UnityEditor.SerializedObject(staging);
                    var b = (Transform)so.FindProperty("witnessB").objectReferenceValue;
                    BranchAssert(Vector2.Distance(b.position, so.FindProperty("trashPosition").vector2Value) < .01f, "T1 revisit B waits at trash");
                    continue;
                }
                var actions = ActionsInScene();
                for (int actor = 0; actor < 2; actor++)
                    for (int i = 1; i < 3; i++)
                    {
                        var previous = actions[actor * 3 + i - 1].clip.beats.Last();
                        Vector2 previousEnd = previous.point != null ? (Vector2)previous.point.position : previous.position;
                        var current = actions[actor * 3 + i].clip;
                        Vector2 start = current.startPoint != null ? (Vector2)current.startPoint.position : current.startPosition;
                        BranchAssert(Vector2.Distance(previousEnd, start) < .01f, scene + " " + actions[actor * 3].actor.name + " action " + i + " starts at previous action endpoint");
                    }
                if (scene == "T2")
                {
                    var staging = new UnityEditor.SerializedObject(UnityEngine.Object.FindFirstObjectByType<TutorialWitnessStaging>());
                    BranchAssert(Vector2.Distance(Find<EvidenceReconstruction>("B").transform.position,
                        staging.FindProperty("trashPosition").vector2Value) < .01f, "T2 revisit B waits at trash");
                }
            }
            yield return SceneLoadManager.Instance.LoadContent("T2");
            yield return null; yield return null; yield return DrainBranchDialogue();
            foreach (var conclusion in UnityEngine.Object.FindObjectsByType<TutorialEvidenceConclusion>(FindObjectsSortMode.None)) conclusion.enabled = false;
            Time.timeScale = 20;
            var links = ActionsInScene();
            BranchAssert(links.Count == 6, "Six chronological reconstruction actions exist");
            ApplyMask(links, 0);
            BranchPhase = "Ordinary walking and interactions with B at trash";
            foreach (string point in new[] { "phone", "Trash Can", "A", "B", "Cabinet Number 2", "Counter Watch" })
            {
                yield return WalkAndInteract(point);
                if (Selecting) typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
                else yield return DrainBranchDialogue();
            }
            var matrixPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
            matrixPlayer.TeleportToWorld(new Vector2(5, -1));
            matrixPlayer.SetFacingDirection(Vector2.up);
            Physics2D.SyncTransforms();
            typeof(PlayerMoveController3).GetMethod("TryInteract", Private).Invoke(matrixPlayer, null);
            BranchAssert(!Dialog.IsDialogOpen && !Selecting, "Normal player interaction cannot activate remote witnesses");
            foreach (var link in links)
            {
                BranchAssert(GameMenuController.Inventory.Has(link.item.Id), "Baseline acquired selectable evidence " + link.item.Id);
                foreach (var required in link.clip.requiredItems)
                    BranchAssert(GameMenuController.Inventory.Has(required.Id), "Baseline acquired required evidence " + required.Id);
            }
            var matrixRows = new List<string> { "scene,case,values,result" };
            foreach (string matrixScene in new[] { "T2", "Tutorial_T3_VaseBreak", "Tutorial_T4_BroochMissing" })
            {
            if (Scene != matrixScene)
            {
                yield return SceneLoadManager.Instance.LoadContent(matrixScene);
                yield return null; yield return null; yield return DrainBranchDialogue();
                foreach (var conclusion in UnityEngine.Object.FindObjectsByType<TutorialEvidenceConclusion>(FindObjectsSortMode.None)) conclusion.enabled = false;
                links = ActionsInScene();
            }
            matrixPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
            matrixPlayer.TeleportToWorld(new Vector2(5, -1));
            Physics2D.SyncTransforms();
            int orderIndex = 0;
            int initialMasks = MasksTested;
            foreach (var order in Orders(Array.Empty<int>(), Enumerable.Range(0, 6).ToArray()))
            {
                ApplyMask(links, 0);
                BranchPhase = matrixScene + " order " + orderIndex + ": " + string.Join(",", order);
                foreach (int index in order) yield return ReplayBranch(links[index], "Order " + orderIndex + " action " + index);
                BranchAssert(links.All(l => ConditionManager.Instance.Get(l.clip.observed) == 1), "Order " + orderIndex + " completes all six independent actions");
                matrixRows.Add(matrixScene + ",order," + string.Join("-", order) + ",PASS");
                orderIndex++; OrdersTested++;
            }
            Check(orderIndex == 720, matrixScene + " all 720 six-evidence permutations tested");
            for (int mask = 0; mask < 64; mask++)
            {
                BranchPhase = matrixScene + " completion mask " + mask;
                foreach (string name in new[] { "A", "B" })
                {
                    ApplyMask(links, mask);
                    var actor = Find<EvidenceReconstruction>(name);
                    actor.GetComponent<InvestigationPoint>().Interact();
                    if (actor.ReadyForConclusion)
                    {
                        BranchAssert(!Selecting && Dialog.IsDialogOpen && !actor.IsReplaying, "Mask " + mask + " " + name + " completed witness shows completion dialogue");
                        yield return DrainBranchDialogue();
                        continue;
                    }
                    BranchAssert(Selecting, "Mask " + mask + " " + name + " incomplete witness opens selection");
                    typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
                    BranchAssert(!Selecting && !actor.IsReplaying && !SceneLoadManager.IsCutscene, "Mask " + mask + " cancel keeps gameplay unlocked");
                    foreach (var candidate in links)
                    {
                        ApplyMask(links, mask);
                        if (candidate.actor == actor) yield return ReplayBranch(candidate, "Mask " + mask + " " + candidate.item.Id);
                        else
                        {
                            actor.GetComponent<InvestigationPoint>().Interact(); Select(candidate.item.Id);
                            BranchAssert(!actor.IsReplaying && Dialog.IsDialogOpen, "Mask " + mask + " wrong witness rejects " + candidate.item.Id);
                            yield return DrainBranchDialogue();
                            BranchAssert(links.Select((l, i) => ConditionManager.Instance.Get(l.clip.observed) == ((mask >> i) & 1)).All(v => v),
                                "Mask " + mask + " wrong evidence preserves all progress flags");
                        }
                    }
                }
                MasksTested++;
                matrixRows.Add(matrixScene + ",mask," + mask + ",PASS");
            }
            Check(MasksTested == initialMasks + 64, matrixScene + " all 64 completion masks tested with selection, cancellation and wrong-witness evidence");
            foreach (var action in links.Where(l => l.clip.requiredItems.Length > 0))
            {
                var inventory = GameMenuController.Inventory.Capture();
                var optional = action.clip.requiredItems.Where(i => i.Id != action.item.Id).ToArray();
                for (int mask = 0; mask < (1 << optional.Length); mask++)
                {
                    GameMenuController.Inventory.Restore(inventory); ApplyMask(links, 0);
                    for (int i = 0; i < optional.Length; i++) if ((mask & (1 << i)) == 0) GameMenuController.Inventory.Remove(optional[i].Id);
                    if (mask == (1 << optional.Length) - 1) yield return ReplayBranch(action, "All required items present");
                    else
                    {
                        action.actor.GetComponent<InvestigationPoint>().Interact(); Select(action.item.Id);
                        BranchAssert(!action.actor.IsReplaying && ConditionManager.Instance.Get(action.clip.observed) == 0 && Dialog.IsDialogOpen,
                            "Missing required-item subset " + mask + " blocks reconstruction");
                        yield return DrainBranchDialogue();
                    }
                }
                GameMenuController.Inventory.Restore(inventory);
            }
            foreach (var action in links.Where(l => l.clip.conditions.Count > 0))
            {
                var conditions = action.clip.conditions;
                var previousValues = conditions.Select(c => ConditionManager.Instance.Get(c.id)).ToArray();
                for (int mask = 0; mask < (1 << conditions.Count); mask++)
                {
                    ApplyMask(links, 0);
                    for (int i = 0; i < conditions.Count; i++)
                    {
                        var condition = conditions[i]; bool expected = (mask & (1 << i)) != 0; bool assigned = false;
                        foreach (int candidate in new[] { condition.value, condition.value + 1, condition.value - 1, 0, 1 })
                        {
                            ConditionManager.Instance.Set(condition.id, candidate);
                            if (SceneFlowContainer.Passes(new List<DialogCondition> { condition }) == expected) { assigned = true; break; }
                        }
                        BranchAssert(assigned, "Condition truth fixture can be constructed");
                    }
                    yield return ReplayBranch(action, "Prerequisite condition mask " + mask);
                    matrixRows.Add(matrixScene + ",prerequisite," + mask + ",PASS");
                }
                for (int i = 0; i < conditions.Count; i++) ConditionManager.Instance.Set(conditions[i].id, previousValues[i]);
            }
            if (matrixScene != "T2")
            {
                BranchPhase = matrixScene + " native driver presentation with player at cabinet";
                ApplyMask(links, 0);
                var driver = links.Single(l => l.item.Id == "evidence-flathead-driver");
                var destination = driver.clip.beats.Last();
                matrixPlayer.TeleportToWorld(destination.point != null ? (Vector2)destination.point.position : destination.position);
                Physics2D.SyncTransforms(); yield return null;
                yield return ReplayBranch(driver, "Occupied cabinet approach reroutes safely", true, true);
                matrixPlayer.TeleportToWorld(new Vector2(5, -1));
            }
            }
            Check(OrdersTested == 2160 && MasksTested == 192, "All three replay scenes complete their order and state matrices");
            BranchPhase = "Complete";
            System.IO.Directory.CreateDirectory("Captures/Branches-20261102");
            System.IO.File.WriteAllText("Captures/Branches-20261102/matrix-details.txt", string.Join("\n", Passed));
            System.IO.File.WriteAllLines("Captures/Branches-20261102/matrix-cases.csv", matrixRows);
        }
        finally
        {
            Time.timeScale = originalScale;
            SceneLoadManager.Instance.SetContainer(originalFlow);
            if (objective != null) objective.enabled = true;
            UnityEngine.Object.Destroy(isolatedFlow);
        }
    }
}
