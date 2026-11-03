using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Repairs authored tutorial interaction targets without replacing the current room layout.</summary>
public static class TutorialQaFixSetup
{
    private const string Case = "Assets/Story/Tutorial/Case/";
    private const string T3 = "Tutorial_T3_VaseBreak";
    private const string T4 = "Tutorial_T4_BroochMissing";

    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    [MenuItem("Tools/Naknak/Apply Tutorial QA Fixes")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before applying scene fixes.");
        var active = SceneManager.GetActiveScene();
        var flow = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var firstExit = AssetDatabase.LoadAssetAtPath<StoryData>(Case + "ExitBlocked.asset");
        var watch = AssetDatabase.LoadAssetAtPath<StoryData>(Case + "WatchFound.asset");
        watch.text = "카운터 뒤에 오래된 회중시계가 놓여 있다. 판매 경위는 A에게 확인해 보자.";
        watch.activatedImage = -1;
        EditorUtility.SetDirty(watch);
        try
        {
            foreach (string path in flow.scenes.Select(entry => entry.scene.Path).Concat(new[] { "Assets/Scenes/PrototypeScene.unity" }).Distinct())
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var actor in All<Transform>(scene).Where(t => t.GetComponent<NPC>() != null || t.GetComponent<EvidenceReconstruction>() != null))
                    {
                        // Preserve this legacy object as an inactive marker, with no visual or interaction footprint.
                        var sprite = actor.GetComponent<SpriteRenderer>();
                        if (actor.name == "S#3" && sprite != null && sprite.sprite != null && sprite.sprite.name == "Circle")
                        {
                            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                            foreach (var collider in actor.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
                            foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                            actor.GetComponent<NPC>().enabled = false;
                            actor.tag = "Untagged";
                            actor.gameObject.SetActive(false);
                            continue;
                        }
                        var box = actor.GetComponent<BoxCollider2D>();
                        if (box == null || box.offset.y < -1) continue; // Owner's counter-front talk target.
                        box.offset = new Vector2(0, -.5f);
                        box.size = new Vector2(.7f, .7f);
                    }
                    foreach (var point in All<InvestigationPoint>(scene).Where(p => p.name == "Memory Exit"))
                    {
                        var so = new SerializedObject(point);
                        so.FindProperty("firstMemoryDialog").objectReferenceValue = firstExit;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    if (scene.name == T3) RepairFragments(scene);
                    if (scene.name == "PrototypeScene")
                        foreach (var camera in All<Camera>(scene))
                            if (camera.GetComponent<Unity.Cinemachine.CinemachineCamera>() != null) camera.enabled = false;
                    EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssetIfDirty(watch);
        }
        finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
    }

    private static void RepairFragments(Scene targetScene)
    {
        var source = SceneManager.GetSceneByName(T4);
        bool opened = !source.IsValid() || !source.isLoaded;
        if (opened) source = EditorSceneManager.OpenScene("Assets/Scenes/" + T4 + ".unity", OpenSceneMode.Additive);
        try
        {
            var reference = All<MemoryInvestigationPoint>(source).First(point => point.name == "VaseBroken");
            var target = All<Transform>(targetScene).First(t => t.name == "VaseBroken").gameObject;
            var point = target.GetComponent<MemoryInvestigationPoint>();
            if (point == null) point = target.AddComponent<MemoryInvestigationPoint>();
            EditorUtility.CopySerialized(reference, point);
            var so = new SerializedObject(point);
            var controller = All<TutorialT3T4SceneController>(targetScene).First();
            so.FindProperty("available").objectReferenceValue = new SerializedObject(controller).FindProperty("t3Occurred").objectReferenceValue;
            so.FindProperty("sparkle").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            var box = target.GetComponent<BoxCollider2D>();
            if (box == null) box = target.AddComponent<BoxCollider2D>();
            EditorUtility.CopySerialized(reference.GetComponent<BoxCollider2D>(), box);
            target.tag = "Interactable";
            target.layer = reference.gameObject.layer;
        }
        finally { if (opened) EditorSceneManager.CloseScene(source, true); }
    }
}
