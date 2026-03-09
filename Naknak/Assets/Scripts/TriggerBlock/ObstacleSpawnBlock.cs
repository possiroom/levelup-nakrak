using System.Collections;
using UnityEngine;

public class ObstacleSpawnBlock : TriggerBlock
{
    [Header("Obstacle Spawn Attributes")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float waitTime = 0f;
    [SerializeField] private Vector2 position;
    private bool trigWait = false;

    protected override void OnTriggered()
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("Obstacle Prefab is NULL of " + gameObject.name);
            TriggerExecutor.Instance.RemoveIndex(this);
            Destroy(gameObject);
            return;
        }
        if (trigWait) return;
        StartCoroutine(SpawnObstacle());
    }

    private IEnumerator SpawnObstacle()
    {
        trigWait = true;
        yield return new WaitForSeconds(waitTime);
        TriggerExecutor.Instance.RemoveIndex(this);
        if (obstaclePrefab != null)
        {
            GameObject obstacle = Instantiate(obstaclePrefab, MapManager.Instance.GetTriggerTransform());
            obstacle.transform.position = (Vector3)position;
            Debug.Log("[ObstacleSpawnBlock] Spawn Obstacle: " + obstacle.name);
        }
        Destroy(gameObject);
    }
}
