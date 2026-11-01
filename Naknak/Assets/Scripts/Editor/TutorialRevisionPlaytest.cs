using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Naknak.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit Play-mode regression checks. Disables autosave while exercising tutorial transitions.</summary>
public static class TutorialRevisionPlaytest
{
    public static bool Running { get; private set; }
    public static string Result { get; private set; }
    private static readonly List<string> Passed = new List<string>();
    private static int walkingSamples;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static DialogManager Dialog => DialogManager.Instance;
    private static GameMenuController Menu => GameMenuController.Instance;
    private static string Scene => System.IO.Path.GetFileNameWithoutExtension(SceneLoadManager.CurrentScenePath);
    private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    private static T Find<T>(string name) where T : Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,
        FindObjectsSortMode.None).First(x => x.gameObject.scene.path == SceneLoadManager.CurrentScenePath && x.name == name);
    private static bool Selecting => typeof(GameMenuController).GetField("itemSelected", Private).GetValue(Menu) != null;

    public static void Begin()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter Play mode first.");
        Passed.Clear(); walkingSamples = 0; Result = "Running"; Running = true;
        Menu.enabled = false; // In-memory testing must not overwrite the player's automatic saves.
        SceneLoadManager.Instance.StartCoroutine(Guarded(Run()));
    }
    public static void BeginEvidence()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter Play mode first.");
        Passed.Clear(); walkingSamples = 0; Result = "Running"; Running = true;
        Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(BootstrapEvidence()));
    }
    private static IEnumerator BootstrapEvidence()
    {
        if (!SceneManager.GetSceneByName("PrototypeScene").isLoaded)
            yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Additive);
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        GameStateManager.Instance.ChangeGameState(GameState.Gameplay);
        ConditionManager.Instance.ResetForNewGame();
        GameMenuController.Inventory.Restore(null);
        yield return RunEvidence();
    }
    private static IEnumerator Guarded(IEnumerator root)
    {
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var stack = new Stack<IEnumerator>(); stack.Push(root);
        try
        {
            while (stack.Count > 0)
            {
                object current = null;
                bool moved;
                try { ValidateWalkingClearance(); moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception error) { Result = "FAIL: " + error + "\nPassed:\n" + string.Join("\n", Passed); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
            if (Result == "Running")
            {
                Check(walkingSamples > 0, "Reconstruction walking avoids people and walls (" + walkingSamples + " samples)");
                Result = "PASS\n" + string.Join("\n", Passed);
            }
        }
        finally
        {
            Application.runInBackground = background;
            Running = false;
            SessionState.SetString("Naknak.TutorialRevisionTest", Result);
            Debug.Log("[Tutorial revision test] " + Result);
        }
    }
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Passed.Add(label);
    }

    private static void ValidateWalkingClearance()
    {
        if (!TutorialDeductionTrack.IsMoving) return;
        var people = UnityEngine.Object.FindObjectsByType<NPC>(FindObjectsSortMode.None).Select(p => p.transform)
            .Concat(UnityEngine.Object.FindObjectsByType<PlayerMoveController3>(FindObjectsSortMode.None).Select(p => p.transform)).ToArray();
        foreach (var track in UnityEngine.Object.FindObjectsByType<TutorialDeductionTrack>(FindObjectsSortMode.None))
        {
            var actor = (Transform)typeof(TutorialDeductionTrack).GetField("actor", Private).GetValue(track);
            if (actor == null) continue;
            foreach (var person in people.Where(p => p != actor && p.gameObject.activeInHierarchy))
                if (Mathf.Abs(actor.position.x - person.position.x) < .65f && Mathf.Abs(actor.position.y - person.position.y) < 1.05f)
                    throw new InvalidOperationException("Walking overlaps " + person.name + ": " + actor.name + " at " + actor.position);
            var foot = actor.position + Vector3.down * .5f;
            foreach (var wall in actor.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>())
                         .Where(m => m.GetComponent<Collider2D>() != null))
            {
                var cell = wall.WorldToCell(foot);
                if (wall.HasTile(cell) && wall.GetColliderType(cell) != UnityEngine.Tilemaps.Tile.ColliderType.None)
                    throw new InvalidOperationException("Walking crosses wall: " + actor.name + " at " + actor.position);
            }
            walkingSamples++;
        }
    }
    private static IEnumerator Wait(Func<bool> ready, string label, float timeout = 30)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!ready())
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(label + " | scene=" + Scene + " dialog=" + Dialog?.CurrentStory?.name);
            yield return null;
        }
    }
    private static IEnumerator CloseDialog()
    {
        var chain = new HashSet<StoryData>();
        var pending = new Stack<StoryData>();
        if (Dialog.CurrentStory != null) pending.Push(Dialog.CurrentStory);
        while (pending.Count > 0)
        {
            var line = pending.Pop();
            if (!chain.Add(line)) continue;
            if (line.nextData != null) pending.Push(line.nextData);
            if (line.selects != null) foreach (var choice in line.selects) if (choice.nextData != null) pending.Push(choice.nextData);
        }
        float deadline = Time.realtimeSinceStartup + 35;
        while (Dialog != null && Dialog.IsDialogOpen && chain.Contains(Dialog.CurrentStory))
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Dialogue did not close: " + Dialog.CurrentStory.name);
            Dialog.NextDialog();
            yield return null;
        }
    }
    private static void Select(string id)
    {
        var items = (List<InventoryItem>)typeof(GameMenuController).GetField("visibleItems", Private).GetValue(Menu);
        int index = items.FindIndex(i => i.Id == id);
        if (index < 0) throw new InvalidOperationException("No selectable item: " + id);
        typeof(GameMenuController).GetMethod("ActivateInventoryItem", Private).Invoke(Menu, new object[] { index });
    }
    private static IEnumerator Replay(string actor, string id, int expected)
    {
        Find<InvestigationPoint>(actor).Interact();
        Check(Selecting, actor + " opens evidence menu by interaction (" + expected + ")");
        Select(id);
        yield return CloseDialog();
        yield return Wait(() => !Find<EvidenceReconstruction>(actor).IsReplaying, "Replay restore");
        Check(Find<EvidenceReconstruction>(actor).CompletedEvidence.Length == expected, actor + " distinct route count " + expected);
    }
    private static IEnumerator Capture(string name)
    {
        System.IO.Directory.CreateDirectory("Captures");
        if (Dialog.IsDialogOpen && (bool)typeof(DialogManager).GetField("isPlaying", Private).GetValue(Dialog)) Dialog.NextDialog();
        float settle = Time.realtimeSinceStartup + 0.25f;
        while (Time.realtimeSinceStartup < settle) yield return null;
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("Captures/tutorial-" + name + ".png");
        yield return null;
    }

    private static IEnumerator Run()
    {
        if (!SceneManager.GetSceneByName("PrototypeScene").isLoaded)
            yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Additive);
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        GameStateManager.Instance.ChangeGameState(GameState.Gameplay);
        ConditionManager.Instance.ResetForNewGame();
        GameMenuController.Inventory.Restore(null);
        yield return SceneLoadManager.Instance.LoadContent("Tutorial_Prologue");
        yield return Wait(() => Dialog.IsDialogOpen && Dialog.CurrentStory.name == "Prologue_01", "S0 entry");
        var story = Dialog.CurrentStory;
        int count = 0;
        while (story != null) { count++; story = story.nextData; if (count > 30) break; }
        Check(count == 18, "S0 has 18 ordered dialogue/sound beats");
        Check(Load<StoryData>("Assets/Story/Tutorial/Prologue/Prologue_09.asset").portrait ==
            AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Portraits/탐정 잠.png").OfType<Sprite>().First(), "S0 sleeping portrait");
        yield return Capture("s0");
        yield return CloseDialog();
        yield return Wait(() => Scene == "S1" && !SceneLoadManager.IsLoading, "S1 entry");
        var cutscene = UnityEngine.Object.FindFirstObjectByType<TutorialScene1Cutscene>();
        bool walked = false, entranceAfterWalk = false;
        float deadline = Time.realtimeSinceStartup + 35;
        while (Scene == "S1")
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("S1 staging");
            if (cutscene != null && cutscene.Phase == TutorialScene1Cutscene.CutscenePhase.Walking) walked = true;
            if (Dialog.IsDialogOpen && Dialog.CurrentStory.name == "S1_Entrance_01") entranceAfterWalk = walked;
            if (Dialog.IsDialogOpen) Dialog.NextDialog();
            yield return null;
        }
        Check(walked && entranceAfterWalk, "S1 detective walks before entrance dialogue");
        yield return Wait(() => Scene == "S2" && !SceneLoadManager.IsLoading, "S2 entry");
        Find<NPC>("assist").Interact();
        yield return Wait(() => Dialog.IsDialogOpen, "S2 assistant conversation");
        Check(UnityEngine.Object.FindObjectsByType<TutorialAttentionMarker>(FindObjectsSortMode.None).Any(), "S2 assistant attention marker");
        while (Dialog.CurrentStory == null || Dialog.CurrentStory.name != "MemoryEntryNarration")
        {
            if (Dialog.IsDialogOpen) Dialog.NextDialog();
            yield return null;
        }
        Check(Scene == "S2" && Dialog.DialogueCanvas.sortingOrder == 30001, "Memory narration appears over the black transition");
        yield return Capture("memory-entry");
        yield return CloseDialog();
        yield return Wait(() => Scene == "T0" && !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "T0 entry");
        yield return CloseDialog();
        Check(!SceneLoadManager.IsColorMemory, "T0 memory uses grayscale");
        var room = UnityEngine.Object.FindFirstObjectByType<TutorialRoom0>();
        room.CollectBrooch(); yield return CloseDialog();
        room.InteractWithBox(); Check(Selecting, "T0 packing opens inventory");
        Select("tutorial-brooch");
        Check(Dialog.CurrentStory.text == "미안해요. 팔 수밖에 없어요.", "T0 packing assistant line");
        yield return Capture("t0-packed");
        yield return CloseDialog(); room.TryLeaveRoom();
        yield return Wait(() => Scene == "T1" && !SceneLoadManager.IsLoading, "T1 entry");
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        Check(Vector2.Distance(player.transform.position, new Vector2(1, -4)) < .05f, "T1 assistant starts at the entrance");
        yield return Wait(() => Dialog.IsDialogOpen, "T1 conversation");
        yield return CloseDialog();
        if (Scene == "T1") yield return SceneLoadManager.Instance.LoadContent("T2");
        yield return Wait(() => Scene == "T2" && !SceneLoadManager.IsLoading, "T2 entry");
        yield return null;
        Find<NPC>("Owner").Interact();
        Dialog.SkipUntilChoice();
        yield return Wait(() => Selecting, "Owner asks for appraisal item");
        Select("tutorial-packed-jewelry-box");
        yield return Wait(() => Dialog.IsDialogOpen, "Appraisal dialogue");
        Dialog.SkipUntilChoice();
        Check(!Dialog.IsDialogOpen, "Ctrl skip follows the first price choice and closes appraisal");
        Check(Scene == "T2", "T2 waits for A's walk before changing scenes");
        yield return Wait(() => Scene == "Tutorial_T3_VaseBreak" && !SceneLoadManager.IsLoading, "A walks to telephone");
        Check(Scene == "Tutorial_T3_VaseBreak", "T2 continues to blackout after A moves");
        deadline = Time.realtimeSinceStartup + 35;
        while (Scene != "S3" || SceneLoadManager.IsLoading)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("First memory return: " + Scene);
            if (Dialog.IsDialogOpen) Dialog.NextDialog();
            yield return null;
        }
        Check(true, "First memory returns through T4 without enabling investigation");
        yield return RunEvidence(true);
    }

    private static IEnumerator RunEvidence(bool alreadyInReality = false)
    {
        // Enter a second memory without touching persistent saves.
        yield return Wait(() => !SceneLoadManager.IsLoading, "First blackout ready");
        if (!alreadyInReality)
        {
            yield return CloseDialog();
            yield return SceneLoadManager.Instance.LoadContent("S3");
        }
        yield return Wait(() => !SceneLoadManager.IsLoading, "Reality ready");
        yield return Wait(() => Dialog.IsDialogOpen, "S3 conversation");
        yield return CloseDialog();
        yield return SceneLoadManager.Instance.LoadContent("T1");
        yield return Wait(() => !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "Replay entry");
        yield return CloseDialog();
        if (Scene == "T1") yield return SceneLoadManager.Instance.LoadContent("T2");
        yield return Wait(() => Scene == "T2" && !SceneLoadManager.IsLoading, "Replay T2");
        Check(SceneLoadManager.IsMemoryRevisit, "Second memory enables investigation");
        Find<InvestigationPoint>("Cabinet Number 2").Interact();
        Check(Dialog.CurrentStory.text == "진열장이다.", "Pre-blackout cabinet is ordinary");
        yield return CloseDialog();
        Find<InvestigationPoint>("Trash Can").Interact(); yield return CloseDialog();
        Find<InvestigationPoint>("phone").Interact(); yield return CloseDialog();
        Find<InvestigationPoint>("A").Interact();
        Check(Selecting, "A interaction opens evidence immediately");
        typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
        Check(!Selecting && Time.timeScale > 0, "Cancel restores gameplay without evidence credit");
        Find<InvestigationPoint>("A").Interact(); Select("evidence-expired-ticket");
        Check(!Selecting && Dialog.CurrentStory.name == "WrongEvidence", "Unrelated evidence shows the rejection line");
        yield return CloseDialog();
        Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == 0, "Rejected evidence does not increase progress");
        yield return Replay("A", "evidence-returned-letter", 1);
        yield return Replay("A", "evidence-returned-letter", 1);
        Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == 1, "Repeated evidence counts once");
        Find<InvestigationPoint>("A").Interact();
        Check(((string[])typeof(GameMenuController).GetField("itemSelectionCompleted", Private).GetValue(Menu)).Length == 1,
            "Evidence menu shows one successful item");
        yield return Capture("evidence-1-of-3");
        typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
        yield return Replay("A", "evidence-watch-box", 2);
        yield return Replay("A", "evidence-telephone-trace", 3);
        Find<InvestigationPoint>("A").Interact();
        Check(!Selecting && Dialog.CurrentStory.text == "A의 동선을 전부 파악했다.", "A complete interaction shows the completion line");
        yield return CloseDialog();
        Find<InvestigationPoint>("Vase Toy").Interact(); yield return CloseDialog();
        yield return Replay("B", "evidence-expired-ticket", 1);
        yield return Replay("B", "evidence-clockwork-toy", 2);
        Find<InvestigationPoint>("B").Interact(); Select("evidence-flathead-driver");
        yield return CloseDialog();
        Check(Find<EvidenceReconstruction>("B").CompletedEvidence.Length == 2,
            "Driver without cabinet investigation does not complete the third route");
        yield return SceneLoadManager.Instance.LoadContent("Tutorial_T3_VaseBreak");
        yield return Wait(() => !SceneLoadManager.IsLoading && !SceneLoadManager.IsCutscene, "Dark T3 replay exploration");
        var controller = UnityEngine.Object.FindFirstObjectByType<TutorialT3T4SceneController>();
        var overlay = new SerializedObject(controller).FindProperty("darkOverlayImage").objectReferenceValue as UnityEngine.UI.Image;
        Check(overlay != null && overlay.color.a > .6f && overlay.color.a < 1, "T3 revisit remains dark and playable");
        Find<InvestigationPoint>("Cabinet Number 2").Interact(); yield return CloseDialog();
        Find<InvestigationPoint>("B").Interact(); Select("evidence-flathead-driver");
        yield return CloseDialog();
        yield return Wait(() => Dialog.IsDialogOpen && Dialog.CurrentStory.name == "AlibisVerified", "Automatic combined completion");
        Check(Dialog.CurrentStory.text == "A와 B의 동선을 전부 파악했다.", "All six actions trigger combined completion");
        yield return Capture("both-routes-complete");
        yield return CloseDialog();
        yield return Wait(() => Scene == "Tutorial_PA_Alibi" && !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "Automatic memory exit to PA");
        Check(!SceneLoadManager.IsMemorySpace && !UnityEngine.Object.FindObjectsByType<TutorialDeductionTrack>(FindObjectsSortMode.None).Any(),
            "PA is a stationary reality conversation");
        yield return Capture("pa-reality");
        yield return CloseDialog();
        yield return Wait(() => Scene == "Tutorial_PB_Alibi" && !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "PA to PB");
        Check(!SceneLoadManager.IsMemorySpace, "PB is a reality conversation");
        yield return CloseDialog();
        yield return Wait(() => Scene == "S#6" && !SceneLoadManager.IsLoading, "PB to owner confrontation");
        Check(ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/PACompleted.asset")) == 1 &&
            ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/PBCompleted.asset")) == 1,
            "Both truth confirmations remain complete after memory exit");
    }
}
