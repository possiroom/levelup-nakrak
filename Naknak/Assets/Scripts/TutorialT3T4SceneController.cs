using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialT3T4SceneController : MonoBehaviour
{
    private struct CutsceneLine
    {
        public string speaker;
        public string text;
        public int activatedImage;
        public string portraitResource;

        public CutsceneLine(string speaker, string text, int activatedImage = 0, string portraitResource = null)
        {
            this.speaker = speaker;
            this.text = text;
            this.activatedImage = activatedImage;
            this.portraitResource = portraitResource;
        }
    }

    public enum TimelineSceneType
    {
        T3_VaseBreak,
        T4_BroochMissing
    }

    [Header("Scene Type")]
    [SerializeField] private TimelineSceneType sceneType;

    [Header("Dialog")]
    [SerializeField] private DialogViewer dialogView;
    [SerializeField] private bool handleDialogInputHere = true;
    [SerializeField] private KeyCode nextKey = KeyCode.Z;

    [Header("T3 - Story")]
    [SerializeField] private DialogContainer t3ReactionDialog;
    [SerializeField] private ConditionID t3Occurred;
    public enum T3Phase { WaitingForServices, Flicker, Dark, VaseBroken, Reaction, Finished }
    public T3Phase Phase { get; private set; }
    private bool completionSent;

    [Header("T3 - Dark Overlay")]
    [Tooltip("화면 전체를 덮는 검은 UI Image")]
    [SerializeField] private Image darkOverlayImage;
    [Tooltip("이전 씬에서 암전한 상태로 진입하고, 불이 다시 켜질 때까지 유지합니다.")]
    [SerializeField] private bool startWithLightsOff = true;

    [Header("T3 - Flicker")]
    [SerializeField] private int flickerCount = 3;
    [SerializeField, Range(0f, 1f)] private float flickerAlpha = 0.45f;
    [SerializeField] private float flickerDarkTime = 0.07f;
    [SerializeField] private float flickerLightTime = 0.09f;

    [Header("T3 - Timing")]
    [SerializeField] private float waitBeforeBreak = 0.5f;
    [SerializeField] private float darkFadeInTime = 0.25f;
    [SerializeField] private float waitAfterLightsOffBeforeBreak = 0.75f;
    [SerializeField] private float darkFadeOutTime = 0.45f;
    [SerializeField, Min(0f)] private float waitAfterDarkReaction = 2f;
    [SerializeField, Min(0f)] private float extraBlackoutHold = 1f;
    [SerializeField] private bool restoreLightAfterBreak = true;

    [Header("T3 - Vase Break")]
    [SerializeField] private GameObject vaseNormal;
    [SerializeField] private GameObject vaseBroken;
    [SerializeField] private GameObject t3InvestigationObjects;

    [Header("T3 - Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip vaseBreakClip;

    [Header("T4 - Brooch Missing")]
    [SerializeField] private GameObject emptyJewelryBoxObject;
    [SerializeField] private bool showEmptyJewelryBoxAfterDialog = true;

    [Header("Scene Flow")]
    [SerializeField] private bool loadNextSceneAfterEnd = false;
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private float waitBeforeLoadNextScene = 0.3f;

    private bool waitingForDialogEnd = false;

    private void Awake()
    {
        AutoFindDialogView();
        AutoFindAudioSource();
        if (sceneType == TimelineSceneType.T3_VaseBreak && startWithLightsOff)
            SetDarkOverlayAlpha(1f);
    }

    private IEnumerator Start()
    {
        yield return null;

        if (sceneType == TimelineSceneType.T3_VaseBreak)
        {
            Phase = T3Phase.WaitingForServices;
            while (SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession || SceneLoadManager.IsPreparingMemory || GameMenuController.IsBlockingGameplay ||
                   DialogManager.Instance == null || !DialogManager.Instance.CanShowDialog || DialogManager.Instance.IsDialogOpen)
                yield return null;
            DialogManager.Instance.BindSceneViews(gameObject.scene);
            if (SceneLoadManager.IsMemoryRevisit)
            {
                SetupT3StartState();
                if (vaseNormal != null) vaseNormal.SetActive(false);
                if (vaseBroken != null) vaseBroken.SetActive(true);
                if (t3Occurred != null) ConditionManager.Instance.Set(t3Occurred, 1);
                SetDarkOverlayAlpha(0.68f);
                if (darkOverlayImage != null) darkOverlayImage.raycastTarget = false;
                if (t3InvestigationObjects != null) t3InvestigationObjects.SetActive(true);
                Phase = T3Phase.Finished;
                SceneLoadManager.Instance?.SetPlayerInputLocked(false);
                yield break;
            }
        }

        yield return RunScene();
    }

    private void Update()
    {
        // Managed scenes have one input owner; otherwise one key advances twice.
        if (SceneLoadManager.HasManagedScene || GameMenuController.IsBlockingGameplay) return;
        if (!waitingForDialogEnd)
            return;

        if (!handleDialogInputHere)
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

    private IEnumerator RunScene()
    {
        if (sceneType == TimelineSceneType.T3_VaseBreak)
        {
            yield return RunT3();
        }
        else if (sceneType == TimelineSceneType.T4_BroochMissing)
        {
            yield return RunT4();
        }

        if (sceneType == TimelineSceneType.T3_VaseBreak && Phase == T3Phase.Finished && SceneLoadManager.HasManagedScene)
        {
            yield return new WaitForSeconds(waitBeforeLoadNextScene);
            if (!completionSent)
            {
                completionSent = true;
                if (!SceneLoadManager.Instance.TryCompleteScene())
                    Debug.LogWarning("[T3] Scene Flow Container의 T3 완료 연결을 확인해주세요.", this);
            }
            yield break;
        }

        if (loadNextSceneAfterEnd && !string.IsNullOrEmpty(nextSceneName))
        {
            yield return new WaitForSeconds(waitBeforeLoadNextScene);
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator RunT3()
    {
        SetupT3StartState();

        if (!startWithLightsOff)
        {
            yield return new WaitForSeconds(waitBeforeBreak);
            Phase = T3Phase.Flicker;
            yield return FlickerLight();
            yield return FadeDarkOverlay(1f, darkFadeInTime);
        }
        Phase = T3Phase.Dark;

        // Both first visits and revisits fully hide the world beneath the dialogue.
        Phase = T3Phase.Reaction;
        yield return PlayStory(CreateStoryChain("T3_LightsOff", new[]
        {
            new CutsceneLine("전당포 주인", "뭐, 뭐야?")
        }));

        float breakDelay = Mathf.Min(waitAfterLightsOffBeforeBreak, waitAfterDarkReaction);
        yield return new WaitForSeconds(breakDelay);

        if (vaseNormal != null)
            vaseNormal.SetActive(false);

        if (vaseBroken != null)
            vaseBroken.SetActive(true);

        Phase = T3Phase.VaseBroken;
        if (t3Occurred != null && ConditionManager.Instance != null) ConditionManager.Instance.Set(t3Occurred, 1);
        PlayVaseBreakSound();

        yield return new WaitForSeconds(Mathf.Max(0, waitAfterDarkReaction - breakDelay) + extraBlackoutHold);

        if (restoreLightAfterBreak)
            yield return FadeDarkOverlay(0f, darkFadeOutTime);

        StoryData t3Story = t3ReactionDialog != null ? t3ReactionDialog.GetMatchingDialog() : CreateStoryChain(
            "T3",
            new CutsceneLine[]
            {
                new CutsceneLine("전당포 주인", "이런 젠장! 누가 건드렸어?")
            }
        );

        Phase = T3Phase.Reaction;
        if (t3Story == null)
        {
            Debug.LogError("[T3] 반응 대화 컨테이너의 시작 대사와 조건을 확인해주세요.", this);
            yield break;
        }
        waitingForDialogEnd = true;
        DialogManager.Instance.ShowDialog(t3Story, true);
        yield return null;
        while (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
        waitingForDialogEnd = false;

        if (t3InvestigationObjects != null)
            t3InvestigationObjects.SetActive(true);

        Phase = T3Phase.Finished;
        Debug.Log("[TutorialT3T4SceneController] T3 종료. 조사 가능 상태.");
    }

    private IEnumerator RunT4()
    {
        SetupT4StartState();

        StoryData t4Story = CreateStoryChain(
            "T4",
            new CutsceneLine[]
            {
                new CutsceneLine("조수", "잠깐만요. 제 브로치부터 돌려주세요."),
                new CutsceneLine("주인", "아, 그래. 여기 있소."),
                new CutsceneLine("", "[브로치가 없는 보석함 아이템을 획득한다.]"),
                new CutsceneLine("조수", "…….", portraitResource: "조수 당황"),
                new CutsceneLine("조수", "브로치가 없는데요.", portraitResource: "조수 당황"),
                new CutsceneLine("주인", "뭐?"),
                new CutsceneLine("조수", "분명 여기 넣어뒀잖아요.", portraitResource: "조수 화남"),
                new CutsceneLine("주인", "난 그대로 돌려줬소."),
                new CutsceneLine("조수", "지금 장난하세요?", portraitResource: "조수 화남"),
                new CutsceneLine("주인", "나한테 따져봐야 소용없소!")
            }
        );

        yield return PlayStory(t4Story);

        if (showEmptyJewelryBoxAfterDialog && emptyJewelryBoxObject != null)
            emptyJewelryBoxObject.SetActive(true);

        Debug.Log("[TutorialT3T4SceneController] T4 종료. 플레이어 이동 가능 상태.");
    }

    private void SetupT3StartState()
    {
        // Hide the world while keeping the existing dialogue canvas (order 0) readable.
        if (darkOverlayImage != null)
        {
            var canvas = darkOverlayImage.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.sortingOrder = -1;
        }
        SetDarkOverlayAlpha(startWithLightsOff ? 1f : 0f);

        if (vaseNormal != null)
            vaseNormal.SetActive(true);

        if (vaseBroken != null)
            vaseBroken.SetActive(false);

        if (t3InvestigationObjects != null)
            t3InvestigationObjects.SetActive(false);
    }

    private void SetupT4StartState()
    {
        if (emptyJewelryBoxObject != null)
            emptyJewelryBoxObject.SetActive(false);
    }

    private IEnumerator FlickerLight()
    {
        if (darkOverlayImage == null)
            yield break;

        for (int i = 0; i < flickerCount; i++)
        {
            yield return FadeDarkOverlay(flickerAlpha, flickerDarkTime);
            yield return FadeDarkOverlay(0f, flickerLightTime);
        }
    }

    private IEnumerator FadeDarkOverlay(float targetAlpha, float duration)
    {
        if (darkOverlayImage == null)
            yield break;

        float startAlpha = darkOverlayImage.color.a;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            SetDarkOverlayAlpha(targetAlpha);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetDarkOverlayAlpha(alpha);

            yield return null;
        }

        SetDarkOverlayAlpha(targetAlpha);
    }

    private void SetDarkOverlayAlpha(float alpha)
    {
        if (darkOverlayImage == null)
            return;

        Color color = darkOverlayImage.color;
        color.a = alpha;
        darkOverlayImage.color = color;
    }

    private void PlayVaseBreakSound()
    {
        AutoFindAudioSource();

        if (vaseBreakClip == null)
        {
            Debug.LogWarning("[TutorialT3T4SceneController] Vase Break Clip이 비어 있습니다. 깨지는 효과음이 재생되지 않습니다.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("[TutorialT3T4SceneController] AudioSource가 없습니다. 깨지는 효과음이 재생되지 않습니다.");
            return;
        }

        audioSource.PlayOneShot(vaseBreakClip);
    }

    private void AutoFindAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private StoryData CreateStoryChain(string prefix, CutsceneLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return null;

        StoryData[] stories = new StoryData[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            StoryData story = ScriptableObject.CreateInstance<StoryData>();

            ClearStorySideEffects(story);

            story.name = prefix + "_" + i.ToString("00");
            story.charName = lines[i].speaker;
            story.text = lines[i].text;
            story.activatedImage = lines[i].activatedImage;
            if (!string.IsNullOrEmpty(lines[i].portraitResource))
            {
                var sprites = Resources.LoadAll<Sprite>("Portraits/" + lines[i].portraitResource);
                if (sprites.Length > 0) story.portrait = sprites[0];
            }

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
            Debug.LogWarning("[TutorialT3T4SceneController] StoryData가 비어 있습니다.");
            yield break;
        }

        if (DialogManager.Instance == null)
        {
            Debug.LogError("[TutorialT3T4SceneController] DialogManager.Instance가 없습니다.");
            yield break;
        }

        if (dialogView == null)
            AutoFindDialogView();

        waitingForDialogEnd = true;

        DialogManager.Instance.ShowDialog(storyData, true);

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
                Debug.LogError("[TutorialT3T4SceneController] DialogViewer도 없고 GameStateManager도 없어 대사 종료를 감지할 수 없습니다.");
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
