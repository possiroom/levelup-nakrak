using UnityEngine;
using UnityEngine.UI;

public class VisionCircleOverlayController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Image overlayImage;
    [SerializeField] private float visionRadiusWorld = 1.8f;
    [SerializeField] private float softness = 0.02f;
    [SerializeField] private bool fixedToScreenCenter = true;

    private Material runtimeMaterial;
    private bool noiseActive = false;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (overlayImage == null) overlayImage = GetComponent<Image>();
        if (overlayImage != null && overlayImage.material != null)
        {
            runtimeMaterial = new Material(overlayImage.material);
            overlayImage.material = runtimeMaterial;
        }
        HideVision();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || runtimeMaterial == null) return;
        UpdateVisionPosition();
        UpdateNoise();
    }

    private void UpdateVisionPosition()
    {
        if (fixedToScreenCenter)
        {
            runtimeMaterial.SetVector("_Center", new Vector4(0.5f, 0.5f, 0f, 0f));
        }
        else if (player != null)
        {
            Vector3 viewportPos = targetCamera.WorldToViewportPoint(player.position);
            runtimeMaterial.SetVector("_Center", new Vector4(viewportPos.x, viewportPos.y, 0f, 0f));
        }

        float aspect = (float)Screen.width / Screen.height;
        runtimeMaterial.SetFloat("_Aspect", aspect);

        if (player != null)
        {
            Vector3 centerViewport = targetCamera.WorldToViewportPoint(player.position);
            Vector3 edgeViewport = targetCamera.WorldToViewportPoint(player.position + Vector3.right * visionRadiusWorld);
            float radiusViewport = Mathf.Abs(edgeViewport.x - centerViewport.x);
            runtimeMaterial.SetFloat("_Radius", radiusViewport);
        }
        runtimeMaterial.SetFloat("_Softness", softness);
    }

    private void UpdateNoise()
    {
        runtimeMaterial.SetFloat("_NoisePower", noiseActive ? 0.35f : 0f);
        runtimeMaterial.SetFloat("_NoiseSpeed", 20f);
        runtimeMaterial.SetFloat("_NoiseScale", 80f);
        runtimeMaterial.SetFloat("_NoiseWidth", 0.06f);
        runtimeMaterial.SetFloat("_TimeValue", Time.time);
    }

    public void SetNoise(bool active) => noiseActive = active;
    public void ShowVision() { if (overlayImage != null) overlayImage.enabled = true; }
    public void HideVision() { if (overlayImage != null) overlayImage.enabled = false; SetNoise(false); }
}
