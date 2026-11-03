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
public static partial class TutorialRevisionPlaytest
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
    public static void BeginDepth()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter Play mode first.");
        Passed.Clear(); Result = "Running"; Running = true;
        Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(RunDepth(), false));
    }

    public static void BeginSystems()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter Play mode first.");
        Passed.Clear(); Result = "Running"; Running = true;
        Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(RunSystems(), false));
    }

    private static void ShowMenuPage(string page)
    {
        var pageType = typeof(GameMenuController).GetField("page", Private).FieldType;
        typeof(GameMenuController).GetMethod("ShowPage", Private).Invoke(Menu, new[] { Enum.Parse(pageType, page) });
    }

    private static IEnumerator RunSystems()
    {
        if (!SceneManager.GetSceneByName("PrototypeScene").isLoaded)
            yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Additive);
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        yield return CloseDialog();
        ConditionManager.Instance.ResetForNewGame();
        foreach (var name in new[] { "T0", "T1", "T2", "S3" })
        {
            yield return SceneLoadManager.Instance.LoadContent(name);
            yield return Wait(() => !SceneLoadManager.IsLoading, name + " camera ready");
            yield return CloseDialog();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
            var camera = SceneManager.GetSceneByName("PrototypeScene").GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>(true)).First();
            Check(camera.Follow == player.transform, name + " camera follows controlled player");
            Check(!UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t => t.name == "Room Camera Bounds"),
                name + " has no generated room bounds");
            float size = camera.Lens.OrthographicSize;
            Check(size > 0 && !float.IsNaN(size), name + " has a finite positive camera size");
            if (name == "T0")
            {
                var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
                Check(player.GetComponent<Animator>().runtimeAnimatorController == flow.assistantPlayerController,
                    "First-memory controlled actor uses assistant animation");
                var original = player.transform.position;
                player.TeleportToWorld((Vector2)original + Vector2.right * 2);
                float deadline = Time.realtimeSinceStartup + 2;
                while (Time.realtimeSinceStartup < deadline) yield return null;
                var output = Camera.main;
                var viewport = output.WorldToViewportPoint(player.transform.position);
                Check(viewport.z > 0 && viewport.x > .15f && viewport.x < .85f && viewport.y > .15f && viewport.y < .85f,
                    "Camera keeps moving assistant on screen");
                Check(Mathf.Approximately(size, camera.Lens.OrthographicSize), "Walking does not change gameplay zoom");
                SceneLoadManager.Instance.SetPlayerInputLocked(true);
                var focus = new GameObject("Camera regression cutscene focus");
                camera.Follow = focus.transform;
                SceneLoadManager.Instance.SetPlayerInputLocked(false);
                Check(camera.Follow == player.transform, "Releasing cutscene restores player camera follow");
                UnityEngine.Object.Destroy(focus);
                player.TeleportToWorld(original);
            }
        }
        typeof(GameMenuController).GetMethod("Pause", Private).Invoke(Menu, null);
        foreach (string page in new[] { "Title", "Options", "Title", "Load", "Title", "Menu", "Items", "Save", "Menu", "Options", "Title" })
        {
            ShowMenuPage(page);
            yield return null; yield return null;
            var panel = (RectTransform)typeof(GameMenuController).GetField("panel", Private).GetValue(Menu);
            Check(panel.Find("Border") != null, page + " page preserves persistent window border");
            if (page == "Title")
            {
                var art = panel.GetComponentInChildren<UnityEngine.UI.RawImage>();
                Check(art != null && art.texture != null, "Title loads requested artwork");
                var buttons = (List<UnityEngine.UI.Button>)typeof(GameMenuController).GetField("buttons", Private).GetValue(Menu);
                Check(buttons.Count == 4 && ((RectTransform)buttons[0].transform).anchorMin.x < ((RectTransform)buttons[3].transform).anchorMin.x,
                    "Title has four horizontally arranged commands");
            }
        }
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        Check(Time.timeScale > 0 && !GameMenuController.IsBlockingGameplay, "Closing menu restores gameplay and time");
        var actor = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var checkpoint = (GameCheckpoint)typeof(GameMenuController).GetMethod("CaptureCheckpoint", Private).Invoke(Menu, new object[] { actor });
        string directory = System.IO.Path.GetFullPath("Captures/Regression-20261102/saves-" + Guid.NewGuid().ToString("N"));
        var retry = typeof(GameCheckpointStore).GetMethod("RetryFileOperation", BindingFlags.Static | BindingFlags.NonPublic);
        int attempts = 0;
        Action temporaryLock = () => { attempts++; if (attempts < 3) throw new System.IO.IOException("Test transient lock"); };
        retry.Invoke(null, new object[] { temporaryLock });
        Check(attempts == 3, "Temporary file failure is retried until success");
        attempts = 0;
        Action permanentLock = () => { attempts++; throw new System.IO.IOException("Test persistent lock"); };
        bool bounded = false;
        try { retry.Invoke(null, new object[] { permanentLock }); }
        catch (TargetInvocationException exception) when (exception.InnerException is System.IO.IOException) { bounded = true; }
        Check(bounded && attempts == 5, "Persistent file failure stops after bounded retries");
        for (int slot = 1; slot <= GameCheckpointStore.SlotCount; slot++)
        {
            GameCheckpointStore.WriteManual(slot, checkpoint, directory);
            Check(GameCheckpointStore.TryReadManual(slot, out var saved, out _, directory) && saved.position == checkpoint.position,
                "Manual save roundtrip slot " + slot);
        }
        var lockedFile = new System.IO.FileStream(GameCheckpointStore.ManualSlotPath(1, directory),
            System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.None);
        var release = System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(80); lockedFile.Dispose(); });
        checkpoint.playSeconds = 99;
        try { GameCheckpointStore.WriteManual(1, checkpoint, directory); }
        finally { release.Wait(); }
        Check(GameCheckpointStore.TryReadManual(1, out var unlocked, out _, directory) && unlocked.playSeconds == 99,
            "Manual save succeeds after a real exclusive file lock is released");
        for (int i = 0; i < 4; i++) { checkpoint.playSeconds = i; GameCheckpointStore.Write(checkpoint, directory); }
        for (int slot = 1; slot <= 3; slot++)
            Check(GameCheckpointStore.TryRead(slot, out var saved, out _, directory) && saved.playSeconds == 4 - slot,
                "Autosave rotation slot " + slot);
        GameCheckpointStore.WriteManual(1, checkpoint, directory);
        System.IO.File.WriteAllText(GameCheckpointStore.ManualSlotPath(1, directory), "{broken json");
        Check(GameCheckpointStore.TryReadManual(1, out _, out _, directory), "Corrupted primary recovers manual backup");
        System.IO.File.WriteAllText(GameCheckpointStore.SlotPath(3, directory), "{broken json");
        Check(!GameCheckpointStore.TryRead(3, out _, out var error, directory) && error != null, "Corrupted save is rejected without crashing");
        var expectedPosition = checkpoint.position;
        yield return (IEnumerator)typeof(GameMenuController).GetMethod("ChangeSession", Private).Invoke(Menu, new object[] { checkpoint.scene, checkpoint });
        yield return Wait(() => !SceneLoadManager.IsLoading && !SceneLoadManager.IsRestoringSession, "Session restore ready");
        actor = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        Check(actor != null && Vector3.Distance(actor.transform.position, expectedPosition) < .01f, "Session load restores player position");
        Check(Scene == "S3", "Session load restores additive story scene");
    }

    private static IEnumerator RunDepth()
    {
        if (!SceneManager.GetSceneByName("PrototypeScene").isLoaded)
            yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Additive);
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        yield return CloseDialog();
        ConditionManager.Instance.ResetForNewGame();
        GameStateManager.Instance.ChangeGameState(GameState.Gameplay);
        yield return SceneLoadManager.Instance.LoadContent("T2");
        yield return Wait(() => Scene == "T2" && !SceneLoadManager.IsLoading, "Depth preview shop");
        yield return null; yield return null;
        Physics2D.SyncTransforms();
        SceneLoadManager.Instance.SetPlayerInputLocked(true);
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var sprite = player.GetComponent<SpriteRenderer>();
        var camera = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        var focus = new GameObject("Depth preview focus");
        focus.transform.position = new Vector3(1, 1, 0);
        camera.Follow = focus.transform;
        var lens = camera.Lens; lens.OrthographicSize = 7.5f; camera.Lens = lens;
        var counter = Find<UnityEngine.Rendering.SortingGroup>("Depth - Counter");
        var display = Find<UnityEngine.Rendering.SortingGroup>("Depth - Display left");
        var person = Find<EvidenceReconstruction>("A").GetComponent<SpriteRenderer>();
        foreach (var test in new[]
        {
            (new Vector2(-2, counter.transform.position.y + .6f), (Renderer)null, counter, false, "counter-behind"),
            (new Vector2(-2, counter.transform.position.y - .6f), (Renderer)null, counter, true, "counter-front"),
            (new Vector2(0, display.transform.position.y + .6f), (Renderer)null, display, false, "display-behind"),
            (new Vector2(0, display.transform.position.y - .6f), (Renderer)null, display, true, "display-front"),
            (new Vector2(-3, person.bounds.min.y + .7f), (Renderer)person, (UnityEngine.Rendering.SortingGroup)null, false, "person-behind"),
            (new Vector2(-3, person.bounds.min.y - .7f), (Renderer)person, (UnityEngine.Rendering.SortingGroup)null, true, "person-front")
        })
        {
            var position = test.Item1;
            position.y -= player.GetComponent<WorldDepthSort>().GroundY - player.transform.position.y;
            player.TeleportToWorld(position);
            yield return null;
            yield return Capture("depth-" + test.Item5);
            int otherOrder = test.Item2 != null ? test.Item2.sortingOrder : test.Item3.sortingOrder;
            int otherLayer = test.Item2 != null ? test.Item2.sortingLayerID : test.Item3.sortingLayerID;
            Check(sprite.sortingLayerID == otherLayer && (test.Item4 ? sprite.sortingOrder > otherOrder : sprite.sortingOrder < otherOrder),
                "Depth: " + test.Item5);
        }
        UnityEngine.Object.Destroy(focus);
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
    private static IEnumerator Guarded(IEnumerator root, bool requireWalking = true)
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
                if (requireWalking) Check(walkingSamples > 0, "Reconstruction walking avoids people and walls (" + walkingSamples + " samples)");
                Result = "PASS\n" + string.Join("\n", Passed);
            }
        }
        finally
        {
            while (stack.Count > 0)
                if (stack.Pop() is IDisposable cleanup) cleanup.Dispose();
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
            .Concat(UnityEngine.Object.FindObjectsByType<EvidenceReconstruction>(FindObjectsSortMode.None).Select(p => p.transform))
            .Concat(UnityEngine.Object.FindObjectsByType<PlayerMoveController3>(FindObjectsSortMode.None).Select(p => p.transform)).ToArray();
        foreach (var track in UnityEngine.Object.FindObjectsByType<TutorialDeductionTrack>(FindObjectsSortMode.None))
        {
            if (!track.isActiveAndEnabled) continue;
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

    private static IEnumerator Run(bool repeatedVisits = false)
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
        Check(TutorialEntranceAudio.PlayCount > 0 && !TutorialEntranceAudio.IsPlaying, "S0/S1 footsteps and door bell finish before S1");
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
        yield return null;
        Check(player.FacingDirection == Vector2.up, "T1 assistant faces away from the camera");
        var t1Staging = UnityEngine.Object.FindFirstObjectByType<TutorialWitnessStaging>();
        yield return CloseDialog();
        Check(Scene == "T1" && !t1Staging.AppraisalWalkComplete, "A starts walking after the A/owner conversation in T1");
        yield return Wait(() => Scene == "T2" && !SceneLoadManager.IsLoading, "T2 entry");
        yield return null;
        var witnessA = Find<EvidenceReconstruction>("A").transform;
        var phonePosition = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<TutorialWitnessStaging>()).FindProperty("phonePosition").vector2Value;
        Check(Vector2.Distance(witnessA.position, phonePosition) < .05f, "A is already at the telephone on T2 entry");
        var counter = Find<UnityEngine.Rendering.SortingGroup>("Depth - Counter");
        var owner = Find<NPC>("Owner");
        Check(Mathf.Abs(owner.transform.position.x - counter.transform.position.x) < .01f &&
            owner.GetComponent<SpriteRenderer>().sortingOrder < counter.sortingOrder, "Owner stands behind the center of the counter");
        player.TeleportToWorld(new Vector2(owner.transform.position.x, 1));
        player.SetFacingDirection(Vector2.up);
        Physics2D.SyncTransforms();
        typeof(PlayerMoveController3).GetMethod("TryInteract", Private).Invoke(player, null);
        Check(Dialog.IsDialogOpen, "Owner is reachable from the counter front through normal interaction");
        Dialog.SkipUntilChoice();
        yield return Wait(() => Selecting, "Owner asks for appraisal item");
        Select("tutorial-packed-jewelry-box");
        yield return Wait(() => Dialog.IsDialogOpen, "Appraisal dialogue");
        Dialog.SkipUntilChoice();
        Check(!Dialog.IsDialogOpen, "Ctrl skip follows the first price choice and closes appraisal");
        Check(Vector2.Distance(witnessA.position, phonePosition) < .05f, "Assistant appraisal does not start another A walk");
        yield return Wait(() => Scene == "Tutorial_T3_VaseBreak" && !SceneLoadManager.IsLoading, "Appraisal proceeds to blackout");
        float breakAt = -1;
        bool extendedBlackout = false;
        deadline = Time.realtimeSinceStartup + 35;
        while (Scene != "S3" || SceneLoadManager.IsLoading)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("First memory return: " + Scene);
            if (Scene == "Tutorial_T3_VaseBreak")
            {
                var blackout = UnityEngine.Object.FindFirstObjectByType<TutorialT3T4SceneController>();
                if (blackout != null)
                {
                    if (blackout.Phase == TutorialT3T4SceneController.T3Phase.VaseBroken && breakAt < 0) breakAt = Time.realtimeSinceStartup;
                    if (breakAt >= 0 && blackout.Phase == TutorialT3T4SceneController.T3Phase.Reaction)
                        extendedBlackout |= Time.realtimeSinceStartup - breakAt >= 2.2f;
                }
            }
            if (Dialog.IsDialogOpen) Dialog.NextDialog();
            yield return null;
        }
        Check(extendedBlackout, "Blackout remains held longer after the vase breaks");
        Check(true, "First memory returns through T4 without enabling investigation");
        if (repeatedVisits) yield return RepeatMemoryVisits();
        yield return RunEvidence(true, repeatedVisits);
        yield return RunEnding();
    }

    private static IEnumerator InspectWatchBox()
    {
        typeof(GameMenuController).GetMethod("Pause", Private).Invoke(Menu, null);
        ShowMenuPage("Items");
        typeof(GameMenuController).GetMethod("ChangeInventoryCategory", Private).Invoke(Menu, new object[] { 0 });
        Select("evidence-watch-box");
        yield return CloseDialog();
    }

    private static IEnumerator RunEnding()
    {
        yield return CloseDialog();
        float searchDeadline = Time.realtimeSinceStartup + 40;
        while (Scene != "Tutorial_T35_BlackoutSearch" || SceneLoadManager.IsLoading)
        {
            if (Time.realtimeSinceStartup > searchDeadline) throw new TimeoutException("Owner blackout search: " + Scene);
            if (Dialog.IsDialogOpen) Dialog.NextDialog();
            yield return null;
        }
        var search = UnityEngine.Object.FindFirstObjectByType<TutorialT35EventController>();
        var box = (Transform)new SerializedObject(search).FindProperty("droppedOpenBoxInteract").objectReferenceValue;
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        // Interaction rays originate half a unit below the player's transform.
        player.TeleportToWorld((Vector2)box.position + Vector2.down * .5f);
        player.SetFacingDirection(Vector2.up);
        Physics2D.SyncTransforms();
        typeof(PlayerMoveController3).GetMethod("TryInteract", Private).Invoke(player, null);
        yield return null; yield return CloseDialog();
        yield return Wait(() => Scene == "S#6" && !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "Owner confession");
        yield return CloseDialog();
        Check(ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/OwnerConfessed.asset")) == 1,
            "Ordinary dropped-box interaction reaches owner confession");
        Find<InvestigationPoint>("A").Interact(); yield return CloseDialog();
        Find<InvestigationPoint>("assist").Interact();
        float deadline = Time.realtimeSinceStartup + 40;
        while (Scene != "T2" || SceneLoadManager.IsLoading)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Late memory entry: " + Scene);
            if (Dialog.IsDialogOpen) Dialog.NextDialog();
            yield return null;
        }
        Check(GameMenuController.Inventory.Has("evidence-watch-box"), "Late memory retains collected watch box");
        yield return InspectWatchBox();
        Check(ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/WatchBoxSeenEmpty.asset")) == 1,
            "Inspecting pre-blackout watch box records empty state");
        Find<NPC>("Owner").Interact();
        yield return CloseDialog();
        yield return Wait(() => Selecting, "Late appraisal selection");
        Select("tutorial-packed-jewelry-box"); yield return CloseDialog();
        yield return Wait(() => Scene == "Tutorial_T3_VaseBreak" && !SceneLoadManager.IsLoading && !SceneLoadManager.IsCutscene, "Late blackout exploration");
        yield return InspectWatchBox();
        Check(ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/FinalBoxEvidence.asset")) == 1,
            "Natural before/after comparison grants final evidence");
        Find<InvestigationPoint>("Memory Exit").Interact(); yield return CloseDialog();
        yield return Wait(() => Scene == "S#6" && !SceneLoadManager.IsLoading, "Final reality return");
        Find<InvestigationPoint>("A").Interact(); yield return CloseDialog();
        yield return null;
        Check(ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/CaseSolved.asset")) == 1,
            "Natural ending solves case without injecting evidence");
    }

    private static IEnumerator RunEvidence(bool alreadyInReality = false, bool realityDialogConsumed = false)
    {
        // Enter a second memory without touching persistent saves.
        yield return Wait(() => !SceneLoadManager.IsLoading, "First blackout ready");
        if (!alreadyInReality)
        {
            yield return CloseDialog();
            yield return SceneLoadManager.Instance.LoadContent("S3");
        }
        yield return Wait(() => !SceneLoadManager.IsLoading, "Reality ready");
        if (!realityDialogConsumed)
        {
            yield return Wait(() => Dialog.IsDialogOpen, "S3 conversation");
            yield return CloseDialog();
        }
        yield return SceneLoadManager.Instance.LoadContent("T1");
        yield return Wait(() => !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "Replay entry");
        yield return CloseDialog();
        if (Scene == "T1") yield return SceneLoadManager.Instance.LoadContent("T2");
        yield return Wait(() => Scene == "T2" && !SceneLoadManager.IsLoading, "Replay T2");
        Check(SceneLoadManager.IsMemoryRevisit, "Second memory enables investigation");
        int priorAProgress = Find<EvidenceReconstruction>("A").CompletedEvidence.Length;
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
        Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == priorAProgress, "Rejected evidence does not increase progress");
        yield return Replay("A", "evidence-returned-letter", priorAProgress + 1);
        yield return Replay("A", "evidence-returned-letter", priorAProgress + 1);
        Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == priorAProgress + 1, "Repeated evidence counts once");
        Find<InvestigationPoint>("A").Interact();
        Check(((string[])typeof(GameMenuController).GetField("itemSelectionCompleted", Private).GetValue(Menu)).Length == priorAProgress + 1,
            "Evidence menu shows successful items including prior visits");
        yield return Capture("evidence-1-of-3");
        typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
        yield return Replay("A", "evidence-watch-box", Mathf.Min(3, priorAProgress + 2));
        if (priorAProgress == 0) yield return Replay("A", "evidence-telephone-trace", 3);
        else Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == 3,
            "Previously explained telephone route contributes to complete evidence set");
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
        Check(overlay != null && Mathf.Approximately(overlay.color.a, 0f), "T3 revisit restores normal brightness before investigation");
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        player.TeleportToWorld(new Vector2(5, 4));
        player.SetFacingDirection(Vector2.up);
        Physics2D.SyncTransforms();
        typeof(PlayerMoveController3).GetMethod("TryInteract", Private).Invoke(player, null);
        Check(Dialog.IsDialogOpen && !Selecting, "Dark T3 cabinet investigation works through normal interaction without B blocking it");
        yield return Capture("t3-dark-investigation");
        yield return CloseDialog();
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
