using UnityEngine;
using UnityEngine.UI;

public class VisionCircleOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Image overlayImage;

    [Header("Vision Settings")]
    [SerializeField] private float visionRadiusWorld = 1.8f;
    [SerializeField] private float softness = 0.02f;

    [Header("Center Fixed")]
    [SerializeField] private bool fixedToScreenCenter = true;

    [Header("Noise Settings")]
    [SerializeField] private bool noiseAlwaysOnForTest = false;
    [SerializeField] private float noisePower = 0.35f;
    [SerializeField] private float noiseSpeed = 20f;
    [SerializeField] private float noiseScale = 80f;
    [SerializeField] private float noiseWidth = 0.06f;

    private Material runtimeMaterial;
    private bool noiseActive = false;

    private void Awake()
    {
        // 카메라가 비어 있으면 메인 카메라 사용
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // 오버레이 이미지가 비어 있으면 자기 자신에서 찾음
        if (overlayImage == null)
        {
            overlayImage = GetComponent<Image>();
        }

        // 원본 머티리얼을 복사해서 런타임 전용으로 사용
        if (overlayImage != null && overlayImage.material != null)
        {
            runtimeMaterial = new Material(overlayImage.material);
            overlayImage.material = runtimeMaterial;
        }

        // 시작 시 시야 효과 숨김
        HideVision();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || runtimeMaterial == null)
            return;

        // 시야 위치와 노이즈 상태 갱신
        UpdateVisionPosition();
        UpdateNoise();
    }

    private void UpdateVisionPosition()
    {
        if (fixedToScreenCenter)
        {
            // 시야 중심을 화면 중앙에 고정
            runtimeMaterial.SetVector("_Center", new Vector4(0.5f, 0.5f, 0f, 0f));
        }
        else
        {
            if (player == null)
                return;

            // 플레이어 위치를 화면 좌표로 변환
            Vector3 viewportPos = targetCamera.WorldToViewportPoint(player.position);
            runtimeMaterial.SetVector("_Center", new Vector4(viewportPos.x, viewportPos.y, 0f, 0f));
        }

        // 화면 비율 보정
        float aspect = (float)Screen.width / Screen.height;
        runtimeMaterial.SetFloat("_Aspect", aspect);

        if (player != null)
        {
            // 월드 기준 시야 반지름을 화면 좌표 반지름으로 변환
            Vector3 centerViewport = targetCamera.WorldToViewportPoint(player.position);
            Vector3 edgeViewport = targetCamera.WorldToViewportPoint(player.position + Vector3.right * visionRadiusWorld);

            float radiusViewport = Mathf.Abs(edgeViewport.x - centerViewport.x);

            runtimeMaterial.SetFloat("_Radius", radiusViewport);
        }

        // 경계 부드러움 적용
        runtimeMaterial.SetFloat("_Softness", softness);
    }

    private void UpdateNoise()
    {
        // 테스트 옵션이 켜져 있으면 항상 노이즈 활성화
        bool finalNoiseState = noiseActive || noiseAlwaysOnForTest;

        // 노이즈 값들을 쉐이더에 전달
        runtimeMaterial.SetFloat("_NoisePower", finalNoiseState ? noisePower : 0f);
        runtimeMaterial.SetFloat("_NoiseSpeed", noiseSpeed);
        runtimeMaterial.SetFloat("_NoiseScale", noiseScale);
        runtimeMaterial.SetFloat("_NoiseWidth", noiseWidth);
        runtimeMaterial.SetFloat("_TimeValue", Time.time);
    }

    public void SetNoise(bool active)
    {
        // 노이즈 활성화 여부 설정
        noiseActive = active;
    }

    public void ShowVision()
    {
        // 오버레이 표시
        if (overlayImage != null)
        {
            overlayImage.enabled = true;
        }
    }

    public void HideVision()
    {
        // 오버레이 숨김
        if (overlayImage != null)
        {
            overlayImage.enabled = false;
        }

        // 숨길 때 노이즈도 비활성화
        SetNoise(false);
    }
}
