using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class TutorialRevisionPlaytest
{
    private static readonly Vector2[] WalkDirections = { Vector2.left, Vector2.right, Vector2.down, Vector2.up };
    private static int walkedTiles;

    public static void BeginRepeated()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter Play mode first.");
        Passed.Clear(); walkingSamples = 0; walkedTiles = 0; Result = "Running"; Running = true;
        Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(RepeatedSessions()));
    }

    private static IEnumerator RepeatedSessions()
    {
        for (int session = 1; session <= 2; session++)
        {
            typeof(GameMenuController).GetMethod("ClearItemSelection", Private).Invoke(Menu, null);
            typeof(GameMenuController).GetField("completionSavePending", Private).SetValue(Menu, false);
            yield return (IEnumerator)typeof(GameMenuController).GetMethod("ChangeSession", Private)
                .Invoke(Menu, new object[] { "Title", null });
            yield return null;
            var buttons = (List<UnityEngine.UI.Button>)typeof(GameMenuController).GetField("buttons", Private).GetValue(Menu);
            Check(buttons.Count == 4, "Session " + session + " title shows four menu choices");
            buttons[2].onClick.Invoke(); yield return null; yield return null;
            Check(typeof(GameMenuController).GetField("page", Private).GetValue(Menu).ToString() == "Options",
                "Session " + session + " options opens by button click");
            typeof(GameMenuController).GetMethod("SaveOptions", Private).Invoke(Menu, null);
            yield return null; yield return null;
            buttons = (List<UnityEngine.UI.Button>)typeof(GameMenuController).GetField("buttons", Private).GetValue(Menu);
            buttons[0].onClick.Invoke();
            yield return Wait(() => !GameMenuController.IsBlockingGameplay && Scene == "Tutorial_Prologue", "Native new game", 120);
            Check(ConditionManager.Instance.Get(Resources.Load<SceneFlowContainer>("TutorialSceneFlow").memoryVisitCount) == 0,
                "Session " + session + " new game resets memory visit count");
            yield return Run(true);
            Check(ConditionManager.Instance.Get(Load<ConditionID>("Assets/Story/Tutorial/Case/CaseSolved.asset")) == 1,
                "Session " + session + " finishes after repeated memory exits and entries");
            System.IO.Directory.CreateDirectory("Captures/Repeated-20261102");
            System.IO.File.WriteAllText("Captures/Repeated-20261102/session-" + session + ".txt", string.Join("\n", Passed));
        }
        Check(walkedTiles > 0, "Interactions approached by ordinary movement (" + walkedTiles + " tiles)");
    }

    private static IEnumerator ChooseDialogue(int choice)
    {
        yield return Wait(() => Dialog.IsDialogOpen, "Dialogue choice opens");
        float deadline = Time.realtimeSinceStartup + 20;
        while (!(bool)typeof(DialogManager).GetField("isSelectActivate", Private).GetValue(Dialog))
        {
            if (Time.realtimeSinceStartup > deadline || !Dialog.IsDialogOpen) throw new TimeoutException("Expected choice: " + Scene);
            Dialog.NextDialog(); yield return null;
        }
        typeof(DialogManager).GetField("selectIndex", Private).SetValue(Dialog, choice);
        Dialog.NextDialog(); yield return null;
    }

    private static IEnumerator WalkAndInteract(string name)
    {
        // Respect the short input lock and deferred collider rebuild after loading.
        yield return new WaitForSecondsRealtime(.25f);
        // Scene staging can move NPC transforms immediately before this query.
        Physics2D.SyncTransforms();
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var target = SceneManager.GetSceneByPath(SceneLoadManager.CurrentScenePath).GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>()).First(t => t.name == name && t.GetComponent<IInteractable>() != null);
        var start = Vector2Int.RoundToInt(player.transform.position);
        var parents = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var pending = new Queue<Vector2Int>(); pending.Enqueue(start);
        Vector2Int? approach = null; Vector2 facing = Vector2.up;
        while (pending.Count > 0 && parents.Count < 500 && approach == null)
        {
            var cell = pending.Dequeue();
            foreach (var direction in WalkDirections)
            {
                var hits = Physics2D.RaycastAll((Vector2)cell + Vector2.down * .5f, direction, 1, LayerMask.GetMask("Col 1F"));
                var hit = hits.FirstOrDefault(h => h.collider != null && h.collider.CompareTag("Interactable"));
                if (hit.collider != null && hit.collider.transform == target) { approach = cell; facing = direction; break; }
                var next = cell + Vector2Int.RoundToInt(direction);
                if (next.x < -9 || next.x > 10 || next.y < -9 || next.y > 10 || parents.ContainsKey(next)) continue;
                if ((bool)typeof(PlayerMoveController3).GetMethod("checkCollider", Private).Invoke(player,
                    new object[] { new Vector3(cell.x, cell.y, player.transform.position.z), (Vector3)direction })) continue;
                parents[next] = cell; pending.Enqueue(next);
            }
        }
        Check(approach.HasValue, Scene + " " + name + " reachable from player without teleport");
        var route = new List<Vector2Int>(); var step = approach.Value;
        while (parents[step] != step) { route.Add(step); step = parents[step]; }
        route.Reverse();
        foreach (var cell in route)
        {
            Vector2 direction = (Vector2)cell - (Vector2)player.transform.position;
            bool horizontal = Mathf.Abs(direction.x) > .5f;
            typeof(PlayerMoveController3).GetField(horizontal ? "_queuedDirectionX" : "_queuedDirectionY", Private)
                .SetValue(player, horizontal ? new Vector2(Mathf.Sign(direction.x), 0) : new Vector2(0, Mathf.Sign(direction.y)));
            typeof(PlayerMoveController3).GetMethod(horizontal ? "StartMoveX" : "StartMoveY", Private).Invoke(player, null);
            yield return null;
            yield return Wait(() => !player.isMoving, "Walk to " + name, 3);
            Check(Vector2.Distance(player.transform.position, cell) < .05f, "Walk step toward " + name + " at " + cell);
            walkedTiles++;
        }
        player.SetFacingDirection(facing); Physics2D.SyncTransforms();
        typeof(PlayerMoveController3).GetMethod("TryInteract", Private).Invoke(player, null);
        yield return null;
    }

    private static IEnumerator RepeatMemoryVisits()
    {
        yield return Wait(() => !SceneLoadManager.IsLoading && Dialog.IsDialogOpen, "Reality conversation ready");
        yield return CloseDialog();
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        int previousVisits = ConditionManager.Instance.Get(flow.memoryVisitCount);
        for (int visit = 1; visit <= 4; visit++)
        {
            yield return WalkAndInteract("assist");
            yield return CloseDialog();
            yield return Wait(() => SceneLoadManager.IsMemoryRevisit && !SceneLoadManager.IsLoading, "Memory revisit " + visit);
            yield return null; yield return null;
            yield return CloseDialog();
            yield return Wait(() => !Dialog.IsDialogOpen && !SceneLoadManager.IsLoading && !SceneLoadManager.IsCutscene,
                "Memory exploration ready " + visit);
            Check(ConditionManager.Instance.Get(flow.memoryVisitCount) == previousVisits + 1, "Revisit " + visit + " counts entry exactly once");
            previousVisits++;
            Check(!SceneLoadManager.IsFirstMemoryVisit, "Revisit " + visit + " does not replay first-visit restrictions");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
            Check(player.GetComponent<Animator>().runtimeAnimatorController == flow.detectivePlayerController,
                "Revisit " + visit + " uses detective instead of first-memory assistant");
            var camera = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            Check(camera.Follow == player.transform, "Revisit " + visit + " camera follows controlled actor");
            if (visit > 2) Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == 1,
                "Revisit " + visit + " preserves previously explained action");
            yield return WalkAndInteract("phone"); yield return CloseDialog();
            int phoneCount = GameMenuController.Inventory.Count("evidence-telephone-trace");
            yield return WalkAndInteract("phone"); yield return CloseDialog();
            Check(GameMenuController.Inventory.Count("evidence-telephone-trace") == phoneCount && phoneCount == 1,
                "Revisit " + visit + " repeated clue interaction does not duplicate evidence");
            yield return WalkAndInteract("A");
            Check(Selecting, "Revisit " + visit + " witness opens evidence menu");
            typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
            Check(!Selecting && Time.timeScale > 0, "Revisit " + visit + " cancel evidence restores movement");
            if (visit == 2)
            {
                yield return WalkAndInteract("A"); Select("evidence-telephone-trace"); yield return CloseDialog();
                yield return Wait(() => !Find<EvidenceReconstruction>("A").IsReplaying, "Partial replay finishes");
                Check(Find<EvidenceReconstruction>("A").CompletedEvidence.Length == 1, "Partial evidence replay before leaving memory");
            }
            if (visit == 3)
            {
                var checkpoint = (GameCheckpoint)typeof(GameMenuController).GetMethod("CaptureCheckpoint", Private).Invoke(Menu, new object[] { player });
                var inventory = GameMenuController.Inventory.Capture().Select(i => i.itemId + ":" + i.quantity).OrderBy(i => i).ToArray();
                yield return (IEnumerator)typeof(GameMenuController).GetMethod("ChangeSession", Private).Invoke(Menu, new object[] { checkpoint.scene, checkpoint });
                yield return Wait(() => !SceneLoadManager.IsLoading && !SceneLoadManager.IsRestoringSession, "Restore during repeated memory");
                Check(SceneLoadManager.IsMemoryRevisit && ConditionManager.Instance.Get(flow.memoryVisitCount) == previousVisits,
                    "Loading a memory save does not count another visit");
                Check(inventory.SequenceEqual(GameMenuController.Inventory.Capture().Select(i => i.itemId + ":" + i.quantity).OrderBy(i => i)),
                    "Loading memory preserves collected items without duplicates");
                yield return CloseDialog();
            }
            yield return WalkAndInteract("Memory Exit");
            yield return ChooseDialogue(2); yield return CloseDialog();
            Check(SceneLoadManager.IsMemorySpace && !SceneLoadManager.IsLoading, "Revisit " + visit + " cancel exit stays in memory");
            yield return WalkAndInteract("Memory Exit");
            yield return ChooseDialogue(1); yield return CloseDialog();
            yield return Wait(() => Scene == "S3" && !SceneLoadManager.IsLoading, "Return to reality " + visit);
            yield return null; yield return CloseDialog();
            Check(!SceneLoadManager.IsMemorySpace && !GameMenuController.Inventory.IsMemorySpace, "Revisit " + visit + " restores reality inventory mode");
            Check(GameMenuController.Inventory.Capture().All(i => !GameMenuController.Inventory.Find(i.itemId).MemoryOnly),
                "Revisit " + visit + " removes memory-only items on return");
            Check(!TutorialDeductionTrack.IsMoving, "Revisit " + visit + " leaves no stuck reconstruction");
            yield return Capture("repeat-return-" + visit);
        }
    }
}
