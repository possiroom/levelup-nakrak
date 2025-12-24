using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DialogDashboard : EditorWindow
{
    // 저장 경로 설정
    private const string PATH_ROOT = "Assets/Story";
    private const string PATH_CONTAINER = "Assets/Story/Dialog Container";
    private const string PATH_STORY = "Assets/Story/Story Data";
    
    // 선택된 항목
    private DialogContainer selectedContainer;
    private StoryData selectedStory;

    // 스크롤 및 접기/펼치기 상태 저장
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;
    
    // 어떤 대사가 펼쳐져 있는지 저장
    private Dictionary<StoryData, bool> foldoutStatus = new Dictionary<StoryData, bool>();

    // 데이터 캐싱
    private List<DialogContainer> allContainers = new List<DialogContainer>();

    [MenuItem("Tools/Dialog Dashboard")]
    public static void ShowWindow()
    {
        GetWindow<DialogDashboard>("Dialog Dashboard");
    }

    private void OnEnable()
    {
        RefreshContainerList();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        // [Left Panel] 컨테이너 목록
        GUILayout.BeginVertical("box", GUILayout.Width(320), GUILayout.ExpandHeight(true));
        DrawLeftPanel();
        GUILayout.EndVertical();

        // [Right Panel] 요소 편집
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        DrawRightPanel();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    // Left Panel
    void DrawLeftPanel()
    {
        // 컨테이너 생성 및 새로고침
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshContainerList();
        if (GUILayout.Button("New Container (+)", GUILayout.ExpandWidth(true))) CreateNewContainer();
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialog Story Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

        foreach (DialogContainer container in allContainers)
        {
            if (container == null) continue;

            GUILayout.BeginVertical("helpBox");

            // 컨테이너 선택
            if (GUILayout.Button($"📁 {container.name}", EditorStyles.boldLabel, GUILayout.Height(24)))
            {
                selectedContainer = container;
                selectedStory = null;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;

            // 컨테이너 내부 그리기
            if (selectedContainer == container)
            {
                if (container.dialogSlots != null)
                {
                    for (int i = 0; i < container.dialogSlots.Count; i++)
                    {
                        var slot = container.dialogSlots[i];
                        
                        // 슬롯 구분
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        string slotLabel = $"[Slot {i}]";
                        if (!string.IsNullOrEmpty(slot.memo)) slotLabel += $" {slot.memo}";
                        GUILayout.Label(slotLabel, EditorStyles.miniLabel);
                        GUILayout.EndHorizontal();

                        // 재귀 디렉토리
                        DrawStoryRecursive(slot.headStoryData, 1, new HashSet<StoryData>());
                    }
                }

                // Append Story 로직
                if (selectedStory == null)
                {
                    // 컨테이너가 선택됨
                    if (GUILayout.Button("+ Add New Slot (Root)", EditorStyles.miniButton))
                    {
                        CreateStoryInSlot(container);
                    }
                }
                else
                {
                    // 스토리가 선택됨
                    bool isConnected = (selectedStory.nextData != null) || selectedStory.isSelect;

                    // 스토리가 이미 연결 되었으면 추가 생성 X
                    if (isConnected)
                    {
                        GUI.backgroundColor = Color.gray;
                        GUILayout.Label("Node is full linked.", EditorStyles.centeredGreyMiniLabel);
                        GUI.backgroundColor = Color.white;
                    }
                    else // 연결이 안 되어있으면 생성
                    {
                        GUI.backgroundColor = Color.green;
                        if (GUILayout.Button("⬇ Append Next Story", EditorStyles.miniButton))
                        {
                            CreateStoryLinked(selectedStory);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }
            GUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        EditorGUILayout.EndScrollView();
    }

    // 재귀 디렉토리 함수
    void DrawStoryRecursive(StoryData story, int depth, HashSet<StoryData> visited)
    {
        if (story == null) return;

        // 왼쪽 정렬
        GUIStyle leftButton = new GUIStyle(EditorStyles.miniButton);
        leftButton.alignment = TextAnchor.MiddleLeft;
        leftButton.padding.left = 10;
        leftButton.richText = true;

        // 무한 루프 방지
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

        HashSet<StoryData> newVisited = new(visited){ story };

        GUILayout.BeginHorizontal();
        GUILayout.Space(depth * 10); // 전체 들여쓰기

        // 선택된 스토리 색상 변경
        if (selectedStory == story) GUI.backgroundColor = Color.yellow;
        else GUI.backgroundColor = Color.white;

        string icon = story.isSelect ? "?" : "↓"; 
        if (story.isEnd) icon = "X"; 
        
        string btnLabel = $"{icon}  {story.name}";
        if (!string.IsNullOrEmpty(story.text)) 
        {
            // 텍스트 미리보기
            string preview = story.text.Length > 15 ? story.text.Substring(0, 15) + "..." : story.text;
            btnLabel += $"  <color=#B0B0B0>\"{preview}\"</color>"; 
        }

        // 버튼
        if (GUILayout.Button(btnLabel, leftButton, GUILayout.Height(20)))
        {
            selectedStory = story;
            GUI.FocusControl(null);
        }
        GUI.backgroundColor = Color.white;

        // 디렉토리 폴딩
        bool isBranch = story.isSelect && story.selects != null && story.selects.Count > 0;
        
        if (isBranch)
        {
            if (!foldoutStatus.ContainsKey(story)) foldoutStatus[story] = true; 
            foldoutStatus[story] = EditorGUILayout.Foldout(foldoutStatus[story], "", true);
        }
        else
        {
            GUILayout.Label("", GUILayout.Width(13));
        }

        GUILayout.EndHorizontal();

        // 자식 디렉토리
        if (isBranch && foldoutStatus[story])
        {
            foreach (var sel in story.selects)
            {
                if (sel.nextData != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space((depth + 1) * 10); // 추가 들여쓰기
                    GUI.color = Color.cyan;
                    GUILayout.Label($"└ [{sel.text}]", EditorStyles.miniLabel, GUILayout.Height(16));
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();

                    DrawStoryRecursive(sel.nextData, depth + 1, newVisited);
                }
            }
        }
        else if (!isBranch && story.nextData != null && !story.isEnd)
        {
            // 다음이 선택이 아니면 들여쓰기 X
            DrawStoryRecursive(story.nextData, depth, newVisited);
        }
    }

    // Right Panel
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

    // 스토리 편집
    // Story Data: Scriptable Object 인스펙터 창을 수정하는 곳
    void DrawStoryEditor(StoryData data)
    {
        SerializedObject so = new SerializedObject(data);
        so.Update();

        EditorGUILayout.LabelField($"Editing Story: {data.name}", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("charName"));
        EditorGUILayout.PropertyField(so.FindProperty("activatedImage"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("text"), GUILayout.Height(100));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
        SerializedProperty isEndProp = so.FindProperty("isEnd");
        SerializedProperty isSelectProp = so.FindProperty("isSelect");
        
        EditorGUILayout.PropertyField(isEndProp);
        EditorGUILayout.PropertyField(isSelectProp);

        if (isSelectProp.boolValue == true)
        {
            // 선택지
            DrawSelectsList(so.FindProperty("selects"));
        }
        else
        {
            SerializedProperty nextStoryData = so.FindProperty("nextData");

            if (nextStoryData.objectReferenceValue == null)
            {
                if (!isEndProp.boolValue)
                    EditorGUILayout.HelpBox("Link is missing!", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("Story End", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Next story is linked.", MessageType.None);
            }
            EditorGUILayout.PropertyField(so.FindProperty("nextData"));
        }

        so.ApplyModifiedProperties();

        EditorGUILayout.Space(20);
        if (GUILayout.Button("Delete This Story File", GUILayout.Height(30)))
        {
            DeleteStory(data);
        }
    }

    // 선택지 리스트
    void DrawSelectsList(SerializedProperty listProp)
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
            
            // 버튼 텍스트
            EditorGUILayout.LabelField("Btn Text:", GUILayout.Width(60));
            text.stringValue = EditorGUILayout.TextField(text.stringValue, GUILayout.Width(100));

            // 다음 스토리 데이터
            nextData.objectReferenceValue = EditorGUILayout.ObjectField(
                nextData.objectReferenceValue, 
                typeof(StoryData), 
                false
            );

            // 생성
            // 다음 스토리 데이터가 비어있으면 새로 생성할 수 있도록
            if (nextData.objectReferenceValue == null)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    CreateStoryForSelect(nextData, text.stringValue);
                    GUIUtility.ExitGUI(); 
                }
                GUI.backgroundColor = Color.white;
            }

            // 삭제
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
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

        // 리스트 추가
        if (GUILayout.Button("+ Add Select Option"))
        {
            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            
            // 새로 추가된 요소 가져와서 초기화
            SerializedProperty newItem = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
            newItem.FindPropertyRelative("text").stringValue = ""; 
            newItem.FindPropertyRelative("nextData").objectReferenceValue = null;
        }
        GUILayout.EndVertical();
    }

    // 컨테이너 편집
    void DrawContainerEditor(DialogContainer container)
    {
        SerializedObject so = new SerializedObject(container);
        so.Update();

        EditorGUILayout.LabelField($"Container: {container.name}", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Manage Dialogue Slots", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(so.FindProperty("dialogSlots"), true);

        so.ApplyModifiedProperties();
    }

    // 컨테이너 업데이트
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
    }

    // 컨테이너 생성
    void CreateNewContainer()
    {
        EnsureFolderStructure(); 

        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Dialog Container", 
            "NewDialogContainer", 
            "asset", 
            "Enter a name for the new container", 
            PATH_CONTAINER
        );

        if (string.IsNullOrEmpty(path)) return;

        // 이미 파일이 존재하는지 확인
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            EditorUtility.DisplayDialog("생성 실패", $"'{path}'\n\n이미 같은 이름의 파일이 존재합니다.\n덮어쓰기를 방지하기 위해 생성을 취소합니다.", "확인");
            return;
        }

        DialogContainer newContainer = CreateInstance<DialogContainer>();
        
        AssetDatabase.CreateAsset(newContainer, path);
        AssetDatabase.SaveAssets();

        RefreshContainerList();
        selectedContainer = newContainer;
        selectedStory = null;
        EditorGUIUtility.PingObject(newContainer);
    }

    // 슬롯: 대사 경우의 수 추가
    // 추후 Condition에 관한 변경 예정
    void CreateStoryInSlot(DialogContainer container)
    {
        EnsureFolderStructure();

        string defaultName = $"{container.name}_Start";

        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Story (Root)", 
            defaultName, 
            "asset", 
            "Enter a name for the new story file", 
            PATH_STORY
        );

        if (string.IsNullOrEmpty(path)) return;

        // 중복 확인
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            EditorUtility.DisplayDialog("생성 실패", "이미 같은 이름의 스토리 파일이 존재합니다.\n다른 이름을 지정해주세요.", "확인");
            return;
        }

        StoryData newData = CreateInstance<StoryData>();
        AssetDatabase.CreateAsset(newData, path);

        Undo.RecordObject(container, "Add Dialog Slot");
        
        DialogContainer.DialogSlot newSlot = new DialogContainer.DialogSlot
        {
            memo = "기본",
            condition = null,
            headStoryData = newData
        };

        // 복합 할당
        container.dialogSlots ??= new List<DialogContainer.DialogSlot>();
        container.dialogSlots.Add(newSlot);
        
        EditorUtility.SetDirty(container);
        AssetDatabase.SaveAssets();

        selectedStory = newData;
        EditorGUIUtility.PingObject(newData);
    }

    // 다음 스토리 생성 및 연결 함수
    void CreateStoryLinked(StoryData parentStory)
    {
        EnsureFolderStructure();

        string defaultName = $"{parentStory.name}_Next";

        string path = EditorUtility.SaveFilePanelInProject(
            "Append Next Story", 
            defaultName, 
            "asset", 
            "Enter a name for the next story file", 
            PATH_STORY
        );

        if (string.IsNullOrEmpty(path)) return;

        // 중복 확인
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            EditorUtility.DisplayDialog("생성 실패", "이미 같은 이름의 스토리 파일이 존재합니다.\n다른 이름을 지정해주세요.", "확인");
            return;
        }

        // 스토리 생성
        StoryData newData = CreateInstance<StoryData>();
        newData.charName = parentStory.charName; 
        newData.isEnd = true;

        AssetDatabase.CreateAsset(newData, path);

        // 부모 데이터 수정
        Undo.RecordObject(parentStory, "Link Next Story");
        
        parentStory.nextData = newData; // 연결
        parentStory.isEnd = false; // 부모는 이제 끝이 아니므로
        
        EditorUtility.SetDirty(parentStory);
        AssetDatabase.SaveAssets();

        // 업데이트
        selectedStory = newData;
        EditorGUIUtility.PingObject(newData);
    }

    // 선택지 생성 함수
    void CreateStoryForSelect(SerializedProperty slotProp, string suggestName)
    {
        EnsureFolderStructure();

        // 텍스트가 없으면 기본 이름 사용
        if (string.IsNullOrEmpty(suggestName)) suggestName = "Choice_Result";
        
        // 현재 선택된 스토리 이름_선택지내용
        string defaultName = $"{selectedStory.name}_{suggestName}";

        string path = EditorUtility.SaveFilePanelInProject(
            "Create Choice Result Story", 
            defaultName, 
            "asset", 
            "Enter a name for the choice result file", 
            PATH_STORY
        );

        if (string.IsNullOrEmpty(path)) return;

        // 중복 방지
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            EditorUtility.DisplayDialog("Error", "File already exists!", "OK");
            return;
        }

        // 파일 생성
        StoryData newData = CreateInstance<StoryData>();
        newData.isEnd = false;

        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();

        // 연결
        slotProp.objectReferenceValue = newData;
        slotProp.serializedObject.ApplyModifiedProperties();
    }

    // 폴더가 없으면 만드는 함수 (혹시 몰라서)
    void EnsureFolderStructure()
    {
        // 1. Assets/Story
        if (!AssetDatabase.IsValidFolder(PATH_ROOT))
            AssetDatabase.CreateFolder("Assets", "Story");

        // 2. Assets/Story/Dialog Container
        if (!AssetDatabase.IsValidFolder(PATH_CONTAINER))
            AssetDatabase.CreateFolder(PATH_ROOT, "Dialog Container");

        // 3. Assets/Story/Story Data
        if (!AssetDatabase.IsValidFolder(PATH_STORY))
            AssetDatabase.CreateFolder(PATH_ROOT, "Story Data");
    }

    // 스토리 삭제 매소드
    void DeleteStory(StoryData story)
    {
        // 재 확인
        if (EditorUtility.DisplayDialog("Delete Data", $"Delete '{story.name}'?", "Delete", "Cancel"))
        {
            string path = AssetDatabase.GetAssetPath(story);
            AssetDatabase.DeleteAsset(path);
            selectedStory = null;
            RefreshContainerList(); // 삭제 후 갱신
        }
    }
}