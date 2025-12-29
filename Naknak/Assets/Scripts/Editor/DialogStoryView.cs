using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class DialogStoryView
{
    // 선택된 컨테이너/대사
    public DialogContainer selectedContainer;
    public StoryData selectedStory;
    
    public event Action OnRequestRefresh;

    // 분리된 패널
    private DialogStoryLeftPanel leftPanel;
    private DialogStoryRightPanel rightPanel;

    public void OnEnable()
    {
        leftPanel = new DialogStoryLeftPanel(this);
        rightPanel = new DialogStoryRightPanel(this);

        OnRequestRefresh += leftPanel.RefreshContainerList;

        leftPanel.OnEnable();
    }

    public void OnDisable()
    {
        OnRequestRefresh -= leftPanel.RefreshContainerList;
    }

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical("box", GUILayout.Width(320), GUILayout.ExpandHeight(true));
        leftPanel.Draw();
        GUILayout.EndVertical();

        // 오른쪽 패널 (인스펙터)
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
        rightPanel.Draw();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    public void RequestRefresh()
    {
        OnRequestRefresh?.Invoke();
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