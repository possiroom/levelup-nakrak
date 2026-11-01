using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Confines the shared gameplay camera to the current room, including after aspect changes.</summary>
public sealed class TutorialCameraFraming : MonoBehaviour
{
    private CinemachineCamera virtualCamera;
    private CinemachineConfiner2D confiner;
    private float originalSize;
    private Bounds roomBounds;
    private bool configured;
    private float previousAspect;

    public static void Configure(Scene content, bool sharedCamera)
    {
        var core = SceneManager.GetSceneByName("PrototypeScene");
        if (!core.isLoaded) return;
        var camera = core.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CinemachineCamera>(true)).FirstOrDefault();
        if (camera == null) return;
        var framing = camera.GetComponent<TutorialCameraFraming>();
        if (framing == null) framing = camera.gameObject.AddComponent<TutorialCameraFraming>();
        framing.Bind(content, sharedCamera);
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
        if (confiner == null) confiner = gameObject.AddComponent<CinemachineConfiner2D>();
    }

    private void Bind(Scene content, bool sharedCamera)
    {
        var floor = content.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(true))
            .FirstOrDefault(m => m.name == "Tilemap" && m.GetComponent<Collider2D>() == null);
        configured = sharedCamera && floor != null && floor.GetComponent<TilemapRenderer>() != null;
        confiner.enabled = configured;
        if (!configured)
        {
            var originalLens = virtualCamera.Lens;
            originalLens.OrthographicSize = originalSize;
            virtualCamera.Lens = originalLens;
            return;
        }
        roomBounds = floor.GetComponent<TilemapRenderer>().bounds;
        if (confiner.BoundingShape2D != null && confiner.BoundingShape2D.gameObject.scene == content)
        { UpdateLens(); return; }
        var target = new GameObject("Room Camera Bounds");
        SceneManager.MoveGameObjectToScene(target, content);
        target.layer = LayerMask.NameToLayer("Ignore Raycast");
        target.transform.position = roomBounds.center;
        var boundary = target.AddComponent<PolygonCollider2D>();
        boundary.isTrigger = true;
        Vector2 half = roomBounds.extents;
        boundary.points = new[] { new Vector2(-half.x, -half.y), new Vector2(-half.x, half.y),
            new Vector2(half.x, half.y), new Vector2(half.x, -half.y) };
        confiner.BoundingShape2D = boundary;
        confiner.Damping = 0;
        confiner.SlowingDistance = 0;
        confiner.InvalidateBoundingShapeCache();
        previousAspect = 0;
        UpdateLens();
    }

    private void LateUpdate() { if (configured) UpdateLens(); }

    private void UpdateLens()
    {
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9;
        if (Mathf.Approximately(aspect, previousAspect)) return;
        previousAspect = aspect;
        var lens = virtualCamera.Lens;
        lens.OrthographicSize = Mathf.Max(.1f, Mathf.Min(originalSize, roomBounds.extents.y, roomBounds.extents.x / aspect));
        virtualCamera.Lens = lens;
        confiner.InvalidateLensCache();
    }
}
