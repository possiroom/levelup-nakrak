using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Pick a route by name instead of copying its internal save-stable identifier.</summary>
[CustomPropertyDrawer(typeof(InvestigationPoint.Rule))]
public sealed class InvestigationRuleDrawer : PropertyDrawer
{
    static readonly string[] Fields = { "memo", "conditions", "requiredItems", "anyItems", "rewards", "actions", "dialogue", "showUnknownSpace", "presentEvidence", "evidencePrompt", "evidenceConditions", "wrongEvidenceDialog", "missingEvidenceDialog" };
    const float Gap = 2;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return height;
        foreach (string name in Fields) height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + Gap;
        return height + EditorGUIUtility.singleLineHeight + Gap;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var memo = property.FindPropertyRelative("memo");
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, string.IsNullOrEmpty(memo.stringValue) ? label.text : memo.stringValue, true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (string name in Fields)
            {
                var child = property.FindPropertyRelative(name);
                row.y += row.height + Gap; row.height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(row, child, true);
            }
            row.y += row.height + Gap; row.height = EditorGUIUtility.singleLineHeight;
            var flow = property.serializedObject.FindProperty("flow").objectReferenceValue as SceneFlowContainer;
            var point = property.serializedObject.targetObject as InvestigationPoint;
            var id = property.FindPropertyRelative("connectionId");
            if (flow != null && point != null)
            {
                var routes = flow.connections.Where(c => c != null && c.trigger == SceneConnectionTrigger.Manual && c.from.Path == point.gameObject.scene.path).ToArray();
                var labels = new[] { "대화만 진행 (씬 이동 없음)" }.Concat(routes.Select(c => c.memo + " → " + c.to.Name)).ToArray();
                int current = System.Array.FindIndex(routes, c => c.id == id.stringValue) + 1;
                EditorGUI.BeginChangeCheck();
                int selected = EditorGUI.Popup(row, "이동할 씬 연결", current, labels);
                if (EditorGUI.EndChangeCheck()) id.stringValue = selected == 0 ? "" : routes[selected - 1].id;
            }
            else EditorGUI.LabelField(row, "이동할 씬 연결", "먼저 Scene Flow를 지정하세요.");
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
