using System.Collections;
using UnityEngine;

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
    [SerializeField] private float spawnCheckInterval = 3f;
    [SerializeField] private float spawnOmenTime = 2f;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;

    [Header("Boundary Spawn")]
    [SerializeField] private float boundaryRadius = 0.6f;
    [SerializeField] private float boundaryRandomOffset = 0.05f;

    private float remainBlindTime = 0f;
    private Coroutine spawnLoopCoroutine;
    private bool omenPlaying = false;

    private GameObject currentAlter;

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

        if (visionOverlayObject == null && visionOverlay != null)
        {
            visionOverlayObject = visionOverlay.gameObject;
        }
    }

    private void Update()
    {
        if (remainBlindTime <= 0f)
            return;

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

    private void StartBlindDebuff(float time)
    {
        remainBlindTime = time;
        omenPlaying = false;

        ActivateVisionOverlay();

        if (visionOverlay != null)
        {
            visionOverlay.ShowVision();
            visionOverlay.SetNoise(false);
        }

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
        }

        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private void EndBlindDebuff()
    {
        remainBlindTime = 0f;
        omenPlaying = false;

        if (visionOverlay != null)
        {
            visionOverlay.SetNoise(false);
            visionOverlay.HideVision();
        }

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        ClearCurrentAlter();

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

    private IEnumerator SpawnLoop()
    {
        while (remainBlindTime > 0f)
        {
            yield return new WaitForSeconds(spawnCheckInterval);

            if (remainBlindTime <= 0f)
                break;

            if (omenPlaying)
                continue;

            if (Random.value <= spawnChance)
            {
                yield return StartCoroutine(SpawnWithOmen());
            }
        }

        spawnLoopCoroutine = null;
    }

    private IEnumerator SpawnWithOmen()
    {
        omenPlaying = true;

        ActivateVisionOverlay();

        if (visionOverlay != null)
        {
            visionOverlay.SetNoise(true);
        }

        yield return new WaitForSeconds(spawnOmenTime);

        if (visionOverlay != null)
        {
            visionOverlay.SetNoise(false);
        }

        if (remainBlindTime > 0f)
        {
            SpawnAlter();
        }

        omenPlaying = false;
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
}