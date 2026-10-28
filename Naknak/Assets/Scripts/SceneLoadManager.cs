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
    public static bool IsCutscene => HasManagedScene && Instance.current.hidePlayer;
    [SerializeField] private SceneFlowContainer container;
    private SceneFlowContainer.SceneEntry current;
    private Scene contentScene;
    private bool entryPending;
    private bool preparing;

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
            if (mode == LoadSceneMode.Single) { current = null; entryPending = false; }
            return;
        }
        contentScene = scene;
        current = entry;
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
        if (current == null || IsLoading || IsRestoringSession || GameMenuController.IsBlockingGameplay) return;
        if (entryPending && DialogManager.Instance != null && !DialogManager.Instance.IsDialogOpen)
        {
            entryPending = false;
            if (current.entryDialog != null) { DialogManager.Instance.ShowDialog(current.entryDialog); return; }
        }
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogOpen &&
            (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
             Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))) DialogManager.Instance.NextDialog();
    }

    public void SetContainer(SceneFlowContainer value) { container = value; }
    public bool NotifyDialogFinished(StoryData dialog) => Follow(SceneConnectionTrigger.DialogFinished, dialog);
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
        StartCoroutine(LoadContent(connection.to.Path));
        return true;
    }

    public IEnumerator LoadContent(string scenePathOrName)
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
        IsLoading = false;
    }

    private void ConfigureScene(Scene scene, SceneFlowContainer.SceneEntry entry)
    {
        var core = SceneManager.GetSceneByName("PrototypeScene");
        if (core.isLoaded) SceneManager.SetActiveScene(core);
        Camera contentCamera = null;
        GridLayout grid = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (contentCamera == null) contentCamera = root.GetComponentInChildren<Camera>();
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
            player.enabled = !entry.hidePlayer;
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

    public void ResumeRestoredSession() { IsRestoringSession = false; entryPending = false; }
}
