using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Naknak.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed partial class GameMenuController : MonoBehaviour
{
    public static GameMenuController Instance { get; private set; }
    public static bool IsBlockingGameplay => SceneLoadManager.IsLoading || Instance != null && (Instance.page != Page.Closed || Instance.loading);
    public static InventoryModel Inventory => Instance.inventory;

    private enum Page { Closed, Title, Menu, Items, Options, Save, Load, Confirm }
    private Page page;
    private GameMenuConfiguration configuration;
    private InventoryModel inventory;
    private GameMenuOptions options;
    private bool loading;
    private AudioSource itemAudio;
    private AudioClip generatedItemSound;
    private bool paused;
    private float previousTimeScale = 1;
    private GameState previousState;
    private float playSeconds;
    private float autosaveTimer;
    private string lastCheckpointLocation;
    private readonly Dictionary<string, GameObject> trackedObjects = new Dictionary<string, GameObject>();
    private readonly HashSet<int> trackedObjectIds = new HashSet<int>();
    private readonly Dictionary<string, MansionRoomSystem> trackedMansions = new Dictionary<string, MansionRoomSystem>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("Naknak Game Menu").AddComponent<GameMenuController>();
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 240;
        DontDestroyOnLoad(gameObject);
        configuration = Resources.Load<GameMenuConfiguration>("NaknakMenu");
        if (configuration == null) configuration = ScriptableObject.CreateInstance<GameMenuConfiguration>();
        inventory = new InventoryModel(configuration.items);
        inventory.Changed += OnInventoryChanged;
        inventory.Acquired += PlayItemAcquiredSound;
        try { options = new GameMenuOptions(); }
        catch (Exception exception) { Debug.LogException(exception); }
        BuildCanvas();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        TrackWorld();
        if (IsTitle) ShowTitle();
        else ShowPage(Page.Closed);
    }

    private bool IsTitle => SceneManager.GetActiveScene().name == configuration.titleScene;

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        inventory.Changed -= OnInventoryChanged;
        inventory.Acquired -= PlayItemAcquiredSound;
        if (generatedItemSound != null) Destroy(generatedItemSound);
        options?.Dispose();
        if (menuFont != null) Destroy(menuFont);
        if (paused) Time.timeScale = previousTimeScale;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureMenuEventSystem();
        TrackWorld();
        if (!loading && scene.name == configuration.titleScene) ShowTitle();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (loading) return;
        var prefix = scene.path + "|";
        foreach (var key in new List<string>(trackedObjects.Keys))
            if (key.StartsWith(prefix, StringComparison.Ordinal)) trackedObjects.Remove(key);
        foreach (var key in new List<string>(trackedMansions.Keys))
            if (key.StartsWith(prefix, StringComparison.Ordinal)) trackedMansions.Remove(key);
    }

    private void Update()
    {
        if (loading || SceneLoadManager.IsLoading || page == Page.Closed && SceneLoadManager.IsCutscene) return;
        if (page == Page.Closed)
        {
            playSeconds += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I))
            {
                var player = FindFirstObjectByType<PlayerMoveController3>();
                if (GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;
                if (player == null || player.isMoving || player.GetIsJumping()) return;
                inventoryCategory = 0;
                selected = 0;
                Pause();
                ShowPage(Input.GetKeyDown(KeyCode.I) ? Page.Items : Page.Menu);
                return;
            }
            TryAutosave();
            return;
        }
        HandleMenuInput();
    }

    private void Pause()
    {
        if (paused) return;
        previousTimeScale = Time.timeScale;
        previousState = GameStateManager.Instance != null ? GameStateManager.Instance.GameState : GameState.Gameplay;
        if (GameStateManager.Instance != null) GameStateManager.Instance.ChangeGameState(GameState.Pause);
        Time.timeScale = 0;
        paused = true;
    }

    private void Resume()
    {
        InteractionInput.ConsumeConfirm();
        ShowPage(Page.Closed);
        if (paused)
        {
            Time.timeScale = previousTimeScale;
            if (GameStateManager.Instance != null) GameStateManager.Instance.ChangeGameState(previousState);
            paused = false;
        }
        foreach (var input in FindObjectsByType<LastInputManager>(FindObjectsSortMode.None)) input.IgnoreInput(0.15f);
    }

    private void ShowTitle()
    {
        paused = false;
        Time.timeScale = 1;
        ShowPage(Page.Title);
    }

    private void TryAutosave()
    {
        autosaveTimer += Time.unscaledDeltaTime;
        var player = FindFirstObjectByType<PlayerMoveController3>();
        if (player == null || player.isMoving || player.GetIsJumping() || Time.timeSinceLevelLoad < 2 || MapStreamingManager.IsLoading) return;
        if (MansionRoomSystem.Instance != null && MansionRoomSystem.Instance.IsTransitioning) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.GameState != GameState.Gameplay) return;
        string location = SceneManager.GetActiveScene().path + ":" +
            (MansionRoomSystem.Instance != null ? MansionRoomSystem.Instance.GetCurrentRoomId() : 0);
        if (location == lastCheckpointLocation && autosaveTimer < 60) return;
        lastCheckpointLocation = location;
        autosaveTimer = 0;
        try { SaveCheckpoint(player); }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        { Debug.LogWarning("[Checkpoint] 저장하지 못했습니다: " + exception.Message); }
    }

    private void SaveCheckpoint(PlayerMoveController3 player) => GameCheckpointStore.Write(CaptureCheckpoint(player));

    private GameCheckpoint CaptureCheckpoint(PlayerMoveController3 player)
    {
        TrackWorld();
        var data = new GameCheckpoint
        {
            savedAt = DateTimeOffset.Now.ToString("O"), scene = SceneManager.GetActiveScene().name,
            position = player.transform.position, facing = player.FacingDirection, floor = player.GetCurrentFloor(),
            health = PlayerStatus.Instance != null ? PlayerStatus.Instance.GetHealth() : 100,
            playSeconds = playSeconds, inventory = inventory.Capture(),
            conditions = ConditionManager.Instance != null ? ConditionManager.Instance.CaptureValues() : new List<ConditionValue>()
        };
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != data.scene && scene.name != configuration.titleScene) data.additiveScenes.Add(scene.name);
        }
        foreach (var pair in trackedObjects)
            data.world.Add(new WorldObjectState { path = pair.Key, exists = pair.Value != null, active = pair.Value != null && pair.Value.activeSelf });
        foreach (var pair in trackedMansions)
        {
            if (pair.Value == null) continue;
            var mansion = pair.Value.CaptureCheckpoint();
            mansion.path = pair.Key;
            data.mansions.Add(mansion);
        }
        return data;
    }

    private static string ObjectPath(Transform target)
    {
        string path = "";
        for (Transform current = target; current != null; current = current.parent)
            path = current.GetSiblingIndex() + ":" + current.name + "/" + path;
        return target.gameObject.scene.path + "|" + path;
    }

    private void TrackWorld()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
            {
                foreach (var map in root.GetComponentsInChildren<MapContainer>(true))
                    foreach (var node in map.GetComponentsInChildren<Transform>(true)) RegisterObject(node.gameObject);
                foreach (var pickup in root.GetComponentsInChildren<InventoryPickup>(true)) RegisterObject(pickup.gameObject);
                foreach (var mansion in root.GetComponentsInChildren<MansionRoomSystem>(true))
                    trackedMansions.TryAdd(ObjectPath(mansion.transform), mansion);
            }
        }
    }

    private void RegisterObject(GameObject target)
    {
        // Do not recompute sibling indices after another object has been destroyed.
        if (trackedObjectIds.Add(target.GetInstanceID())) trackedObjects.TryAdd(ObjectPath(target.transform), target);
    }

    private void BeginNewGame() => StartCoroutine(ChangeSession(configuration.startScene, null));

    private void BeginSave(int slot)
    {
        var player = FindFirstObjectByType<PlayerMoveController3>();
        if (loading || IsTitle || !paused || previousState != GameState.Gameplay || player == null ||
            player.isMoving || player.GetIsJumping() || MapStreamingManager.IsLoading ||
            (MansionRoomSystem.Instance != null && MansionRoomSystem.Instance.IsTransitioning))
        { Notice("이동과 연출이 끝난 뒤 저장해 주세요."); return; }
        try
        {
            if (File.Exists(GameCheckpointStore.ManualSlotPath(slot)))
                Confirm("슬롯 " + slot + "의 기존 기록을 덮어쓸까요?", () => SaveManualSlot(slot));
            else SaveManualSlot(slot);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        { Debug.LogWarning("[Save] " + exception.Message); Notice("저장 파일에 접근할 수 없습니다."); }
    }

    private void SaveManualSlot(int slot)
    {
        try
        {
            var player = FindFirstObjectByType<PlayerMoveController3>();
            if (player == null) { Notice("플레이어를 찾을 수 없어 저장하지 못했습니다."); return; }
            GameCheckpointStore.WriteManual(slot, CaptureCheckpoint(player));
            HandleManualSaveSuccess(slot);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        { Debug.LogWarning("[Save] " + exception.Message); Notice("저장하지 못했습니다. 저장 폴더의 공간과 권한을 확인해 주세요."); }
    }

    private void BeginLoad(int slot, bool manual = false)
    {
        GameCheckpoint data;
        string error;
        bool read = manual ? GameCheckpointStore.TryReadManual(slot, out data, out error) : GameCheckpointStore.TryRead(slot, out data, out error);
        if (!read)
        { Notice(error == null ? "저장된 기록이 없습니다." : "저장 파일을 읽을 수 없습니다."); return; }
        try { new InventoryModel(configuration.items).Restore(data.inventory); }
        catch (ArgumentException) { Notice("저장 파일에 현재 사용할 수 없는 아이템이 있습니다."); return; }
        if (data.scene == configuration.titleScene || !Application.CanStreamedLevelBeLoaded(data.scene))
        { Notice("저장된 장소를 불러올 수 없습니다."); return; }
        foreach (var scene in data.additiveScenes)
            if (!Application.CanStreamedLevelBeLoaded(scene)) { Notice("저장된 맵을 찾을 수 없습니다."); return; }
        Confirm("현재 진행을 종료하고 이 기록을 불러올까요?", () => StartCoroutine(ChangeSession(data.scene, data)));
    }

    private IEnumerator ChangeSession(string sceneName, GameCheckpoint checkpoint)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName)) { Notice("시작할 씬을 찾을 수 없습니다."); yield break; }
        string validationError = null;
        if (checkpoint != null)
        {
            try
            {
                GameCheckpointStore.Validate(checkpoint);
                new InventoryModel(configuration.items).Restore(checkpoint.inventory);
                foreach (string extra in checkpoint.additiveScenes)
                    if (!Application.CanStreamedLevelBeLoaded(extra))
                        throw new ArgumentException("저장된 맵을 찾을 수 없습니다.");
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            { validationError = "저장 파일을 불러올 수 없습니다. 아이템 또는 장소 정보가 올바르지 않습니다."; }
        }
        // Validate before unloading the current session or taking its input lock.
        if (validationError != null) { Notice(validationError); yield break; }
        completionSavePending = false;
        SceneLoadManager.IsRestoringSession = checkpoint != null;
        loading = true;
        Pause();
        ClearPage();
        Label(panel, "불러오는 중…", 32, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.6f), TextAlignmentOptions.Center);
        // Recreate gameplay singletons as a group; their old scene UI and player references cannot be reused.
        var roots = new HashSet<GameObject>();
        foreach (var behaviour in new MonoBehaviour[] { DialogManager.Instance, GameEventManager.Instance,
                     GameStateManager.Instance, ConditionManager.Instance, PlayerStatus.Instance, MapManager.Instance, TriggerExecutor.Instance })
            if (behaviour != null) roots.Add(behaviour.transform.root.gameObject);
        foreach (var player in FindObjectsByType<PlayerMoveController3>(FindObjectsSortMode.None)) roots.Add(player.transform.root.gameObject);
        foreach (var root in roots) if (root != gameObject) Destroy(root);
        yield return null;
        trackedObjects.Clear(); trackedObjectIds.Clear(); trackedMansions.Clear();
        Time.timeScale = 1;
        paused = false;
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        // Give Start and the additive map loader time to complete before restoring position or triggers.
        yield return null;
        // Cold scene loads can exceed 20 seconds on slower disks. The scene's event system
        // stays suppressed until the asynchronous map load has really completed.
        float deadline = Time.realtimeSinceStartup + 120;
        while ((MapStreamingManager.IsLoading || SceneLoadManager.IsLoading) && Time.realtimeSinceStartup < deadline) yield return null;
        if (MapStreamingManager.IsLoading || SceneLoadManager.IsLoading)
        {
            SceneLoadManager.IsRestoringSession = false;
            loading = false; Pause(); ShowPage(Page.Menu); Notice("맵을 불러오는 시간이 초과되었습니다."); yield break;
        }
        if (checkpoint != null)
        {
            foreach (string extraScene in checkpoint.additiveScenes)
                if (!SceneManager.GetSceneByName(extraScene).isLoaded) yield return SceneManager.LoadSceneAsync(extraScene, LoadSceneMode.Additive);
        }
        yield return null;
        TrackWorld();
        if (checkpoint == null)
        {
            inventory.Restore(null);
            ConditionManager.Instance?.ResetForNewGame();
            playSeconds = 0;
        }
        else
        {
            inventory.Restore(checkpoint.inventory);
            ConditionManager.Instance?.RestoreValues(checkpoint.conditions);
            var tutorialObjective = FindFirstObjectByType<TutorialObjectiveController>();
            if (tutorialObjective != null) tutorialObjective.AcknowledgeRestoredCompletion();
            foreach (var state in checkpoint.mansions)
                if (state != null && trackedMansions.TryGetValue(state.path, out var mansion) && mansion != null) mansion.RestoreCheckpoint(state);
            foreach (var state in checkpoint.world)
                if (state != null && trackedObjects.TryGetValue(state.path, out var target) && target != null)
                { if (!state.exists) Destroy(target); else target.SetActive(state.active); }
            var player = FindFirstObjectByType<PlayerMoveController3>();
            if (player != null)
            {
                player.SetFloor(checkpoint.floor);
                player.TeleportToWorld(checkpoint.position);
                player.SetFacingDirection(checkpoint.facing);
            }
            PlayerStatus.Instance?.RestoreHealth(checkpoint.health);
            playSeconds = checkpoint.playSeconds;
        }
        yield return null; // Wait for destroyed trigger objects before rebuilding their index.
        TriggerExecutor.Instance?.BuildIndex();
        loading = false;
        if (checkpoint != null) SceneLoadManager.Instance?.ResumeRestoredSession();
        lastCheckpointLocation = null;
        autosaveTimer = 0;
        if (sceneName == configuration.titleScene) ShowTitle();
        else
        {
            GameStateManager.Instance?.ChangeGameState(GameState.Gameplay);
            ShowPage(Page.Closed);
            foreach (var input in FindObjectsByType<LastInputManager>(FindObjectsSortMode.None)) input.IgnoreInput(0.2f);
        }
    }

    private void OnInventoryChanged() { if (IsInventoryPage) ShowPage(page); }

    private void PlayItemAcquiredSound()
    {
        if (SceneLoadManager.IsRestoringSession || loading) return;
        if (itemAudio == null)
        {
            itemAudio = gameObject.AddComponent<AudioSource>();
            itemAudio.playOnAwake = false;
            itemAudio.spatialBlend = 0;
            itemAudio.ignoreListenerPause = true;
        }
        var clip = configuration.itemAcquiredSound;
        if (clip == null)
        {
            if (generatedItemSound == null)
            {
                const int rate = 22050;
                var samples = new float[(int)(rate * .18f)];
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = (float)i / rate;
                    float envelope = Mathf.Min(1, t / .006f) * Mathf.Pow(1 - (float)i / samples.Length, 2);
                    samples[i] = .25f * envelope * (Mathf.Sin(2 * Mathf.PI * 880 * t) + .35f * Mathf.Sin(2 * Mathf.PI * 1320 * t));
                }
                generatedItemSound = AudioClip.Create("Item acquired", samples.Length, 1, rate, false);
                generatedItemSound.SetData(samples, 0);
            }
            clip = generatedItemSound;
        }
        itemAudio.PlayOneShot(clip, configuration.itemAcquiredVolume);
    }
}
