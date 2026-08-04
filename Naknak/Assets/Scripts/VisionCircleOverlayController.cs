using UnityEngine;
using UnityEngine.UI;

public class VisionCircleOverlayController : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private Transform player;

	[SerializeField]
	private Camera targetCamera;

	[SerializeField]
	private Image overlayImage;

	[Header("Overlay Fit")]
	[SerializeField]
	private bool fitToFullScreen = true;

	[Header("Vision Settings")]
	[SerializeField]
	private float visionRadiusWorld = 2.1f;

	[SerializeField]
	private float softness = 0.08f;

	[Header("Center Fixed")]
	[SerializeField]
	private bool fixedToScreenCenter = true;

	[Header("Dark Overlay")]
	[SerializeField]
	private Color darkColor = new Color(0f, 0f, 0f, 0.82f);

	[Header("Noise Settings")]
	[SerializeField]
	private bool noiseAlwaysOnForTest;

	[SerializeField]
	private float noisePower = 0.28f;

	[SerializeField]
	private float noiseSpeed = 20f;

	[SerializeField]
	private float noiseScale = 85f;

	[SerializeField]
	private float noiseWidth = 0.08f;

	private Material runtimeMaterial;

	private bool noiseActive;

	private void Awake()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}
		if (player == null)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			if (gameObject != null)
			{
				player = gameObject.transform;
			}
		}
		if (overlayImage == null)
		{
			overlayImage = GetComponent<Image>();
		}
		FitOverlayToFullScreen();
		if (overlayImage != null && overlayImage.material != null)
		{
			runtimeMaterial = new Material(overlayImage.material);
			overlayImage.material = runtimeMaterial;
		}
		HideVision();
	}

	private void LateUpdate()
	{
		if (!(targetCamera == null) && !(runtimeMaterial == null))
		{
			UpdateVisionPosition();
			UpdateNoise();
		}
	}

	private void FitOverlayToFullScreen()
	{
		if (fitToFullScreen && !(overlayImage == null))
		{
			RectTransform rectTransform = overlayImage.rectTransform;
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.localScale = Vector3.one;
		}
	}

	private void UpdateVisionPosition()
	{
		if (fixedToScreenCenter)
		{
			runtimeMaterial.SetVector("_Center", new Vector4(0.5f, 0.5f, 0f, 0f));
		}
		else
		{
			if (player == null)
			{
				return;
			}
			Vector3 vector = targetCamera.WorldToViewportPoint(player.position);
			runtimeMaterial.SetVector("_Center", new Vector4(vector.x, vector.y, 0f, 0f));
		}
		float value = (float)Screen.width / (float)Screen.height;
		runtimeMaterial.SetFloat("_Aspect", value);
		if (player != null)
		{
			Vector3 vector2 = targetCamera.WorldToViewportPoint(player.position);
			float value2 = Mathf.Abs(targetCamera.WorldToViewportPoint(player.position + Vector3.right * visionRadiusWorld).x - vector2.x);
			runtimeMaterial.SetFloat("_Radius", value2);
		}
		runtimeMaterial.SetFloat("_Softness", softness);
		runtimeMaterial.SetColor("_DarkColor", darkColor);
	}

	private void UpdateNoise()
	{
		bool flag = noiseActive || noiseAlwaysOnForTest;
		runtimeMaterial.SetFloat("_NoisePower", flag ? noisePower : 0f);
		runtimeMaterial.SetFloat("_NoiseSpeed", noiseSpeed);
		runtimeMaterial.SetFloat("_NoiseScale", noiseScale);
		runtimeMaterial.SetFloat("_NoiseWidth", noiseWidth);
		runtimeMaterial.SetFloat("_TimeValue", Time.time);
	}

	public void SetNoise(bool active)
	{
		noiseActive = active;
	}

	public void ShowVision()
	{
		if (overlayImage != null)
		{
			overlayImage.enabled = true;
		}
	}

	public void HideVision()
	{
		if (overlayImage != null)
		{
			overlayImage.enabled = false;
		}
		SetNoise(active: false);
	}
}
