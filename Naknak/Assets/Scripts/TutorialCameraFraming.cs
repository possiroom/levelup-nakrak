using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>Restores the gameplay lens and follows the controlled player without room confinement.</summary>
public sealed class TutorialCameraFraming : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private CinemachineConfiner2D confiner;
    private float originalSize;

    public static void Configure(Scene content, bool sharedCamera, Transform player = null)
    {
        var core = SceneManager.GetSceneByName("PrototypeScene");
        if (!core.isLoaded) return;
        var camera = core.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CinemachineCamera>(true)).FirstOrDefault();
        if (camera == null) return;
        if (sharedCamera && player != null)
        {
            camera.Follow = player;
            // Scene entry can teleport the player or replace the controlled actor.
            // Discard the old detective/cutscene tracking position before rendering.
            camera.PreviousStateIsValid = false;
        }
        var framing = camera.GetComponent<TutorialCameraFraming>();
        if (framing == null) framing = camera.gameObject.AddComponent<TutorialCameraFraming>();
        framing.RestoreLens();
    }

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        originalSize = virtualCamera.Lens.OrthographicSize;
        var legacyOutput = GetComponent<Camera>();
        if (legacyOutput != null && !legacyOutput.orthographic)
            originalSize = Mathf.Max(originalSize, Mathf.Abs(transform.position.z) *
                Mathf.Tan(legacyOutput.fieldOfView * Mathf.Deg2Rad * .5f));
        confiner = GetComponent<CinemachineConfiner2D>();
        if (confiner != null) confiner.enabled = false;
    }

    private void RestoreLens()
    {
        if (confiner != null) confiner.enabled = false;
        var lens = virtualCamera.Lens;
        lens.OrthographicSize = originalSize;
        virtualCamera.Lens = lens;
    }
}