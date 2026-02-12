using UnityEngine;

/// <summary>
/// 실행은 TriggerSystem이 호출
/// Awake는 Override 필요
/// </summary>
public abstract class TriggerBlock : MonoBehaviour
{
    [Header("Trigger Options")]
    [SerializeField] private bool DepartActive = false;
    [SerializeField] private bool PauseMove = false;
    private SpriteRenderer sprite;

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

    public void ActivateSprite(bool activate)
    {
        if (sprite == null) return;
        sprite.enabled = activate;
    }

    protected abstract void OnTriggered();

    protected virtual void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    } 
}
