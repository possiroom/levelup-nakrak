using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialS6EventController : MonoBehaviour
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
    [Tooltip("기존에 잘 작동하던 일반 StoryData 하나를 넣기. 비워도 작동함.")]
    [SerializeField] private StoryData storyTemplate;

    [Header("Dialog")]
    [SerializeField] private DialogViewer dialogView;
    [SerializeField] private DialogSelectViewer dialogSelectView;
    [SerializeField] private bool forceRebindDialogManagerUI = true;

    [Tooltip("이 스크립트가 Z/Enter/Space/Click으로 DialogManager.NextDialog() 호출")]
    [SerializeField] private bool handleDialogInputHere = true;

    [SerializeField] private KeyCode nextKey = KeyCode.Z;

    [Tooltip("대사 시작 직후 입력이 바로 먹히는 것 방지")]
    [SerializeField] private float inputLockAfterDialogStart = 0.2f;

    [Header("Scene Flow")]
    [SerializeField] private bool loadNextSceneAfterEnd = false;
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private float waitBeforeLoadNextScene = 0.3f;

    private bool waitingForDialogEnd = false;
    private float inputLockUntil = 0f;

    private void Awake()
    {
        AutoFindDialogUI();
    }

    private IEnumerator Start()
    {
        yield return null;

        yield return RunS6();
    }

    private void Update()
    {
        if (SceneLoadManager.HasManagedScene) return;
        if (!waitingForDialogEnd)
            return;

        if (!handleDialogInputHere)
            return;

        if (Time.unscaledTime < inputLockUntil)
            return;

        if (GetAdvanceInput())
        {
            if (DialogManager.Instance != null)
                DialogManager.Instance.AdvanceFromInput();
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

    private IEnumerator RunS6()
    {
        while (GameMenuController.IsBlockingGameplay) yield return null;
        StoryData s6Story = CreateStoryChain(
            "S6",
            new CutsceneLine[]
            {
                new CutsceneLine("주인공", "A와 B가 숨기고 있던 건 확인했습니다."),
                new CutsceneLine("주인", "그래서요?"),
                new CutsceneLine("주인공", "둘 다 수상하긴 했지만, 브로치가 사라진 이유는 설명되지 않더군요."),
                new CutsceneLine("주인공", "사건이 일어난 시점에 카운터에 있던 사람은 두 사람뿐입니다."),
                new CutsceneLine("주인", "……."),
                new CutsceneLine("주인공", "하나만 다시 묻죠."),
                new CutsceneLine("주인공", "브로치의 행방을 알려주시죠."),
                new CutsceneLine("주인", "나는 모르는 일일세."),
                new CutsceneLine("주인공", "이보세요! 이건 당신이 범인이라고 광고하는 꼴이야!"),
                new CutsceneLine("주인공", "어쩔 수 없다. 불이 꺼진 시점의 전당포 주인을 조사해보자."),

                new CutsceneLine("주인공", "상자를 떨어뜨렸군요."),
                new CutsceneLine("주인", "그걸 어떻게…"),
                new CutsceneLine("주인", "……"),
                new CutsceneLine("주인", "바로 주워서 확인했는데… 브로치가 안 보였어."),
                new CutsceneLine("주인공", "그런데 그대로 돌려줬군요."),
                new CutsceneLine("주인", "내가 잃어버렸으니 변상해야되잖소."),
                new CutsceneLine("주인", "그래서 그냥… 모른 척했어."),
                new CutsceneLine("조수", "그럼 처음부터 빈 상자를 준 거잖아요."),
                new CutsceneLine("주인", "……그렇소."),
                new CutsceneLine("주인", "제발 도와주시오! 내가 가져간 게 아니란 말이오!")
            }
        );

        yield return PlayStory(s6Story);

        Debug.Log("[TutorialS6EventController] S6 종료");

        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.TryCompleteScene()) yield break;

        if (loadNextSceneAfterEnd && !string.IsNullOrEmpty(nextSceneName))
        {
            yield return new WaitForSeconds(waitBeforeLoadNextScene);
            if (SceneLoadManager.Instance != null) yield return SceneLoadManager.Instance.LoadContent(nextSceneName);
            else SceneManager.LoadScene(nextSceneName);
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

            ClearStorySideEffects(story);

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
        if (storyTemplate != null)
            return Instantiate(storyTemplate);

        return ScriptableObject.CreateInstance<StoryData>();
    }

    private void ClearStorySideEffects(StoryData story)
    {
        if (story == null)
            return;

        if (story.endActions != null)
            story.endActions.Clear();

        if (story.selects != null)
            story.selects.Clear();

        story.isSelect = false;
        story.nextData = null;
        story.isEnd = false;
    }

    private IEnumerator PlayStory(StoryData storyData)
    {
        if (storyData == null)
        {
            Debug.LogWarning("[TutorialS6EventController] StoryData가 비어 있습니다.");
            yield break;
        }

        if (DialogManager.Instance == null)
        {
            Debug.LogError("[TutorialS6EventController] DialogManager.Instance가 없습니다. 프로토타입 씬의 DialogManager가 살아있는지 확인하세요.");
            yield break;
        }

        AutoFindDialogUI();

        if (forceRebindDialogManagerUI)
            ForceRebindDialogManagerUI();

        waitingForDialogEnd = true;
        inputLockUntil = Time.unscaledTime + inputLockAfterDialogStart;

        DialogManager.Instance.ShowDialog(storyData, true);

        yield return null;

        if (GameStateManager.Instance != null)
        {
            float waitDialogEnterTime = 0f;

            while (GameStateManager.Instance.GameState != GameState.Dialog && waitDialogEnterTime < 0.5f)
            {
                waitDialogEnterTime += Time.deltaTime;
                yield return null;
            }

            while (GameStateManager.Instance.GameState == GameState.Dialog)
                yield return null;
        }
        else if (dialogView != null)
        {
            float waitOpenTime = 0f;

            while (dialogView != null && !dialogView.gameObject.activeSelf && waitOpenTime < 0.5f)
            {
                waitOpenTime += Time.deltaTime;
                yield return null;
            }

            while (dialogView != null && dialogView.gameObject.activeSelf)
                yield return null;
        }
        else
        {
            Debug.LogWarning("[TutorialS6EventController] GameStateManager와 DialogViewer를 둘 다 찾지 못했습니다.");
            yield return new WaitForSeconds(0.5f);
        }

        waitingForDialogEnd = false;

        yield return null;
    }

    private void AutoFindDialogUI()
    {
        DialogViewer[] views = FindObjectsByType<DialogViewer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (views.Length > 0)
            dialogView = views[0];

        DialogSelectViewer[] selectViews = FindObjectsByType<DialogSelectViewer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (selectViews.Length > 0)
            dialogSelectView = selectViews[0];
    }

    private void ForceRebindDialogManagerUI()
    {
        if (DialogManager.Instance == null)
            return;

        AutoFindDialogUI();

        if (dialogView == null)
        {
            Debug.LogWarning("[TutorialS6EventController] DialogViewer를 찾지 못해서 DialogManager UI 재연결을 건너뜁니다.");
            return;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        FieldInfo viewField = typeof(DialogManager).GetField("view", flags);
        if (viewField != null)
            viewField.SetValue(DialogManager.Instance, dialogView);

        if (dialogSelectView != null)
        {
            FieldInfo selectViewField = typeof(DialogManager).GetField("selectView", flags);
            if (selectViewField != null)
                selectViewField.SetValue(DialogManager.Instance, dialogSelectView);
        }

        Debug.Log("[TutorialS6EventController] DialogManager UI 재연결 완료");
    }
}