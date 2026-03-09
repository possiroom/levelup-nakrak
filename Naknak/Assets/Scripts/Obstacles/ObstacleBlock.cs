using UnityEngine;

public abstract class ObstacleBlock : TriggerBlock
{
    [Header("Obstacle Attributes")]
    [SerializeField, Tooltip("플레이어가 점프 중 무시")] 
    private bool ignoreOnJump = false;

    [SerializeField, Tooltip("한 번 밟았을 때 오브젝트를 파괴")] 
    private bool destroyObstacle = true;

    protected override void OnTriggered()
    {
        if (ignoreOnJump && PlayerStatus.Instance.GetIsJumping()) return;

        Debug.Log("Obstacle Triggered: " + gameObject.name);
        this.RunObstacleAction();
        if (destroyObstacle) {
            TriggerExecutor.Instance.RemoveIndex(this);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isJumping">현재 플레이어가 점프 중인지 전달합니다.</param>
    /// <returns>Action을 실행하고 나서 오브젝트를 다시 사용할 수 있는지 여부를 반환합니다.</returns>
    protected abstract void RunObstacleAction();
    public abstract void LoadOnCamera();
}
