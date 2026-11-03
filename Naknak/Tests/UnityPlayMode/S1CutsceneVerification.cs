using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tests the real S1 director, real dialogue assets and real scene loader in an
// isolated project. Gameplay services/UI rendering are supplied by the fixture.
public static class S1CutsceneVerification
{
    private static readonly Vector3 StartPosition = new Vector3(1, -2.3f, 0);
    private static readonly Vector3 CenterPosition = new Vector3(3.5f, 0.3f, 0);
    private static readonly FieldInfo CurrentStory = typeof(DialogManager).GetField("currentStory", BindingFlags.NonPublic | BindingFlags.Instance);

    public static SceneFlowContainer CreateScenes()
    {
        var s1 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var actor = new GameObject("S1 Detective");
        var start = Point("S1 Start", StartPosition);
        var corner = Point("S1 Corner", new Vector3(3.5f, -2.3f, 0));
        var center = Point("S1 Center", CenterPosition);
        var opening = Point("S1 Opening Camera", new Vector3(5.3f, 1.8f, -11.5f));
        var entrance = Point("S1 Entrance Camera", new Vector3(4.6f, 0.4f, -11.5f));
        var group = Point("S1 Group Camera", new Vector3(4.6f, 0.6f, -11.5f));
        var camera = new GameObject("S1 Camera").AddComponent<Camera>();
        camera.orthographic = true;
        var view = new GameObject("S1 View"); view.AddComponent<DialogViewer>(); view.SetActive(false);
        new GameObject("S1 EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>();
        var canvas = new GameObject("S1 Fade Canvas", typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var fadeObject = new GameObject("S1 Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeObject.transform.SetParent(canvas.transform, false);
        var director = new GameObject("S1 Director").AddComponent<TutorialScene1Cutscene>();
        var serialized = new SerializedObject(director);
        serialized.FindProperty("disputeDialog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogContainer>("Assets/S1Dialogue/S1_Dispute.asset");
        serialized.FindProperty("entranceDialog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogContainer>("Assets/S1Dialogue/S1_Entrance.asset");
        serialized.FindProperty("investigationDialog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogContainer>("Assets/S1Dialogue/S1_Investigation.asset");
        serialized.FindProperty("detective").objectReferenceValue = actor.transform;
        serialized.FindProperty("detectiveStartPoint").objectReferenceValue = start;
        serialized.FindProperty("detectiveWaypoints").arraySize = 1;
        serialized.FindProperty("detectiveWaypoints").GetArrayElementAtIndex(0).objectReferenceValue = corner;
        serialized.FindProperty("detectiveCenterPoint").objectReferenceValue = center;
        serialized.FindProperty("cutsceneCamera").objectReferenceValue = camera;
        serialized.FindProperty("openingCameraPoint").objectReferenceValue = opening;
        serialized.FindProperty("entranceCameraPoint").objectReferenceValue = entrance;
        serialized.FindProperty("groupCameraPoint").objectReferenceValue = group;
        serialized.FindProperty("fadeImage").objectReferenceValue = fadeObject.GetComponent<Image>();
        serialized.FindProperty("fadeInTime").floatValue = 0.05f;
        serialized.FindProperty("cameraMoveTime").floatValue = 0.05f;
        serialized.FindProperty("walkSpeed").floatValue = 15;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(s1, "Assets/Scenes/S1.unity");
        var s2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("S2 Entry Marker");
        EditorSceneManager.SaveScene(s2, "Assets/Scenes/S2.unity");
        // NewScene unloads unused asset instances. Reload the saved assets before editing them.
        var flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        var s2Entry = AssetDatabase.LoadAssetAtPath<StoryData>("Assets/Resources/Last.asset");
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("S1"), hidePlayer = true });
        flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("S2"), hidePlayer = true, entryDialog = s2Entry });
        flow.connections.Add(new SceneFlowContainer.Connection { from = Ref("S1"), to = Ref("S2"), trigger = SceneConnectionTrigger.SceneCompleted });
        return flow;
    }

    private static Transform Point(string name, Vector3 position)
    { var result = new GameObject(name).transform; result.position = position; return result; }
    private static SceneReference Ref(string name) => JsonUtility.FromJson<SceneReference>("{\"path\":\"Assets/Scenes/" + name + ".unity\"}");

    public static IEnumerator Checks(Action<bool, string> assert)
    {
        int s2Loads = 0;
        void Loaded(Scene scene, LoadSceneMode mode) { if (scene.name == "S2") s2Loads++; }
        SceneManager.sceneLoaded += Loaded;
        GameMenuController.BlockingForTest = true;
        yield return SceneLoadManager.Instance.LoadContent("S1");
        var director = UnityEngine.Object.FindFirstObjectByType<TutorialScene1Cutscene>();
        var actor = GameObject.Find("S1 Start").scene.GetRootGameObjects();
        Transform detective = null;
        foreach (var root in actor) if (root.name == "S1 Detective") detective = root.transform;
        assert(director != null && detective != null, "S1 director and detective are staged");
        for (int i = 0; i < 3; i++) yield return null;
        assert(director.Phase == TutorialScene1Cutscene.CutscenePhase.WaitingForServices, "S1 waits for the loading menu to finish");
        assert(!detective.gameObject.activeSelf, "Detective is hidden before entrance");
        assert(!DialogManager.Instance.IsDialogOpen, "Loading does not start or skip cutscene dialogue");
        var camera = GameObject.Find("S1 Camera").GetComponent<Camera>();
        assert(Mathf.Approximately(camera.orthographicSize, 2.6f), "Opening camera frames the dispute");
        GameMenuController.BlockingForTest = false;

        StoryData previous = null;
        bool sawEntrance = false, sawWalking = false, sawInvestigation = false;
        for (int line = 0; line < 13; line++)
        {
            StoryData current;
            do
            {
                yield return null;
                if (!sawWalking && director != null && director.Phase == TutorialScene1Cutscene.CutscenePhase.Walking)
                {
                    sawWalking = true;
                    assert(!DialogManager.Instance.IsDialogOpen, "Dialogue stays closed during forced walking");
                    assert(!UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>().enabled, "Player cannot move during detective walk");
                }
                current = CurrentStory.GetValue(DialogManager.Instance) as StoryData;
            } while (!DialogManager.Instance.IsDialogOpen || current == previous);
            assert(current != null && !string.IsNullOrEmpty(current.text) && !current.text.Contains("\uFFFD"), "S1 dialogue line " + (line + 1) + " is readable");
            if (line == 0) assert(current.text == "……없어요." && !detective.gameObject.activeSelf, "S1 opens with the missing brooch");
            if (line == 6)
            {
                sawEntrance = true;
                assert(detective.gameObject.activeSelf && Vector3.Distance(detective.position, StartPosition) < 0.01f,
                    "Detective appears at entrance before walking");
            }
            if (line == 8)
            {
                sawInvestigation = true;
                assert(Vector3.Distance(detective.position, CenterPosition) < 0.01f, "Detective reaches center before announcement");
                assert(Mathf.Approximately(camera.orthographicSize, 3.8f), "Group camera settles before investigation dialogue");
            }
            previous = current;
            DialogManager.Instance.NextDialog();
            if (CurrentStory.GetValue(DialogManager.Instance) == current) DialogManager.Instance.NextDialog();
        }
        assert(sawEntrance && sawWalking && sawInvestigation, "S1 plays all three story sections and movement");
        while (!SceneManager.GetSceneByName("S2").isLoaded || SceneLoadManager.IsLoading) yield return null;
        while (!DialogManager.Instance.IsDialogOpen) yield return null;
        assert(!SceneManager.GetSceneByName("S1").isLoaded, "S1 unloads after its final line");
        assert(s2Loads == 1, "S1 completion enters S2 exactly once");
        assert(SceneManager.GetSceneByName("PrototypeScene").isLoaded && SceneManager.sceneCount == 2, "S1 preserves common systems when entering S2");
        assert((CurrentStory.GetValue(DialogManager.Instance) as StoryData).text == "last line", "S2 entry dialogue starts after S1 completion");
        SceneManager.sceneLoaded -= Loaded;
    }
}
