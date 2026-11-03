using System;
using System.Collections;
using System.Reflection;
using Naknak.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Production owner NPC, conversation assets, dialogue choices and appraisal controller.
// The existing fixture substitutes only menu presentation and movement/game-event plumbing.
public static class T2AppraisalVerification
{
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Data<T>(string name) where T : UnityEngine.Object => Resources.Load<T>("T2Dialogue/" + name);
    private static StoryData CurrentStory => typeof(DialogManager).GetField("currentStory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DialogManager.Instance) as StoryData;
    private static bool Choosing => (bool)typeof(DialogManager).GetField("isSelectActivate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(DialogManager.Instance);
    private static SceneReference Ref(string name) => JsonUtility.FromJson<SceneReference>("{\"path\":\"Assets/Scenes/" + name + ".unity\"}");

    private static void Advance()
    {
        var previous = CurrentStory;
        DialogManager.Instance.NextDialog();
        if (CurrentStory == previous && DialogManager.Instance.IsDialogOpen && !Choosing) DialogManager.Instance.NextDialog();
    }

    private static void Choose(int index)
    {
        if (!Choosing) Advance();
        if (!Choosing) throw new Exception("Choice UI did not open");
        Set(DialogManager.Instance, "selectIndex", index);
        DialogManager.Instance.NextDialog();
    }

    public static SceneFlowContainer CreateScenes()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Grid").AddComponent<Grid>();
        var owner = new GameObject("T2 Owner");
        var npc = owner.AddComponent<NPC>(); Set(npc, "dialogContainer", Data<DialogContainer>("T2_Owner_Container"));
        var appraisal = owner.AddComponent<TutorialT2Appraisal>();
        Set(appraisal, "awaitingItem", Data<ConditionID>("T2AwaitingItem")); Set(appraisal, "submitted", Data<ConditionID>("T2AppraisalSubmitted"));
        Set(appraisal, "packedBox", Resources.Load<InventoryItem>("T4Items/PackedJewelryBox"));
        Set(appraisal, "appraisalStart", Data<StoryData>("T2_Owner_02")); Set(appraisal, "wrongItemDialog", Data<StoryData>("T2WrongItem"));
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestT2Appraisal.unity");
        var flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestT2Appraisal"), memorySpace = true, memoryEntryItem = Resources.Load<InventoryItem>("T4Items/PackedJewelryBox"), useSharedCamera = true, placePlayer = false });
        var connection = new SceneFlowContainer.Connection { from = Ref("TestT2Appraisal"), to = Ref("TestB"), trigger = SceneConnectionTrigger.DialogFinished, completedDialog = Data<StoryData>("T2_Owner_09") };
        connection.conditions.Add(new DialogCondition { id = Data<ConditionID>("T2AppraisalSubmitted"), compare = CompareType.IsTrue });
        flow.connections.Add(connection);
        return flow;
    }

    private static IEnumerator RequestItem()
    {
        UnityEngine.Object.FindFirstObjectByType<NPC>().Interact();
        Choose(1);
        Advance();
        while (GameMenuController.Instance.Selection == null) yield return null;
    }

    public static IEnumerator Checks(Action<bool, string> assert)
    {
        var awaiting = Data<ConditionID>("T2AwaitingItem"); var submitted = Data<ConditionID>("T2AppraisalSubmitted");
        ConditionManager.Instance.Set(awaiting, 0); ConditionManager.Instance.Set(submitted, 0);
        var packed = Resources.Load<InventoryItem>("T4Items/PackedJewelryBox");
        var wrong = Resources.Load<InventoryItem>("T4Items/EmptyJewelryBox");
        GameMenuController.Instance = new GameMenuController();
        GameMenuController.Inventory = new InventoryModel(new[] { packed, wrong });
        var inventory = GameMenuController.Inventory; inventory.Add(packed.Id); inventory.Add(wrong.Id);
        var manager = SceneLoadManager.Instance;
        manager.SetContainer(Resources.Load<SceneFlowContainer>("TutorialSceneFlow"));
        yield return manager.LoadContent("TestT2Appraisal");
        yield return null;
        inventory.Add(wrong.Id);
        assert(!DialogManager.Instance.IsDialogOpen && !SceneLoadManager.IsCutscene, "T2 starts with free movement and no automatic conversation");
        var owner = UnityEngine.Object.FindFirstObjectByType<NPC>();
        owner.Interact();
        assert(CurrentStory == Data<StoryData>("T2_Owner_01"), "T2 uses the existing owner NPC and greeting container");
        Choose(2); Advance(); yield return null;
        assert(!DialogManager.Instance.IsDialogOpen && GameMenuController.Instance.Selection == null && inventory.Has(packed.Id),
            "T2 No choice returns to exploration without selecting or consuming an item");
        assert(!manager.NotifyDialogFinished(Data<StoryData>("T2_Owner_09")), "T2 cannot skip to next scene before item submission");
        yield return RequestItem();
        assert(!DialogManager.Instance.IsDialogOpen && ConditionManager.Instance.Get(awaiting) == 0,
            "T2 closes show-me dialogue before opening the existing inventory selector once");
        GameMenuController.Instance.CancelForTest(); yield return null;
        assert(GameMenuController.Instance.Selection == null && inventory.Count(packed.Id) == 1 && ConditionManager.Instance.Get(submitted) == 0,
            "Cancelling appraisal preserves box and allows retry");
        yield return RequestItem(); GameMenuController.Instance.ChooseForTest(wrong);
        assert(CurrentStory == Data<StoryData>("T2WrongItem") && inventory.Count(packed.Id) == 1 && inventory.Count(wrong.Id) == 1 && ConditionManager.Instance.Get(submitted) == 0,
            "Wrong appraisal item shows guidance and consumes nothing");
        Advance();
        SceneLoadManager.IsRestoringSession = true; ConditionManager.Instance.Set(awaiting, 1);
        yield return null; yield return null;
        assert(GameMenuController.Instance.Selection == null, "T2 waits for session restoration before handling pending selection");
        manager.ResumeRestoredSession();
        while (GameMenuController.Instance.Selection == null) yield return null;
        GameMenuController.Instance.CancelForTest();
        int arrivals = 0;
        void Loaded(Scene scene, LoadSceneMode mode) { if (scene.name == "TestB") arrivals++; }
        SceneManager.sceneLoaded += Loaded;
        for (int branch = 1; branch <= 3; branch++)
        {
            ConditionManager.Instance.Set(submitted, 0);
            yield return RequestItem(); GameMenuController.Instance.ChooseForTest(packed);
            assert(CurrentStory == Data<StoryData>("T2_Owner_02") && inventory.Count(packed.Id) == 1 && ConditionManager.Instance.Get(submitted) == 1,
                "Presenting packed box starts appraisal and preserves the item");
            int steps = 0;
            while (CurrentStory != Data<StoryData>("T2_Owner_05") && steps++ < 10) { Advance(); yield return null; }
            assert(CurrentStory == Data<StoryData>("T2_Owner_05"), "Existing appraisal reaches price choices");
            Choose(branch);
            assert(CurrentStory == Data<StoryData>("T2_Owner_05-" + branch), "Existing price choice " + branch + " follows its original branch");
            steps = 0;
            while (CurrentStory != Data<StoryData>("T2_Owner_09") && steps++ < 10) { Advance(); yield return null; }
            assert(CurrentStory == Data<StoryData>("T2_Owner_09"), "All price branches merge into sale refusal");
            Advance();
            while (SceneLoadManager.IsLoading) yield return null;
            yield return null;
            assert(arrivals == branch && SceneManager.GetSceneByName("TestB").isLoaded && inventory.Count(packed.Id) == 0,
                "Refusal follows route once and memory box is unavailable in reality");
            if (branch < 3) { yield return manager.LoadContent("TestT2Appraisal"); yield return null; }
        }
        SceneManager.sceneLoaded -= Loaded;
        GameMenuController.Instance = null; GameMenuController.Inventory = null;
    }
}
