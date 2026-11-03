using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Naknak.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class TutorialRevisionPlaytest
{
    public static string NativePhase { get; private set; }
    public static int NativeKeys { get; private set; }
    public static int NativeTiles { get; private set; }
    private const string NativeDirectory = "Captures/NativeWalk-20261102";
    private static readonly List<string> NativeLog = new List<string>();

    public static void BeginNativeWalk()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter idle Play mode.");
        Running = true; Result = "Running"; NativeKeys = NativeTiles = 0; NativeLog.Clear();
        System.IO.Directory.CreateDirectory(NativeDirectory);
        Menu.enabled = true; Application.runInBackground = true;
        EditorApplication.update += NativeSuppressAutosave;
        SceneLoadManager.Instance.StartCoroutine(NativeGuard(NativeStory()));
    }

    private static void NativeSuppressAutosave()
    {
        if (!EditorApplication.isPlaying || !Running) { EditorApplication.update -= NativeSuppressAutosave; return; }
        if (Menu == null) return;
        typeof(GameMenuController).GetField("autosaveTimer", Private).SetValue(Menu, -1000000f);
        string location = SceneManager.GetActiveScene().path + ":" +
            (MansionRoomSystem.Instance != null ? MansionRoomSystem.Instance.GetCurrentRoomId() : 0);
        typeof(GameMenuController).GetField("lastCheckpointLocation", Private).SetValue(Menu, location);
    }

    private static IEnumerator NativeGuard(IEnumerator root)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(root);
        try
        {
            while (stack.Count > 0)
            {
                bool more; object next = null;
                try { more = stack.Peek().MoveNext(); if (more) next = stack.Peek().Current; }
                catch (Exception error) { Result = "FAIL at " + NativePhase + "\n" + error; break; }
                if (!more) { stack.Pop(); continue; }
                if (next is IEnumerator nested) stack.Push(nested);
                else yield return next;
            }
            if (Result == "Running") Result = "PASS";
        }
        finally
        {
            while (stack.Count > 0) if (stack.Pop() is IDisposable cleanup) cleanup.Dispose();
            Running = false; EditorApplication.update -= NativeSuppressAutosave;
            if (Menu != null) Menu.enabled = false;
            System.IO.File.WriteAllText(NativeDirectory + "/result.txt", Result + "\nKeys=" + NativeKeys + "\nWalked tiles=" + NativeTiles + "\n" + string.Join("\n", NativeLog));
        }
    }

    private static IEnumerator NativeKey(byte key)
    {
        TutorialKeyboardInput.Tap(key, .06); NativeKeys++;
        yield return new WaitForSecondsRealtime(.16f);
    }

    private static byte NativeArrow(Vector2 direction) => Mathf.Abs(direction.x) > .5f ?
        (byte)(direction.x > 0 ? 0x27 : 0x25) : (byte)(direction.y > 0 ? 0x26 : 0x28);

    private static IEnumerator NativeUntil(Func<bool> ready, string label, float timeout = 120)
    {
        NativePhase = label;
        float end = Time.realtimeSinceStartup + timeout;
        float stable = -1;
        while (true)
        {
            if (Time.realtimeSinceStartup > end) throw new TimeoutException(label + " | " + Scene + " | " + Dialog?.CurrentStory?.name);
            if (ready())
            {
                if (stable < 0) stable = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - stable >= .5f) yield break;
                yield return null;
            }
            else
            {
                stable = -1;
                if (Dialog != null && Dialog.IsDialogOpen && !Selecting) yield return NativeKey(0x5A);
                else yield return new WaitForSecondsRealtime(.1f);
            }
        }
    }

    private static IEnumerator NativeCloseDialogue()
    {
        yield return NativeUntil(() => (Dialog == null || !Dialog.IsDialogOpen) &&
            !UnityEngine.Object.FindObjectsByType<EvidenceReconstruction>(FindObjectsSortMode.None).Any(r => r.IsReplaying), "Dialogue/replay closes by Z");
    }

    private static IEnumerator NativeInvestigate(string name)
    {
        yield return NativeUntil(() => !SceneLoadManager.IsLoading && !SceneLoadManager.IsCutscene &&
            !GameMenuController.IsBlockingGameplay && !Dialog.IsDialogOpen, "Ready to walk to " + name);
        yield return new WaitForSecondsRealtime(.3f);
        NativePhase = "Walk and investigate " + Scene + "/" + name;
        Physics2D.SyncTransforms();
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var target = SceneManager.GetSceneByPath(SceneLoadManager.CurrentScenePath).GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>()).First(t => t.name == name && t.GetComponent<IInteractable>() != null);
        var start = Vector2Int.RoundToInt(player.transform.position);
        var parents = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
        Vector2Int? approach = null; Vector2 facing = Vector2.up;
        while (queue.Count > 0 && parents.Count < 500 && approach == null)
        {
            var cell = queue.Dequeue();
            foreach (var direction in WalkDirections)
            {
                var hit = Physics2D.RaycastAll((Vector2)cell + Vector2.down * .5f, direction, 1, LayerMask.GetMask("Col 1F"))
                    .FirstOrDefault(h => h.collider != null && h.collider.CompareTag("Interactable"));
                if (hit.collider != null && hit.collider.transform == target) { approach = cell; facing = direction; break; }
                var next = cell + Vector2Int.RoundToInt(direction);
                if (next.x < -9 || next.x > 10 || next.y < -9 || next.y > 10 || parents.ContainsKey(next)) continue;
                // Read-only path planning. Movement itself uses physical arrow keys.
                if ((bool)typeof(PlayerMoveController3).GetMethod("checkCollider", Private).Invoke(player,
                    new object[] { new Vector3(cell.x, cell.y, player.transform.position.z), (Vector3)direction })) continue;
                parents[next] = cell; queue.Enqueue(next);
            }
        }
        if (!approach.HasValue) throw new InvalidOperationException("No walking approach for " + name);
        var route = new List<Vector2Int>(); var cursor = approach.Value;
        while (parents[cursor] != cursor) { route.Add(cursor); cursor = parents[cursor]; }
        route.Reverse();
        foreach (var cell in route)
        {
            NativePhase = "Arrow walk " + name + " -> " + cell;
            yield return NativeKey(NativeArrow((Vector2)cell - (Vector2)player.transform.position));
            float end = Time.realtimeSinceStartup + 3;
            while (player.isMoving && Time.realtimeSinceStartup < end) yield return null;
            if (Vector2.Distance(player.transform.position, cell) > .08f)
                throw new InvalidOperationException("Arrow movement expected " + cell + " actual " + player.transform.position);
            NativeTiles++;
        }
        if (player.FacingDirection != facing)
        {
            yield return NativeKey(NativeArrow(facing));
            float end = Time.realtimeSinceStartup + 3;
            while (player.isMoving && Time.realtimeSinceStartup < end) yield return null;
        }
        NativePhase = "Z investigate " + Scene + "/" + name;
        var before = player.transform.position;
        string scene = Scene;
        yield return NativeKey(0x5A);
        if (!Selecting && !Dialog.IsDialogOpen && !SceneLoadManager.IsLoading && Scene == scene)
            throw new InvalidOperationException("No response to Z at " + name + " from " + before);
        NativeLog.Add("Z " + scene + "/" + name + " from " + before + " facing " + player.FacingDirection);
        ScreenCapture.CaptureScreenshot(NativeDirectory + "/" + scene + "-" + name.Replace(" ", "_") + ".png");
        yield return null;
    }

    private static IEnumerator NativeSelectItem(string id)
    {
        yield return NativeUntil(() => Selecting, "Evidence menu opens for " + id, 15);
        var items = (List<InventoryItem>)typeof(GameMenuController).GetField("visibleItems", Private).GetValue(Menu);
        int target = items.FindIndex(i => i.Id == id);
        if (target < 0) throw new InvalidOperationException("Item not acquired: " + id);
        for (int attempt = 0; attempt < 30; attempt++)
        {
            int current = (int)typeof(GameMenuController).GetField("selected", Private).GetValue(Menu);
            if (current == target) { yield return NativeKey(0x5A); yield break; }
            if (current >= items.Count) { yield return NativeKey(0x27); continue; }
            if (current % 4 != target % 4) yield return NativeKey((byte)(current % 4 < target % 4 ? 0x27 : 0x25));
            else yield return NativeKey((byte)(current / 4 < target / 4 ? 0x28 : 0x26));
        }
        throw new InvalidOperationException("Keyboard item navigation stuck: " + id);
    }

    private static IEnumerator NativePresent(string actor, string item)
    {
        yield return NativeInvestigate(actor);
        yield return NativeSelectItem(item);
        yield return NativeCloseDialogue();
        NativeLog.Add("Evidence " + actor + " <- " + item);
    }

    private static IEnumerator NativeStory()
    {
        NativePhase = "New game with Z";
        yield return NativeKey(0x5A);
        yield return NativeUntil(() => Scene == "S2" && !SceneLoadManager.IsLoading && !Dialog.IsDialogOpen, "Prologue and entrance via Z");
        yield return NativeInvestigate("assist");
        yield return NativeUntil(() => Scene == "T0" && !SceneLoadManager.IsLoading && !Dialog.IsDialogOpen, "Enter first memory via dialogue");
        yield return NativeInvestigate("T0 Broach"); yield return NativeCloseDialogue();
        if (!GameMenuController.Inventory.Has("tutorial-brooch")) throw new InvalidOperationException("Native brooch pickup failed");
        yield return NativeKey(0x1B); yield return NativeKey(0x58);
        yield return NativeInvestigate("T0 Jewelry Box"); yield return NativeSelectItem("tutorial-brooch"); yield return NativeCloseDialogue();
        yield return NativeInvestigate("T0 Room Exit");
        yield return NativeUntil(() => Scene == "T2" && !SceneLoadManager.IsLoading && !Dialog.IsDialogOpen, "T1 conversation and walk into T2");
        yield return NativeInvestigate("Owner"); yield return NativeSelectItem("tutorial-packed-jewelry-box");
        yield return NativeUntil(() => Scene == "S3" && !SceneLoadManager.IsLoading && !Dialog.IsDialogOpen, "First appraisal, blackout, missing brooch, reality");
        yield return NativeInvestigate("assist");
        yield return NativeUntil(() => Scene == "T2" && !SceneLoadManager.IsLoading && !Dialog.IsDialogOpen, "Return to investigative memory");
        foreach (string point in new[] { "Cabinet Number 2", "Trash Can", "phone", "Counter Watch", "Vase Toy", "Unremembered Back Door" })
        { yield return NativeInvestigate(point); yield return NativeCloseDialogue(); }
        foreach (string witness in new[] { "A", "B" })
        {
            yield return NativeInvestigate(witness);
            if (Selecting || !Dialog.IsDialogOpen || Dialog.CurrentStory.name != witness + "_FirstInvestigation")
                throw new InvalidOperationException(witness + " first investigation skipped its introduction");
            yield return NativeCloseDialogue();
            NativeLog.Add(witness + " first investigation shows acquisition and evidence instructions before selection");
        }
        yield return NativeInvestigate("A");
        if (!Selecting) throw new InvalidOperationException("A second interaction must open evidence selection");
        yield return NativeKey(0x58);
        foreach (string item in new[] { "evidence-returned-letter", "evidence-watch-box", "evidence-telephone-trace" })
            yield return NativePresent("A", item);
        foreach (string item in new[] { "evidence-expired-ticket", "evidence-clockwork-toy" })
            yield return NativePresent("B", item);
        yield return NativeInvestigate("Owner"); yield return NativeSelectItem("tutorial-packed-jewelry-box");
        yield return NativeUntil(() => Scene == "Tutorial_T3_VaseBreak" && !SceneLoadManager.IsLoading && !SceneLoadManager.IsCutscene && !Dialog.IsDialogOpen,
            "Appraisal reaches dark memory by dialogue");
        foreach (string point in new[] { "VaseBroken", "Trash Can", "phone", "Cabinet Number 2" })
        { yield return NativeInvestigate(point); yield return NativeCloseDialogue(); }
        yield return NativePresent("B", "evidence-flathead-driver");
        yield return NativeUntil(() => !SceneLoadManager.IsMemorySpace && !SceneLoadManager.IsLoading && !Dialog.IsDialogOpen,
            "All evidence finishes and returns to reality");
        NativeLog.Add("Six reconstructions completed using walking and Z/item keys only. Scene=" + Scene);
        NativePhase = "Complete";
    }
}
