using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Restores the loader referenced by PrototypeScene while avoiding duplicate additive maps.</summary>
public sealed class MapStreamingManager : MonoBehaviour
{
    [SerializeField] private PlayerMoveController3 playerCtrl;
    [SerializeField] private string initialMapScene = "Map_01";
    [SerializeField] private string[] preloadMapScenes = new string[0];
    [SerializeField] private bool debugMode;
    public static bool IsLoading { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsLoading = false;

    private IEnumerator Start()
    {
        IsLoading = true;
        yield return Load(initialMapScene);
        foreach (var scene in preloadMapScenes) yield return Load(scene);
        IsLoading = false;
    }

    private IEnumerator Load(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) yield break;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        { Debug.LogError("[MapStreamingManager] Build Settings에 없는 맵: " + sceneName); yield break; }
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        var scene = SceneManager.GetSceneByName(sceneName);
        foreach (var root in scene.GetRootGameObjects())
            foreach (var map in root.GetComponentsInChildren<MapContainer>(true))
            {
                map.debugMode = debugMode;
                if (TriggerExecutor.Instance != null)
                { TriggerExecutor.Instance.ChangeMap(map.GridLayout, map.TriggerTilemap); TriggerExecutor.Instance.ChangeC2E(map.GetC2E()); }
            }
    }

    private void OnDestroy() => IsLoading = false;
}
