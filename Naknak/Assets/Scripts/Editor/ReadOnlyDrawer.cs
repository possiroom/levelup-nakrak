using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. GUI 상태를 비활성화(회색)로 변경
        GUI.enabled = false;
        
        // 2. 원래대로 변수 그리기
        EditorGUI.PropertyField(position, property, label, true);
        
        // 3. 다시 활성화 (다른 변수들은 정상적으로 그려져야 하므로)
        GUI.enabled = true;
    }
}