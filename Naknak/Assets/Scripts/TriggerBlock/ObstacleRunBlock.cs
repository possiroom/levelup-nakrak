using UnityEngine;

public sealed class ObstacleRunBlock : TriggerBlock
{
    [Header("Obstacle Attributes")]
    [SerializeField] private ObstacleBase obstacle;
    [SerializeField] private bool ignoreOnJump = false;

    private bool connectObstacle = false;

    protected override void OnTriggered()
    {
        if (!connectObstacle) return;
        if (obstacle == null) {
            TriggerExecutor.Instance.RemoveIndex(this);
            Destroy(gameObject);
            return;
        }

        // 만약 점프 중일 때 발동을 무시하지 않으면 무조건 false를 보냄
        connectObstacle = obstacle.RunObstacleAction(ignoreOnJump && MapManager.Instance.GetPlayerIsJumping());
        if (!connectObstacle) {
            obstacle = null;
            TriggerExecutor.Instance.RemoveIndex(this);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (obstacle != null) connectObstacle = true;
    } 
}
