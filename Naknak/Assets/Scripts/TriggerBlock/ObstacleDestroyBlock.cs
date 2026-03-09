using System.Collections;
using UnityEngine;

public class ObstacleDestroyBlock : TriggerBlock
{
    [Header("Obstacle Destory Attributes")]
    [SerializeField] private ObstacleBlock obstacle;
    [SerializeField] private float destroyTime = 1f;
    private bool destroyWait = false;

    protected override void OnTriggered()
    {
        if (obstacle == null)
        {
            TriggerExecutor.Instance.RemoveIndex(this);
            Destroy(gameObject);
            return;
        }
        if (destroyWait) return;
        StartCoroutine(DestroyObstacle());
        Debug.Log("[ObstacleDestroyBlock] 카메라 내부 진입, 트리거 발동: " + destroyTime + "초");
    }

    private IEnumerator DestroyObstacle()
    {
        destroyWait = true;
        yield return new WaitForSeconds(destroyTime);
        TriggerExecutor.Instance.RemoveIndex(this);
        if (obstacle != null) obstacle.LoadOnCamera();
        Destroy(gameObject);
    }
}
