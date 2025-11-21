using System.Collections.Generic;

using UnityEngine;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }
    public bool IsDialogActivate => _isDialogActivate;


    [SerializeField] private DialogViewer view;
    private bool _isDialogActivate;

    
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
        text:           "잠깐, 저기 덤불 사이에 반짝이는 거 뭐야?",
        name:           "민서", 
        activatedImage: 1, 
        isSelect:       false, 
        nextStoryID:    "test2"
    );

    private readonly StoryTextData test2 = new StoryTextData(
        storyID:        "test2", 
        text:           "어디? 난 풀밖에 안 보이는데...",
        name:           "진수", 
        activatedImage: 2, 
        isSelect:       false, 
        nextStoryID:    "test3"
    );

    private readonly StoryTextData test3 =new StoryTextData(
        storyID:        "test3", 
        text:           "자세히 봐봐. 오래된 열쇠 같은 게 떨어져 있어.",
        name:           "민서", 
        activatedImage: 1, 
        isSelect:       false, 
        nextStoryID:    "test4"
    );

    private readonly StoryTextData test4 = new StoryTextData(
        storyID:        "test4", 
        text:           "진짜네? 함부로 만지면 위험하니까 조심해.", 
        name:           "민수", 
        activatedImage: 2, 
        isSelect:       false, 
        nextStoryID:    "<END>"
    );

    void Start()
    {
        storyStore.Add(test1);
        storyStore.Add(test2);
        storyStore.Add(test3);
        storyStore.Add(test4);

        _isDialogActivate = false;
    }

    private StoryTextData currentStory = null;

    // OverLoading, 여기 수정 필요
    public void ShowDialog()
    {
        view.ChangeName(currentStory.name);
        view.ChangeStoryText(currentStory.text);
        view.ChangeActivateImage(currentStory.activatedImage);
        Debug.Log("[DialogManager] Show Dialog : " + currentStory.text);
    }

    public void ShowDialog(string storyID)
    {
        view.gameObject.SetActive(true); _isDialogActivate = true;
        currentStory = GetStoryTextData(storyID);

        ShowDialog();
    }

    public void NextDialog()
    {
        if (currentStory.nextStoryID == "<END>")
        {
            EndDialog();
        } 
        else
        {
            currentStory = GetStoryTextData(currentStory.nextStoryID);
            Debug.Log("[DialogManager] Story ID: " + currentStory.storyID);
            ShowDialog();
        }
    }

    public void EndDialog()
    {
        view.ChangeName("");
        view.ChangeStoryText("");
        view.gameObject.SetActive(false); _isDialogActivate = false;
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
