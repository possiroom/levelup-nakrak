using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

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

    private static List<StoryTextData> storyStore = new();

    private readonly StoryTextData test1 = new StoryTextData(
        storyID:        "test1", 
        text:           "안녕 네 이름은 뭐니?",
        name:           "민서", 
        activatedImage: 2, 
        isSelect:       false, 
        nextStoryID:    "test2"
    );

    private StorySelectData text2_1 = new StorySelectData(
        text:       "민서",
        nextStoryID: "test3"
    );

    private StorySelectData text2_2 = new StorySelectData(
        text:       "민수",
        nextStoryID: "test5"
    );

    private StoryTextData test2 = new StoryTextData(
        storyID:        "test2", 
        text:           "내 이름은 진수야. 네 이름은?",
        name:           "진수", 
        activatedImage: 1, 
        isSelect:       true, 
        nextStoryID:    "<SELECT>",
        selects:        new List<StorySelectData>()
    );

    private readonly StoryTextData test3 =new StoryTextData(
        storyID:        "test3", 
        text:           "오 네 이름은 민서구나 반가워.",
        name:           "진수", 
        activatedImage: 1, 
        isSelect:       false, 
        nextStoryID:    "test4"
    );

    private readonly StoryTextData test4 = new StoryTextData(
        storyID:        "test4", 
        text:           "그래 반가워 진수야.", 
        name:           "민서", 
        activatedImage: 2, 
        isSelect:       false, 
        nextStoryID:    "<END>"
    );

    private readonly StoryTextData test5 =new StoryTextData(
        storyID:        "test5", 
        text:           "거짓말 치지 마. 너 이름 민서인 거 다 보여.",
        name:           "진수", 
        activatedImage: 1, 
        isSelect:       false, 
        nextStoryID:    "test6"
    );

    private readonly StoryTextData test6 = new StoryTextData(
        storyID:        "test6", 
        text:           "헐 들켰네. 거짓말 쳐서 미안해.", 
        name:           "민서", 
        activatedImage: 2, 
        isSelect:       false, 
        nextStoryID:    "<END>"
    );

    void Start()
    {
        storyStore.Add(test1);

        test2.AddSelect(text2_1);
        test2.AddSelect(text2_2);

        text2_1.SetNextData(test3);
        text2_2.SetNextData(test5);

        storyStore.Add(test2);
        storyStore.Add(test3);
        storyStore.Add(test4);
        storyStore.Add(test5);
        storyStore.Add(test6);
        
        MatchStoryData();

        elapedTime = 0f;
        showingText = "";
        isPlaying = false;
        isSelectActivate = false;
        selectIndex = 1;
    }

    private StoryTextData currentStory = null;

    private void ChangeUI()
    {
        isPlaying = true;
        elapedTime = 0f;
        showingText = "";
        view.ChangeName(currentStory.name);
        view.ChangeActivateImage(currentStory.activatedImage);
        view.ActivateEndMark(false);
        Debug.Log("[DialogManager] Show Dialog : " + currentStory.text);
    }

    public void ShowDialog(string storyID)
    {
        view.gameObject.SetActive(true);

        GameEventBase evt = GameEventFactory.CreateGameStateChangeEvent(GameState.Dialog);
        GameEventManager.Instance.Submit(evt);

        currentStory = GetStoryTextData(storyID);

        ChangeUI();
    }

    /// <summary>
    /// 우선은 Z키를 다시 누르면 대사가 빨리 진행되어 스킵되는 걸로
    /// </summary>
    public void NextDialog()
    {
        if (isPlaying) { TextSkip(); return; }

        if (isSelectActivate)
        {
            Select();
            isSelectActivate = false;
            selectView.SelectDeactivate();
            selectView.gameObject.SetActive(false);
            return;
        }

        if (currentStory.nextData != null)
        {
            currentStory = currentStory.nextData;
            Debug.Log("[DialogManager] Story ID: " + currentStory.storyID);
            ChangeUI();
        } 
        else
        {
            if (currentStory.nextStoryID == "<SELECT>")
            {
                selectView.gameObject.SetActive(true);
                selectView.SelectActivate(currentStory.selects);
                isSelectActivate = true;
            }
            else
            {
                EndDialog();
            }
        }
    }

    private void Select()
    {
        selectView.SelectDeactivate();
        selectView.ChangeSelectIndex(selectIndex);

        currentStory = currentStory.selects[selectIndex - 1].nextData;
        ChangeUI();
    }

    private void Update()
    {
        if (isPlaying)
        {
            elapedTime += Time.deltaTime;
            if (elapedTime >= letterSpeed)
            {
                showingText += currentStory.text[showingText.Length];
                view.ChangeStoryText(showingText);
                if (showingText.Length == currentStory.text.Length)
                {
                    TextSkip();
                } 
                else 
                {
                    elapedTime -= letterSpeed;  
                }
            }
        }

        if (isSelectActivate)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // Modulation
                selectIndex = selectIndex + 1 > currentStory.selects.Count ? 1 : selectIndex + 1;
                selectView.ChangeSelectIndex(selectIndex);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectIndex = selectIndex - 1 < 1 ? currentStory.selects.Count : selectIndex - 1;
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

        GameEventBase evt = GameEventFactory.CreateGameStateChangeEvent(GameState.Gameplay);
        GameEventManager.Instance.Submit(evt);
    }

    /// <summary>
    /// Call Only Start, 추후 handler로 분리할 예정
    /// </summary>
    private void MatchStoryData()
    {
        for (int i = 0; i < storyStore.Count; i++)
        {
            StoryTextData storyTextData = storyStore[i];
            storyTextData.SetNextData(SearchNextStoryData(storyTextData, i));
        }
    }

    private StoryTextData SearchNextStoryData(StoryTextData storyTextData, int index)
    {
        if (storyTextData.nextStoryID == "<END>") return null;
        if (storyTextData.nextStoryID == "<SKIP>") return null;
        if (storyTextData.nextStoryID == "<SELECT>") return null;
        if (storyTextData.nextStoryID == "<NULL>") return null;

        int len = storyStore.Count;
        for (int j = (index + 1) % len; j != index; j = (j + 1) % len)
        {
            Debug.Log("search try: " + j);
            if (storyTextData.nextStoryID == storyStore[j].storyID) 
                return storyStore[j];
        }

        Debug.LogWarning("[DialogManager] Next Story Data Not Found : " + storyTextData.nextStoryID);
        return null;
    }

    private StoryTextData GetStoryTextData(string storyID)
    {
        foreach (StoryTextData storyTextData in storyStore)
        {
            if (storyTextData.storyID == storyID)
            {
                return storyTextData;
            }
        }

        Debug.LogError("[DialogManager] Story Data Not Found : " + storyID);
        return null;
    }
}
