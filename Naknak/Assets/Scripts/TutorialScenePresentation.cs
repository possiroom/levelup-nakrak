using System.Collections;
using System.Collections.Generic;
using Naknak.FlashlightSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Memory backgrounds are grayscale; actors and investigation clues retain their colors.</summary>
public sealed class TutorialScenePresentation : MonoBehaviour
{
    [SerializeField] private bool deduction;
    [SerializeField] private bool darkMemory;
    [SerializeField] private Transform focusActor;
    [SerializeField] private FlashlightWorldDesaturateEffect grayscale;
    [SerializeField] private FlashlightFreeSightOverlay vision;
    [SerializeField] private Image unknownOverlay;
    [Header("Memory Colors")]
    [Tooltip("Keep characters and tagged investigation objects in their original colors.")]
    [SerializeField] private bool keepActorsAndCluesInColor = true;
    [Tooltip("Additional objects (including their children) to keep in color. Use a separate Tilemap for colored clue tiles.")]
    [SerializeField] private Transform[] colorObjects = new Transform[0];
    private Transform player;

    public static TutorialScenePresentation EnsureForScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var existing = root.GetComponentInChildren<TutorialScenePresentation>(true);
            if (existing != null) return existing;
        }
        var obj = new GameObject("Memory Presentation");
        SceneManager.MoveGameObjectToScene(obj, scene);
        return obj.AddComponent<TutorialScenePresentation>();
    }

    private void OnEnable() { StartCoroutine(Prepare()); }

    private IEnumerator Prepare()
    {
        // Scene components enable before SceneLoadManager receives sceneLoaded.
        yield return null;
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession || SceneLoadManager.IsPreparingMemory)
            yield return null;
        var controller = FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        player = controller != null ? controller.transform : null;
        if (SceneLoadManager.IsMemorySpace && SceneLoadManager.CurrentScenePath == gameObject.scene.path)
        {
            if (grayscale == null) grayscale = GetComponent<FlashlightWorldDesaturateEffect>();
            if (grayscale == null) grayscale = gameObject.AddComponent<FlashlightWorldDesaturateEffect>();
            var keepColor = new HashSet<Transform>(colorObjects);
            if (focusActor != null) keepColor.Add(focusActor);
            if (player != null) keepColor.Add(player);
            if (keepActorsAndCluesInColor)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var obj in root.GetComponentsInChildren<Transform>(true))
                        if (obj.CompareTag("Interactable") || obj.GetComponent<Animator>() != null ||
                            obj.GetComponent<NPC>() != null || obj.GetComponent<InvestigationPoint>() != null)
                            keepColor.Add(obj);
            grayscale.ApplyToScene(gameObject.scene, keepColor);
        }
        if (vision != null)
        {
            // The project also has a perspective output camera on the Cinemachine object.
            // Use the camera that actually renders last, so the sight circle matches its projection.
            Camera output = null;
            foreach (var camera in Camera.allCameras)
                if (camera.targetTexture == null && camera.targetDisplay == 0 && (output == null || camera.depth > output.depth)) output = camera;
            vision.SetCamera(output);
            // Only the blackout search restricts vision. Confessions show the surrounding shop.
            vision.SetActive(darkMemory);
            vision.SetFlashlightVisible(darkMemory);
        }
        SetUnknownSpace(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (grayscale != null) grayscale.Clear();
        if (vision != null) { vision.SetActive(false); vision.SetFlashlightVisible(false); }
    }

    private void LateUpdate()
    {
        if (vision == null) return;
        var focus = deduction && focusActor != null ? focusActor : player;
        if (focus != null) vision.SetSightCenter(focus.position);
    }

    public void SetUnknownSpace(bool active)
    {
        if (unknownOverlay != null) unknownOverlay.gameObject.SetActive(active);
    }
}
