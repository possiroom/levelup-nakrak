using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DialogStoryLeftPanel
{
    private DialogStoryView parentView;

    // Data
    private List<DialogContainer> allContainers = new List<DialogContainer>();
    private List<StoryData> orphanStories = new List<StoryData>();

    // UI State
    private Vector2 scrollPos;
    private Dictionary<StoryData, bool> foldoutStatus = new Dictionary<StoryData, bool>();
    private bool orphanStoriesFoldout = true;
    private string containerSearchText = "";

    private int currentPage = 0;
    private const int ITEMS_PER_PAGE = 10;

    // Cache
    private List<DialogContainer> cachedFilteredList = new List<DialogContainer>();

    // Rename State
    private DialogContainer renamingContainer = null;
    private string tempRenameText = "";
    private bool requestFocusRename = false;

    private StoryData renamingStory = null;
    private string tempStoryName = "";
    private bool requestFocusStoryRename = false;

    public DialogStoryLeftPanel(DialogStoryView view)
    {
        parentView = view;
    }

    public void OnEnable()
    {
        RefreshContainerList();
    }

    public void Draw()
    {
        // 데이터 변경 감지 시 자동 갱신
        if (allContainers != DialogDataManager.AllContainers)
        {
            RefreshContainerList();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) RefreshContainerList();
        
        if (GUILayout.Button("New (+)", GUILayout.ExpandWidth(true)))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Dialog Container"), false, () => 
            {
                var newCon = DialogEditorUtils.CreateNewContainer();
                if (newCon != null) 
                { 
                    RefreshContainerList(); 
                    parentView.SelectContainer(newCon);
                }
            });

            menu.AddItem(new GUIContent("Story Data"), false, () => 
            {
                var newStory = DialogEditorUtils.CreateOnlyStory();
                if (newStory != null)
                {
                    RefreshContainerList();
                    parentView.SelectStory(newStory);
                    parentView.selectedContainer = null;
                    orphanStoriesFoldout = true; 
                }
            });

            menu.ShowAsContext();
        }
        GUILayout.EndHorizontal();

        // 검색
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck(); 
        containerSearchText = EditorGUILayout.TextField(containerSearchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
        {
            UpdateFilteredList();
        }
        if (containerSearchText != "" && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(25)))
        {
            containerSearchText = "";
            UpdateFilteredList();
            GUI.FocusControl(null);
        }
        GUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawOrphanStories();
        EditorGUILayout.Space();
    
        List<DialogContainer> currentList = cachedFilteredList;

        int totalCount = currentList.Count;
        int totalPages = (totalCount == 0) ? 1 : Mathf.CeilToInt((float)totalCount / ITEMS_PER_PAGE);
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // 헤더
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Container Browser", EditorStyles.boldLabel, GUILayout.Width(130));
        GUILayout.FlexibleSpace();

        GUI.enabled = currentPage > 0;
        if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(25))) currentPage--;
        
        GUI.enabled = true;
        string pageLabel = $"{currentPage + 1} / {totalPages}";
        GUILayout.Label(pageLabel, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40));

        GUI.enabled = currentPage < totalPages - 1;
        if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(25))) currentPage++;
        GUI.enabled = true;

        GUILayout.EndHorizontal();

        int startIndex = currentPage * ITEMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, totalCount);

        for (int i = startIndex; i < endIndex; i++)
        {
            DialogContainer container = currentList[i];
            if (container == null) continue;

            DrawContainerItem(container);
        }
        
        if (totalCount == 0)
        {
            GUILayout.Label("No containers found.", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    public void RefreshContainerList()
    {
        allContainers = DialogDataManager.AllContainers;
        FindOrphanStories();

        // Fake Null 처리
        if (parentView.selectedContainer == null) parentView.selectedContainer = null;
        if (parentView.selectedStory == null) parentView.selectedStory = null;

        UpdateFilteredList();
    }

    private void UpdateFilteredList()
    {
        cachedFilteredList.Clear();

        if (string.IsNullOrEmpty(containerSearchText))
        {
            cachedFilteredList.AddRange(allContainers);
        }
        else
        {
            string lowerText = containerSearchText.ToLower();
            foreach (var con in allContainers)
            {
                if (con != null && con.name.ToLower().Contains(lowerText))
                {
                    cachedFilteredList.Add(con);
                }
            }
        }
        currentPage = 0;
    }

    private void FindOrphanStories()
    {
        orphanStories.Clear();
        HashSet<StoryData> allStories = new HashSet<StoryData>(DialogDataManager.AllStories);
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

    private void CollectReferencedStoriesRecursive(StoryData story, HashSet<StoryData> collectedStories)
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

    private void DrawOrphanStories()
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

                    // 검색어 필터링
                    if (!string.IsNullOrEmpty(containerSearchText) && !story.name.ToLower().Contains(containerSearchText.ToLower()))
                        continue;

                    GUILayout.BeginHorizontal();
                    DrawStoryItem(story); 
                    GUILayout.EndHorizontal();
                }
            }
        }
    }

    private void DrawContainerItem(DialogContainer container)
    {
        GUILayout.BeginVertical("helpBox");

        DrawContainerHeader(container);

        if (parentView.selectedContainer == container)
        {
            DrawContainerContents(container);
        }

        GUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawContainerHeader(DialogContainer container)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("📁", EditorStyles.label, GUILayout.Width(20), GUILayout.Height(20));

        if (renamingContainer == container)
        {
            DrawRenameField(container);
        }
        else
        {
            DrawContainerNameButton(container);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawRenameField(DialogContainer container)
    {
        Event e = Event.current;
        bool isEnter = e.type == EventType.KeyDown && e.keyCode == KeyCode.Return;
        bool isEsc = e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;

        GUI.SetNextControlName("RenameField");
        
        // 텍스트 필드
        tempRenameText = EditorGUILayout.TextField(tempRenameText, GUILayout.Height(20));

        if (requestFocusRename)
        {
            EditorGUI.FocusTextInControl("RenameField");
            requestFocusRename = false;
        }

        if (isEnter)
        {
            if (!string.IsNullOrEmpty(tempRenameText) && tempRenameText != container.name)
            {
                DialogEditorUtils.RenameAsset(container, tempRenameText);
                RefreshContainerList();
                if (parentView.selectedContainer == container) parentView.SelectContainer(container);
            }
            renamingContainer = null;
            tempRenameText = "";
            
            e.Use(); // Textfield 보다 먼저 사용
            GUIUtility.ExitGUI();
        }
        else if (isEsc)
        {
            renamingContainer = null;
            tempRenameText = "";
            e.Use();
        }
    }

    
    private void DrawContainerNameButton(DialogContainer container)
    {
        Rect buttonRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.ExpandWidth(true));
        Event e = Event.current;

        // 마우스가 버튼 영역 안에 있고, 우클릭(MouseDown, button 1) 이벤트가 발생했을 때
        if (buttonRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 1)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename"), false, () => 
            {
                renamingContainer = container;
                tempRenameText = container.name;
                requestFocusRename = true;
            });

            menu.AddItem(new GUIContent("Delete"), false, () => 
            {
                if (DialogEditorUtils.DeleteContainer(container))
                {
                    parentView.RequestRefresh();
                    if (parentView.selectedContainer == container) 
                    {
                        parentView.SelectContainer(null);
                    }
                }
            });
            menu.ShowAsContext();
            e.Use(); // 다른 컨트롤이 이 이벤트를 사용하지 못하도록 막습니다.
        }

        if (GUI.Button(buttonRect, container.name, EditorStyles.boldLabel))
        {
            parentView.SelectContainer(container);
            GUI.FocusControl(null);
        }
    }

    private void DrawContainerContents(DialogContainer container)
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
        if (parentView.selectedStory == null)
        {
            if (GUILayout.Button("+ New Slot/Head Story", EditorStyles.miniButton))
            {
                var newData = DialogEditorUtils.CreateStoryInSlot(container);
                if (newData != null) parentView.SelectStory(newData);
            }
        }
        else
        {
            bool isConnected = (parentView.selectedStory.nextData != null) || parentView.selectedStory.isSelect;
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
                    var newData = DialogEditorUtils.CreateStoryLinked(parentView.selectedStory);
                    if (newData != null) parentView.SelectStory(newData);
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }

    private void DrawStoryRecursive(StoryData story, int depth, HashSet<StoryData> visited)
    {
        if (story == null) return;

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

        DrawStoryItem(story);

        GUILayout.EndHorizontal();

        bool isSelectButton = story.isSelect && story.selects != null && story.selects.Count > 0;

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

                    DrawStoryRecursive(sel.nextData, depth + 1, newVisited);
                }
            }
        }
        else if (!isSelectButton && story.nextData != null && !story.isEnd)
        {
            DrawStoryRecursive(story.nextData, depth, newVisited);
        }
    }

    private void DrawStoryItem(StoryData story)
    {
        // 1. 배경색 설정
        if (parentView.selectedStory == story) GUI.backgroundColor = Color.yellow;
        else GUI.backgroundColor = Color.white;

        // 2. 아이콘 결정
        string icon = story.isSelect ? "?" : "↓";
        if (story.isEnd) icon = "X";

        // 3. 상태에 따라 그리기 방식 분기
        if (renamingStory == story)
        {
            DrawStoryRenameField(story, icon);
        }
        else
        {
            DrawStoryNameButton(story, icon);
        }

        // 4. 폴드아웃 화살표 (오른쪽 끝)
        DrawStoryFoldout(story);

        // 색상 초기화
        GUI.backgroundColor = Color.white;
    }

    private void DrawStoryRenameField(StoryData story, string icon)
    {
        // 아이콘을 Label로 그림 (수정 중에도 아이콘은 보여야 하니까)
        GUILayout.Label(icon, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(20));

        // 키 입력 감지
        Event e = Event.current;
        bool isEnter = e.type == EventType.KeyDown && e.keyCode == KeyCode.Return;
        bool isEsc = e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;

        GUI.SetNextControlName("StoryRename");
        tempStoryName = EditorGUILayout.TextField(tempStoryName, GUILayout.Height(20));

        if (requestFocusStoryRename)
        {
            EditorGUI.FocusTextInControl("StoryRename");
            requestFocusStoryRename = false;
        }

        if (isEnter)
        {
            if (!string.IsNullOrEmpty(tempStoryName) && tempStoryName != story.name)
            {
                DialogEditorUtils.RenameAsset(story, tempStoryName);
                RefreshContainerList();
                if (parentView.selectedStory == story) parentView.SelectStory(story);
            }
            renamingStory = null;
            e.Use();
            GUIUtility.ExitGUI();
        }
        else if (isEsc)
        {
            renamingStory = null;
            e.Use();
        }
    }

    private void DrawStoryNameButton(StoryData story, string icon)
    {
        GUIStyle leftButton = new GUIStyle(EditorStyles.miniButton);
        leftButton.alignment = TextAnchor.MiddleLeft;
        leftButton.padding.left = 10;
        leftButton.richText = true;

        string btnLabel = $"{icon}  {story.name}";
        
        if (!string.IsNullOrEmpty(story.text))
        {
            string preview = story.text.Length > 8 ? story.text.Substring(0, 8) + "..." : story.text;
            btnLabel += $"  <color=#B0B0B0>\"{preview}\"</color>";
        }

        // [핵심 변경] 1. 영역을 먼저 확보합니다. (Container 방식)
        Rect btnRect = EditorGUILayout.GetControlRect(false, 20);

        Event evt = Event.current;

        // [핵심 변경] 2. 버튼을 그리기 전에 우클릭(MouseDown + button 1)을 먼저 감지합니다.
        if (btnRect.Contains(evt.mousePosition) && evt.type == EventType.MouseDown && evt.button == 1)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Rename"), false, () => 
            {
                renamingStory = story;
                tempStoryName = story.name;
                requestFocusStoryRename = true;
            });

            menu.AddItem(new GUIContent("Delete"), false, () => 
            {
                DialogEditorUtils.DeleteStory(story);
                RefreshContainerList();
                if (parentView.selectedStory == story) parentView.SelectStory(null);
            });

            menu.ShowAsContext();
            evt.Use(); // 이벤트를 확실하게 소비하여 다른 동작 방지
        }

        // 3. 확보해둔 영역(btnRect)에 버튼을 그립니다. (Left Click 처리)
        if (GUI.Button(btnRect, btnLabel, leftButton))
        {
            parentView.SelectStory(story);
            
            if (orphanStories.Contains(story)) parentView.selectedContainer = null;
            
            GUI.FocusControl(null);
        }
    }

    // [Util] 폴드아웃 화살표 그리기
    private void DrawStoryFoldout(StoryData story)
    {
        bool isSelectButton = story.isSelect && story.selects != null && story.selects.Count > 0;
        
        if (isSelectButton)
        {
            if (!foldoutStatus.ContainsKey(story)) foldoutStatus[story] = true;
            foldoutStatus[story] = EditorGUILayout.Foldout(foldoutStatus[story], "", true);
        }
        else
        {
            // 폴드아웃이 없어도 레이아웃 유지를 위해 빈 공간 확보
            GUILayout.Label("", GUILayout.Width(13));
        }
    }
}
