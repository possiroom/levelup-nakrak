using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AlterSpawnManager : MonoBehaviour
{
    public static AlterSpawnManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject alterPrefab;
    [SerializeField] private VisionCircleOverlayController visionOverlay;

    [Header("Vision Overlay Object")]
    [SerializeField] private GameObject visionOverlayObject;
    [SerializeField] private bool hideVisionOverlayObjectWhenEnd = true;

    [Header("Blind Debuff")]
    [SerializeField] private float blindTime = 7f;

    [Header("Spawn")]
    [SerializeField] private float spawnOmenTime = 2f;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;

    [Header("Boundary Spawn")]
    [SerializeField] private float boundaryRadius = 0.6f;
    [SerializeField] private float boundaryRandomOffset = 0.05f;

    [Header("World Vision Effect")]
    [SerializeField] private bool useWorldVisionEffect = true;
    [SerializeField] private float worldVisionRadius = 2.1f;
    [SerializeField] private float worldVisionSoftness = 0.8f;
    [SerializeField, Range(0f, 1f)] private float outsideSaturation = 0.08f;
    [SerializeField, Range(0f, 1f)] private float outsideDarkness = 0.58f;
    [SerializeField] private float worldNoiseScale = 18f;
    [SerializeField] private float omenWorldNoisePower = 1.2f;
    [SerializeField] private float omenWorldNoiseScale = 42f;

    private float remainBlindTime = 0f;
    private Coroutine spawnCoroutine;

    private GameObject currentAlter;
    private Material worldVisionMaterial;
    private bool worldVisionApplied = false;
    private bool omenNoiseActive = false;
    private readonly List<RendererMaterialState> rendererMaterialStates = new List<RendererMaterialState>();

    public bool IsBlindActive => remainBlindTime > 0f;
    public float RemainBlindTime => remainBlindTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (visionOverlay == null)
            visionOverlay = FindFirstObjectByType<VisionCircleOverlayController>(FindObjectsInactive.Include);

        if (visionOverlayObject == null && visionOverlay != null)
        {
            visionOverlayObject = visionOverlay.gameObject;
        }
    }

    private void Update()
    {
        if (remainBlindTime <= 0f)
            return;

        UpdateWorldVisionEffect();

        remainBlindTime -= Time.deltaTime;

        if (remainBlindTime <= 0f)
        {
            EndBlindDebuff();
        }
    }

    public void TriggerBlindDebuff()
    {
        StartBlindDebuff(blindTime);
    }

    public void TriggerBlindDebuff(float time)
    {
        StartBlindDebuff(time);
    }

    public void TriggerBlindDebuffAndRollSpawn(float time)
    {
        StartBlindDebuff(time);
        DecideSpawnAfterBlindDebuff();
    }

    private void StartBlindDebuff(float time)
    {
        ResolveReferences();

        remainBlindTime = time;
        Debug.Log($"[BlindDebuff] Start {time:0.##}s / overlay:{visionOverlay != null} / overlayObject:{visionOverlayObject != null} / worldEffect:{useWorldVisionEffect}");

        ActivateVisionOverlay();
        ApplyWorldVisionEffect();

        if (visionOverlay != null)
        {
            visionOverlay.ShowVision();
        }

        SetOmenNoise(false);
    }

    private void EndBlindDebuff()
    {
        remainBlindTime = 0f;

        if (visionOverlay != null)
        {
            visionOverlay.HideVision();
        }

        SetOmenNoise(false);

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        ClearCurrentAlter();
        ClearWorldVisionEffect();

        if (hideVisionOverlayObjectWhenEnd)
        {
            DeactivateVisionOverlay();
        }
    }

    private void ActivateVisionOverlay()
    {
        if (visionOverlayObject != null)
        {
            visionOverlayObject.SetActive(true);
        }
        else if (visionOverlay != null)
        {
            visionOverlay.gameObject.SetActive(true);
            visionOverlayObject = visionOverlay.gameObject;
        }
    }

    private void DeactivateVisionOverlay()
    {
        if (visionOverlayObject != null)
        {
            visionOverlayObject.SetActive(false);
        }
        else if (visionOverlay != null)
        {
            visionOverlay.gameObject.SetActive(false);
        }
    }

    private void DecideSpawnAfterBlindDebuff()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        SetOmenNoise(false);

        if (Random.value > spawnChance)
            return;

        spawnCoroutine = StartCoroutine(SpawnWithOmen());
    }

    private IEnumerator SpawnWithOmen()
    {
        ActivateVisionOverlay();

        SetOmenNoise(true);

        yield return new WaitForSeconds(spawnOmenTime);

        SetOmenNoise(false);

        if (remainBlindTime > 0f)
        {
            SpawnAlter();
        }

        spawnCoroutine = null;
    }

    private void SpawnAlter()
    {
        if (player == null || alterPrefab == null)
            return;

        Vector3 spawnPos = GetBoundarySpawnPosition();

        if (currentAlter != null)
        {
            Destroy(currentAlter);
        }

        currentAlter = Instantiate(alterPrefab, spawnPos, Quaternion.identity);
        currentAlter.transform.position = spawnPos;
        currentAlter.transform.rotation = Quaternion.identity;
        currentAlter.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer[] renderers = currentAlter.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderers)
        {
            sr.enabled = true;
            sr.color = Color.white;

            if (playerRenderer != null)
            {
                sr.sortingLayerID = playerRenderer.sortingLayerID;
                sr.sortingOrder = playerRenderer.sortingOrder + 50;
            }
            else
            {
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 999;
            }
        }

        AlterChaserAI chaserAI = currentAlter.GetComponent<AlterChaserAI>();

        if (chaserAI == null)
        {
            chaserAI = currentAlter.GetComponentInChildren<AlterChaserAI>();
        }

        if (chaserAI != null)
        {
            chaserAI.StartChase(player);
        }
    }

    private void ApplyWorldVisionEffect()
    {
        if (!useWorldVisionEffect || worldVisionApplied)
            return;

        Shader shader = Shader.Find("Custom/VisionDesaturateWorld");
        if (shader == null)
        {
            Debug.LogWarning("[BlindDebuff] Custom/VisionDesaturateWorld shader를 찾을 수 없습니다.");
            return;
        }

        worldVisionMaterial = new Material(shader);
        UpdateWorldVisionMaterial();

        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Renderer targetRenderer in renderers)
        {
            if (!CanApplyWorldVisionEffect(targetRenderer))
                continue;

            Material[] originalMaterials = targetRenderer.sharedMaterials;
            Material[] effectMaterials = new Material[originalMaterials.Length];

            for (int i = 0; i < effectMaterials.Length; i++)
                effectMaterials[i] = worldVisionMaterial;

            rendererMaterialStates.Add(new RendererMaterialState(targetRenderer, originalMaterials));
            targetRenderer.sharedMaterials = effectMaterials;
        }

        worldVisionApplied = true;
        Debug.Log($"[BlindDebuff] World vision effect applied to {rendererMaterialStates.Count} renderers.");
    }

    private bool CanApplyWorldVisionEffect(Renderer targetRenderer)
    {
        if (targetRenderer == null)
            return false;

        if (targetRenderer.GetComponentInParent<Canvas>() != null)
            return false;

        if (targetRenderer.GetComponentInParent<AlterSpawnManager>() != null)
            return false;

        if (player != null && targetRenderer.transform.IsChildOf(player))
            return false;

        return targetRenderer is SpriteRenderer || targetRenderer is TilemapRenderer;
    }

    private void UpdateWorldVisionEffect()
    {
        if (!worldVisionApplied || worldVisionMaterial == null)
            return;

        UpdateWorldVisionMaterial();
    }

    private void UpdateWorldVisionMaterial()
    {
        if (worldVisionMaterial == null)
            return;

        Vector3 center = player != null ? player.position : Vector3.zero;

        worldVisionMaterial.SetVector("_VisionCenter", new Vector4(center.x, center.y, center.z, 0f));
        worldVisionMaterial.SetFloat("_Radius", worldVisionRadius);
        worldVisionMaterial.SetFloat("_Softness", worldVisionSoftness);
        worldVisionMaterial.SetFloat("_OutsideSaturation", outsideSaturation);
        worldVisionMaterial.SetFloat("_OutsideDarkness", outsideDarkness);
        worldVisionMaterial.SetFloat("_NoisePower", omenNoiseActive ? omenWorldNoisePower : 0f);
        worldVisionMaterial.SetFloat("_NoiseScale", omenNoiseActive ? omenWorldNoiseScale : worldNoiseScale);
        worldVisionMaterial.SetFloat("_TimeValue", Time.time);
    }

    private void ClearWorldVisionEffect()
    {
        if (!worldVisionApplied)
            return;

        foreach (RendererMaterialState state in rendererMaterialStates)
        {
            if (state.Renderer != null)
                state.Renderer.sharedMaterials = state.Materials;
        }

        rendererMaterialStates.Clear();

        if (worldVisionMaterial != null)
        {
            Destroy(worldVisionMaterial);
            worldVisionMaterial = null;
        }

        worldVisionApplied = false;
    }

    private void SetOmenNoise(bool active)
    {
        omenNoiseActive = active;

        if (active)
        {
            ActivateVisionOverlay();
            if (visionOverlay != null)
                visionOverlay.ShowVision();
        }

        if (visionOverlay != null)
            visionOverlay.SetNoise(active);

        UpdateWorldVisionEffect();
    }

    private Vector3 GetBoundarySpawnPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ).normalized;

        float radius = boundaryRadius + Random.Range(
            -boundaryRandomOffset,
            boundaryRandomOffset
        );

        Vector3 spawnPos = player.position + new Vector3(
            direction.x * radius,
            direction.y * radius,
            0f
        );

        spawnPos.z = 0f;

        return spawnPos;
    }

    public void ClearCurrentAlter()
    {
        if (currentAlter != null)
        {
            Destroy(currentAlter);
            currentAlter = null;
        }
    }

    private struct RendererMaterialState
    {
        public readonly Renderer Renderer;
        public readonly Material[] Materials;

        public RendererMaterialState(Renderer renderer, Material[] materials)
        {
            Renderer = renderer;
            Materials = materials;
        }
    }
}
