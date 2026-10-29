using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Naknak.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MemoryReplayVerification
{
    private static SceneReference Ref(string name) => JsonUtility.FromJson<SceneReference>("{\"path\":\"Assets/Scenes/" + name + ".unity\"}");
    private static T Data<T>(string folder, string name) where T : UnityEngine.Object => Resources.Load<T>(folder + "/" + name);
    private static InventoryItem Item(string name) => Data<InventoryItem>("T4Items", name);
    private static StoryData Story => typeof(DialogManager).GetField("currentStory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DialogManager.Instance) as StoryData;
    public static SceneFlowContainer CreateScenes()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Memory Grid").AddComponent<Grid>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestMemoryT1.unity");
        var flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestMemoryT1"), memorySpace = true, memoryReplayEntry = true,
            memoryEntryItem = Item("PackedJewelryBox"), lockPlayerInput = true, useSharedCamera = true,
            entryDialog = Data<StoryData>("ReplayDialogue", "T1_01") });
        flow.connections.Add(new SceneFlowContainer.Connection { id = "replay-t1-end", from = Ref("TestMemoryT1"), to = Ref("TestT2Appraisal"),
            trigger = SceneConnectionTrigger.DialogFinished, completedDialog = Data<StoryData>("ReplayDialogue", "T1_10") });
        return flow;
    }

    public static IEnumerator Checks(Action<bool, string> assert)
    {
        var manager = SceneLoadManager.Instance;
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        manager.SetContainer(flow);
        var visits = Data<ConditionID>("ReplayDialogue", "MemoryVisitCount");
        var returned = Data<ConditionID>("T4Dialogue", "T4BoxReturned");
        var completed = Data<ConditionID>("T4Dialogue", "T4DialogueCompleted");
        var fragments = Data<ConditionID>("T4Dialogue", "T4FragmentsInspected");
        var cabinet = Data<ConditionID>("T4Dialogue", "T4CabinetInspected");
        var submitted = Data<ConditionID>("T2Dialogue", "T2AppraisalSubmitted");
        var awaiting = Data<ConditionID>("T2Dialogue", "T2AwaitingItem");
        flow.memoryVisitCount = visits;
        flow.memoryRunConditions.AddRange(new[] { returned, completed, submitted, awaiting });
        var packed = Item("PackedJewelryBox"); var empty = Item("EmptyJewelryBox");
        var spring = Item("WindingSpring"); var screw = Item("FlatheadScrew");
        GameMenuController.Instance = new GameMenuController();
        var inventory = new InventoryModel(new[] { packed, empty, spring, screw });
        GameMenuController.Inventory = inventory;
        // A previous first visit ended with evidence and the empty box in old save data.
        inventory.Restore(new[] { new InventoryEntry(empty.Id, 1), new InventoryEntry(spring.Id, 1), new InventoryEntry(screw.Id, 1) });
        foreach (var flag in new[] { visits, returned, completed, submitted, awaiting, fragments, cabinet }) ConditionManager.Instance.Set(flag, 1);
        ConditionManager.Instance.Set(visits, 0); // Old reality saves predate the visit counter.
        assert(!inventory.Has(empty.Id) && !inventory.Add(packed.Id), "Reality hides and rejects memory items from old saves");
        yield return manager.LoadContent("TestMemoryT1");
        while (!DialogManager.Instance.IsDialogOpen) yield return null;
        assert(SceneLoadManager.IsMemoryRevisit && ConditionManager.Instance.Get(visits) == 2, "Revisit is counted once on crossing into memory");
        assert(inventory.Has(packed.Id) && !inventory.Has(empty.Id), "Replay replaces stale empty box with packed box");
        assert(ConditionManager.Instance.Get(returned) == 0 && ConditionManager.Instance.Get(completed) == 0 &&
            ConditionManager.Instance.Get(submitted) == 0 && ConditionManager.Instance.Get(awaiting) == 0, "Replay resets per-run dialogue and submission flags");
        assert(inventory.Has(spring.Id) && inventory.Has(screw.Id) && ConditionManager.Instance.Get(fragments) == 1 && ConditionManager.Instance.Get(cabinet) == 1,
            "Replay retains collected evidence and permanent inspection records");
        assert(Story == Data<StoryData>("ReplayDialogue", "T1_01"), "Replay begins with the existing T1 watch-sale dialogue");
        DialogManager.Instance.SkipUntilChoice();
        while (SceneLoadManager.IsLoading) yield return null;
        yield return null;
        assert(SceneManager.GetSceneByName("TestT2Appraisal").isLoaded && inventory.Has(packed.Id) && ConditionManager.Instance.Get(visits) == 2,
            "T1 fast-forward routes to T2 without restarting the memory session");
        UnityEngine.Object.FindFirstObjectByType<NPC>().Interact();
        DialogManager.Instance.SkipUntilChoice();
        assert(Story == Data<StoryData>("T2Dialogue", "T2_Owner_01") && DialogManager.Instance.IsDialogOpen,
            "Fast-forward stops at an unconfirmed choice");
        typeof(DialogManager).GetField("selectIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(DialogManager.Instance, 1);
        DialogManager.Instance.NextDialog();
        DialogManager.Instance.SkipUntilChoice();
        while (GameMenuController.Instance.Selection == null) yield return null;
        assert(ConditionManager.Instance.Get(awaiting) == 0 && inventory.Has(packed.Id), "Skipping executes the item-selection end action");
        GameMenuController.Instance.ChooseForTest(packed);
        DialogManager.Instance.SkipUntilChoice();
        assert(Story == Data<StoryData>("T2Dialogue", "T2_Owner_05") && inventory.Has(packed.Id), "Skipping appraisal preserves the price choice and submitted box");
        // Existing T3 sequence has separate coverage. Connect directly to the real T4 controller here.
        flow.connections.Find(row => row.from.Name == "TestT2Appraisal").to = Ref("TestT4Memory");
        typeof(DialogManager).GetField("selectIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(DialogManager.Instance, 1);
        DialogManager.Instance.NextDialog();
        DialogManager.Instance.SkipUntilChoice();
        while (SceneLoadManager.IsLoading) yield return null;
        var director = UnityEngine.Object.FindFirstObjectByType<TutorialT4Controller>();
        while (!DialogManager.Instance.IsDialogOpen) yield return null;
        assert(director.Phase == TutorialT4Controller.T4Phase.ReturnDialogue, "Revisited T4 replays its return dialogue rather than using the first-visit completion flag");
        DialogManager.Instance.SkipUntilChoice();
        while (director.Phase != TutorialT4Controller.T4Phase.MissingDialogue) yield return null;
        assert(inventory.Has(empty.Id) && !inventory.Has(packed.Id), "Revisited T4 exchanges the box at the original story boundary");
        DialogManager.Instance.SkipUntilChoice();
        while (director.Phase != TutorialT4Controller.T4Phase.Exploration) yield return null;
        var savedItems = inventory.Capture(); var savedFlags = ConditionManager.Instance.CaptureValues();
        yield return manager.LoadContent("TestB"); yield return null;
        assert(!inventory.Has(empty.Id) && !inventory.Has(packed.Id) && inventory.Has(spring.Id), "Leaving replay removes memory props and preserves evidence");
        SceneLoadManager.IsRestoringSession = true;
        yield return manager.LoadContent("TestT4Memory");
        inventory.Restore(savedItems); ConditionManager.Instance.RestoreValues(savedFlags);
        manager.ResumeRestoredSession();
        director = UnityEngine.Object.FindFirstObjectByType<TutorialT4Controller>();
        while (director.Phase != TutorialT4Controller.T4Phase.Exploration) yield return null;
        assert(inventory.Has(empty.Id) && !inventory.Has(packed.Id) && ConditionManager.Instance.Get(visits) == 2 && !DialogManager.Instance.IsDialogOpen,
            "Loading a memory checkpoint resumes its phase without resetting or counting another visit");
        yield return manager.LoadContent("TestB"); yield return null;
        yield return manager.LoadContent("TestMemoryT1");
        while (!DialogManager.Instance.IsDialogOpen) yield return null;
        assert(ConditionManager.Instance.Get(visits) == 3 && inventory.Has(packed.Id) && inventory.Count(spring.Id) == 1 && inventory.Count(screw.Id) == 1,
            "Third memory visit starts correctly without duplicating evidence");
        DialogManager.Instance.SkipUntilChoice();
        while (SceneLoadManager.IsLoading) yield return null;
        yield return manager.LoadContent("TestB"); yield return null;
        GameMenuController.Instance = null; GameMenuController.Inventory = null;
    }
}
