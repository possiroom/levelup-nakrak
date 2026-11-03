using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static partial class TutorialRevisionPlaytest
{
    private const string AuditDirectory = "Captures/SceneAudit-20261102";
    public static string AuditPhase { get; private set; }

    public static string AlignFixedShopArt()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var source = EditorSceneManager.OpenScene("Assets/Scenes/T2.unity", OpenSceneMode.Additive);
        var reality = EditorSceneManager.OpenScene("Assets/Scenes/S3.unity", OpenSceneMode.Additive);
        var changes = new List<string>();
        try
        {
            foreach (var entry in flow.scenes.Where(e => e.scene.Name != "T0" && e.scene.Name != "Tutorial_Prologue"))
            {
                var scene = SceneManager.GetSceneByPath(entry.scene.Path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(entry.scene.Path, OpenSceneMode.Additive);
                try
                {
                    foreach (int id in new[] { 8, 20, 67, 70, 71 })
                    {
                        var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Resources/Scene art/684939628bb875b2_" + id + ".asset");
                        var reference = id == 20 ? reality : source;
                        var maps = reference.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(true));
                        foreach (var map in maps)
                        foreach (var cell in map.cellBounds.allPositionsWithin)
                        {
                            if (map.GetTile(cell) != tile) continue;
                            Vector3 world = map.GetCellCenterWorld(cell);
                            var targetMaps = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)).ToArray();
                            if (targetMaps.Any(m => m.GetTile(m.WorldToCell(world)) == tile)) continue;
                            var target = targetMaps.FirstOrDefault(m => m.name == map.name && m.transform.parent.name == map.transform.parent.name &&
                                Vector3.Distance(m.transform.parent.position, map.transform.parent.position) < .01f);
                            if (target == null)
                            {
                                var grid = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Grid>(true)).First();
                                var copy = UnityEngine.Object.Instantiate(map.transform.parent.gameObject, grid.transform);
                                copy.name = map.transform.parent.name;
                                copy.transform.position = map.transform.parent.position;
                                target = copy.GetComponentInChildren<Tilemap>();
                            }
                            else
                            {
                                if (id == 70)
                                    foreach (var otherCell in target.cellBounds.allPositionsWithin)
                                        if (target.GetTile(otherCell) == tile) target.SetTile(otherCell, null);
                                var destination = target.WorldToCell(world);
                                target.SetTile(destination, tile);
                                target.SetTileFlags(destination, TileFlags.None);
                                target.SetTransformMatrix(destination, map.GetTransformMatrix(cell));
                                target.SetColor(destination, map.GetColor(cell));
                                target.SetTileFlags(destination, map.GetTileFlags(cell));
                            }
                            changes.Add(scene.name + ": fixed tile " + id + " at " + world);
                        }
                    }
                    // T1 had the jar and unbroken vase vertically exchanged.
                    if (scene.name == "T1")
                    {
                        var vase = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Resources/Scene art/684939628bb875b2_75.asset");
                        var jar = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Resources/Scene art/684939628bb875b2_70.asset");
                        foreach (var map in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)))
                        {
                            foreach (var cell in map.cellBounds.allPositionsWithin)
                                if (map.GetTile(cell) == jar && Vector3.Distance(map.GetCellCenterWorld(cell), new Vector3(-5, -2.5f, 0)) > .01f)
                                { map.SetTile(cell, null); changes.Add("T1: removed old jar location"); }
                            var cells = new List<Vector3Int>();
                            foreach (var cell in map.cellBounds.allPositionsWithin) if (map.GetTile(cell) == vase) cells.Add(cell);
                            foreach (var cell in cells)
                            {
                                var target = map.WorldToCell(new Vector3(-5, -1.5f, 0));
                                if (cell == target) continue;
                                map.SetTile(cell, null); map.SetTile(target, vase);
                                changes.Add("T1: restored unbroken vase at (-5,-1.5)");
                            }
                        }
                        foreach (var group in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                                     .Where(t => t.name == "Depth - Jar" || t.name == "Depth - Vase"))
                        {
                            var children = group.Cast<Transform>().ToArray();
                            var positions = children.Select(t => t.position).ToArray();
                            group.position = new Vector3(-5, group.name == "Depth - Jar" ? -3 : -2, 0);
                            for (int i = 0; i < children.Length; i++) children[i].position = positions[i];
                            group.GetComponent<WorldDepthSort>()?.Refresh();
                        }
                    }
                    if (changes.Any(c => c.StartsWith(scene.name + ":"))) EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { EditorSceneManager.CloseScene(reality, true); EditorSceneManager.CloseScene(source, true); }
        System.IO.Directory.CreateDirectory(AuditDirectory);
        System.IO.File.WriteAllLines(AuditDirectory + "/layout-fixes.txt", changes);
        return string.Join("\n", changes);
    }

    public static string AuditAuthoredLocations()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before inspecting authored scenes.");
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var tiles = new Dictionary<string, HashSet<string>>();
        var points = new Dictionary<string, Dictionary<string, Vector3>>();
        var lines = new List<string>();
        foreach (var entry in flow.scenes)
        {
            Scene scene = SceneManager.GetSceneByPath(entry.scene.Path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(entry.scene.Path, OpenSceneMode.Additive);
            try
            {
                var nodes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                int missing = nodes.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                if (missing != 0) throw new InvalidOperationException(scene.name + " missing scripts: " + missing);
                var set = new HashSet<string>();
                foreach (var map in nodes.Select(t => t.GetComponent<Tilemap>()).Where(m => m != null))
                    foreach (var cell in map.cellBounds.allPositionsWithin)
                    {
                        var tile = map.GetTile(cell); if (tile == null) continue;
                        var world = map.GetCellCenterWorld(cell);
                        set.Add(AssetDatabase.GetAssetPath(tile) + "|" + world.ToString("F3") + "|" + map.GetTransformMatrix(cell).ToString("F2"));
                    }
                tiles[scene.name] = set;
                var fixedPoints = new Dictionary<string, Vector3>();
                foreach (var name in new[] { "phone", "Counter Watch", "Trash Can", "Cabinet Number 2", "Vase Toy", "Watch Box", "Memory Exit" })
                {
                    var node = nodes.FirstOrDefault(t => t.name == name);
                    if (node != null) fixedPoints[name] = node.position;
                }
                points[scene.name] = fixedPoints;
                lines.Add(scene.name + ": tiles=" + set.Count + ", missingScripts=" + missing + ", fixedPoints=" + string.Join(";", fixedPoints.Select(p => p.Key + "=" + p.Value)));
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        var baseline = tiles["T2"];
        foreach (var scene in tiles.Keys.Where(n => n != "T0" && n != "Tutorial_Prologue"))
        {
            var removed = baseline.Except(tiles[scene]).ToArray();
            var added = tiles[scene].Except(baseline).ToArray();
            lines.Add("DIFF T2 -> " + scene + ": removed=" + removed.Length + ", added=" + added.Length);
            lines.AddRange(removed.Select(s => "  REMOVED " + s));
            lines.AddRange(added.Select(s => "  ADDED " + s));
            foreach (var p in points[scene])
                if (points["T2"].TryGetValue(p.Key, out var original) && Vector3.Distance(original, p.Value) > .001f)
                    lines.Add("  MOVED " + p.Key + " " + original + " -> " + p.Value);
        }
        System.IO.Directory.CreateDirectory(AuditDirectory);
        System.IO.File.WriteAllLines(AuditDirectory + "/authored-locations.txt", lines);
        return "Audited " + tiles.Count + " authored scenes; see authored-locations.txt";
    }

    public static void BeginSceneAudit()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter idle Play mode first.");
        Passed.Clear(); walkingSamples = 0; Result = "Running"; Running = true; Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(SceneAuditSuite()));
    }

    public static void BeginT0NativeAudit()
    {
        if (!EditorApplication.isPlaying || Running) throw new InvalidOperationException("Enter idle Play mode first.");
        Passed.Clear(); Result = "Running"; Running = true; Menu.enabled = false;
        SceneLoadManager.Instance.StartCoroutine(Guarded(T0NativeAudit(), false));
    }

    private static IEnumerator T0NativeAudit()
    {
        AuditPhase = "T0 fresh native interactions";
        if (!SceneManager.GetSceneByName("PrototypeScene").isLoaded)
            yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Additive);
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        ConditionManager.Instance.ResetForNewGame(); GameMenuController.Inventory.Restore(null);
        yield return SceneLoadManager.Instance.LoadContent("T0");
        yield return null; yield return null; yield return DrainBranchDialogue();
        SurveyAuditApproaches("T0 fresh");
        yield return WalkAndInteract("T0 Broach"); yield return DrainBranchDialogue();
        Check(GameMenuController.Inventory.Has("tutorial-brooch"), "T0 normal walking and interaction collect brooch");
        yield return WalkAndInteract("T0 Jewelry Box");
        Check(Selecting, "T0 normal jewelry-box interaction opens item selection");
        typeof(GameMenuController).GetMethod("ReturnFromItems", Private).Invoke(Menu, null);
        yield return WalkAndInteract("T0 Jewelry Box"); Select("tutorial-brooch");
        yield return DrainBranchDialogue();
        Check(UnityEngine.Object.FindFirstObjectByType<TutorialRoom0>().IsPacked, "T0 normal item selection packs brooch");
        yield return AuditMenu("T0", "packed");
        yield return WalkAndInteract("T0 Room Exit");
        yield return Wait(() => Scene == "T1" && !SceneLoadManager.IsLoading, "T0 normal exit reaches T1");
        Check(Scene == "T1", "T0 native pickup, packing, menu and exit reach T1");
        AuditPhase = "Complete";
        System.IO.File.WriteAllText(AuditDirectory + "/t0-native-result.txt", "PASS\n" + string.Join("\n", Passed));
    }

    private static void SurveyAuditApproaches(string scene)
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        if (player == null) return;
        Physics2D.SyncTransforms();
        var targets = SceneManager.GetSceneByPath(SceneLoadManager.CurrentScenePath).GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>()).Where(c => c is IInteractable)
            .Select(c => c.transform).Distinct().ToArray();
        var pending = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var approaches = new Dictionary<Transform, string>();
        var start = Vector2Int.RoundToInt(player.transform.position);
        pending.Enqueue(start); visited.Add(start);
        while (pending.Count > 0 && visited.Count < 500)
        {
            var cell = pending.Dequeue();
            foreach (var direction in WalkDirections)
            {
                var hits = Physics2D.RaycastAll((Vector2)cell + Vector2.down * .5f, direction, 1, LayerMask.GetMask("Col 1F"));
                var hit = hits.FirstOrDefault(h => h.collider != null && h.collider.CompareTag("Interactable"));
                if (hit.collider != null)
                    foreach (var target in targets.Where(t => hit.collider.transform == t || hit.collider.transform.IsChildOf(t)))
                        approaches.TryAdd(target, cell + " facing " + direction);
                var next = cell + Vector2Int.RoundToInt(direction);
                if (next.x < -9 || next.x > 10 || next.y < -9 || next.y > 10 || visited.Contains(next)) continue;
                if ((bool)typeof(PlayerMoveController3).GetMethod("checkCollider", Private).Invoke(player,
                    new object[] { new Vector3(cell.x, cell.y, player.transform.position.z), (Vector3)direction })) continue;
                visited.Add(next); pending.Enqueue(next);
            }
        }
        System.IO.File.AppendAllLines(AuditDirectory + "/approaches.txt", targets.Select(t => scene + " | " + t.name +
            " | " + (approaches.TryGetValue(t, out var approach) ? approach : "NO RAY APPROACH") + " | " + t.position));
    }

    private static IEnumerator AuditMenu(string scene, string suffix)
    {
        AuditPhase = scene + " menus " + suffix;
        var actor = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
        var position = actor != null ? actor.transform.position : Vector3.zero;
        typeof(GameMenuController).GetMethod("Pause", Private).Invoke(Menu, null);
        foreach (string page in new[] { "Menu", "Items", "Options", "Save", "Load", "Menu" })
        {
            ShowMenuPage(page); yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            var panel = (RectTransform)typeof(GameMenuController).GetField("panel", Private).GetValue(Menu);
            var canvas = panel.GetComponentInParent<Canvas>();
            Check(canvas.isActiveAndEnabled && panel.gameObject.activeInHierarchy, scene + " " + suffix + " " + page + " menu renders");
            var labels = panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            Check(labels.Any(t => !string.IsNullOrEmpty(t.text) && t.color.a > .1f && t.GetComponent<CanvasRenderer>().GetAlpha() > .1f),
                scene + " " + suffix + " " + page + " text visible");
            foreach (var image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
            {
                if (image.color.a < .99f || image.color.maxColorComponent > .01f || image.canvas == null || image.canvas == canvas) continue;
                var rect = image.rectTransform.rect;
                bool fullScreen = rect.width * image.transform.lossyScale.x >= Screen.width * .95f && rect.height * image.transform.lossyScale.y >= Screen.height * .95f;
                Check(!fullScreen || image.canvas.sortingOrder < canvas.sortingOrder,
                    scene + " " + page + " no opaque black overlay covers menu (" + image.name + ")");
            }
        }
        ScreenCapture.CaptureScreenshot(AuditDirectory + "/" + scene + "-menu-" + suffix + ".png");
        yield return null; yield return null;
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        yield return null;
        Check(!GameMenuController.IsBlockingGameplay && Time.timeScale > 0, scene + " " + suffix + " menu closes and restores time");
        Check(actor == null || Vector3.Distance(actor.transform.position, position) < .01f, scene + " menu leaves player position unchanged");
    }

    private static IEnumerator SceneAuditSuite()
    {
        System.IO.Directory.CreateDirectory(AuditDirectory);
        System.IO.File.WriteAllText(AuditDirectory + "/approaches.txt", "Reachability survey uses normal player collision/raycast geometry; no world interactions are invoked.\n");
        AuditPhase = "Full story regression";
        yield return Run();
        typeof(GameMenuController).GetMethod("Resume", Private).Invoke(Menu, null);
        var objective = UnityEngine.Object.FindFirstObjectByType<TutorialObjectiveController>();
        if (objective != null) objective.enabled = false;
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var isolated = UnityEngine.Object.Instantiate(flow); isolated.connections.Clear();
        SceneLoadManager.Instance.SetContainer(isolated);
        try
        {
            foreach (var entry in isolated.scenes)
            {
                AuditPhase = "Scene load " + entry.scene.Name;
                yield return SceneLoadManager.Instance.LoadContent(entry.scene.Name);
                yield return null; yield return null;
                // Full-story playback above covers their normal execution. This
                // fixture freezes scene-only cutscenes while checking menu pages.
                foreach (var component in SceneManager.GetSceneByPath(entry.scene.Path).GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
                    if (component != null && component.GetType().Name.StartsWith("Tutorial") &&
                        !(component is TutorialScenePresentation) && !(component is TutorialWitnessStaging))
                    { component.StopAllCoroutines(); component.enabled = false; }
                SceneLoadManager.Instance.SetContainer(isolated);
                yield return DrainBranchDialogue();
                SurveyAuditApproaches(entry.scene.Name);
                // Entry cutscenes may intentionally lock player input. Opening pages
                // directly tests their rendering without changing the authored flow.
                yield return AuditMenu(entry.scene.Name, "entry");
            }
            ConditionManager.Instance.ResetForNewGame(); GameMenuController.Inventory.Restore(null);
            yield return SceneLoadManager.Instance.LoadContent("T0");
            yield return null; yield return null; yield return DrainBranchDialogue();
            var room = UnityEngine.Object.FindFirstObjectByType<TutorialRoom0>();
            room.CollectBrooch(); yield return DrainBranchDialogue();
            Check(GameMenuController.Inventory.Has("tutorial-brooch"), "T0 actual pickup grants brooch for UI verification");
            yield return AuditMenu("T0", "brooch");
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMoveController3>();
            var checkpoint = (GameCheckpoint)typeof(GameMenuController).GetMethod("CaptureCheckpoint", Private).Invoke(Menu, new object[] { player });
            yield return (IEnumerator)typeof(GameMenuController).GetMethod("ChangeSession", Private).Invoke(Menu, new object[] { checkpoint.scene, checkpoint });
            yield return Wait(() => !SceneLoadManager.IsLoading && !SceneLoadManager.IsRestoringSession, "T0 checkpoint reload ready");
            yield return null; yield return null; yield return DrainBranchDialogue();
            Check(Scene == "T0" && GameMenuController.Inventory.Has("tutorial-brooch"), "T0 checkpoint reload preserves room and brooch");
            yield return AuditMenu("T0", "restored");
            AuditPhase = "Complete";
            System.IO.File.WriteAllText(AuditDirectory + "/runtime-result.txt", "PASS\n" + string.Join("\n", Passed));
        }
        finally { SceneLoadManager.Instance.SetContainer(flow); UnityEngine.Object.Destroy(isolated); if (objective != null) objective.enabled = true; }
    }
}
