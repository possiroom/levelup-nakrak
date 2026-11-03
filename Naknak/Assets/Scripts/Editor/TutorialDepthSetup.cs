using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Separates furniture from floor art while preserving tiles, transforms and replay references.</summary>
public static class TutorialDepthSetup
{
    private sealed class Part
    {
        public Tilemap source;
        public Vector3Int cell;
        public string kind;
        public Vector3 origin;
        public Rect region;
    }

    private static readonly (string name, Rect rect)[] Shop =
    {
        ("Counter", new Rect(0, 128, 160, 64)),
        ("Counter props", new Rect(0, 192, 96, 64)),
        ("Display left", new Rect(0, 256, 64, 64)),
        ("Display right", new Rect(64, 256, 64, 64)),
        ("Clock cabinet", new Rect(128, 192, 64, 96)),
        ("White cabinet", new Rect(192, 192, 64, 128)),
        ("Bookcase", new Rect(256, 192, 64, 96)),
        ("Side table", new Rect(224, 96, 32, 64)),
        ("Pillar", new Rect(0, 0, 32, 128)),
        ("Jar", new Rect(32, 96, 32, 32)),
        ("Vase", new Rect(32, 64, 32, 32)),
        ("Broken vase", new Rect(64, 64, 32, 32)),
        ("Box", new Rect(32, 32, 32, 32)),
        ("Trash", new Rect(288, 128, 32, 32)),
        ("Small box", new Rect(256, 96, 32, 32)),
        ("Small box", new Rect(288, 96, 32, 32))
    };
    private static readonly (string name, Rect rect)[] Bedroom =
    {
        ("Bed", new Rect(0, 96, 64, 96)), ("Wardrobe", new Rect(64, 96, 64, 96)),
        ("Bedside table", new Rect(128, 128, 32, 64)), ("Desk", new Rect(160, 128, 32, 64)),
        ("Jewelry box", new Rect(96, 0, 32, 32)), ("Jewelry box", new Rect(128, 0, 32, 32)),
        ("Jewelry box", new Rect(160, 0, 32, 32))
    };

