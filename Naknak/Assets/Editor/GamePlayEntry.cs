using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Play opens the title while Unity restores the working scene after Stop.</summary>
[InitializeOnLoad]
public static class GamePlayEntry
{
    static GamePlayEntry()
    {
        EditorApplication.delayCall += Apply;
        EditorApplication.playModeStateChanged += state =>
        { if (state == PlayModeStateChange.EnteredEditMode) Apply(); };
    }

    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var title = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
        if (title != null) EditorSceneManager.playModeStartScene = title;
    }
}
