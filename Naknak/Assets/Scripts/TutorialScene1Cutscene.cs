using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialScene1Cutscene : MonoBehaviour
{
    private struct CutsceneLine
    {
        public string speaker;
        public string text;
        public int activatedImage;

        public CutsceneLine(string speaker, string text, int activatedImage = 0)
        {
            this.speaker = speaker;
            this.text = text;
            this.activatedImage = activatedImage;
        }
    }

    [Header("Story Template")]
    [Tooltip("기존에 잘 작동하던 StoryData 하나를 여기에 넣기. 이벤트 없는 일반 대사 StoryData 추천.")]
    [SerializeField] private StoryData storyTemplate;

    [Header("Dialog View Check")]
    [SerializeField] private DialogViewer dialogView;

    [Tooltip("이 컷씬 스크립트가 Z/Enter/Space/Click 입력으로 DialogManager.NextDialog()를 호출할지 여부")]
    [SerializeField] private bool handleDialogInputHere = true;

    [Header("Actors")]
    [SerializeField] private Transform detective;

    [Header("Detective Move Points")]
    [SerializeField] private Transform detectiveStartPoint;
    [SerializeField] private Transform detectiveCenterPoint;
    [SerializeField] private float detectiveMoveTime = 1.2f;

    [Header("Input")]
    [SerializeField] private KeyCode nextKey = KeyCode.Z;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Scene Flow")]
    [SerializeField] private bool loadNextSceneAfterCutscene = false;
    [SerializeField] private string nextSceneName = "Tutorial_Scene2_MemoryIntro";

    private bool waitingForDialogEnd = false;

    private void Awake()
    {
        AutoFindDialogView();
    }

    private void Start()
    {
        StartCoroutine(RunCutscene());
    }

    private void Update()
    {
        if (!waitingForDialogEnd)
            return;

        if (!handleDialogInputHere)
            return;

        if (GetAdvanceInput())
        {
            if (DialogManager.Instance != null)
                DialogManager.Instance.NextDialog();
        }
    }

    private bool GetAdvanceInput()
    {
        if (Input.GetKeyDown(nextKey))
            return true;

        if (Input.GetKeyDown(KeyCode.Return))
            return true;

        if (Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;

        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        if (Input.GetMouseButtonDown(0))
            return true;

        return false;
    }

    private IEnumerator RunCutscene()
    {
        SetupStartState();

        StoryData beforeDetectiveStory = CreateStoryChain(
            "S1_Before",
            new CutsceneLine[]
            {
                new CutsceneLine("조수", "……없어요."),
                new CutsceneLine("전당포 주인", "뭐가?"),
                new CutsceneLine("조수", "브로치요. 제가 상자에 넣어서 드렸잖아요."),
                new CutsceneLine("전당포 주인", "분명히 넣었어! 소란 중에 누가 가져간 거야."),
                new CutsceneLine("A", "저를 의심하는 겁니까?"),
                new CutsceneLine("B", "두 분이 해결하시죠.")
            }
        );

        yield return PlayStory(beforeDetectiveStory);

        if (detective != null)
            detective.gameObject.SetActive(true);

        StoryData detectiveEnterStory = CreateStoryChain(
            "S1_Enter",
            new CutsceneLine[]
            {
                new CutsceneLine("주인공", "무슨 일임?"),
                new CutsceneLine("B", "당신은 또 누구임? 내 알바 아니니까 난 감 ㅂㅂ")
            }
        );

        yield return PlayStory(detectiveEnterStory);

        yield return MoveDetectiveToCenter();

        StoryData afterDetectiveMoveStory = CreateStoryChain(
            "S1_After",
            new CutsceneLine[]
            {
                new CutsceneLine("주인공", "나 탐정임 ㅇㅇ"),
                new CutsceneLine("조수", "브로치가 없어짐 ㅠㅠ"),
                new CutsceneLine("주인공", "다 못나가 지금 나가면 너가 범인임 ㅇㅇ"),
                new CutsceneLine("주인공", "알리바이 ㄱㄱ")
            }
        );

        yield return PlayStory(afterDetectiveMoveStory);

        yield return EndCutscene();
    }

    private void SetupStartState()
    {
        if (fadeImage != null)
            SetFadeAlpha(0f);

        if (detective != null && detectiveStartPoint != null)
        {
            detective.position = detectiveStartPoint.position;
            detective.gameObject.SetActive(false);
        }
    }

    private StoryData CreateStoryChain(string prefix, CutsceneLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return null;

        StoryData[] stories = new StoryData[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            StoryData story = CreateStoryDataInstance();

            story.name = prefix + "_" + i.ToString("00");
            story.charName = lines[i].speaker;
            story.text = lines[i].text;
            story.activatedImage = lines[i].activatedImage;

            story.isSelect = false;
            story.isEnd = false;

            stories[i] = story;
        }

        for (int i = 0; i < stories.Length; i++)
        {
            if (i < stories.Length - 1)
            {
                stories[i].nextData = stories[i + 1];
                stories[i].isEnd = false;
            }
            else
            {
                stories[i].nextData = null;
                stories[i].isEnd = true;
            }
        }

        return stories[0];
    }

    private StoryData CreateStoryDataInstance()
    {
        StoryData story;

        if (storyTemplate != null)
        {
            story = Instantiate(storyTemplate);
        }
        else
        {
            story = ScriptableObject.CreateInstance<StoryData>();
            Debug.LogWarning("[TutorialScene1Cutscene] Story Template이 비어 있습니다. 기존 StoryData 하나를 넣는 것을 추천합니다.");
        }

        return story;
    }

    private IEnumerator PlayStory(StoryData storyData)
    {
        if (storyData == null)
        {
            Debug.LogWarning("[TutorialScene1Cutscene] StoryData가 비어 있습니다.");
            yield break;
        }

        if (DialogManager.Instance == null)
        {
            Debug.LogError("[TutorialScene1Cutscene] DialogManager.Instance가 없습니다.");
            yield break;
        }

        if (dialogView == null)
            AutoFindDialogView();

        waitingForDialogEnd = true;

        DialogManager.Instance.ShowDialog(storyData);

        yield return null;

        if (dialogView != null)
        {
            float waitOpenTime = 0f;

            while (!dialogView.gameObject.activeSelf && waitOpenTime < 0.5f)
            {
                waitOpenTime += Time.deltaTime;
                yield return null;
            }

            while (dialogView.gameObject.activeSelf)
            {
                yield return null;
            }
        }
        else
        {
            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[TutorialScene1Cutscene] DialogViewer도 없고 GameStateManager도 없어 대사 종료를 감지할 수 없습니다.");
                waitingForDialogEnd = false;
                yield break;
            }

            while (GameStateManager.Instance.GameState == GameState.Dialog)
            {
                yield return null;
            }
        }

        waitingForDialogEnd = false;

        yield return null;
    }

    private IEnumerator MoveDetectiveToCenter()
    {
        if (detective == null || detectiveCenterPoint == null)
            yield break;

        Vector3 startPos = detective.position;
        Vector3 targetPos = detectiveCenterPoint.position;

        float elapsed = 0f;

        while (elapsed < detectiveMoveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / detectiveMoveTime);

            detective.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        detective.position = targetPos;
    }

    private IEnumerator EndCutscene()
    {
        yield return FadeToBlack(fadeOutTime);

        if (loadNextSceneAfterCutscene && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("[TutorialScene1Cutscene] S#1 컷씬 종료");
        }
    }

    private IEnumerator FadeToBlack(float duration)
    {
        if (fadeImage == null)
            yield break;

        float startAlpha = fadeImage.color.a;
        float targetAlpha = 1f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetFadeAlpha(alpha);

            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void AutoFindDialogView()
    {
        if (dialogView != null)
            return;

        DialogViewer[] views = FindObjectsByType<DialogViewer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (views.Length > 0)
            dialogView = views[0];
    }
}