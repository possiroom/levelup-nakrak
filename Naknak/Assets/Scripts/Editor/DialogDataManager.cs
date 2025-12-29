using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [Only Editor] StoryView의 데이터 처리
/// :AssetPostprocessor
/// </summary>
public class DialogDataManager : AssetPostprocessor
{
    private static List<DialogContainer> _cachedContainers;
    private static List<StoryData> _cachedStories;

    // 외부에서 접근
    public static List<DialogContainer> AllContainers
    {
        get
        {
            if (_cachedContainers == null) LoadAllAssets();
            return _cachedContainers;
        }
    }

    public static List<StoryData> AllStories
    {
        get
        {
            if (_cachedStories == null) LoadAllAssets();
            return _cachedStories;
        }
    }

    // 파일 변경이 감지되면 자동으로 호출됨
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool needsRefresh = false;

        // 파일이 변경되었는지 확인
        foreach (string path in importedAssets.Concat(deletedAssets).Concat(movedAssets))
        {
            if (path.EndsWith(".asset"))
            {
                needsRefresh = true; 
                break;
            }
        }

        if (needsRefresh)
        {
            // 캐시를 날려서 다음 접근 때 다시 로드 (Lazy Load)
            _cachedContainers = null;
            _cachedStories = null;
            
            // 열려있는 윈도우가 있다면 리페인트 요청
            var window = EditorWindow.GetWindow<DialogDashboard>();
            if (window != null) window.Repaint();
        }
    }

    // FindAssets를 수행 (무거움)
    private static void LoadAllAssets()
    {
        _cachedContainers = new List<DialogContainer>();
        _cachedStories = new List<StoryData>();

        string[] guids = AssetDatabase.FindAssets("t:DialogContainer");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<DialogContainer>(path);
            if (asset != null) _cachedContainers.Add(asset);
        }
        _cachedContainers.Sort((a, b) => a.name.CompareTo(b.name));

        string[] storyGuids = AssetDatabase.FindAssets("t:StoryData");
        foreach (string guid in storyGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<StoryData>(path);
            if (asset != null) _cachedStories.Add(asset);
        }
        
        // 정렬
        _cachedStories.Sort((a, b) => a.name.CompareTo(b.name));
    }
}