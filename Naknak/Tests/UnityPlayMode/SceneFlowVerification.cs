using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class SceneFlowVerification
{
    private const string Pending = "Naknak.SceneFlow.Pending";
    private static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    private static readonly List<string> passed = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static bool started;
    private static double deadline;
    private static int eventSystemWarnings;
    private static AsyncOperation pendingOperation;
    [Serializable] private sealed class Report { public bool success; public string failure; public string[] checks; public string[] errors; public int eventSystemWarnings; }
    static SceneFlowVerification() { EditorApplication.update += Tick; }

    public static void Run()
    {
        try
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("/Build/SceneFlowVerification/Assets"))
                throw new InvalidOperationException("Use the isolated loader test project.");
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
            PlayerSettings.companyName = "NaknakVerification";
            PlayerSettings.productName = "SceneFlow_" + Guid.NewGuid().ToString("N");
            var core = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var services = new GameObject("Services");
            services.AddComponent<GameStateManager>(); services.AddComponent<GameEventManager>();
            services.AddComponent<TriggerExecutor>(); services.AddComponent<ConditionManager>(); services.AddComponent<DialogManager>();
            var view = new GameObject("CoreView"); view.transform.SetParent(services.transform); view.AddComponent<DialogViewer>(); view.SetActive(false);
            var selectView = new GameObject("CoreSelectView"); selectView.transform.SetParent(services.transform); selectView.AddComponent<DialogSelectViewer>(); selectView.SetActive(false);
            var player = new GameObject("Player"); player.AddComponent<PlayerMoveController3>();
            var camera = new GameObject("CoreCamera"); camera.AddComponent<Camera>(); camera.AddComponent<AudioListener>();
            new GameObject("CoreEventSystem").AddComponent<EventSystem>();
            EditorSceneManager.SaveScene(core, "Assets/Scenes/PrototypeScene.unity");
            var a = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var aView = new GameObject("ContentView"); aView.AddComponent<DialogViewer>(); aView.SetActive(false);
            var aCamera = new GameObject("ContentCamera"); aCamera.AddComponent<Camera>(); aCamera.AddComponent<AudioListener>();
            new GameObject("ContentEventSystem").AddComponent<EventSystem>();
            EditorSceneManager.SaveScene(a, "Assets/Scenes/TestA.unity");
            var b = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Grid").AddComponent<Grid>();
            EditorSceneManager.SaveScene(b, "Assets/Scenes/TestB.unity");
            var title = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(title, "Assets/Scenes/Title.unity");
            var last = ScriptableObject.CreateInstance<StoryData>(); last.text = "last line"; last.isEnd = true;
            AssetDatabase.CreateAsset(last, "Assets/Resources/Last.asset");
            var flow = ScriptableObject.CreateInstance<SceneFlowContainer>();
            flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestA"), hidePlayer = true });
            flow.scenes.Add(new SceneFlowContainer.SceneEntry { scene = Ref("TestB"), playerPosition = new Vector2(2, 3) });
            flow.connections.Add(new SceneFlowContainer.Connection { from = Ref("TestA"), to = Ref("TestB"), trigger = SceneConnectionTrigger.DialogFinished, completedDialog = last });
            AssetDatabase.CreateAsset(flow, "Assets/Resources/TutorialSceneFlow.asset");
            AssetDatabase.SaveAssets();
            flow = S1CutsceneVerification.CreateScenes();
            EditorUtility.SetDirty(flow);
            AssetDatabase.SaveAssets();
            flow = T3CutsceneVerification.CreateScenes();
            EditorUtility.SetDirty(flow);
            AssetDatabase.SaveAssets();
            flow = T4MemoryVerification.CreateScenes();
            EditorUtility.SetDirty(flow);
            AssetDatabase.SaveAssets();
            flow = T2AppraisalVerification.CreateScenes();
            EditorUtility.SetDirty(flow);
            AssetDatabase.SaveAssets();
            flow = MemoryReplayVerification.CreateScenes();
            EditorUtility.SetDirty(flow);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes = new[] { "Title", "PrototypeScene", "TestA", "TestB", "S1", "S2", "TestT3", "TestT4", "TestT4Memory", "TestT2Appraisal", "TestMemoryT1" }.SelectSceneSettings();
            EditorSceneManager.OpenScene("Assets/Scenes/PrototypeScene.unity");
            GamePlayEntry.Apply();
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static EditorBuildSettingsScene[] SelectSceneSettings(this string[] names)
    {
        var result = new EditorBuildSettingsScene[names.Length];
        for (int i = 0; i < names.Length; i++) result[i] = new EditorBuildSettingsScene("Assets/Scenes/" + names[i] + ".unity", true);
        return result;
    }
    private static SceneReference Ref(string name) => JsonUtility.FromJson<SceneReference>("{\"path\":\"Assets/Scenes/" + name + ".unity\"}");
    private static void Tick()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (!started)
            {
                started = true; deadline = EditorApplication.timeSinceStartup + 90;
                Application.logMessageReceived += (message, trace, type) =>
                {
                    if (type == LogType.Error || type == LogType.Exception) errors.Add(message);
                    if (type == LogType.Warning && message.Contains("event systems")) eventSystemWarnings++;
                };
                routines.Push(Checks());
            }
            if (errors.Count > 0) throw new InvalidOperationException(errors[0]);
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Scene verification timed out");
            if (pendingOperation != null && !pendingOperation.isDone) return;
            pendingOperation = null;
            if (routines.Count == 0) { Finish(errors.Count == 0 && eventSystemWarnings == 0, null); return; }
            var routine = routines.Peek();
            if (!routine.MoveNext()) routines.Pop();
            else if (routine.Current is IEnumerator nested) routines.Push(nested);
            else if (routine.Current is AsyncOperation operation) pendingOperation = operation;
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static IEnumerator Checks()
    {
        yield return null;
        Assert(SceneManager.GetActiveScene().name == "Title", "Play starts at title even with gameplay scene open in Editor");
        var eventObject = new GameObject("PersistentMenuEventSystem");
        eventObject.AddComponent<EventSystem>();
        var eventOwner = eventObject.AddComponent<MenuEventSystemOwner>();
        UnityEngine.Object.DontDestroyOnLoad(eventObject);
        yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Single);
        yield return null;
        var manager = SceneLoadManager.Instance;
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var last = Resources.Load<StoryData>("Last");
        Assert(manager != null, "Loader bootstraps once");
        Assert(UnityEngine.Object.FindObjectsByType<SceneLoadManager>(FindObjectsSortMode.None).Length == 1, "Single loader instance");
        Assert(flow.FindConnection("TestA", SceneConnectionTrigger.SceneCompleted) == null, "Wrong event does not advance");
        Assert(flow.FindConnection("TestA", SceneConnectionTrigger.DialogFinished, last) != null, "Correct dialogue matches route");
        var other = ScriptableObject.CreateInstance<StoryData>();
        Assert(flow.FindConnection("TestA", SceneConnectionTrigger.DialogFinished, other) == null, "Unrelated dialogue does not advance");
        var badConditions = new List<DialogCondition> { new DialogCondition { id = null } };
        Assert(!SceneFlowContainer.Passes(badConditions), "Missing condition asset keeps gate closed");
        yield return manager.LoadContent("TestA");
        Assert(SceneManager.GetSceneByName("TestA").isLoaded, "Content A loaded additively");
        Assert(SceneManager.GetSceneByName("PrototypeScene").isLoaded, "Core survives content load");
        Assert(SceneManager.GetActiveScene().name == "PrototypeScene", "Active save scene remains core");
        Assert(!UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>().enabled, "Cutscene disables player input");
        Assert(!GameObject.Find("CoreCamera").GetComponent<Camera>().enabled, "Only content camera enabled");
        Assert(ActiveEventSystems() == 1, "Core and additive content share one enabled EventSystem");
        Assert(!GameObject.Find("CoreEventSystem").GetComponent<EventSystem>().enabled &&
            !GameObject.Find("ContentEventSystem").GetComponent<EventSystem>().enabled, "Both scene-local EventSystems stay disabled");
        eventOwner.EnsureSingle(); eventOwner.EnsureSingle();
        Assert(ActiveEventSystems() == 1, "Repeated menu open-close synchronization keeps one EventSystem");
        DialogManager.Instance.ShowDialog(last);
        Assert(DialogManager.Instance.IsDialogOpen, "Real DialogManager opens content UI");
        DialogManager.Instance.NextDialog(); DialogManager.Instance.NextDialog();
        Assert(SceneLoadManager.IsLoading, "Dialogue end starts asynchronous transition");
        Assert(!manager.NotifyDialogFinished(last), "Repeated request ignored during transition");
        while (SceneLoadManager.IsLoading) yield return null;
        Assert(!SceneManager.GetSceneByName("TestA").isLoaded && SceneManager.GetSceneByName("TestB").isLoaded, "Previous content replaced");
        Assert(SceneManager.sceneCount == 2, "No extra content scenes accumulate");
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        Assert(player.enabled && (Vector2)player.transform.position == new Vector2(2, 3), "Player restored at destination position");
        Assert(GameObject.Find("CoreCamera").GetComponent<Camera>().enabled, "Core camera restored when content has none");
        Assert(TriggerExecutor.Instance.grid != null, "Destination grid rebound");
        Assert(ActiveEventSystems() == 1, "EventSystem remains unique after previous scene unloads");
        DialogManager.Instance.ShowDialog(last);
        Assert(DialogManager.Instance.IsDialogOpen, "Dialogue UI falls back to core after old view unloads");
        DialogManager.Instance.NextDialog(); DialogManager.Instance.NextDialog();
        Assert(!SceneLoadManager.IsLoading, "No matching exit leaves current scene loaded");
        var flag = ScriptableObject.CreateInstance<ConditionID>(); flag.name = "scene-test-gate";
        var route = flow.connections[0];
        route.conditions.Add(new DialogCondition { id = flag, compare = CompareType.Equal, value = 4 });
        Assert(!SceneFlowContainer.Passes(route.conditions), "Unmet evidence condition blocks route");
        ConditionManager.Instance.Set(flag, 4);
        Assert(SceneFlowContainer.Passes(route.conditions), "Matching evidence condition opens route");
        player.transform.position = new Vector2(9, 9);
        SceneLoadManager.IsRestoringSession = true;
        yield return manager.LoadContent("TestA");
        Assert((Vector2)player.transform.position == new Vector2(9, 9), "Restore does not overwrite saved player position");
        manager.ResumeRestoredSession();
        Assert(!DialogManager.Instance.IsDialogOpen, "Restore does not replay entry dialogue");
        var point = new GameObject("ExitPoint").AddComponent<SceneTransitionPoint>();
        point.container = flow; point.connectionId = route.id;
        flow.connections.Insert(0, new SceneFlowContainer.Connection { from = Ref("TestB"), to = Ref("TestA") });
        point.Interact();
        while (SceneLoadManager.IsLoading) yield return null;
        Assert(SceneManager.GetSceneByName("TestB").isLoaded, "Exit point retains its chosen route after rows are reordered");
        Assert(ConditionManager.Instance.Get(flag) == 4, "Story variables survive scene transition");
        Assert(!manager.Travel(flow, route), "Connection cannot be used from the wrong scene");
        UnityEngine.Object.Destroy(flag);
        UnityEngine.Object.Destroy(other);
        yield return S1CutsceneVerification.Checks(Assert);
        yield return T0RoomVerification.Checks(Assert);
        yield return T3CutsceneVerification.Checks(Assert);
        yield return T4MemoryVerification.Checks(Assert);
        yield return T2AppraisalVerification.Checks(Assert);
        yield return MemoryReplayVerification.Checks(Assert);
    }
    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); passed.Add(message); }
    private static void Finish(bool success, string failure)
    {
        SessionState.SetBool(Pending, false);
        File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "scene-flow-report.json"),
            JsonUtility.ToJson(new Report { success = success, failure = failure, checks = passed.ToArray(), errors = errors.ToArray(), eventSystemWarnings = eventSystemWarnings }, true));
        EditorApplication.Exit(success ? 0 : 1);
    }
    private static int ActiveEventSystems()
    {
        int count = 0;
        foreach (var system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            if (system.isActiveAndEnabled) count++;
        return count;
    }
}
