using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Rebuilds scene route markers from the current evidence locations and collision tiles.</summary>
public static class TutorialRouteSetup
{
    [MenuItem("Tools/Naknak/Rebuild Tutorial Walking Routes")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding routes.");
        var active = SceneManager.GetActiveScene();
        try
        {
            foreach (var name in new[] { "T1", "T2", "Tutorial_T3_VaseBreak", "Tutorial_T4_BroochMissing" })
            {
                var path = "Assets/Scenes/" + name + ".unity";
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { Rebuild(scene); EditorSceneManager.SaveScene(scene); }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
    }

    public static void Rebuild(Scene scene)
    {
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform Find(string name) => all.FirstOrDefault(t => t != null && t.name == name);
        var a = Find("A");
        var b = Find("B");
        if (a == null || b == null) return;
        Vector2 phone = (Vector2)Find("phone").position + Vector2.right;
        Vector2 trash = (Vector2)Find("Trash Can").position + Vector2.up;
        Vector2 cabinet = (Vector2)Find("Cabinet Number 2").position + Vector2.down;
        Vector2 counter = (Vector2)Find("Counter Watch").position + Vector2.down;
        Vector2 vase = Find("Vase Toy") != null ? (Vector2)Find("Vase Toy").position : new Vector2(-5, -1);
        Vector2 toy = vase + new Vector2(1, -1);
        b.position = new Vector3(trash.x, trash.y, b.position.z);
        if (scene.name != "T1" && scene.name != "T2") a.position = new Vector3(phone.x, phone.y, a.position.z);
        foreach (var staging in all.Select(t => t.GetComponent<TutorialWitnessStaging>()).Where(s => s != null))
        {
            var so = new SerializedObject(staging);
            so.FindProperty("phonePosition").vector2Value = phone;
            so.FindProperty("trashPosition").vector2Value = trash;
            so.FindProperty("cabinetPosition").vector2Value = cabinet;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var actor in new[] { a, b })
        {
            Vector2 previousCounter = counter;
            Vector2 previousToy = toy;
            var reconstruction = actor.GetComponent<EvidenceReconstruction>();
            if (reconstruction == null) continue;
            var so = new SerializedObject(reconstruction);
            var clips = so.FindProperty("clips");
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i);
                Vector2 start = actor == a ? (i == 2 ? previousCounter : new Vector2(-3, 1)) :
                    (i == 2 ? previousToy : new Vector2(1, 2));
                Vector2 target = actor == a ? (i == 0 ? trash : i == 1 ? counter : phone) :
                    (i == 0 ? trash : i == 1 ? toy : cabinet);
                var startMarker = clip.FindPropertyRelative("startPoint").objectReferenceValue as Transform;
                if (startMarker != null) startMarker.position = new Vector3(start.x, start.y, 0);
                clip.FindPropertyRelative("startPosition").vector2Value = start;
                var beats = clip.FindPropertyRelative("beats");
                for (int j = 0; j < beats.arraySize; j++)
                {
                    var beat = beats.GetArrayElementAtIndex(j);
                    if (!TutorialWalkingRoute.TryBuild(actor, start, target, out var route, includeInactivePeople: true))
                        throw new InvalidOperationException(scene.name + ": no safe route for " + actor.name + " clip " + i);
                    var destination = beat.FindPropertyRelative("point").objectReferenceValue as Transform;
                    var end = route.Length > 0 ? (Vector2)route[route.Length - 1] : start;
                    if (destination != null) destination.position = new Vector3(end.x, end.y, 0);
                    beat.FindPropertyRelative("position").vector2Value = end;
                    var parent = destination != null ? destination.parent : actor.parent;
                    string prefix = destination != null ? destination.name.Replace(" Destination", "") : actor.name + "_" + i;
                    if (parent != null)
                        foreach (var old in parent.GetComponentsInChildren<Transform>(true)
                                     .Where(t => t.name.StartsWith(prefix + " Via ")).ToArray())
                            UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var waypoints = beat.FindPropertyRelative("waypoints");
                    waypoints.arraySize = Math.Max(0, route.Length - 1);
                    for (int k = 0; k < waypoints.arraySize; k++)
                    {
                        var marker = new GameObject(prefix + " Via " + k).transform;
                        SceneManager.MoveGameObjectToScene(marker.gameObject, scene);
                        marker.SetParent(parent, true);
                        marker.position = route[k];
                        waypoints.GetArrayElementAtIndex(k).objectReferenceValue = marker;
                    }
                    beat.FindPropertyRelative("facing").vector2Value = actor == a && i == 2 ? Vector2.left :
                        i == 0 ? Vector2.down : actor == b && i == 1 ? Vector2.left : Vector2.up;
                    start = end;
                }
                if (i == 1) { previousCounter = start; previousToy = start; }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