    [MenuItem("Tools/Naknak/Apply Tutorial Depth Sorting")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before editing scenes.");
        var active = SceneManager.GetActiveScene();
        var flow = AssetDatabase.LoadAssetAtPath<SceneFlowContainer>("Assets/Resources/TutorialSceneFlow.asset");
        try
        {
            foreach (var path in flow.scenes.Select(e => e.scene.Path).Distinct())
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { ApplyToScene(scene); EditorSceneManager.SaveScene(scene); }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
    }

    public static void ApplyToScene(Scene scene)
    {
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var parts = new List<Part>();
        foreach (var source in all.Select(t => t.GetComponent<Tilemap>()).Where(m => m != null && m.name.StartsWith("OBG")))
            foreach (var cell in source.cellBounds.allPositionsWithin)
            {
                var sprite = source.GetSprite(cell);
                if (sprite == null) continue;
                var regions = sprite.texture.name == "684939628bb875b2" ? Shop :
                    sprite.texture.name == "Furniture_Sheet" ? Bedroom : Array.Empty<(string name, Rect rect)>();
                foreach (var region in regions)
                {
                    if (!region.rect.Contains(sprite.rect.center)) continue;
                    var origin = source.GetCellCenterWorld(cell) -
                        new Vector3((sprite.rect.x + sprite.pivot.x) / sprite.pixelsPerUnit,
                            (sprite.rect.y + sprite.pivot.y) / sprite.pixelsPerUnit, 0);
                    parts.Add(new Part { source = source, cell = cell, kind = region.name, origin = origin, region = region.rect });
                    break;
                }
            }
        var moved = new Dictionary<(Tilemap, Vector3Int), Tilemap>();
        var groups = new List<Transform>();
        foreach (var set in parts.GroupBy(p => (p.kind, Vector3Int.RoundToInt(p.origin * 100), p.region)))
        {
            var first = set.First();
            var root = new GameObject("Depth - " + first.kind);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetParent(first.source.transform.parent, true);
            root.transform.position = first.origin + new Vector3(first.region.center.x / 32f, first.region.yMin / 32f, 0);
            root.AddComponent<SortingGroup>();
            var depth = root.AddComponent<WorldDepthSort>();
            var depthSO = new SerializedObject(depth);
            depthSO.FindProperty("useSpriteBottom").boolValue = false;
            depthSO.ApplyModifiedPropertiesWithoutUndo();
            groups.Add(root.transform);
            foreach (var sourceSet in set.GroupBy(p => p.source))
            {
                var source = sourceSet.Key;
                var obj = new GameObject("Tiles - " + source.name, typeof(Tilemap), typeof(TilemapRenderer));
                obj.transform.SetParent(root.transform, false);
                obj.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                obj.transform.localScale = source.transform.localScale;
                var target = obj.GetComponent<Tilemap>();
                target.tileAnchor = source.tileAnchor;
                target.color = source.color;
                target.orientation = source.orientation;
                target.orientationMatrix = source.orientationMatrix;
                var renderer = obj.GetComponent<TilemapRenderer>();
                var originalRenderer = source.GetComponent<TilemapRenderer>();
                renderer.sharedMaterials = originalRenderer.sharedMaterials;
                renderer.sortingLayerName = "Foreground";
                renderer.sortingOrder = originalRenderer.sortingOrder;
                foreach (var part in sourceSet)
                {
                    target.SetTile(part.cell, source.GetTile(part.cell));
                    target.SetTileFlags(part.cell, TileFlags.None);
                    target.SetTransformMatrix(part.cell, source.GetTransformMatrix(part.cell));
                    target.SetColor(part.cell, source.GetColor(part.cell));
                    target.SetTileFlags(part.cell, source.GetTileFlags(part.cell));
                    source.SetTile(part.cell, null);
                    moved[(source, part.cell)] = target;
                }
                target.CompressBounds();
            }
            depth.Refresh();
        }
        // Objects resting on the counter belong to its depth group.
        foreach (var props in groups.Where(g => g.name == "Depth - Counter props").ToArray())
        {
            var counter = groups.FirstOrDefault(g => g.name == "Depth - Counter" &&
                Mathf.Abs(g.position.x - props.position.x) < 3 && props.position.y >= g.position.y && props.position.y <= g.position.y + 2);
            if (counter == null) continue;
            foreach (var child in props.Cast<Transform>().ToArray()) child.SetParent(counter, true);
            UnityEngine.Object.DestroyImmediate(props.gameObject);
        }
        foreach (var replay in all.Select(t => t.GetComponent<EvidenceReconstruction>()).Where(c => c != null))
        {
            var so = new SerializedObject(replay);
            var clips = so.FindProperty("clips");
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i);
                var source = clip.FindPropertyRelative("hideTilemap").objectReferenceValue as Tilemap;
                if (source != null && moved.TryGetValue((source, clip.FindPropertyRelative("hideTileCell").vector3IntValue), out var target))
                    clip.FindPropertyRelative("hideTilemap").objectReferenceValue = target;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var sprite in all.Select(t => t.GetComponent<SpriteRenderer>()).Where(s => s != null && s.sprite != null))
        {
            if (sprite.GetComponent<NPC>() != null || sprite.GetComponent<EvidenceReconstruction>() != null || sprite.GetComponent<Animator>() != null ||
                sprite.sprite.texture.name == "684939628bb875b2" || sprite.name.Contains("vase"))
                WorldDepthSort.EnsureActor(sprite.transform);
            // The telephone has actual artwork; its interaction collider needs no placeholder circle.
            if (sprite.name == "phone" && sprite.sprite.name == "Circle") sprite.enabled = false;
        }
        foreach (var marker in all.Select(t => t.GetComponent<SpriteRenderer>()).Where(s => s != null && s.sprite != null &&
                     (s.name.Contains("Sparkle") || s.sprite.texture.name == "Warning")))
        {
            marker.sortingLayerName = "Foreground";
            marker.sortingOrder = 31000;
        }
    }
}
