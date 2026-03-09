using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

// 비교 커맨드
public enum CompareType 
{ 
    IsTrue,
    IsFalse,
    Equal, 
    NotEqual, 
    Greater, 
    GreaterEqual, 
    Less, 
    LessEqual 
}

// 액션 커맨드
public enum ActionType 
{ 
    Set, 
    Add
}

// 조건 구조체
[System.Serializable]
public struct DialogCondition
{
    public ConditionID id;
    public CompareType compare;
    public int value;
}

// Action 구조체
[System.Serializable]
public struct DialogAction
{
    public ActionType type;
    public ConditionID id;
    public int value;
}

public class ConditionManager : MonoBehaviour
{
    public static ConditionManager Instance;

    private Dictionary<string, int> variables = new Dictionary<string, int>();
    private string SavePath => Application.dataPath + "/Story/GameData.csv";

    // Singleton
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        LoadFromCSV();
    }

    // Action Command Method
    public void Set(ConditionID id, int value)
    {
        if (id == null) return;
        variables[id.name] = value;
        Debug.Log("[ConditionManager] Set: " + id.name + " = " + value);
    }

    public void Add(ConditionID id, int amount)
    {
        if (id == null) return;
        if (variables.ContainsKey(id.name)) variables[id.name] += amount;
        else variables.Add(id.name, amount);
    }

    public void ObjectDestroy(GameObject target)
    {
        if (target == null) return;
        Destroy(target);
    }

    // 값 가져오기
    public int Get(ConditionID id)
    {
        if (id != null && variables.ContainsKey(id.name)) return variables[id.name];
        return 0; 
    }

    // Condition 커맨드 실행
    public bool CheckConditions(List<DialogCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true; // 조건 없으면 통과

        foreach (var condition in conditions)
        {
            if (condition.id == null) continue;

            int currentVal = Get(condition.id);
            bool isPass = false;

            switch (condition.compare)
            {
                case CompareType.IsTrue:       isPass = currentVal != 0; break;
                case CompareType.IsFalse:      isPass = currentVal == 0; break;
                case CompareType.Equal:        isPass = currentVal == condition.value; break;
                case CompareType.NotEqual:     isPass = currentVal != condition.value; break;
                case CompareType.Greater:      isPass = currentVal > condition.value; break;
                case CompareType.GreaterEqual: isPass = currentVal >= condition.value; break;
                case CompareType.Less:         isPass = currentVal < condition.value; break;
                case CompareType.LessEqual:    isPass = currentVal <= condition.value; break;
            }

            if (!isPass) return false;
        }
        return true;
    }

    // Action 커맨드 실행
    public void ExecuteActions(List<DialogAction> actions)
    {
        if (actions == null) return;

        foreach (var cmd in actions)
        {
            if (cmd.id == null) continue;
            if (cmd.type == ActionType.Set) Set(cmd.id, cmd.value);
            else if (cmd.type == ActionType.Add) Add(cmd.id, cmd.value);
        }
    }

    // 일단 인게임에서 즉시 Save/Load 할 수 있도록
    public void SaveToCSV()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Key,Value"); // 헤더

        // 한 줄씩 쓰기
        foreach (var pair in variables)
        {
            sb.AppendLine($"{pair.Key},{pair.Value}");
        }

        File.WriteAllText(SavePath, sb.ToString(), Encoding.UTF8);
        
#if UNITY_EDITOR
        // 에디터에서 파일이 변경되었음을 알림 (동기화)
        // 에디터 성능이 떨어지면 삭제
        UnityEditor.AssetDatabase.Refresh(); 
#endif
    }

    public void LoadFromCSV()
    {
        variables.Clear();
        if (!File.Exists(SavePath)) return;

        string[] lines = File.ReadAllLines(SavePath);
        
        // 헤더 건너뛰기
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length >= 2)
            {
                string key = parts[0];
                if (int.TryParse(parts[1], out int val))
                {
                    variables[key] = val;
                }
            }
        }
    }
}