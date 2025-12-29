using UnityEngine;
using UnityEditor;

public class DialogDashboard : EditorWindow
{
    // 탭
    private DialogStoryView storyView;
    private DialogTriggerView triggerView;

    // 탭 정보
    private int toolbarInt = 0;
    private string[] toolbarStrings = { "Story Editor", "Trigger Editor" };

    [MenuItem("Tools/Dialog Dashboard")]
    public static void ShowWindow()
    {
        GetWindow<DialogDashboard>("Dialog Dashboard");
    }

    private void OnEnable()
    {
        // 탭들 초기화
        storyView = new DialogStoryView();
        triggerView = new DialogTriggerView();

        storyView.OnEnable();
        triggerView.OnEnable();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(240));
        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings, EditorStyles.toolbarButton, GUILayout.Height(25));
        GUILayout.EndHorizontal();

        switch (toolbarInt)
        {
            case 0:
                storyView.Draw();
                break;
            case 1:
                triggerView.Draw();
                break;
        }

        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}