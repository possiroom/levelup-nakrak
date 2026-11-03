using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Naknak.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Uses the production T4 dialogue, conditions, item assets and runtime components.
// Gameplay movement and menu presentation retain the isolated fixture's stand-ins.
public static class T4MemoryVerification
{
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static SceneReference Ref(string name) => JsonUtility.FromJson<SceneReference>("{\"path\":\"Assets/Scenes/" + name + ".unity\"}");
    private static T Data<T>(string name) where T : UnityEngine.Object => Resources.Load<T>("T4Dialogue/" + name);
    private static InventoryItem Item(string name) => Resources.Load<InventoryItem>("T4Items/" + name);
    private static StoryData CurrentStory => typeof(DialogManager).GetField("currentStory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DialogManager.Instance) as StoryData;
    private static void CloseLine()
    {
        DialogManager.Instance.NextDialog();
        if (DialogManager.Instance.IsDialogOpen) DialogManager.Instance.NextDialog();
    }

    public static SceneFlowContainer CreateScenes()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Grid").AddComponent<Grid>();
        var broken = new GameObject("T4 Broken Vase");
        var investigations = new GameObject("T4 Investigations");
        var director = new GameObject("T4 Memory Controller").AddComponent<TutorialT4Controller>();
        Set(director, "returnDialog", Data<DialogContainer>("T4_Return"));
        Set(director, "missingDialog", Data<DialogContainer>("T4_Missing"));
        Set(director, "packedBox", Item("PackedJewelryBox")); Set(director, "emptyBox", Item("EmptyJewelryBox"));
        Set(director, "returned", Data<ConditionID>("T4BoxReturned")); Set(director, "completed", Data<ConditionID>("T4DialogueCompleted"));
        Set(director, "brokenVase", broken); Set(director, "investigationObjects", investigations);
        var fragments = broken.AddComponent<MemoryInvestigationPoint>();
        Set(fragments, "available", Data<ConditionID>("T4DialogueCompleted")); Set(fragments, "inspected", Data<ConditionID>("T4FragmentsInspected"));
        Set(fragments, "rewards", new[] { Item("WindingSpring"), Item("FlatheadScrew") });
        Set(fragments, "firstDialog", Data<StoryData>("T4_Fragments_First")); Set(fragments, "repeatDialog", Data<StoryData>("T4_Fragments_Again"));
        var cabinet = new GameObject("T4 Cabinet").AddComponent<MemoryInvestigationPoint>();
        cabinet.transform.SetParent(investigations.transform);
        Set(cabinet, "available", Data<ConditionID>("T4DialogueCompleted")); Set(cabinet, "inspected", Data<ConditionID>("T4CabinetInspected"));
        Set(cabinet, "firstDialog", Data<StoryData>("T4_Cabinet_First")); Set(cabinet, "repeatDialog", Data<StoryData>("T4_Cabinet_Again"));
        var exit = new GameObject("T4 Exit").AddComponent<SceneTransitionPoint>(); exit.transform.SetParent(investigations.transform);
        exit.container = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        exit.connectionId = "t4-memory-exit";
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestT4Memory.unity");
        var flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestT4Memory"), memorySpace = true, memoryEntryItem = Item("PackedJewelryBox"), lockPlayerInput = true, useSharedCamera = true, placePlayer = false });
        var route = new SceneFlowContainer.Connection { id = "t4-memory-exit", from = Ref("TestT4Memory"), to = Ref("TestB"), trigger = SceneConnectionTrigger.Manual };
        route.conditions.Add(new DialogCondition { id = Data<ConditionID>("T4DialogueCompleted"), compare = CompareType.IsTrue });
        flow.connections.Add(route);
        return flow;
    }

