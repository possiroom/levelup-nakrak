using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class T3CutsceneVerification
{
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static SceneReference Ref(string name)
    {
        var result = new SceneReference();
        JsonUtility.FromJsonOverwrite("{\"path\":\"Assets/Scenes/" + name + ".unity\"}", result);
        return result;
    }

    public static SceneFlowContainer CreateScenes()
    {
        var occurred = ScriptableObject.CreateInstance<ConditionID>();
        occurred.name = "T3Occurred";
        AssetDatabase.CreateAsset(occurred, "Assets/Resources/T3Occurred.asset");
        var s3Seen = ScriptableObject.CreateInstance<ConditionID>();
        s3Seen.name = "S3Seen";
        AssetDatabase.CreateAsset(s3Seen, "Assets/Resources/S3Seen.asset");
        var story = ScriptableObject.CreateInstance<StoryData>();
        story.charName = "전당포 주인"; story.text = "이런 젠장! 누가 건드렸어?"; story.isEnd = true;
        AssetDatabase.CreateAsset(story, "Assets/Resources/T3ReactionLine.asset");
        var container = ScriptableObject.CreateInstance<DialogContainer>();
        var data = new SerializedObject(container);
        var slots = data.FindProperty("dialogSlots"); slots.arraySize = 1;
        slots.GetArrayElementAtIndex(0).FindPropertyRelative("headStoryData").objectReferenceValue = story;
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(container, "Assets/Resources/T3Reaction.asset");
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        container = AssetDatabase.LoadAssetAtPath<DialogContainer>("Assets/Resources/T3Reaction.asset");
        occurred = AssetDatabase.LoadAssetAtPath<ConditionID>("Assets/Resources/T3Occurred.asset");
        new GameObject("Grid").AddComponent<Grid>();
        new GameObject("T3 Fixed Camera").AddComponent<Camera>();
        var normal = new GameObject("T3 Vase Normal");
        var broken = new GameObject("T3 Vase Broken"); broken.SetActive(false);
        var investigations = new GameObject("T3 Investigations"); investigations.SetActive(false);
        var overlayObject = new GameObject("T3 Blackout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.GetComponent<Image>().color = Color.clear;
        var director = new GameObject("T3 Director").AddComponent<TutorialT3T4SceneController>();
        Set(director, "t3ReactionDialog", container); Set(director, "t3Occurred", occurred);
        Set(director, "s3Seen", AssetDatabase.LoadAssetAtPath<ConditionID>("Assets/Resources/S3Seen.asset"));
        Set(director, "vaseNormal", normal); Set(director, "vaseBroken", broken);
        Set(director, "t3InvestigationObjects", investigations); Set(director, "darkOverlayImage", overlayObject.GetComponent<Image>());
        Set(director, "flickerCount", 1); Set(director, "flickerDarkTime", 0.03f); Set(director, "flickerLightTime", 0.03f);
        Set(director, "waitBeforeBreak", 0.02f); Set(director, "darkFadeInTime", 0.05f);
        Set(director, "waitAfterLightsOffBeforeBreak", 0.05f); Set(director, "waitAfterBreak", 0.1f);
        Set(director, "darkFadeOutTime", 0.05f); Set(director, "waitBeforeLoadNextScene", 0.02f);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestT3.unity");
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Grid").AddComponent<Grid>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TestT4.unity");
        var flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestT3"), lockPlayerInput = true, useSharedCamera = true, placePlayer = false });
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestT4"), placePlayer = false });
        flow.connections.Add(new SceneFlowContainer.Connection { from = Ref("TestT3"), to = Ref("TestT4"), trigger = SceneConnectionTrigger.SceneCompleted });
        return flow;
    }

    public static IEnumerator Checks(Action<bool, string> assert)
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        var renderer = player.gameObject.AddComponent<SpriteRenderer>();
        var position = new Vector3(7, 8, 0); player.transform.position = position;
        // T0's preceding route test intentionally installs a temporary container.
        SceneLoadManager.Instance.SetContainer(Resources.Load<SceneFlowContainer>("TutorialSceneFlow"));
        GameMenuController.BlockingForTest = true;
        yield return SceneLoadManager.Instance.LoadContent("TestT3");
        yield return null;
        var director = UnityEngine.Object.FindFirstObjectByType<TutorialT3T4SceneController>();
        assert(director.Phase == TutorialT3T4SceneController.T3Phase.WaitingForServices, "T3 waits for loading/menu to finish");
        assert(renderer.enabled && !player.enabled && SceneLoadManager.IsCutscene, "T3 keeps player visible while locking movement/menu");
        assert(player.transform.position == position, "T3 preserves player's T2 position");
        assert(!GameObject.Find("T3 Fixed Camera").GetComponent<Camera>().enabled && GameObject.Find("CoreCamera").GetComponent<Camera>().enabled,
            "T3 reuses common tracking camera instead of fixed content camera");
        GameMenuController.BlockingForTest = false;
        bool sawDark = false, sawBreak = false;
        while (director.Phase != TutorialT3T4SceneController.T3Phase.Reaction)
        {
            if (director.Phase == TutorialT3T4SceneController.T3Phase.Dark) sawDark = true;
            if (director.Phase == TutorialT3T4SceneController.T3Phase.VaseBroken)
            {
                sawBreak = true;
                var overlay = (Image)typeof(TutorialT3T4SceneController).GetField("darkOverlayImage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
                assert(overlay.color == Color.black, "Before S3, the blackout is completely opaque while the vase breaks");
                var normal = SceneManager.GetSceneByName("TestT3").GetRootGameObjects();
                foreach (var root in normal)
                {
                    if (root.name == "T3 Vase Normal") assert(!root.activeSelf, "T3 hides intact vase on break");
                    if (root.name == "T3 Vase Broken") assert(root.activeSelf, "T3 shows broken vase on break");
                }
            }
            yield return null;
        }
        assert(sawDark && sawBreak, "T3 runs blackout before breaking vase");
        assert(DialogManager.Instance.IsDialogOpen, "T3 shows reaction using existing dialogue manager");
        var condition = Resources.Load<ConditionID>("T3Occurred");
        assert(ConditionManager.Instance.Get(condition) == 1, "T3 records persistent investigation unlock");
        assert(!SceneManager.GetSceneByName("TestT4").isLoaded, "T3 waits for reaction dialogue before T4");
        int loads = 0;
        void Loaded(Scene value, LoadSceneMode mode) { if (value.name == "TestT4") loads++; }
        SceneManager.sceneLoaded += Loaded;
        DialogManager.Instance.NextDialog();
        if (DialogManager.Instance.IsDialogOpen) DialogManager.Instance.NextDialog();
        while (!SceneManager.GetSceneByName("TestT4").isLoaded || SceneLoadManager.IsLoading) yield return null;
        yield return null;
        assert(loads == 1 && !SceneManager.GetSceneByName("TestT3").isLoaded, "T3 completes through scene-flow container exactly once");
        assert(player.enabled && renderer.enabled, "Leaving T3 restores normal player control");
        assert(ConditionManager.Instance.Get(condition) == 1, "T3 event state survives the next scene");
        SceneManager.sceneLoaded -= Loaded;

        ConditionManager.Instance.Set(Resources.Load<ConditionID>("S3Seen"), 1);
        yield return SceneLoadManager.Instance.LoadContent("TestT3");
        director = UnityEngine.Object.FindFirstObjectByType<TutorialT3T4SceneController>();
        while (director.Phase != TutorialT3T4SceneController.T3Phase.VaseBroken) yield return null;
        var replayOverlay = (Image)typeof(TutorialT3T4SceneController).GetField("darkOverlayImage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
        assert(Mathf.Approximately(replayOverlay.color.a, 0.5f), "After S3, T3 uses a 50 percent black overlay");
        assert(!player.enabled && SceneLoadManager.IsCutscene, "T3 keeps movement locked during replay blackout");
        while (director.Phase != TutorialT3T4SceneController.T3Phase.Reaction) yield return null;
        assert(Mathf.Approximately(replayOverlay.color.a, 0f) && DialogManager.Instance.IsDialogOpen,
            "Lights return before the owner's line begins");
        DialogManager.Instance.NextDialog();
        if (DialogManager.Instance.IsDialogOpen) DialogManager.Instance.NextDialog();
        while (!SceneManager.GetSceneByName("TestT4").isLoaded || SceneLoadManager.IsLoading) yield return null;
        assert(player.enabled, "Finishing the owner's line advances to T4 and restores movement");
    }
}
