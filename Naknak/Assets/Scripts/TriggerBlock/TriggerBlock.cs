using UnityEngine;

/// <summary>
/// 실행은 TriggerSystem이 호출
/// </summary>
public abstract class TriggerBlock : MonoBehaviour
{
    [Header("Trigger Options")]
    [SerializeField] private bool DepartActive = false;
    [SerializeField] private bool PauseMove = false;


    /// <summary>
    /// 트리거 실행 진입점
    /// 밟았을 때 플레이어가 멈춰야 하는지 여부를 반환
    /// </summary>
    public bool DepartTrigger()
    {
        if (!DepartActive) return false;
        OnTriggered();
        return PauseMove;
    }

    public bool ArrivedTrigger()
    {
        if (DepartActive) return false;
        OnTriggered();
        return PauseMove;
    }

    protected abstract void OnTriggered();
}