    public static IEnumerator Checks(Action<bool, string> assert)
    {
        var packed = Item("PackedJewelryBox"); var empty = Item("EmptyJewelryBox");
        var spring = Item("WindingSpring"); var screw = Item("FlatheadScrew");
        var completed = Data<ConditionID>("T4DialogueCompleted"); var returned = Data<ConditionID>("T4BoxReturned");
        var fragmentFlag = Data<ConditionID>("T4FragmentsInspected"); var cabinetFlag = Data<ConditionID>("T4CabinetInspected");
        foreach (var flag in new[] { completed, returned, fragmentFlag, cabinetFlag }) ConditionManager.Instance.Set(flag, 0);
        GameMenuController.Instance = new GameMenuController();
        GameMenuController.Inventory = new InventoryModel(new[] { packed, empty, spring, screw });
        var inventory = GameMenuController.Inventory; inventory.Add(packed.Id);
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow"); SceneLoadManager.Instance.SetContainer(flow);
        yield return SceneLoadManager.Instance.LoadContent("TestT4Memory");
        while (!DialogManager.Instance.IsDialogOpen) yield return null;
        var director = UnityEngine.Object.FindFirstObjectByType<TutorialT4Controller>();
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        assert(CurrentStory.text == "잠깐만요. 제 브로치부터 돌려주세요.", "T4 uses readable production dialogue assets");
        assert(!player.enabled && player.GetComponent<SpriteRenderer>().enabled && SceneLoadManager.IsCutscene, "T4 keeps player visible and locks control during dialogue");
        assert(inventory.Has(packed.Id) && !inventory.Has(empty.Id), "T4 holds packed box until owner returns it");
        assert(!SceneLoadManager.Instance.TryCompleteScene(), "T4 dialogue cannot automatically return to reality");
        var route = flow.connections.Find(value => value.id == "t4-memory-exit");
        assert(!SceneLoadManager.Instance.Travel(flow, route), "T4 exit is gated before dialogue completes");
        var fragments = UnityEngine.Object.FindObjectsByType<MemoryInvestigationPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(value => value.name == "T4 Broken Vase");
        fragments.Interact(); assert(!inventory.Has(spring.Id), "T4 investigation is blocked during dialogue");
        for (int i = 0; i < 2; i++)
        {
            DialogManager.Instance.NextDialog(); DialogManager.Instance.NextDialog(); yield return null;
        }
        while (director.Phase != TutorialT4Controller.T4Phase.MissingDialogue) yield return null;
        assert(!inventory.Has(packed.Id) && inventory.Count(empty.Id) == 1 && ConditionManager.Instance.Get(returned) == 1,
            "T4 return exchanges packed box for one empty box at the dialogue boundary");
        for (int i = 0; i < 8; i++)
        {
            assert(CurrentStory != null && !string.IsNullOrEmpty(CurrentStory.text) && !CurrentStory.text.Contains("\uFFFD"), "T4 missing-brooch line is readable");
            DialogManager.Instance.NextDialog(); DialogManager.Instance.NextDialog(); yield return null;
        }
        while (director.Phase != TutorialT4Controller.T4Phase.Exploration) yield return null;
        assert(SceneManager.GetSceneByName("TestT4Memory").isLoaded && player.enabled && !SceneLoadManager.IsCutscene && ConditionManager.Instance.Get(completed) == 1,
            "T4 unlocks exploration/menu and stays in memory after dialogue");
        fragments.Interact();
        assert(inventory.Count(spring.Id) == 1 && inventory.Count(screw.Id) == 1 && ConditionManager.Instance.Get(fragmentFlag) == 1,
            "T4 fragments grant spring and screw exactly once");
        CloseLine(); fragments.Interact(); CloseLine();
        assert(inventory.Count(spring.Id) == 1 && inventory.Count(screw.Id) == 1, "Repeated fragment investigation cannot duplicate rewards");
        var cabinet = UnityEngine.Object.FindObjectsByType<MemoryInvestigationPoint>(FindObjectsSortMode.None).First(value => value.name == "T4 Cabinet");
        cabinet.Interact(); CloseLine();
        assert(ConditionManager.Instance.Get(cabinetFlag) == 1, "T4 cabinet inspection records opening-attempt evidence");
        var savedItems = inventory.Capture(); var savedConditions = ConditionManager.Instance.CaptureValues();
        var exit = GameObject.Find("T4 Exit").GetComponent<SceneTransitionPoint>(); exit.Interact();
        while (SceneLoadManager.IsLoading) yield return null;
        assert(SceneManager.GetSceneByName("TestB").isLoaded, "T4 exits only through the manual scene transition point");
        SceneLoadManager.IsRestoringSession = true;
        inventory.Restore(null); ConditionManager.Instance.RestoreValues(null);
        yield return SceneLoadManager.Instance.LoadContent("TestT4Memory");
        yield return null;
        assert(!inventory.Has(empty.Id) && !DialogManager.Instance.IsDialogOpen, "T4 waits for save restoration without replay or early item grants");
        inventory.Restore(savedItems); ConditionManager.Instance.RestoreValues(savedConditions);
        SceneLoadManager.Instance.ResumeRestoredSession();
        director = UnityEngine.Object.FindFirstObjectByType<TutorialT4Controller>();
        while (director.Phase != TutorialT4Controller.T4Phase.Exploration) yield return null;
        assert(!DialogManager.Instance.IsDialogOpen && inventory.Count(empty.Id) == 1 && player.enabled,
            "Restored T4 resumes exploration without replaying dialogue or granting another box");
        fragments = UnityEngine.Object.FindObjectsByType<MemoryInvestigationPoint>(FindObjectsSortMode.None).First(value => value.name == "T4 Broken Vase");
        fragments.Interact(); CloseLine();
        assert(inventory.Count(spring.Id) == 1 && inventory.Count(screw.Id) == 1 && ConditionManager.Instance.Get(cabinetFlag) == 1,
            "T4 save restoration preserves rewards and both investigation flags");
        SceneLoadManager.Instance.SetPlayerInputLocked(true);
        assert(!player.enabled && SceneLoadManager.IsCutscene, "Runtime cutscene lock disables movement");
        yield return SceneLoadManager.Instance.LoadContent("TestB");
        assert(player.enabled && !SceneLoadManager.IsCutscene, "Runtime cutscene lock does not leak into the next scene");
        GameMenuController.Instance = null; GameMenuController.Inventory = null;
    }
}
