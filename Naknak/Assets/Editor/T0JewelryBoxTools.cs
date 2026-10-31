using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class T0JewelryBoxTools
{
    private const string ScenePath = "Assets/Scenes/T0.unity";
    private const string SpritePath = "Assets/Resources/Sample Sprites/Pixel Art Top Down - Basic v1.2.2/TX Props.png";
    private const string RequestPath = "Build/T0BoxRepair.request";
    private const string ResultPath = "Build/T0BoxRepair.json";

    [Serializable] private sealed class Result
    {
        public bool success;
        public bool wasLoaded;
        public bool wasDirty;
        public bool saved;
        public string message;
    }

    static T0JewelryBoxTools() { EditorApplication.delayCall += RunRequested; }

    private static void RunRequested()
    {
        if (!File.Exists(RequestPath)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += RunRequested;
            return;
        }
        File.Delete(RequestPath);
        RestoreBox();
    }

    [MenuItem("Tools/Naknak/T0/Restore Jewelry Box")]
    public static void RestoreBox()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var result = new Result();
        var originalActive = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        result.wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!result.wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        result.wasDirty = scene.isDirty;
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore T0 jewelry box");
        try
        {
            var room = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TutorialRoom0>(true)).FirstOrDefault();
            if (room == null) throw new InvalidOperationException("T0 Room Progress component was not found.");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().ToArray();
            var boxSprite = sprites.First(sprite => sprite.name == "TX Props_1");
            var sparkleSprite = sprites.First(sprite => sprite.name == "TX Props_4");
            var box = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "T0 Jewelry Box");
            if (box == null)
            {
                box = new GameObject("T0 Jewelry Box");
                SceneManager.MoveGameObjectToScene(box, scene);
                Undo.RegisterCreatedObjectUndo(box, "Restore jewelry box");
                box.transform.position = new Vector3(2, 0, 0);
            }
            Undo.RecordObject(box, "Configure jewelry box");
            box.SetActive(true);
            box.tag = "Interactable";
            box.layer = LayerMask.NameToLayer("Col 1F");
            Undo.RecordObject(box.transform, "Set jewelry box size");
            box.transform.localScale = Vector3.one;
            var point = box.GetComponent<TutorialRoom0Point>();
            if (point == null) point = Undo.AddComponent<TutorialRoom0Point>(box);
            var pointData = new SerializedObject(point);
            pointData.FindProperty("room").objectReferenceValue = room;
            pointData.FindProperty("action").enumValueIndex = 0;
            pointData.ApplyModifiedProperties();
            var collider = box.GetComponent<BoxCollider2D>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider2D>(box);
            Undo.RecordObject(collider, "Set jewelry box interaction area");
            collider.enabled = true;
            collider.isTrigger = false;
            collider.offset = new Vector2(0, -0.5f);
            collider.size = new Vector2(0.8f, 0.7f);
            var oldRenderer = box.GetComponent<SpriteRenderer>();
            if (oldRenderer != null) { Undo.RecordObject(oldRenderer, "Replace UI placeholder"); oldRenderer.enabled = false; }
            var body = Visual(box.transform, "Box Visual", boxSprite, new Vector3(-0.425f, -0.5f, 0), 0.8f, 10);
            var sparkle = Visual(box.transform, "Sparkle", sparkleSprite, new Vector3(0, 0.45f, 0), 1.2f, 11);
            var roomData = new SerializedObject(room);
            roomData.FindProperty("sparkle").objectReferenceValue = sparkle;
            roomData.FindProperty("jewelryBox").objectReferenceValue = box;
            roomData.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            // Preserve any unrelated unsaved scene edits; the restore is undoable in that case.
            if (!result.wasDirty) result.saved = EditorSceneManager.SaveScene(scene);
            result.success = box.activeSelf && body.sprite != null && sparkle.sprite != null && pointData.FindProperty("room").objectReferenceValue == room;
            result.message = result.saved ? "Restored and saved T0 jewelry box using existing chest and sparkle sprites." : "Restored in the open T0 scene; existing unsaved scene changes were preserved.";
            Selection.activeGameObject = box;
            Undo.CollapseUndoOperations(undoGroup);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            result.message = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            if (!result.wasLoaded && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
            Directory.CreateDirectory("Build");
            File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true));
        }
    }

    private static SpriteRenderer Visual(Transform parent, string name, Sprite sprite, Vector3 position, float scale, int order)
    {
        var target = parent.Find(name);
        if (target == null)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Restore box visual");
            Undo.SetTransformParent(go.transform, parent, "Place box visual");
            target = go.transform;
        }
        Undo.RecordObject(target, "Size box visual");
        target.localPosition = position;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one * scale;
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(target.gameObject);
        Undo.RecordObject(renderer, "Assign existing sprite");
        renderer.sprite = sprite;
        renderer.enabled = true;
        renderer.color = Color.white;
        renderer.sortingLayerName = "Default";
        var oldRenderer = parent.GetComponent<SpriteRenderer>();
        if (oldRenderer != null) renderer.sortingLayerID = oldRenderer.sortingLayerID;
        renderer.sortingOrder = order;
        return renderer;
    }
}
