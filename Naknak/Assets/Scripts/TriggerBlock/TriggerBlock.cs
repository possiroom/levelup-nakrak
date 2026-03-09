using System.ComponentModel;
using UnityEngine;

/// <summary>
/// 실행은 TriggerSystem이 호출
/// Awake는 Override 필요
/// </summary>
public abstract class TriggerBlock : MonoBehaviour
{
    [Header("Trigger Options")]

    [SerializeField, Tooltip("플레이어가 해당 칸으로 출발 시 작동")]
    private bool DepartActive = false;

    [SerializeField, Tooltip("플레이어가 해당 칸으로 도착 시 멈춰야 하는지 여부")] 
    private bool PauseMove = false;

    [SerializeField, Tooltip("디버그 모드가 아닐 때 스프라이트를 활성화")] 
    private bool activeSprite = false;

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
        sprite.enabled = activate || activeSprite;
    }

    protected abstract void OnTriggered();

    protected virtual void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    } 
}
