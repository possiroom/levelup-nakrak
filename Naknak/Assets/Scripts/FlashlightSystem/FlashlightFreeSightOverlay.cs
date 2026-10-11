using UnityEngine;
using UnityEngine.UI;

namespace Naknak.FlashlightSystem
{
    [RequireComponent(typeof(Image))]
    public class FlashlightFreeSightOverlay : MonoBehaviour
    {
        private const int MaxShaderOccluders = 32;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Image overlayImage;
        [SerializeField] private Shader overlayShader;
        [SerializeField] private float visionRadiusWorld = 3f;
        [SerializeField] private float softness = 0.03f;
        [SerializeField, Range(0f, 1f)] private float darkAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float shadowAlpha = 1f;
        [SerializeField] private bool includeOnlyOccludersInsideVision = true;
        [SerializeField] private float shadowPaddingViewport = 0.01f;

        private readonly Vector4[] occluderRects = new Vector4[MaxShaderOccluders];
        private readonly Vector4[] shadowDirections = new Vector4[MaxShaderOccluders];
        private Material runtimeMaterial;
        private Vector3 sightCenter;
        private bool flashlightVisible = false;

        public float VisionRadiusWorld => visionRadiusWorld;
        public float Softness => softness;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (overlayImage == null)
                overlayImage = GetComponent<Image>();

            if (overlayShader == null)
                overlayShader = Shader.Find("Naknak/Flashlight Free Sight Overlay");

            if (overlayImage != null)
            {
                Material sourceMaterial = overlayImage.material;
                if (sourceMaterial != null && sourceMaterial.shader == overlayShader)
                    runtimeMaterial = new Material(sourceMaterial);
                else if (overlayShader != null)
                    runtimeMaterial = new Material(overlayShader);

                if (runtimeMaterial != null)
                    overlayImage.material = runtimeMaterial;
                else
                    overlayImage.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (runtimeMaterial == null || targetCamera == null)
                return;

            UpdateShaderValues();
        }

        public void SetActive(bool active)
        {
            if (overlayImage != null)
                overlayImage.enabled = active;
        }

        public void SetSightCenter(Vector3 worldCenter)
        {
            sightCenter = worldCenter;
        }

        public void SetFlashlightVisible(bool visible)
        {
            flashlightVisible = visible;
        }

        private void UpdateShaderValues()
        {
            Vector3 centerViewport = targetCamera.WorldToViewportPoint(sightCenter);
            Vector3 edgeViewport = targetCamera.WorldToViewportPoint(sightCenter + Vector3.right * visionRadiusWorld);
            float radiusViewport = Mathf.Abs(edgeViewport.x - centerViewport.x);
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

            runtimeMaterial.SetVector("_Center", new Vector4(centerViewport.x, centerViewport.y, 0f, 0f));
            runtimeMaterial.SetFloat("_Radius", radiusViewport);
            runtimeMaterial.SetFloat("_Softness", softness);
            runtimeMaterial.SetFloat("_Aspect", aspect);
            runtimeMaterial.SetFloat("_DarkAlpha", darkAlpha);
            runtimeMaterial.SetFloat("_ShadowAlpha", shadowAlpha);
            runtimeMaterial.SetFloat("_FlashlightVisible", flashlightVisible ? 1f : 0f);

            int count = BuildOccluderShaderData(centerViewport, radiusViewport);
            runtimeMaterial.SetInt("_OccluderCount", count);
            runtimeMaterial.SetVectorArray("_OccluderRects", occluderRects);
            runtimeMaterial.SetVectorArray("_ShadowDirections", shadowDirections);
        }

        private int BuildOccluderShaderData(Vector3 centerViewport, float radiusViewport)
        {
            int count = 0;
            foreach (FlashlightOccluder occluder in FlashlightOccluder.All)
            {
                if (occluder == null || !occluder.BlocksVision)
                    continue;

                Bounds bounds = occluder.GetWorldBounds();
                Vector4 viewportRect = GetViewportRect(bounds);
                if (!IsRectOnScreen(viewportRect))
                    continue;

                Vector3 occluderCenterViewport = targetCamera.WorldToViewportPoint(bounds.center);
                if (includeOnlyOccludersInsideVision)
                {
                    float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
                    Vector2 diff = new Vector2(
                        (occluderCenterViewport.x - centerViewport.x) * aspect,
                        occluderCenterViewport.y - centerViewport.y);

                    if (diff.magnitude > radiusViewport + GetRectApproxRadius(viewportRect))
                        continue;
                }

                occluderRects[count] = viewportRect;
                shadowDirections[count] = GetShadowDirection(centerViewport, occluderCenterViewport);
                count++;

                if (count >= MaxShaderOccluders)
                    break;
            }

            for (int i = count; i < MaxShaderOccluders; i++)
            {
                occluderRects[i] = Vector4.zero;
                shadowDirections[i] = Vector4.zero;
            }

            return count;
        }

        private Vector4 GetViewportRect(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, bounds.center.z),
                new Vector3(min.x, max.y, bounds.center.z),
                new Vector3(max.x, min.y, bounds.center.z),
                new Vector3(max.x, max.y, bounds.center.z)
            };

            Vector2 rectMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 rectMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            foreach (Vector3 corner in corners)
            {
                Vector3 viewport = targetCamera.WorldToViewportPoint(corner);
                rectMin = Vector2.Min(rectMin, viewport);
                rectMax = Vector2.Max(rectMax, viewport);
            }

            rectMin -= Vector2.one * shadowPaddingViewport;
            rectMax += Vector2.one * shadowPaddingViewport;
            return new Vector4(rectMin.x, rectMin.y, rectMax.x, rectMax.y);
        }

        private static bool IsRectOnScreen(Vector4 rect)
        {
            return rect.z >= 0f && rect.x <= 1f && rect.w >= 0f && rect.y <= 1f;
        }

        private static float GetRectApproxRadius(Vector4 rect)
        {
            return Vector2.Distance(new Vector2(rect.x, rect.y), new Vector2(rect.z, rect.w)) * 0.5f;
        }

        private static Vector4 GetShadowDirection(Vector3 originViewport, Vector3 occluderViewport)
        {
            Vector2 delta = occluderViewport - originViewport;

            if (Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
                return delta.y >= 0f ? new Vector4(0f, 1f, 0f, 0f) : new Vector4(0f, -1f, 0f, 0f);

            return delta.x >= 0f ? new Vector4(1f, 0f, 0f, 0f) : new Vector4(-1f, 0f, 0f, 0f);
        }
    }
}
