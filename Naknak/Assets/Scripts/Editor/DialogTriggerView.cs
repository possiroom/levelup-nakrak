using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class DialogTriggerView
{
    // 테이터 임시 보관 (캐싱)
    private List<ConditionID> cachedIDs = new List<ConditionID>();
    private Dictionary<string, int> csvData = new Dictionary<string, int>(); 
    
    private Vector2 scrollPos;
    private string searchText = "";
    private string CsvPath => Application.dataPath + "/Story/GameData.csv";

    public void OnEnable()
    {
        ReloadIDs();
        LoadCSV();
    }

    public void Draw()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // 검색
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (searchText != "" && GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(25)))
        { 
            searchText = ""; GUI.FocusControl(null); 
        }
        
        GUILayout.Space(10);
        
        // 생성
        if (GUILayout.Button("Create ID (+)", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            DialogEditorUtils.CreateAsset<ConditionID>(DialogEditorUtils.PATH_CONDITION_ID, "NewID");
            ReloadIDs();
        }

        GUILayout.Space(10);

        // 저장/로드 버튼 배치
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("Save CSV", EditorStyles.toolbarButton, GUILayout.Width(70))) SaveCSV();
        GUI.backgroundColor = Color.white;
        
        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            ReloadIDs();
            LoadCSV();
        }
        
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("box");
        GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(205));
        GUILayout.Label("Value (Int)", EditorStyles.boldLabel, GUILayout.ExpandWidth(true)); 
        GUILayout.Label("Del", EditorStyles.boldLabel, GUILayout.Width(25));
        GUILayout.EndHorizontal();

        // ID 리스트
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        bool isIngame = Application.isPlaying && ConditionManager.Instance != null;

        if (!isIngame)
        {
            EditorGUILayout.HelpBox("Modify File Data", MessageType.Info);
        } 
        else
        {
            EditorGUILayout.HelpBox("Modify InGame Data", MessageType.Info);
        }

        ConditionID idToDelete = null;

        foreach (var id in cachedIDs)
        {
            if (id == null) continue;
            // 검색 기능
            if (!string.IsNullOrEmpty(searchText) && !id.name.ToLower().Contains(searchText.ToLower())) continue;

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 아이콘
            GUILayout.Label(EditorGUIUtility.IconContent("ScriptableObject Icon"), GUILayout.Height(20), GUILayout.Width(20));
            if (GUILayout.Button(id.name, EditorStyles.label, GUILayout.Width(180)))
            {
                EditorGUIUtility.PingObject(id);
                Selection.activeObject = id;
            }

            // 값 수정
            int currentVal = 0;
            if (isIngame)
            {
                // 접근 권한 인하여 일단 이렇게
                currentVal = ConditionManager.Instance.Get(id);
            }
            else
            {
                currentVal = csvData.ContainsKey(id.name) ? csvData[id.name] : 0;
            }

            if (currentVal != 0) GUI.backgroundColor = Color.cyan;
            
            EditorGUI.BeginChangeCheck();
            int newVal = EditorGUILayout.IntField(currentVal, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                if (isIngame) ConditionManager.Instance.Set(id, newVal);
                else csvData[id.name] = newVal;
            }

            GUI.backgroundColor = Color.white;

            // 삭제 버튼
            GUI.backgroundColor = Color.red;
            if (!isIngame && GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Delete", $"Delete Key '{id.name}'?", "Yes", "No"))
                {
                    idToDelete = id;
                }
            }
            GUI.backgroundColor = Color.gray;
            if (isIngame && GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(25)))
            {
                Debug.Log("[DialogTriggerView] Cannot Delete ID in Game.");
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (idToDelete != null)
        {
            string delName = idToDelete.name;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(idToDelete));
            if (csvData.ContainsKey(delName)) csvData.Remove(delName);
            ReloadIDs();
            SaveCSV();
            GUIUtility.ExitGUI();
        }
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
        cachedIDs.Sort((a, b) => a.name.CompareTo(b.name));
    }

    void LoadCSV()
    {
        csvData.Clear();
        if (!File.Exists(CsvPath)) return;

        string[] lines = File.ReadAllLines(CsvPath);
        for (int i = 1; i < lines.Length; i++) 
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int v))
            {
                if(!csvData.ContainsKey(parts[0])) csvData[parts[0]] = v;
            }
        }
    }

    void SaveCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Key,Value");
        foreach (var id in cachedIDs)
        {
            if (id == null) continue;
            int val = csvData.ContainsKey(id.name) ? csvData[id.name] : 0;
            sb.AppendLine($"{id.name},{val}");
        }
        File.WriteAllText(CsvPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh(); 

        Debug.Log($"[ID Manager] CSV Saved to: {CsvPath}");
    }
}