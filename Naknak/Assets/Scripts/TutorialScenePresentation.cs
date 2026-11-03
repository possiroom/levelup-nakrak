using System.Collections;
using Naknak.FlashlightSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Applies grayscale to the complete memory world before its first rendered frame.</summary>
public sealed class TutorialScenePresentation : MonoBehaviour
{
    [SerializeField] private bool deduction;
    [SerializeField] private bool darkMemory;
    [SerializeField] private Transform focusActor;
    [SerializeField] private FlashlightWorldDesaturateEffect grayscale;
    [SerializeField] private FlashlightFreeSightOverlay vision;
    [SerializeField] private Image unknownOverlay;
    private Transform player;
    private bool styleApplied;

    public void ApplyMemoryStyle()
    {
        if (!SceneLoadManager.IsMemorySpace || SceneLoadManager.CurrentScenePath != gameObject.scene.path) return;
        if (SceneLoadManager.IsColorMemory) return;
        var controller = FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        var target = controller != null ? controller.transform : null;
        if (styleApplied && player == target) return;
        player = target;
        if (grayscale == null) grayscale = GetComponent<FlashlightWorldDesaturateEffect>();
        if (grayscale == null) grayscale = gameObject.AddComponent<FlashlightWorldDesaturateEffect>();
        grayscale.ApplyToScene(gameObject.scene, null, true);
        styleApplied = true;
    }

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
        ApplyMemoryStyle();
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
        styleApplied = false;
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
