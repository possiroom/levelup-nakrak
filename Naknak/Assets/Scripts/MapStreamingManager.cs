using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the bootstrap scene alive while map scenes are loaded additively.
/// Every map scene must contain one MapContainer.
/// </summary>
public sealed class MapStreamingManager : MonoBehaviour
{
    public static MapStreamingManager Instance { get; private set; }

    [SerializeField] private PlayerMoveController3 playerCtrl;
    [SerializeField] private string initialMapScene = "Map_01";
    [SerializeField] private string[] preloadMapScenes;
    [SerializeField] private bool debugMode;

    private readonly Dictionary<string, MapContainer> loadedMaps = new();
    private string currentMapScene;

    public string CurrentMapScene => currentMapScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(LoadInitialMaps());
    }

    private IEnumerator LoadInitialMaps()
    {
        yield return LoadMapAdditive(initialMapScene, true);

        foreach (string sceneName in preloadMapScenes)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) && sceneName != initialMapScene)
                yield return LoadMapAdditive(sceneName, false);
        }
    }

    public IEnumerator MoveToMap(string sceneName, Vector2 spawnPosition, int floor = 1)
    {
        yield return LoadMapAdditive(sceneName, true);

        if (playerCtrl != null)
        {
            playerCtrl.SetFloor(floor);
            playerCtrl.TeleportToWorld(spawnPosition);
        }
    }

    public IEnumerator LoadMapAdditive(string sceneName, bool makeCurrent)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[MapStreamingManager] Map scene name is empty.");
            yield break;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError("[MapStreamingManager] Cannot load map scene: " + sceneName);
                yield break;
            }

            yield return operation;
            scene = SceneManager.GetSceneByName(sceneName);
        }

        RegisterMap(scene);
        if (makeCurrent)
            SetCurrentMap(sceneName);
    }

    public IEnumerator UnloadMap(string sceneName)
    {
        if (sceneName == currentMapScene)
        {
            Debug.LogWarning("[MapStreamingManager] Load another map before unloading the current map.");
            yield break;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
            yield break;

        loadedMaps.Remove(sceneName);
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    public void SetCurrentMap(string sceneName)
    {
        if (!loadedMaps.TryGetValue(sceneName, out MapContainer map))
        {
            Debug.LogError("[MapStreamingManager] Map has not been loaded: " + sceneName);
            return;
        }

        currentMapScene = sceneName;
        map.debugMode = debugMode;

        if (TriggerExecutor.Instance != null)
        {
            TriggerExecutor.Instance.ChangeMap(map.GridLayout, map.gameObject);
            TriggerExecutor.Instance.ChangeC2E(map.GetC2E());
        }
    }

    public void ChangePlayerFloor(int floor)
    {
        if (playerCtrl != null)
            playerCtrl.SetFloor(floor);
    }

    private void RegisterMap(Scene scene)
    {
        if (!scene.isLoaded || loadedMaps.ContainsKey(scene.name))
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MapContainer map = root.GetComponentInChildren<MapContainer>(true);
            if (map == null)
                continue;

            loadedMaps.Add(scene.name, map);
            return;
        }

        Debug.LogError("[MapStreamingManager] Map scene needs one MapContainer: " + scene.name);
    }
}
