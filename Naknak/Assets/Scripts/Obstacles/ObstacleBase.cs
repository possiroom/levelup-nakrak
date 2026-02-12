using UnityEngine;

public abstract class ObstacleBase : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="isJumping">현재 플레이어가 점프 중인지 전달합니다.</param>
    /// <returns>Action을 실행하고 나서 오브젝트를 다시 사용할 수 있는지 여부를 반환합니다.</returns>
    public abstract bool RunObstacleAction(bool isJumping = false);
    public abstract void LoadOnCamera();
}
