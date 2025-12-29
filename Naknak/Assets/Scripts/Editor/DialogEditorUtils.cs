using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class DialogEditorUtils
{
    // 파일 경로
    public const string PATH_ROOT = "Assets/Story";
    public const string PATH_CONTAINER = "Assets/Story/Dialog Container";
    public const string PATH_STORY = "Assets/Story/Story Data";
    public const string PATH_CONDITION_ROOT = "Assets/Story/Dialog Condition";
    public const string PATH_CONDITION_ID = "Assets/Story/Dialog Condition/IDs";

    // 폴더가 없으면 생성하는 매소드
    public static void EnsureFolderStructure()
    {
        CreateFolderIfNotExists("Assets", "Story");

        CreateFolderIfNotExists(PATH_ROOT, "Dialog Container");
        CreateFolderIfNotExists(PATH_ROOT, "Story Data");
        CreateFolderIfNotExists(PATH_ROOT, "Dialog Condition");

        CreateFolderIfNotExists(PATH_CONDITION_ROOT, "IDs");
    }

    private static void CreateFolderIfNotExists(string parent, string folderName)
    {
        string fullPath = $"{parent}/{folderName}";
        
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    // 덮어쓰기 방지: 원활한 파일 관리를 위해
    public static bool CheckDuplicate(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            EditorUtility.DisplayDialog("Error", "파일을 덮어씌울 수 없습니다.", "OK");
            return true;
        }
        return false;
    }

    // 컨테이너 생성 매소드
    public static DialogContainer CreateNewContainer()
    {
        EnsureFolderStructure();
        string path = EditorUtility.SaveFilePanelInProject("Create New Dialog Container", "NewDialogContainer", "asset", "Save", PATH_CONTAINER);
        if (string.IsNullOrEmpty(path) || CheckDuplicate(path)) return null;

        DialogContainer newAsset = ScriptableObject.CreateInstance<DialogContainer>();
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        return newAsset;
    }

    // Container 내부의 'Slot' 생성
    public static StoryData CreateStoryInSlot(DialogContainer container)
    {
        EnsureFolderStructure();
        string defaultName = $"{container.name}_1";
        string path = EditorUtility.SaveFilePanelInProject("Create Head Story Data", defaultName, "asset", "Save", PATH_STORY);
        
        if (string.IsNullOrEmpty(path) || CheckDuplicate(path)) return null;

        StoryData newData = ScriptableObject.CreateInstance<StoryData>();
        AssetDatabase.CreateAsset(newData, path);

        // 컨테이너 연결
        Undo.RecordObject(container, "Add Dialog Slot");
        
        DialogContainer.DialogSlot newSlot = new DialogContainer.DialogSlot 
        { 
            memo = "기본", 
            conditions = new List<DialogCondition>(), // 리스트로 초기화
            headStoryData = newData 
        };
        
        if (container.dialogSlots == null) container.dialogSlots = new List<DialogContainer.DialogSlot>();
        container.dialogSlots.Add(newSlot);
        
        EditorUtility.SetDirty(container);
        AssetDatabase.SaveAssets();
        return newData;
    }

    // 스토리 생성 + 스토리 연결
    public static StoryData CreateStoryLinked(StoryData parentStory)
    {
        EnsureFolderStructure();
        // 다음 숫자 추천
        string defaultName = GetNextFileName(parentStory.name);
        string path = EditorUtility.SaveFilePanelInProject("Append Next Story", defaultName, "asset", "Save", PATH_STORY);

        if (string.IsNullOrEmpty(path) || CheckDuplicate(path)) return null;

        StoryData newData = ScriptableObject.CreateInstance<StoryData>();
        newData.charName = parentStory.charName; 
        newData.isEnd = true; 

        AssetDatabase.CreateAsset(newData, path);

        // 직전 노드와 연결
        Undo.RecordObject(parentStory, "Link Next Story");
        parentStory.nextData = newData;
        parentStory.isEnd = false; 
        
        EditorUtility.SetDirty(parentStory);
        AssetDatabase.SaveAssets();
        return newData;
    }

    // 다음 파일이름 자동 생성
    private static string GetNextFileName(string currentName)
    {
        Match match = Regex.Match(currentName, "(\\d+)$");

        if (match.Success)
        {
            // 숫자 부분
            string numberStr = match.Value;
            string prefix = currentName.Substring(0, match.Index);

            // 숫자로 변환 후 1 증가
            if (int.TryParse(numberStr, out int number))
            {
                number++;
                string newNumberStr = number.ToString("D" + numberStr.Length);
                
                return prefix + newNumberStr;
            }
        }

        // 숫자가 없으면 뒤에 _1
        return currentName + "_1";
    }

    // 선택지 내에서 새 StoryData 생성
    public static void CreateStoryForSelect(SerializedProperty slotProp, StoryData parentStory, string suggestName)
    {
        EnsureFolderStructure();
        if (string.IsNullOrEmpty(suggestName)) suggestName = "sel";
        string defaultName = $"{parentStory.name}_{suggestName}";

        string path = EditorUtility.SaveFilePanelInProject("Create Selected Story", defaultName, "asset", "Save", PATH_STORY);
        if (string.IsNullOrEmpty(path) || CheckDuplicate(path)) return;

        StoryData newData = ScriptableObject.CreateInstance<StoryData>();
        newData.isEnd = true; 

        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();

        // 선택지 칸에 연결
        slotProp.objectReferenceValue = newData;
        slotProp.serializedObject.ApplyModifiedProperties();
    }

    public static StoryData CreateOnlyStory()
    {
        EnsureFolderStructure();
        
        string path = EditorUtility.SaveFilePanelInProject("Create New Story Data", "NewStory", "asset", "Save", PATH_STORY);
        if (string.IsNullOrEmpty(path) || CheckDuplicate(path)) return null;

        StoryData newData = ScriptableObject.CreateInstance<StoryData>();
        newData.isEnd = true; // 독립된 스토리는 isEnd

        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();
        
        return newData;
    }

    // 제네릭 생성
    public static T CreateAsset<T>(string folderPath, string defaultName) where T : ScriptableObject
    {
        EnsureFolderStructure();
        string path = EditorUtility.SaveFilePanelInProject("Create Asset", defaultName, "asset", "Save", folderPath);
        if (string.IsNullOrEmpty(path) || CheckDuplicate(path)) return null;

        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    public static void RenameAsset(Object asset, string newName)
    {
        // 유효성 검사
        if (asset == null || string.IsNullOrEmpty(newName)) return;
        if (asset.name == newName) return; // 이름이 같으면 패스

        string path = AssetDatabase.GetAssetPath(asset);
        
        // 3. 이름 변경 실행 (성공 시 빈 문자열, 실패 시 에러 메시지 반환)
        string error = AssetDatabase.RenameAsset(path, newName);

        if (string.IsNullOrEmpty(error))
        {
            AssetDatabase.SaveAssets(); // 저장
        }
        else
        {
            // 실패
            EditorUtility.DisplayDialog("Error", error, "OK");
        }
    }

    public static bool DeleteContainer(DialogContainer container)
    {
        if (container == null) return false;

        if (EditorUtility.DisplayDialog("Delete Container", 
                $"정말로 삭제할까요? '{container.name}'\n\n(연결된 스토리는 삭제되지 않고 연결 해제됩니다.)", "Delete", "Cancel"))
        {
            string path = AssetDatabase.GetAssetPath(container);
            bool success = AssetDatabase.DeleteAsset(path);
            if (success) AssetDatabase.SaveAssets();
            return success;
        }
        return false;
    }

    public static void DeleteStory(StoryData story)
    {
        if (EditorUtility.DisplayDialog("Delete Data", $"정말로 삭제할까요? '{story.name}'", "Delete", "Cancel"))
        {
            string path = AssetDatabase.GetAssetPath(story);
            AssetDatabase.DeleteAsset(path);
        }
    }
}