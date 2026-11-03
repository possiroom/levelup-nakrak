using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneReference))]
public sealed class SceneReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var path = property.FindPropertyRelative("path");
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
        EditorGUI.BeginChangeCheck();
        scene = (SceneAsset)EditorGUI.ObjectField(position, label, scene, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck()) path.stringValue = scene == null ? "" : AssetDatabase.GetAssetPath(scene);
        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(SceneFlowContainer))]
public sealed class SceneFlowEditor : Editor
{
    [MenuItem("Naknak/Scene Connections")]
    public static void SelectFlow()
    {
        Selection.activeObject = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        if (Selection.activeObject != null) EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("씬 파일은 클릭 또는 드래그로 지정합니다.\nScenes: 진입 대사·플레이어 위치\nConnections: 출발 → 도착·전환 시점·조건\n대화 종료는 대화의 마지막 StoryData를 지정하세요. 위쪽 조건부터 검사합니다.", MessageType.Info);
        DrawDefaultInspector();
        var flow = (SceneFlowContainer)target;
        EditorGUILayout.Space();
        foreach (var row in flow.connections)
        {
            if (row == null) continue;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(row.from.Name, GUILayout.MinWidth(90))) Ping(row.from.Path);
            GUILayout.Label("→", GUILayout.Width(20));
            if (GUILayout.Button(row.to.Name, GUILayout.MinWidth(90))) Ping(row.to.Path);
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("연결된 씬을 Build Settings에 등록"))
        {
            var paths = flow.scenes.Select(s => s.scene.Path)
                .Concat(flow.connections.SelectMany(c => new[] { c.from.Path, c.to.Path }))
                .Where(p => !string.IsNullOrEmpty(p) && AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null);
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var path in paths.Distinct())
            {
                var scene = scenes.Find(s => s.path == path);
                if (scene == null) scenes.Add(new EditorBuildSettingsScene(path, true));
                else scene.enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        foreach (var row in flow.connections)
        {
            if (row == null) continue;
            if (string.IsNullOrEmpty(row.from.Path) || string.IsNullOrEmpty(row.to.Path))
                EditorGUILayout.HelpBox("출발/도착 씬이 비어 있는 연결이 있습니다.", MessageType.Warning);
            else if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == row.to.Path))
                EditorGUILayout.HelpBox(row.to.Name + ": Build Settings 등록이 필요합니다.", MessageType.Warning);
            if (row.trigger == SceneConnectionTrigger.DialogFinished && row.completedDialog == null)
                EditorGUILayout.HelpBox(row.memo + ": 마지막 대사를 지정하세요.", MessageType.Warning);
        }
    }

    private static void Ping(string path)
    { var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path); if (asset != null) EditorGUIUtility.PingObject(asset); }
}

[CustomEditor(typeof(SceneTransitionPoint))]
public sealed class SceneTransitionPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("container"));
        var flow = serializedObject.FindProperty("container").objectReferenceValue as SceneFlowContainer;
        if (flow != null)
        {
            var id = serializedObject.FindProperty("connectionId");
            var labels = new[] { "연결 선택…" }.Concat(flow.connections.Select(c => c.memo + " (" + c.from.Name + " → " + c.to.Name + ")")).ToArray();
            int selected = flow.connections.FindIndex(c => c.id == id.stringValue) + 1;
            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup("이동할 연결", selected, labels);
            if (EditorGUI.EndChangeCheck()) id.stringValue = selected == 0 ? "" : flow.connections[selected - 1].id;
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enterOnTrigger"), new GUIContent("접촉하면 이동"));
        EditorGUILayout.HelpBox("접촉 이동: Collider의 Is Trigger를 켜세요.\n상호작용 이동: Interactable 태그와 Col 1F 레이어를 지정하세요.\nUI 버튼 OnClick에서도 Interact()를 연결할 수 있습니다.", MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/Naknak/Scene Transition Point", false, 10)]
    private static void CreatePoint(MenuCommand command)
    {
        var point = new GameObject("Scene Transition Point");
        GameObjectUtility.SetParentAndAlign(point, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(point, "Create Scene Transition Point");
        point.AddComponent<SceneTransitionPoint>().container = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        var collider = point.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        point.tag = "Interactable";
        int layer = LayerMask.NameToLayer("Col 1F");
        if (layer >= 0) point.layer = layer;
        Selection.activeGameObject = point;
    }
}
