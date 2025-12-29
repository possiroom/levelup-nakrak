using UnityEngine;
using UnityEditor;

public class DialogStoryRightPanel
{
    private DialogStoryView parentView;

    // UI State
    private Vector2 scrollPos;

    // Data Caching
    private SerializedObject cachedContainerSO;
    private SerializedObject cachedStorySO;

    public DialogStoryRightPanel(DialogStoryView view)
    {
        parentView = view;
    }

    public void Draw()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (parentView.selectedStory != null)
        {
            DrawStoryEditor(parentView.selectedStory);
        }
        else if (parentView.selectedContainer != null)
        {
            DrawContainerEditor(parentView.selectedContainer);
        }
        else
        {
            GUILayout.Label("Select a Container or Story.", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    // 대사 인스펙터 창
    // 만약 StoryData의 속성이 바뀌면 여기서 수정
    void DrawStoryEditor(StoryData data)
    {
        if (cachedStorySO == null || cachedStorySO.targetObject != data)
        {
            cachedStorySO = new SerializedObject(data);
        }
        cachedStorySO.Update();

        EditorGUILayout.LabelField($"Story Data: {data.name}", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cachedStorySO.FindProperty("charName"));
        EditorGUILayout.PropertyField(cachedStorySO.FindProperty("activatedImage"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(cachedStorySO.FindProperty("text"), GUILayout.Height(100));

        EditorGUILayout.Space();

        // isEnd - isSelect
        EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
        SerializedProperty isEndProp = cachedStorySO.FindProperty("isEnd");
        SerializedProperty isSelectProp = cachedStorySO.FindProperty("isSelect");
        
        EditorGUILayout.PropertyField(isEndProp);
        EditorGUILayout.PropertyField(isSelectProp);

        if (isSelectProp.boolValue == true)
        {
            DrawSelectsList(cachedStorySO.FindProperty("selects"), data);
        }
        else
        {
            SerializedProperty nextStoryData = cachedStorySO.FindProperty("nextData");

            if (nextStoryData.objectReferenceValue == null)
            {
                if (!isEndProp.boolValue)
                    EditorGUILayout.HelpBox("Next Story is missing!", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("Story End", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Next story is linked.", MessageType.None);
            }
            EditorGUILayout.PropertyField(cachedStorySO.FindProperty("nextData"));
        }
        
        EditorGUILayout.Space();
        
        // Actions
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        SerializedProperty actionsProp = cachedStorySO.FindProperty("endActions");
        EditorGUILayout.PropertyField(actionsProp, true); 

        cachedStorySO.ApplyModifiedProperties();

        GUILayout.FlexibleSpace(); // 버튼을 바닥으로
        EditorGUILayout.Space(20);

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Delete This Story File", GUILayout.Height(30)))
        {
            DialogEditorUtils.DeleteStory(data);
            parentView.SelectStory(null);
            parentView.RequestRefresh();
            GUIUtility.ExitGUI();
        }
        GUI.backgroundColor = Color.white;
    }

    void DrawSelectsList(SerializedProperty listProp, StoryData parent)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Select Config", EditorStyles.boldLabel);
        GUILayout.BeginVertical("helpBox");

        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(i);
            SerializedProperty text = element.FindPropertyRelative("text");
            SerializedProperty nextData = element.FindPropertyRelative("nextData");

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Btn:", GUILayout.Width(30));
            text.stringValue = EditorGUILayout.TextField(text.stringValue, GUILayout.Width(100));
            nextData.objectReferenceValue = EditorGUILayout.ObjectField(nextData.objectReferenceValue, typeof(StoryData), false);

            if (nextData.objectReferenceValue == null)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    DialogEditorUtils.CreateStoryForSelect(nextData, parent, text.stringValue);
                    GUIUtility.ExitGUI(); 
                }
                GUI.backgroundColor = Color.white;
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            // x 버튼은 연결 해제만 함.
            // 만약 연결되어있는 대사를 삭제하고 싶으면 수동 삭제
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
            {
                listProp.DeleteArrayElementAtIndex(i);
                listProp.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI(); 
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("+ Add Select Option"))
        {
            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            SerializedProperty newItem = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
            newItem.FindPropertyRelative("text").stringValue = ""; 
            newItem.FindPropertyRelative("nextData").objectReferenceValue = null;
        }
        GUILayout.EndVertical();
    }

    // 컨테이너 인스펙터 창
    void DrawContainerEditor(DialogContainer container)
    {
        if (cachedContainerSO == null || cachedContainerSO.targetObject != container)
        {
            cachedContainerSO = new SerializedObject(container);
        }
        
        cachedContainerSO.Update();

        EditorGUILayout.LabelField($"Container: {container.name}", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Edit Dialog Container", MessageType.Info);
        EditorGUILayout.Space();

        SerializedProperty slotsProp = cachedContainerSO.FindProperty("dialogSlots");
        EditorGUILayout.PropertyField(slotsProp, true);

        cachedContainerSO.ApplyModifiedProperties();

        GUILayout.FlexibleSpace(); // 버튼을 바닥으로
        EditorGUILayout.Space(20);

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Delete This Container File", GUILayout.Height(30)))
        {
            if (DialogEditorUtils.DeleteContainer(container))
            {
                parentView.RequestRefresh();
                if (parentView.selectedContainer == container) parentView.selectedContainer = null;
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
