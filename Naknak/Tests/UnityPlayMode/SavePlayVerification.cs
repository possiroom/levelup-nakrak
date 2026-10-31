// Copied into an isolated Unity project's Assets/Editor for -executeMethod SavePlayVerification.Run.
// This runner refuses to run in the user's main project and never uses their save directory.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Naknak.Inventory;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SavePlayVerification
{
    private const string Pending = "Naknak.SaveVerification.Pending";
    private static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    private static readonly List<string> passed = new List<string>();
    private static readonly List<string> runtimeErrors = new List<string>();
    private static double deadline;
    private static bool started;
    private static int eventSystemWarnings;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Serializable] private sealed class Report
    {
        public bool success;
        public string failure;
        public string[] checks;
        public string[] runtimeErrors;
        public int eventSystemWarnings;
    }

    static SavePlayVerification() { EditorApplication.update += Tick; }

    public static void Run()
    {
        try
        {
            if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/SavePlayVerification/Assets"))
                throw new InvalidOperationException("Verification must run in the isolated temporary project.");
            if (PlayerSettings.companyName != "NaknakVerification")
                throw new InvalidOperationException("Isolated save identity is required.");
            PlayerSettings.productName = "SavePlayVerification_" + Guid.NewGuid().ToString("N");
            // These checks use the real Unity JSON serializer and purchased XML saver.
            InventoryTools.RunChecks();
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity", OpenSceneMode.Single);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (!started)
            {
                started = true;
                deadline = EditorApplication.timeSinceStartup + 600;
                Application.logMessageReceived += Log;
                routines.Push(Checks());
            }
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Play mode verification timed out.");
            if (routines.Count == 0) { Finish(runtimeErrors.Count == 0, runtimeErrors.Count == 0 ? null : "Runtime errors were logged."); return; }
            var current = routines.Peek();
            if (!current.MoveNext()) routines.Pop();
            else if (current.Current is IEnumerator nested) routines.Push(nested);
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            runtimeErrors.Add(message + "\n" + trace);
        if (type == LogType.Warning && message.Contains("event systems")) eventSystemWarnings++;
    }

    private static void Finish(bool success, string failure)
    {
        SessionState.SetBool(Pending, false);
        var report = new Report { success = success, failure = failure, checks = passed.ToArray(), runtimeErrors = runtimeErrors.ToArray(), eventSystemWarnings = eventSystemWarnings };
        string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "save-play-report.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log("[Save Verification] " + (success ? "PASS" : "FAIL") + " / " + passed.Count + " checks / " + failure);
        EditorApplication.Exit(success ? 0 : 1);
    }

    private static GameMenuController Menu => GameMenuController.Instance;
    private static object Field(string name) => typeof(GameMenuController).GetField(name, Private).GetValue(Menu);
    private static string Page => Field("page").ToString();
    private static object Invoke(string name, params object[] args) => typeof(GameMenuController).GetMethod(name, Private).Invoke(Menu, args);
    private static List<Button> Buttons => (List<Button>)Field("buttons");

    private static void ShowMenu()
    {
        Invoke("Pause");
        object menuPage = Enum.Parse(typeof(GameMenuController).GetNestedType("Page", BindingFlags.NonPublic), "Menu");
        Invoke("ShowPage", menuPage);
    }

    private static void Click(string text)
    {
        foreach (var button in new List<Button>(Buttons))
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && label.text == text && button.interactable)
            { button.onClick.Invoke(); return; }
        }
        throw new InvalidOperationException("Button not available: " + text + " on " + Page);
    }

    private static void ClickSlot(int slot)
    {
        Check(Buttons[slot - 1].interactable, "Slot " + slot + " is selectable on " + Page);
        Buttons[slot - 1].onClick.Invoke();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed.Add(message);
    }

    private static IEnumerator Wait(Func<bool> condition, string what)
    {
        double timeout = EditorApplication.timeSinceStartup + 150;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("Waiting for " + what +
                "; scene=" + SceneManager.GetActiveScene().name + "; page=" + (Menu != null ? Page : "none") + "; mapLoading=" + MapStreamingManager.IsLoading);
            yield return null;
        }
        yield return null;
    }

    private static bool Ready() => Menu != null && !GameMenuController.IsBlockingGameplay && !MapStreamingManager.IsLoading &&
                                   UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>() != null;

    private static IEnumerator Checks()
    {
        yield return Wait(() => Menu != null && Page == "Title", "title bootstrap");
        Check(SceneManager.GetActiveScene().name == "Title", "Title scene opens");
        Check(!Buttons[1].interactable, "Continue disabled before first save");
        Click("새 게임");
        yield return Wait(Ready, "new game and additive map");
        Check(SceneManager.GetActiveScene().name == "PrototypeScene", "New game loads PrototypeScene");
        Check(SceneManager.GetSceneByName("Map_01").isLoaded, "Map_01 loads additively");
        Check(Array.FindAll(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None), system => system.isActiveAndEnabled).Length == 1,
            "One active event system after scene transition");
        Check(TriggerExecutor.Instance != null && TriggerExecutor.Instance.grid != null, "Step trigger grid is bound");
        TemporaryFeatureSwitches.HealthSystemEnabled = true;
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var id = ScriptableObject.CreateInstance<ConditionID>(); id.name = "verification-condition";
        var config = Resources.Load<GameMenuConfiguration>("NaknakMenu");
        InventoryItem note = null, key = null;
        foreach (var item in config.items) { if (item.IsKeyItem) key = item; else note = item; }
        Check(note != null && key != null, "Real item assets loaded");
        Check(GameMenuController.Inventory.Add(note.Id, 2) && GameMenuController.Inventory.Add(key.Id), "Acquire normal and key items");
        player.TeleportToWorld(new Vector2(2.5f, 0)); player.SetFloor(2); player.SetFacingDirection(Vector2.left);
        PlayerStatus.Instance.RestoreHealth(73);
        ConditionManager.Instance.Set(id, 7);
        Invoke("TrackWorld");
        var world = (Dictionary<string, GameObject>)Field("trackedObjects");
        string worldKey = null;
        foreach (var pair in world)
            if (pair.Value != null && pair.Value.GetComponent<NPC>() != null) { worldKey = pair.Key; pair.Value.SetActive(false); break; }
        Check(worldKey != null, "Map NPC available for world-state restoration check");
        ShowMenu();
        Check(Time.timeScale == 0 && GameStateManager.Instance.GameState == GameState.Pause, "Game pauses while menu is open");
        Click("세이브"); ClickSlot(1);
        Check(Page == "Confirm", "Save success notice appears");
        Check(GameCheckpointStore.TryReadManual(1, out var first, out _), "Manual save creates a real JSON file");
        Check(first.position.x == 2.5f && first.floor == 2 && first.health == 73, "Saved position, floor and health");
        Check(first.inventory.Count == 2 && first.conditions.Exists(c => c.key == id.name && c.value == 7), "Saved inventory and story flags");
        Check(first.world.Exists(w => w.path == worldKey && !w.active), "Saved disabled NPC state");
        string firstBytes = File.ReadAllText(GameCheckpointStore.ManualSlotPath(1));
        Click("돌아가기"); ClickSlot(1);
        Check(Page == "Confirm" && !(bool)Field("noticeOnly"), "Existing slot asks before overwrite");
        Check((int)Field("selected") == 1, "Overwrite defaults to cancel");
        Click("취소");
        Check(File.ReadAllText(GameCheckpointStore.ManualSlotPath(1)) == firstBytes, "Cancelling overwrite leaves file unchanged");
        Click("뒤로"); Invoke("Resume"); yield return null;
        player.TeleportToWorld(new Vector2(9.5f, 4));
        GameMenuController.Inventory.Remove(note.Id); ConditionManager.Instance.Set(id, 42);
        world[worldKey].SetActive(true); PlayerStatus.Instance.RestoreHealth(51);
        ShowMenu(); Click("세이브"); ClickSlot(2); Click("돌아가기");
        Check(File.ReadAllText(GameCheckpointStore.ManualSlotPath(1)) == firstBytes, "Slot 2 does not change slot 1");
        Click("뒤로"); Click("로드");
        Check((bool)Field("loadManual"), "Load menu selects manual saves");
        ClickSlot(1); Click("취소");
        Check(player.transform.position.x == 9.5f && GameMenuController.Inventory.Count(note.Id) == 1, "Cancelling load leaves gameplay state unchanged");
        ClickSlot(1); Click("확인");
        yield return Wait(Ready, "manual load and scene reconstruction");
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        Check(player.transform.position.x == 2.5f && player.transform.position.y == 0 && player.GetCurrentFloor() == 2, "Load restores actual player position and floor");
        Check(player.FacingDirection == Vector2.left && PlayerStatus.Instance.GetHealth() == 73, "Load restores facing and health");
        Check(GameMenuController.Inventory.Count(note.Id) == 2 && GameMenuController.Inventory.Count(key.Id) == 1, "Load restores actual inventory quantities");
        Check(ConditionManager.Instance.Get(id) == 7, "Load restores actual story flags");
        world = (Dictionary<string, GameObject>)Field("trackedObjects");
        Check(world.TryGetValue(worldKey, out var restoredNpc) && restoredNpc != null && !restoredNpc.activeSelf, "Load restores actual NPC active state");
        Check(Time.timeScale == 1 && GameStateManager.Instance.GameState == GameState.Gameplay, "Load resumes gameplay");
        ShowMenu(); Click("세이브"); ClickSlot(1); Click("확인");
        Check(File.Exists(GameCheckpointStore.ManualSlotPath(1) + ".bak"), "Confirmed overwrite creates backup");
        Click("돌아가기"); Click("뒤로");
        File.WriteAllText(GameCheckpointStore.ManualSlotPath(3), "{broken");
        Click("로드");
        Check(!Buttons[2].interactable, "Corrupt manual save is disabled in load UI");
        Click("뒤로"); Click("타이틀로"); Click("확인");
        yield return Wait(() => Menu != null && Page == "Title" && !(bool)Field("loading"), "return to title");
        Check(Buttons[1].interactable, "Continue enabled after manual saves");
        Click("이어하기"); ClickSlot(2); Click("확인");
        yield return Wait(Ready, "continue from title");
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        Check(player.transform.position.x == 9.5f && player.transform.position.y == 4, "Continue loads selected slot 2 position");
        Check(GameMenuController.Inventory.Count(note.Id) == 1 && ConditionManager.Instance.Get(id) == 42, "Continue loads selected slot 2 inventory and flags");
        Check(PlayerStatus.Instance.GetHealth() == 51, "Continue loads selected slot 2 health");
        Check(eventSystemWarnings == 0, "No duplicate event-system warnings during transitions");
        UnityEngine.Object.Destroy(id);
        yield return null;
    }
}
