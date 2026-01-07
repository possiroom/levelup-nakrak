using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

public class DialogTriggerView
{
    // 데이터
    private List<ConditionID> cachedIDs = new List<ConditionID>();
    private List<ConditionID> filteredIDs = new List<ConditionID>();
    private Dictionary<string, int> fileData = new Dictionary<string, int>();

    // 페이지네이션
    private Vector2 scrollPos;
    private string searchText = "";
    private int currentPage = 0;
    private const int ITEMS_PER_PAGE = 50;

    // 수정 모드 관리 변수
    private enum EditMode { None, Rename, ChangeOrder }
    private EditMode currentMode = EditMode.None;
    private ConditionID editingID = null;
    private string tempName = "";
    private int tempOrder = 0;

    // 파일 경로
    private string CsvPath => Application.dataPath + "/Story/GameData.csv";

    public void OnEnable()
    {
        ReloadIDs();
        LoadCSV();
    }

    public void Draw()
    {
        bool isIngame = Application.isPlaying;
        
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredIDs.Count / ITEMS_PER_PAGE));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // 상단 버튼
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        
        GUI.enabled = !isIngame; 
        if (GUILayout.Button("Create New ID (+)", GUILayout.Height(20), GUILayout.ExpandWidth(true)))
        {
            CreateNewID();
        }

        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Save CSV", GUILayout.Height(20), GUILayout.Width(80))) 
        {
            SaveCSV();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Load CSV", GUILayout.Height(20), GUILayout.Width(80)))
        {
            ReloadIDs();
            LoadCSV();
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("RESET", GUILayout.Height(20), GUILayout.Width(55)))
        {
            if (EditorUtility.DisplayDialog("초기화", "모든 값을 0으로 수정하시겠습니까?", "Yes", "Cancel"))
            {
                var keys = new List<string>(fileData.Keys);
                foreach (var key in keys) fileData[key] = 0;
                GUI.FocusControl(null);
            }
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        // 인게임이면 HelpBox 추가
        if (isIngame) 
        {
            EditorGUILayout.HelpBox("인게임 데이터에 접근 중입니다.", MessageType.Info);
        }

        // 검색바, 페이지네이션
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Space(5);
        string newSearch = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (newSearch != searchText) 
        { 
            searchText = newSearch; 
            currentPage = 0; 
            UpdateFilter();
        }
        
        if (!string.IsNullOrEmpty(searchText) && GUILayout.Button("x", EditorStyles.miniButtonMid, GUILayout.Width(20)))
        {
            searchText = ""; 
            currentPage = 0; 
            GUI.FocusControl(null);
            UpdateFilter();
        }

        GUILayout.Space(10);

        GUI.enabled = currentPage > 0;
        if (GUILayout.Button("◀", EditorStyles.miniButtonLeft, GUILayout.Width(25))) currentPage--;
        
        GUI.enabled = true;
        GUILayout.Label($"{currentPage + 1} / {totalPages}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40));
        
        GUI.enabled = currentPage < totalPages - 1;
        if (GUILayout.Button("▶", EditorStyles.miniButtonRight, GUILayout.Width(25))) currentPage++;
        
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        
        // 표 헤더
        GUILayout.BeginHorizontal("box");
        GUILayout.Label("ID Name", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
        GUILayout.Label("Value", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Del", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.EndHorizontal();

        // 표 ListView
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        var pageItems = filteredIDs.Skip(currentPage * ITEMS_PER_PAGE).Take(ITEMS_PER_PAGE);
        ConditionID idToDelete = null;

        foreach (var id in pageItems)
        {
            if (id == null) continue;

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 아이콘
            GUILayout.Label(EditorGUIUtility.IconContent("ScriptableObject Icon"), GUILayout.Height(18), GUILayout.Width(18));
            
            if (editingID == id && !isIngame)
            {
                // 엔터/ESC
                Event e = Event.current;
                if (e.isKey)
                {
                    if (e.keyCode == KeyCode.Return) { ApplyEdit(); e.Use(); }
                    else if (e.keyCode == KeyCode.Escape) { CancelEdit(); e.Use(); }
                }

                if (currentMode == EditMode.Rename)
                {
                    // (000) [TextField]
                    GUILayout.Label($"({id.sortOrder})", EditorStyles.label, GUILayout.ExpandWidth(false));
                    GUI.SetNextControlName("RenameField");
                    tempName = EditorGUILayout.TextField(tempName, GUILayout.ExpandWidth(true));
                    if (GUI.GetNameOfFocusedControl() != "RenameField") GUI.FocusControl("RenameField");
                }
                else if (currentMode == EditMode.ChangeOrder)
                {
                    // ([TextField]) Name_Name
                    GUILayout.Label("(", EditorStyles.label, GUILayout.Width(5));
                    GUI.SetNextControlName("OrderField");
                    tempOrder = EditorGUILayout.IntField(tempOrder, GUILayout.Width(40));
                    GUILayout.Label($") {id.name}", EditorStyles.label, GUILayout.ExpandWidth(true));
                    if (GUI.GetNameOfFocusedControl() != "OrderField") GUI.FocusControl("OrderField");
                }
            }
            else
            {
                string displayName = $"({id.sortOrder}) {id.name}";
                
                Rect labelRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.ExpandWidth(true));
                
                EditorGUI.LabelField(labelRect, displayName, EditorStyles.label);

                if (!isIngame && Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                {
                    // 좌클릭인데 왜인지 실행이 안 됨.
                    if (Event.current.button == 0)
                    {
                        EditorGUIUtility.PingObject(id);
                        Selection.activeObject = id;
                        Event.current.Use();
                    }
                    // 우클릭: 메뉴
                    else if (Event.current.button == 1 && !isIngame)
                    {
                        ShowContextMenu(id);
                        Event.current.Use();
                    }
                }
                // 우클릭: Mac 호환
                else if (!isIngame && Event.current.type == EventType.ContextClick && labelRect.Contains(Event.current.mousePosition))
                {
                        ShowContextMenu(id);
                        Event.current.Use();
                }
            }

            int currentVal = 0;
            if (isIngame)
            {
                if (ConditionManager.Instance != null) currentVal = ConditionManager.Instance.Get(id);
            }
            else
            {
                currentVal = fileData.ContainsKey(id.name) ? fileData[id.name] : 0;
            }

            if (currentVal != 0) GUI.backgroundColor = Color.cyan;
            
            EditorGUI.BeginChangeCheck();
            int newVal = EditorGUILayout.IntField(currentVal, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                if (isIngame) ConditionManager.Instance?.Set(id, newVal);
                else fileData[id.name] = newVal;
            }
            GUI.backgroundColor = Color.white;

            // 삭제 버튼
            GUI.enabled = !isIngame;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                if (EditorUtility.DisplayDialog("Delete", $"Delete '{id.name}'?", "Yes", "No")) idToDelete = id;
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (idToDelete != null) DeleteID(idToDelete);
    }

    // 검색 캐시 업데이트
    void UpdateFilter()
    {
        if (string.IsNullOrEmpty(searchText))
        {
            filteredIDs = new List<ConditionID>(cachedIDs);
        }
        else
        {
            filteredIDs = cachedIDs.Where(id => 
                id != null && id.name.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }
    }

    void ShowContextMenu(ConditionID id)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Rename"), false, () => StartEdit(id, EditMode.Rename));
        menu.AddItem(new GUIContent("Change Order"), false, () => StartEdit(id, EditMode.ChangeOrder));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete"), false, () => { if (EditorUtility.DisplayDialog("Delete", $"Delete '{id.name}'?", "Yes", "No")) DeleteID(id); });
        menu.ShowAsContext();
    }

    // 수정 매소드
    void StartEdit(ConditionID id, EditMode mode)
    {
        editingID = id;
        currentMode = mode;
        if (mode == EditMode.Rename) tempName = id.name;
        if (mode == EditMode.ChangeOrder) tempOrder = id.sortOrder;
    }

    void CancelEdit()
    {
        editingID = null;
        currentMode = EditMode.None;
        GUI.FocusControl(null);
    }

    void ApplyEdit()
    {
        if (editingID == null) return;

        if (currentMode == EditMode.Rename) // 수정 모드가 이름 변경
        {
            if (string.IsNullOrEmpty(tempName)) { CancelEdit(); return; }
            string oldName = editingID.name;
            string path = AssetDatabase.GetAssetPath(editingID);
            string error = AssetDatabase.RenameAsset(path, tempName);
            
            if (string.IsNullOrEmpty(error))
            {
                if (fileData.ContainsKey(oldName))
                {
                    int val = fileData[oldName];
                    fileData.Remove(oldName);
                    fileData[tempName] = val;
                }
            }
        }
        else if (currentMode == EditMode.ChangeOrder) // 인덱스 변경
        {
            if (tempOrder != editingID.sortOrder)
            {
                // 충돌 검사
                bool conflict = cachedIDs.Any(x => x != editingID && x.sortOrder == tempOrder);
                
                if (conflict)
                {
                    bool shift = EditorUtility.DisplayDialog("번호 충돌", 
                        $"{tempOrder}번은 이미 존재합니다.\n정말로 번호를 끼워 넣겠습니까?\n(이 작업은 되돌릴 수 없습니다.)", "Yes", "No");
                    
                    if (shift)
                    {
                        var potentialConflicts = cachedIDs
                            .Where(x => x != editingID && x.sortOrder >= tempOrder)
                            .OrderBy(x => x.sortOrder)
                            .ToList();

                        int nextExpected = tempOrder;

                        foreach (var target in potentialConflicts)
                        {
                            if (target.sortOrder == nextExpected)
                            {
                                target.sortOrder++;
                                EditorUtility.SetDirty(target);
                                nextExpected++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else // Not Shift
                    {
                        CancelEdit();
                        return;
                    }
                }
                editingID.sortOrder = tempOrder;
                EditorUtility.SetDirty(editingID);
            }
        }

        SaveCSV();
        ReloadIDs();
        CancelEdit();
    }

    // 기타 Utils
    void CreateNewID()
    {
        int maxOrder = -1;
        if (cachedIDs.Count > 0) maxOrder = cachedIDs.Max(id => id.sortOrder);

        ConditionID newID = DialogEditorUtils.CreateAsset<ConditionID>(DialogEditorUtils.PATH_CONDITION_ID, "NewID");
        if (newID != null)
        {
            newID.sortOrder = maxOrder + 1;
            EditorUtility.SetDirty(newID);
        }
        ReloadIDs();
    }

    void DeleteID(ConditionID id)
    {
        string delName = id.name;
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(id));
        if (fileData.ContainsKey(delName)) fileData.Remove(delName);
        ReloadIDs();
        SaveCSV();
    }

    void ReloadIDs()
    {
        cachedIDs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ConditionID");
        foreach (string guid in guids)
        {
            ConditionID id = AssetDatabase.LoadAssetAtPath<ConditionID>(AssetDatabase.GUIDToAssetPath(guid));
            if (id != null) cachedIDs.Add(id);
        }
        
        cachedIDs.Sort((a, b) => {
            int ret = a.sortOrder.CompareTo(b.sortOrder);
            if (ret != 0) return ret;
            return a.name.CompareTo(b.name);
        });

        UpdateFilter();
    }

    void LoadCSV()
    {
        fileData.Clear();
        if (!File.Exists(CsvPath)) return;
        string[] lines = File.ReadAllLines(CsvPath);
        for (int i = 1; i < lines.Length; i++) 
        {
            var parts = lines[i].Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int v))
                if (!fileData.ContainsKey(parts[0])) fileData[parts[0]] = v;
        }
    }

    void SaveCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Key,Value");
        foreach (var id in cachedIDs)
        {
            if (id == null) continue;
            int val = fileData.ContainsKey(id.name) ? fileData[id.name] : 0;
            sb.AppendLine($"{id.name},{val}");
        }
        string dir = Path.GetDirectoryName(CsvPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(CsvPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"Saved GameData: {CsvPath}");
    }
}