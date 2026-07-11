using System.Collections;
using UnityEngine;

public class AlterSpawnManager : MonoBehaviour
{
    public static AlterSpawnManager Instance { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private GameObject alterPrefab;

    [SerializeField] private float blindTime = 7f;
    [SerializeField] private float spawnCheckInterval = 3f;
    [SerializeField] private float spawnOmenTime = 2f;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;

    private float remainBlindTime = 0f;
    private Coroutine spawnLoopCoroutine;
    private bool omenPlaying = false;
    private GameObject currentAlter;

    public bool IsBlindActive => remainBlindTime > 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (remainBlindTime <= 0f) return;
        remainBlindTime -= Time.deltaTime;
        if (remainBlindTime <= 0f) EndBlindDebuff();
    }

    public void TriggerBlindDebuff() => StartBlindDebuff(blindTime);

    private void StartBlindDebuff(float time)
    {
        remainBlindTime = time;
        omenPlaying = false;
        if (spawnLoopCoroutine != null) StopCoroutine(spawnLoopCoroutine);
        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private void EndBlindDebuff()
    {
        remainBlindTime = 0f;
        omenPlaying = false;
        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }
        ClearCurrentAlter();
    }

    private IEnumerator SpawnLoop()
    {
        while (remainBlindTime > 0f)
        {
            yield return new WaitForSeconds(spawnCheckInterval);
            if (remainBlindTime <= 0f) break;
            if (omenPlaying) continue;
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
        yield return new WaitForSeconds(spawnOmenTime);
        if (remainBlindTime > 0f) SpawnAlter();
        omenPlaying = false;
    }

    private void SpawnAlter()
    {
        if (player == null || alterPrefab == null) return;
        Vector3 spawnPos = player.position + (Vector3)(Random.insideUnitCircle.normalized * 2f);
        if (currentAlter != null) Destroy(currentAlter);
        currentAlter = Instantiate(alterPrefab, spawnPos, Quaternion.identity);
        AlterChaserAI chaserAI = currentAlter.GetComponent<AlterChaserAI>();
        if (chaserAI == null) chaserAI = currentAlter.GetComponentInChildren<AlterChaserAI>();
        if (chaserAI != null) chaserAI.StartChase(player);
    }

    private void ClearCurrentAlter()
    {
        if (currentAlter != null)
        {
            Destroy(currentAlter);
            currentAlter = null;
        }
    }
}
