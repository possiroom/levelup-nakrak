using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DialogStoryView
{
    // 선택된 컨테이너/대사
    public DialogContainer selectedContainer;
    public StoryData selectedStory;
    
    private List<DialogContainer> allContainers = new List<DialogContainer>();
    
    // UI 상태 저장
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;
    private Dictionary<StoryData, bool> foldoutStatus = new Dictionary<StoryData, bool>();
    private List<StoryData> orphanStories = new List<StoryData>();
    private bool orphanStoriesFoldout = true;
    private string containerSearchText = "";

    // 데이터 캐싱
    private SerializedObject cachedContainerSO; 
    private SerializedObject cachedStorySO;

    public void OnEnable()
    {
        RefreshContainerList();
    }

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        // 왼쪽 패널
        GUILayout.BeginVertical("box", GUILayout.Width(320), GUILayout.ExpandHeight(true));
        DrawLeftPanel();
        GUILayout.EndVertical();

        // 오른쪽 패널 (인스펙터)
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawRightPanel();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    #region LeftPanel
    void DrawLeftPanel()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshContainerList();
        if (GUILayout.Button("New Container (+)", GUILayout.ExpandWidth(true)))
        {
            var newCon = DialogEditorUtils.CreateNewContainer();
            if (newCon != null) 
            { 
                RefreshContainerList(); 
                SelectContainer(newCon); 
            }
        }
        GUILayout.EndHorizontal();

        // 검색
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        containerSearchText = EditorGUILayout.TextField(containerSearchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (containerSearchText != "" && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(25)))
        {
            containerSearchText = "";
            GUI.FocusControl(null);
        }
        GUILayout.EndHorizontal();

        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        DrawOrphanStories();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Container Browser", EditorStyles.boldLabel);

        // 컨테이너 그리기
        foreach (DialogContainer container in allContainers)
        {
            if (container == null) continue;
            // 검색 기능
            if (!string.IsNullOrEmpty(containerSearchText) && !container.name.ToLower().Contains(containerSearchText.ToLower()))
                continue;

            GUILayout.BeginVertical("helpBox");

            if (GUILayout.Button($"📁 {container.name}", EditorStyles.boldLabel, GUILayout.Height(20)))
            {
                SelectContainer(container);
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;

            // 선택되었다면 내용 그리기
            if (selectedContainer == container)
            {
                DrawContainerContents(container);
            }
            GUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawContainerContents(DialogContainer container)
    {
        if (container.dialogSlots != null)
        {
            for (int i = 0; i < container.dialogSlots.Count; i++)
            {
                var slot = container.dialogSlots[i];
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                string slotLabel = $"[Slot {i}]";
                if (!string.IsNullOrEmpty(slot.memo)) slotLabel += $" {slot.memo}";
                GUILayout.Label(slotLabel, EditorStyles.miniLabel);
                GUILayout.EndHorizontal();

                DrawStoryRecursive(slot.headStoryData, 1, new HashSet<StoryData>());
            }
        }

        GUILayout.Space(5);
        if (selectedStory == null)
        {
            if (GUILayout.Button("+ Add New Slot", EditorStyles.miniButton))
            {
                var newData = DialogEditorUtils.CreateStoryInSlot(container);
                if (newData != null) SelectStory(newData);
            }
        }
        else
        {
            // 다음 스토리를 생성할 수 있는지 여부
            bool isConnected = (selectedStory.nextData != null) || selectedStory.isSelect;
            if (isConnected)
            {
                GUI.backgroundColor = Color.gray;
                GUILayout.Label("Cannot create next story.", EditorStyles.centeredGreyMiniLabel);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("⬇ Append Next Story", EditorStyles.miniButton))
                {
                    var newData = DialogEditorUtils.CreateStoryLinked(selectedStory);
                    if (newData != null) SelectStory(newData);
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }

    // 재귀 디렉토리
    void DrawStoryRecursive(StoryData story, int depth, HashSet<StoryData> visited)
    {
        if (story == null) return;

        // 무한 루프 감지
        if (visited.Contains(story))
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * 10);
            GUI.color = Color.red;
            GUILayout.Label($"↺ Loop Detect: {story.name}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            return;
        }

        HashSet<StoryData> newVisited = new HashSet<StoryData>(visited) { story };

        GUILayout.BeginHorizontal();
        GUILayout.Space(depth * 10);

        if (selectedStory == story) GUI.backgroundColor = Color.yellow;
        else GUI.backgroundColor = Color.white;

        GUIStyle leftButton = new GUIStyle(EditorStyles.miniButton);
        leftButton.alignment = TextAnchor.MiddleLeft;
        leftButton.padding.left = 10;
        leftButton.richText = true;

        // 대사 선택 버튼
        string icon = story.isSelect ? "?" : "↓"; 
        if (story.isEnd) icon = "X"; 
        
        string btnLabel = $"{icon}  {story.name}";
        if (!string.IsNullOrEmpty(story.text)) 
        {
            string preview = story.text.Length > 8 ? story.text.Substring(0, 8) + "..." : story.text;
            btnLabel += $"  <color=#B0B0B0>\"{preview}\"</color>"; 
        }

        if (GUILayout.Button(btnLabel, leftButton, GUILayout.Height(20)))
        {
            SelectStory(story);
            GUI.FocusControl(null);
        }
        GUI.backgroundColor = Color.white;

        // 선택지라면
        bool isSelectButton = story.isSelect && story.selects != null && story.selects.Count > 0;
        
        if (isSelectButton)
        {
            if (!foldoutStatus.ContainsKey(story)) foldoutStatus[story] = true; 
            foldoutStatus[story] = EditorGUILayout.Foldout(foldoutStatus[story], "", true);
        }
        else
        {
            GUILayout.Label("", GUILayout.Width(13));
        }

        GUILayout.EndHorizontal();

        if (isSelectButton && foldoutStatus[story])
        {
            foreach (var sel in story.selects)
            {
                if (sel.nextData != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space((depth + 1) * 10);
                    GUI.color = Color.cyan;
                    GUILayout.Label($"└ [{sel.text}]", EditorStyles.miniLabel, GUILayout.Height(16));
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();

                    // 깊은 단계로 재귀 호출
                    DrawStoryRecursive(sel.nextData, depth + 1, newVisited);
                }
            }
        }
        else if (!isSelectButton && story.nextData != null && !story.isEnd)
        {
            // 선택지가 아니므로 그대로 재귀 호출
            DrawStoryRecursive(story.nextData, depth, newVisited);
        }
    }

    void DrawOrphanStories()
    {
        if (orphanStories.Count > 0)
        {
            EditorGUILayout.Space();
            orphanStoriesFoldout = EditorGUILayout.Foldout(orphanStoriesFoldout, $"▶ Unlinked Stories ({orphanStories.Count})", true, EditorStyles.boldLabel);

            if (orphanStoriesFoldout)
            {
                foreach (var story in orphanStories)
                {
                    if (story == null) continue;

                    if (!string.IsNullOrEmpty(containerSearchText) && !story.name.ToLower().Contains(containerSearchText.ToLower()))
                        continue;

                    GUILayout.BeginHorizontal();

                    if (selectedStory == story) GUI.backgroundColor = Color.yellow;
                    else GUI.backgroundColor = Color.white;

                    GUIStyle leftButton = new GUIStyle(EditorStyles.miniButton);
                    leftButton.alignment = TextAnchor.MiddleLeft;
                    leftButton.padding.left = 10;
                    leftButton.richText = true;

                    string icon = story.isSelect ? "?" : "↓";
                    if (story.isEnd) icon = "X";

                    string btnLabel = $"{icon}  {story.name}";
                    if (!string.IsNullOrEmpty(story.text))
                    {
                        string preview = story.text.Length > 8 ? story.text.Substring(0, 8) + "..." : story.text;
                        btnLabel += $"  <color=#B0B0B0>\"{preview}\"</color>";
                    }

                    if (GUILayout.Button(btnLabel, leftButton, GUILayout.Height(20)))
                    {
                        SelectStory(story);
                        selectedContainer = null;
                        GUI.FocusControl(null);
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }
            }
        }
    }

    #endregion LeftPanel

    #region RightPanel
    void DrawRightPanel()
    {
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

        if (selectedStory != null)
        {
            DrawStoryEditor(selectedStory);
        }
        else if (selectedContainer != null)
        {
            DrawContainerEditor(selectedContainer);
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

        EditorGUILayout.LabelField($"Editing Story: {data.name}", EditorStyles.largeLabel);
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

        EditorGUILayout.Space(20);
        if (GUILayout.Button("Delete This Story File", GUILayout.Height(30)))
        {
            DialogEditorUtils.DeleteStory(data);
            SelectStory(null);
            RefreshContainerList();
        }
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
        EditorGUILayout.HelpBox("Manage Dialogue Slots", MessageType.Info);
        EditorGUILayout.Space();

        SerializedProperty slotsProp = cachedContainerSO.FindProperty("dialogSlots");
        EditorGUILayout.PropertyField(slotsProp, true);

        cachedContainerSO.ApplyModifiedProperties();
    }

    #endregion RightPanel

    // Refresh
    void RefreshContainerList()
    {
        allContainers.Clear();
        string[] guids = AssetDatabase.FindAssets("t:DialogContainer");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DialogContainer container = AssetDatabase.LoadAssetAtPath<DialogContainer>(path);
            if (container != null) allContainers.Add(container);
        }
        FindOrphanStories();
    }

    void FindOrphanStories()
    {
        orphanStories.Clear();

        string[] storyGuids = AssetDatabase.FindAssets("t:StoryData");
        HashSet<StoryData> allStories = new HashSet<StoryData>();
        foreach (string guid in storyGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StoryData story = AssetDatabase.LoadAssetAtPath<StoryData>(path);
            if (story != null)
            {
                allStories.Add(story);
            }
        }

        HashSet<StoryData> referencedStories = new HashSet<StoryData>();
        foreach (DialogContainer container in allContainers)
        {
            if (container.dialogSlots == null) continue;
            foreach (var slot in container.dialogSlots)
            {
                CollectReferencedStoriesRecursive(slot.headStoryData, referencedStories);
            }
        }

        allStories.ExceptWith(referencedStories);
        orphanStories = new List<StoryData>(allStories);
        orphanStories.Sort((a, b) => a.name.CompareTo(b.name));
    }

    void CollectReferencedStoriesRecursive(StoryData story, HashSet<StoryData> collectedStories)
    {
        if (story == null || collectedStories.Contains(story)) return;

        collectedStories.Add(story);

        if (story.isSelect)
        {
            if (story.selects != null)
            {
                foreach (var selection in story.selects)
                {
                    CollectReferencedStoriesRecursive(selection.nextData, collectedStories);
                }
            }
        }
        else if (!story.isEnd)
        {
            CollectReferencedStoriesRecursive(story.nextData, collectedStories);
        }
    }

    // 컨테이너를 선택했을 때
    public void SelectContainer(DialogContainer container)
    {
        selectedContainer = container;
        selectedStory = null;
    }

    // 스토리를 선택했을 때
    public void SelectStory(StoryData story)
    {
        selectedStory = story;
    }
}