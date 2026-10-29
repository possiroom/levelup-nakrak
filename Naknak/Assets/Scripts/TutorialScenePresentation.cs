using System.Collections;
using Naknak.FlashlightSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reuses the project's grayscale and circular-vision effects for deduction and dark memories.</summary>
public sealed class TutorialScenePresentation : MonoBehaviour
{
    [SerializeField] private bool deduction;
    [SerializeField] private bool darkMemory;
    [SerializeField] private Transform focusActor;
    [SerializeField] private FlashlightWorldDesaturateEffect grayscale;
    [SerializeField] private FlashlightFreeSightOverlay vision;
    [SerializeField] private Image unknownOverlay;
    private Transform player;

    private IEnumerator Start()
    {
        while (SceneLoadManager.IsLoading || SceneLoadManager.IsRestoringSession || SceneLoadManager.IsPreparingMemory)
            yield return null;
        var controller = FindFirstObjectByType<PlayerMoveController3>(FindObjectsInactive.Include);
        player = controller != null ? controller.transform : null;
        if (deduction && grayscale != null) grayscale.Apply();
        if (vision != null)
        {
            // The project also has a perspective output camera on the Cinemachine object.
            // Use the camera that actually renders last, so the sight circle matches its projection.
            Camera output = null;
            foreach (var camera in Camera.allCameras)
                if (camera.targetTexture == null && camera.targetDisplay == 0 && (output == null || camera.depth > output.depth)) output = camera;
            vision.SetCamera(output);
            vision.SetActive(darkMemory || deduction);
            vision.SetFlashlightVisible(darkMemory || deduction);
        }
        SetUnknownSpace(false);
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
