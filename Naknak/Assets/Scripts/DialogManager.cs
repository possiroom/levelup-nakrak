using System.Collections.Generic;
using UnityEngine;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [SerializeField] private DialogViewer view;
    [SerializeField] private DialogSelectViewer selectView;
    [SerializeField] private float letterSpeed = 0f;

    private bool isSelectActivate;
    private int selectIndex;

    // Variables for Dialog Animation
    private bool isPlaying;
    private float elapedTime;
    private string showingText;

    // Singleton
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        elapedTime = 0f;
        showingText = "";
        isPlaying = false;
        isSelectActivate = false;
        selectIndex = 1;
    }

    private StoryData currentStory = null;

    private void ChangeUI()
    {
        isPlaying = true;
        elapedTime = 0f;
        showingText = "";
        view.ChangeName(currentStory.charName);
        view.ChangeActivateImage(currentStory.activatedImage);
        view.ActivateEndMark(false);
        Debug.Log("[DialogManager] Show Dialog : " + currentStory.text);
    }

    public void ShowDialog(StoryData storyData)
    {
        view.gameObject.SetActive(true);

        GameEventBase evt = GameEventFactory.CreateGameStateChangeEvent(GameState.Dialog);
        GameEventManager.Instance.Submit(evt);

        currentStory = storyData;

        ChangeUI();
    }

    /// <summary>
    /// 우선은 Z키를 다시 누르면 대사가 빨리 진행되어 스킵되는 걸로
    /// </summary>
    public void NextDialog()
    {
        // 대사 스킵
        if (isPlaying) { TextSkip(); return; }

        // 선택지 선택
        if (isSelectActivate)
        {
            Select();
            isSelectActivate = false;
            selectView.SelectDeactivate();
            selectView.gameObject.SetActive(false);

            // 만약 마지막 선택 후 Action을 위한 빈 대사라면
            if (currentStory.charName == "" && currentStory.text == "" && currentStory.isEnd)
            {
                TryExecuteActions();
                EndDialog();
            }
            return;
        }

        TryExecuteActions();

        if (currentStory.isEnd)
        {
            // 대사 종료
            EndDialog();
        } 
        else if (currentStory.isSelect)
        {
            // 선택지 활성화
            isSelectActivate = true;
            selectView.gameObject.SetActive(true);
            selectView.SelectActivate(currentStory.selects);

            selectIndex = 1;
            selectView.ChangeSelectIndex(selectIndex);
        }
        else
        {
            // 대사 종료 후 이벤트 호출
            TriggerExecutor.Instance.AfterDialogEvent(currentStory);

            // 다음 대사
            currentStory = currentStory.nextData;

            // 만약 마지막 선택 후 Action을 위한 빈 대사라면
            if (currentStory.charName == "" && currentStory.text == "" && currentStory.isEnd)
            {
                TryExecuteActions();
                EndDialog();
                return;
            }

            Debug.Log("[DialogManager] Story ID: " + currentStory.name);
            ChangeUI();
        }
    }

    private void Select()
    {
        selectView.SelectDeactivate();
        selectView.ChangeSelectIndex(selectIndex);

        TriggerExecutor.Instance.AfterDialogEvent(currentStory);

        currentStory = currentStory.selects[selectIndex - 1].nextData;
        ChangeUI();
    }

    private void Update()
    {
        if (isPlaying && showingText.Length < currentStory.text.Length)
        {
            elapedTime += Time.deltaTime;
            if (elapedTime >= letterSpeed)
            {
                if (showingText.Length < currentStory.text.Length)
                {
                    showingText += currentStory.text[showingText.Length];
                    view.ChangeStoryText(showingText);
                    elapedTime -= letterSpeed;
                }
                else
                {
                    TextSkip();
                }
            }
        }

        if (isSelectActivate)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // 원형 인덱스
                selectIndex = selectIndex - 1 < 1 ? currentStory.selects.Count : selectIndex - 1;
                selectView.ChangeSelectIndex(selectIndex);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                // 원형 인덱스
                selectIndex = selectIndex + 1 > currentStory.selects.Count ? 1 : selectIndex + 1;
                selectView.ChangeSelectIndex(selectIndex);
            }
        }
    }

    private void TextSkip()
    {
        showingText = currentStory.text;
        view.ChangeStoryText(showingText);
        view.ActivateEndMark(true);

        isPlaying = false;
        elapedTime = 0f;
        showingText = "";
    }

    private void EndDialog()
    {
        view.ChangeName("");
        view.ChangeStoryText("");
        view.gameObject.SetActive(false); 

        TriggerExecutor.Instance.AfterDialogEvent(currentStory);
        GameEventBase evt = GameEventFactory.CreateGameStateChangeEvent(GameState.Gameplay);
        GameEventManager.Instance.Submit(evt);
    }

    // Action 실행
    private void TryExecuteActions()
    {
        if (currentStory != null && currentStory.endActions != null && currentStory.endActions.Count > 0)
        {
            ConditionManager.Instance.ExecuteActions(currentStory.endActions);
            Debug.Log($"[DialogManager] Action Executed for: {currentStory.name}");
        }
    }
}
