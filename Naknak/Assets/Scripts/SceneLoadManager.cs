using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keeps PrototypeScene's services alive and replaces only the story scene.</summary>
public sealed class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }
    public static bool IsLoading { get; private set; }
    public static bool IsRestoringSession { get; set; }
    public static bool HasManagedScene => Instance != null && Instance.current != null;
    public static string CurrentScenePath => HasManagedScene ? Instance.contentScene.path : "";
    public static bool IsMemorySpace => HasManagedScene && Instance.current.memorySpace;
    public static bool IsColorMemory => HasManagedScene && Instance.current.colorMemory;
    public static bool IsPreparingMemory => Instance != null && Instance.memoryPending;
    public static bool IsMemoryRevisit => IsMemorySpace && Instance.container != null &&
        ConditionManager.Instance != null && ConditionManager.Instance.Get(Instance.container.memoryVisitCount) > 1;
    public static bool IsFirstMemoryVisit => IsMemorySpace && !IsPreparingMemory && !IsMemoryRevisit;
    public static bool IsCutscene => HasManagedScene && (Instance.current.hidePlayer || Instance.PlayerInputLocked);
    [SerializeField] private SceneFlowContainer container;
    private SceneFlowContainer.SceneEntry current;
    private Scene contentScene;
    private bool entryPending;
    private bool preparing;
    private bool memoryActive;
    private bool memoryPending;
    private bool? inputLockOverride;
    private UnityEngine.UI.Image transitionOverlay;
    private bool PlayerInputLocked => inputLockOverride ?? (current != null && current.lockPlayerInput);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; IsLoading = false; IsRestoringSession = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("Scene Load Manager").AddComponent<SceneLoadManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        { if (container != null) Instance.SetContainer(container); Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (container == null) container = Resources.Load<SceneFlowContainer>("TutorialSceneFlow");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Instance = null;
        IsLoading = false;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene != contentScene) return;
        current = null;
        entryPending = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var entry = container != null ? container.FindScene(scene.path) : null;
        if (entry == null)
        {
            if (scene.name == "PrototypeScene" && IsMemorySpace)
                TutorialScenePresentation.EnsureForScene(contentScene).ApplyMemoryStyle();
            if (mode == LoadSceneMode.Single)
            {
                current = null; entryPending = false; memoryActive = false; memoryPending = false;
                if (GameMenuController.Instance != null) GameMenuController.Inventory.SetMemorySpace(false);
            }
            return;
        }
        contentScene = scene;
        current = entry;
        memoryPending = entry.memorySpace || memoryActive;
        if (!memoryPending && GameMenuController.Instance != null) GameMenuController.Inventory.SetMemorySpace(false);
        if (memoryPending && !IsRestoringSession) PrepareMemory(false);
        inputLockOverride = null;
        entryPending = !IsRestoringSession;
        ConfigureScene(scene, entry);
        if (!preparing && !SceneManager.GetSceneByName("PrototypeScene").isLoaded)
            StartCoroutine(PrepareServices());
    }

    private IEnumerator PrepareServices()
    {
        preparing = true;
        IsLoading = true;
        yield return SceneManager.LoadSceneAsync("PrototypeScene", LoadSceneMode.Additive);
        yield return null;
        if (current != null) ConfigureScene(contentScene, current);
        IsLoading = false;
        preparing = false;
    }

    private void Update()
    {
        if (memoryPending && !IsRestoringSession && !IsLoading) PrepareMemory(false);
        if (memoryPending) return;
        if (current == null || IsLoading || IsRestoringSession || GameMenuController.IsBlockingGameplay) return;
        if (entryPending && DialogManager.Instance != null && DialogManager.Instance.CanShowDialog && !DialogManager.Instance.IsDialogOpen &&
            (current.entryDialogs == null || ConditionManager.Instance != null))
        {
            entryPending = false;
            var entryStory = current.entryDialogs != null ? current.entryDialogs.GetMatchingDialog() : current.entryDialog;
            if (entryStory != null) { DialogManager.Instance.ShowDialog(entryStory, true); return; }
        }
        if (DialogManager.Instance != null && DialogManager.Instance.CanFastForward && InteractionInput.CanConfirm &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        { DialogManager.Instance.SkipUntilChoice(); return; }
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen &&
            (InteractionInput.ConfirmPressed || InteractionInput.CanConfirm &&
             (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))) DialogManager.Instance.AdvanceFromInput();
    }

    public void SetContainer(SceneFlowContainer value) { container = value; }
    public bool NotifyDialogFinished(StoryData dialog)
    {
        if (current == null || container == null || IsLoading) return false;
        var connection = container.FindConnection(contentScene.path, SceneConnectionTrigger.DialogFinished, dialog);
        if (connection == null) return false;
        foreach (var root in contentScene.GetRootGameObjects())
        {
            var staging = root.GetComponentInChildren<TutorialWitnessStaging>();
            if (staging == null || !staging.WaitBeforeTransition(dialog)) continue;
            SetPlayerInputLocked(true);
            StartCoroutine(WaitForWitness(staging, connection));
            return true;
        }
        return Travel(container, connection);
    }

    private IEnumerator WaitForWitness(TutorialWitnessStaging staging, SceneFlowContainer.Connection connection)
    {
        while (staging != null && !staging.AppraisalWalkComplete) yield return null;
        if (staging != null) Travel(container, connection);
    }
    public void CompleteScene() { Follow(SceneConnectionTrigger.SceneCompleted); }
    public bool TryCompleteScene() => Follow(SceneConnectionTrigger.SceneCompleted);

    private bool Follow(SceneConnectionTrigger trigger, StoryData dialog = null)
    {
        if (IsLoading || current == null || container == null) return false;
        var connection = container.FindConnection(contentScene.path, trigger, dialog);
        return connection != null && Travel(container, connection);
    }

    public bool Travel(SceneFlowContainer flow, SceneFlowContainer.Connection connection)
    {
        if (IsLoading || flow == null || connection == null || !SceneFlowContainer.Passes(connection.conditions)) return false;
        if (connection.from.Path != contentScene.path && connection.from.Name != contentScene.name) return false;
        if (!Application.CanStreamedLevelBeLoaded(connection.to.Path))
        { Debug.LogError("[SceneLoadManager] Add the target scene to Build Settings: " + connection.to.Path); return false; }
        container = flow;
        StartCoroutine(connection.fade || connection.entranceFootsteps != null || RequiresMemoryTransition(connection.to.Path) ?
            FadeTravel(connection.to.Path, connection.transitionDialog, connection.entranceFootsteps) : LoadContentCore(connection.to.Path));
        return true;
    }

    private IEnumerator FadeTravel(string destination, StoryData transitionDialog = null, AudioClip entranceFootsteps = null)
    {
        if (IsLoading) yield break;
        bool memoryBoundary = CrossesMemoryBoundary(destination);
        if (transitionOverlay == null)
        {
            var canvasObject = new GameObject("Scene Transition", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)imageObject.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            transitionOverlay = imageObject.GetComponent<UnityEngine.UI.Image>(); transitionOverlay.raycastTarget = false;
        }
        IsLoading = true;
        for (float elapsed = 0; elapsed < 0.25f; elapsed += Time.unscaledDeltaTime)
        { transitionOverlay.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / 0.25f)); yield return null; }
        transitionOverlay.color = Color.black;
        if (entranceFootsteps != null) yield return TutorialEntranceAudio.Play(gameObject, entranceFootsteps);
        IsLoading = false;
        if (transitionDialog != null && DialogManager.Instance != null)
        {
            var canvas = DialogManager.Instance.DialogueCanvas;
            int order = canvas != null ? canvas.sortingOrder : 0;
            if (canvas != null) canvas.sortingOrder = 30001;
            SetPlayerInputLocked(true);
            DialogManager.Instance.ShowDialog(transitionDialog, true);
            yield return null;
            while (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen) yield return null;
            if (canvas != null) canvas.sortingOrder = order;
        }
        yield return LoadContentCore(destination, true);
        IsLoading = true;
        if (memoryBoundary) yield return new WaitForSecondsRealtime(2f);
        for (float elapsed = 0; elapsed < 0.25f; elapsed += Time.unscaledDeltaTime)
        { transitionOverlay.color = new Color(0, 0, 0, 1 - Mathf.Clamp01(elapsed / 0.25f)); yield return null; }
        transitionOverlay.color = Color.clear;
        IsLoading = false;
    }

    public IEnumerator LoadContent(string scenePathOrName)
    {
        return RequiresMemoryTransition(scenePathOrName) ? FadeTravel(scenePathOrName) : LoadContentCore(scenePathOrName);
    }

    private bool RequiresMemoryTransition(string destination)
    {
        var target = container != null ? container.FindScene(destination) : null;
        return IsMemorySpace || target != null && target.memorySpace;
    }

    private bool CrossesMemoryBoundary(string destination)
    {
        var target = container != null ? container.FindScene(destination) : null;
        return target != null && (current != null && current.memorySpace) != target.memorySpace;
    }

    public bool ReturnFromFirstMemory()
    {
        if (!IsFirstMemoryVisit || container == null) return false;
        var exit = container.connections.Find(row => row.from.Path == contentScene.path &&
            container.FindScene(row.to.Path) != null && !container.FindScene(row.to.Path).memorySpace &&
            SceneFlowContainer.Passes(row.conditions));
        return exit != null && Travel(container, exit);
    }

    private IEnumerator LoadContentCore(string scenePathOrName, bool keepLoading = false)
    {
        if (IsLoading) yield break;
        if (!Application.CanStreamedLevelBeLoaded(scenePathOrName))
        { Debug.LogError("[SceneLoadManager] Scene is not in Build Settings: " + scenePathOrName); yield break; }
        IsLoading = true;
        var previous = contentScene;
        current = null;
        entryPending = false;
        if (previous.IsValid() && previous.isLoaded) yield return SceneManager.UnloadSceneAsync(previous);
        yield return SceneManager.LoadSceneAsync(scenePathOrName, LoadSceneMode.Additive);
        yield return null;
        if (current != null) ConfigureScene(contentScene, current);
        if (!keepLoading) IsLoading = false;
    }

    private void ConfigureScene(Scene scene, SceneFlowContainer.SceneEntry entry)
    {
        if (entry.memorySpace) TutorialScenePresentation.EnsureForScene(scene).ApplyMemoryStyle();
        var core = SceneManager.GetSceneByName("PrototypeScene");
        if (core.isLoaded) SceneManager.SetActiveScene(core);
        Camera contentCamera = null;
        GridLayout grid = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (contentCamera == null && !entry.useSharedCamera) contentCamera = root.GetComponentInChildren<Camera>();
            if (entry.useSharedCamera)
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
            if (grid == null) grid = root.GetComponentInChildren<GridLayout>();
        }
        if (core.isLoaded)
            foreach (var root in core.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = contentCamera == null;
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = contentCamera == null;
            }
        var player = FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        if (player != null)
        {
            WorldDepthSort.EnsureActor(player.transform);
            ApplyPlayerAppearance(player);
            player.enabled = !entry.hidePlayer && !PlayerInputLocked;
            foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = !entry.hidePlayer;
            if (entry.placePlayer && !IsRestoringSession) player.TeleportToWorld(entry.playerPosition);
        }
        if (TriggerExecutor.Instance != null)
        {
            TriggerExecutor.Instance.ChangeMap(grid, grid != null ? grid.gameObject : null);
            TriggerExecutor.Instance.ChangeC2E(null);
        }
        if (core.isLoaded) DialogManager.Instance?.BindSceneViews(core);
        DialogManager.Instance?.BindSceneViews(scene);
    }

    // A cutscene can release control without mutating the shared flow asset or teleporting again.
    public void SetPlayerInputLocked(bool locked)
    {
        if (current == null) return;
        inputLockOverride = locked;
        var player = FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        if (player != null) player.enabled = !current.hidePlayer && !locked;
    }

    private void PrepareMemory(bool restoring)
    {
        if (GameMenuController.Instance == null || ConditionManager.Instance == null) return;
        var inventory = GameMenuController.Inventory;
        bool inMemory = IsMemorySpace;
        inventory.SetMemorySpace(inMemory);
        if (inMemory && !memoryActive && !restoring)
        {
            inventory.ClearMemoryItems();
            foreach (var flag in container.memoryRunConditions) ConditionManager.Instance.Set(flag, 0);
            int visits = ConditionManager.Instance.Get(container.memoryVisitCount);
            ConditionManager.Instance.Set(container.memoryVisitCount, Mathf.Max(current.memoryReplayEntry ? 2 : 1, visits + 1));
            if (current.memoryEntryItem != null && !inventory.Add(current.memoryEntryItem.Id))
                Debug.LogError("[Memory] Cannot grant entry item: " + current.memoryEntryItem.Id);
        }
        if (inMemory && restoring && ConditionManager.Instance.Get(container.memoryVisitCount) == 0)
            ConditionManager.Instance.Set(container.memoryVisitCount, 1);
        memoryActive = inMemory;
        memoryPending = false;
        ApplyPlayerAppearance(FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include));
    }

    private void ApplyPlayerAppearance(PlayerMoveController3 player)
    {
        if (player == null || container == null) return;
        var animator = player.GetComponent<Animator>();
        var appearance = IsMemorySpace && !IsMemoryRevisit ? container.assistantPlayerController : container.detectivePlayerController;
        if (animator == null || appearance == null || animator.runtimeAnimatorController == appearance) return;
        float direction = animator.GetFloat("direction");
        bool moving = animator.GetBool("isMoving");
        animator.runtimeAnimatorController = appearance;
        animator.SetFloat("direction", direction);
        animator.SetBool("isMoving", moving);
        animator.Update(0f);
    }

    public void ResumeRestoredSession()
    {
        PrepareMemory(true);
        IsRestoringSession = false;
        entryPending = false;
    }
}
