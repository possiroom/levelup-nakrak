using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool allowFastForward;
    private bool inputReady;
    private float elapedTime;
    private string showingText;
    private float autoAdvanceTime;
    private AudioSource storyAudio;
    private AudioClip doorSlamClip;

    // Singleton
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += RebindSceneViews;
        } else {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (currentStory != null) return;
        elapedTime = 0f;
        showingText = "";
        isPlaying = false;
        isSelectActivate = false;
        selectIndex = 1;
    }

    private StoryData currentStory = null;
    public StoryData CurrentStory => currentStory;
    public event System.Action<StoryData> DialogueFinished;
    private DialogViewer fallbackView;
    private DialogSelectViewer fallbackSelectView;
    public bool IsDialogOpen => currentStory != null && view != null && view.gameObject.activeSelf;
    public bool CanShowDialog => view != null && GameEventManager.Instance != null;
    public bool CanFastForward => IsDialogOpen && allowFastForward;
    public Canvas DialogueCanvas => view != null ? view.GetComponentInParent<Canvas>() : null;

    // Holding Ctrl follows the selected choice (the first choice by default), including its actions.
    public void SkipUntilChoice()
    {
        if (GameMenuController.IsBlockingGameplay || !CanFastForward) return;
        var visited = new HashSet<StoryData>();
        while (currentStory != null && !SceneLoadManager.IsLoading)
        {
            if (TutorialDeductionTrack.IsMoving) return;
            if (!isSelectActivate && !visited.Add(currentStory)) return;
            if (isPlaying) TextSkip();
            NextDialog();
        }
    }

    public void BindSceneViews(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var sceneView = root.GetComponentInChildren<DialogViewer>(true);
            var sceneSelect = root.GetComponentInChildren<DialogSelectViewer>(true);
            if (sceneView != null) view = sceneView;
            if (sceneSelect != null) selectView = sceneSelect;
        }
        if (scene.name == "PrototypeScene")
        {
            if (view != null && view.gameObject.scene == scene) fallbackView = view;
            if (selectView != null && selectView.gameObject.scene == scene) fallbackSelectView = selectView;
        }
        if (view == null) view = fallbackView;
        if (selectView == null) selectView = fallbackSelectView;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= RebindSceneViews;
        Instance = null;
        if (doorSlamClip != null) Destroy(doorSlamClip);
    }

    private void RebindSceneViews(Scene scene, LoadSceneMode mode)
    {
        if (view == null) view = FindFirstObjectByType<DialogViewer>(FindObjectsInactive.Include);
        if (selectView == null) selectView = FindFirstObjectByType<DialogSelectViewer>(FindObjectsInactive.Include);
        if (scene.name == "PrototypeScene") { fallbackView = view; fallbackSelectView = selectView; }
    }

    private void ChangeUI()
    {
        isPlaying = true;
        elapedTime = 0f;
        showingText = "";
        autoAdvanceTime = 0;
        if (currentStory.doorSlam) PlayDoorSlam();
        view.ChangeName(currentStory.charName);
        view.ChangePortrait(currentStory);
        view.ActivateEndMark(false);
        Debug.Log("[DialogManager] Show Dialog: " + currentStory.name);
    }

    public void ShowDialog(StoryData storyData) => ShowDialog(storyData, false);

    public void ShowDialog(StoryData storyData, bool canFastForward)
    {
        if (storyData == null || view == null) return;
        InteractionInput.ConsumeConfirm();
        view.gameObject.SetActive(true);

        GameEventBase evt = GameEventFactory.CreateGameStateChangeEvent(GameState.Dialog);
        GameEventManager.Instance.Submit(evt);

        currentStory = storyData;
        // Investigation, inventory and evidence lines require a fresh confirmation.
        // Only explicitly marked story sequences support holding Ctrl to fast-forward.
        allowFastForward = canFastForward;
        inputReady = false;

        ChangeUI();
    }

    // Opening a dialogue and advancing it must be separate presses, even when
    // interaction finishes on a later frame or the operating system repeats a key.
    public void AdvanceFromInput()
    {
        if (!inputReady || !IsDialogOpen) return;
        inputReady = false;
        NextDialog();
    }

    /// <summary>
    /// 우선은 Z키를 다시 누르면 대사가 빨리 진행되어 스킵되는 걸로
    /// </summary>
    public void NextDialog()
    {
        if (GameMenuController.IsBlockingGameplay || currentStory == null) return;
        InteractionInput.ConsumeConfirm();
        // 대사 스킵
        if (isPlaying) { TextSkip(); return; }
        if (TutorialDeductionTrack.IsMoving) return;

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
            if (currentStory == null) { EndDialog(); return; }

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
        if (GameMenuController.IsBlockingGameplay) return;
        if (currentStory != null && currentStory.autoAdvanceAfter > 0)
        {
            autoAdvanceTime += Time.deltaTime;
            if (autoAdvanceTime >= currentStory.autoAdvanceAfter)
            {
                if (isPlaying) TextSkip();
                NextDialog();
                return;
            }
        }
        if (IsDialogOpen && InteractionInput.CanConfirm &&
            !Input.GetKey(KeyCode.Z) && !Input.GetKey(KeyCode.Return) &&
            !Input.GetKey(KeyCode.KeypadEnter) && !Input.GetKey(KeyCode.Space) &&
            !Input.GetMouseButton(0)) inputReady = true;
        if (isPlaying && currentStory != null && showingText.Length >= currentStory.text.Length) TextSkip();
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

    private void PlayDoorSlam()
    {
        if (storyAudio == null)
        {
            storyAudio = gameObject.AddComponent<AudioSource>();
            storyAudio.playOnAwake = false;
            storyAudio.spatialBlend = 0;
        }
        if (doorSlamClip == null)
        {
            const int rate = 44100;
            var samples = new float[rate / 2];
            var random = new System.Random(41);
            float low = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)rate;
                low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, 0.16f);
                float body = Mathf.Sin(2 * Mathf.PI * 76 * time) * Mathf.Exp(-time * 19);
                samples[i] = Mathf.Clamp((low * 1.8f + body * 0.65f) * Mathf.Exp(-time * 12), -1, 1);
            }
            doorSlamClip = AudioClip.Create("Door slam", samples.Length, 1, rate, false);
            doorSlamClip.SetData(samples, 0);
        }
        storyAudio.PlayOneShot(doorSlamClip, 0.7f);
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
        InteractionInput.ConsumeConfirm();
        var completed = currentStory;
        allowFastForward = false;
        isPlaying = false;
        isSelectActivate = false;
        if (selectView != null) selectView.gameObject.SetActive(false);
        view.ChangeName("");
        view.ChangeStoryText("");
        view.gameObject.SetActive(false); 

        TriggerExecutor.Instance.AfterDialogEvent(currentStory);
        GameEventBase evt = GameEventFactory.CreateGameStateChangeEvent(GameState.Gameplay);
        GameEventManager.Instance.Submit(evt);

        currentStory = null;
        DialogueFinished?.Invoke(completed);
        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.NotifyDialogFinished(completed)) return;
        // Keep old data working, but route legacy transitions through the same loader.
        if (completed != null && !string.IsNullOrWhiteSpace(completed.nextSceneName) && SceneLoadManager.Instance != null)
            SceneLoadManager.Instance.StartCoroutine(SceneLoadManager.Instance.LoadContent(completed.nextSceneName));
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
