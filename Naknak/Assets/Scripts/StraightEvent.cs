using UnityEngine;

/// <summary>
/// StraightGamemap에서 플레이어가 지정된 y범위를 벗어나면
/// 원래 스폰 위치로 되돌리는 역할을 하는 컴포넌트입니다.
/// Rigidbody 또는 CharacterController가 함께 붙어 있으면 물리 상태도 함께 리셋합니다.
/// </summary>
public class StraightEvent : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY = 1f;
    [SerializeField] private bool useXBounds = false;
    [SerializeField] private float minX = -0.5f;
    [SerializeField] private float maxX = 0.5f;

    [Header("Leave Tile Settings")]
    [SerializeField] private bool restartOnLeaveBounds = false;
    [SerializeField] private bool damageOnLeave = false;
    [SerializeField] private int leaveDamage = 100;
    [SerializeField] private bool respawnOnLeaveIfNoHealth = false;

    [Header("Respawn Settings")]
    [SerializeField] private bool resetCharacterController = true;
    [SerializeField] private bool resetRigidbodyVelocity = true;
    [SerializeField] private bool restartOnInvalidDirectionInput = true;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Rigidbody targetRigidbody;
    private CharacterController targetController;
    private PlayerMoveController3 targetMoveController;
    private bool isRestarting;

    void Start()
    {
        AssignTargetTransform();

        spawnPosition = targetTransform.position;
        spawnRotation = targetTransform.rotation;

        targetRigidbody = targetTransform.GetComponent<Rigidbody>();
        targetController = targetTransform.GetComponent<CharacterController>();
        targetMoveController = targetTransform.GetComponent<PlayerMoveController3>();
    }

    void Update()
    {
        if (restartOnInvalidDirectionInput
            && !isRestarting
            && GameStateManager.Instance != null
            && GameStateManager.Instance.GameState == GameState.Gameplay
            && IsTargetMoving()
            && IsInvalidDirectionPressed())
        {
            Debug.Log("[StraightEvent] Invalid direction pressed: restart current map.");
            RestartFromInitialState();
            return;
        }

        if (!restartOnLeaveBounds || targetTransform == null) return;

        Vector3 currentPosition = targetTransform.position;
        bool outOfYBounds = currentPosition.y < minY || currentPosition.y > maxY;
        bool outOfXBounds = useXBounds && (currentPosition.x < minX || currentPosition.x > maxX);

        if (outOfYBounds || outOfXBounds)
        {
            if (damageOnLeave && PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.AddHealth(-leaveDamage);
                if (respawnOnLeaveIfNoHealth && PlayerStatus.Instance.GetHealth() <= 0)
                {
                    RestartFromInitialState();
                }
            }
            else
            {
                RestartFromInitialState();
            }
        }
    }

    /// <summary>
    /// 외부에서 호출할 수 있는 스폰 위치 리셋 메서드입니다.
    /// Rigidbody 또는 CharacterController 상태도 필요 시 함께 리셋합니다.
    /// </summary>
    public void ResetToSpawn()
    {
        if (targetTransform == null) return;

        if (resetCharacterController && targetController != null)
        {
            targetController.enabled = false;
            targetTransform.position = spawnPosition;
            targetTransform.rotation = spawnRotation;
            targetController.enabled = true;
        }
        else
        {
            targetTransform.position = spawnPosition;
            targetTransform.rotation = spawnRotation;
        }

        if (resetRigidbodyVelocity && targetRigidbody != null)
        {
            targetRigidbody.linearVelocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;

            if (!targetRigidbody.isKinematic)
            {
                targetRigidbody.position = spawnPosition;
                targetRigidbody.rotation = spawnRotation;
            }
        }
    }

    private bool IsInvalidDirectionPressed()
    {
        return Input.GetKey(KeyCode.LeftArrow)
            || Input.GetKey(KeyCode.RightArrow)
            || Input.GetKey(KeyCode.DownArrow)
            || Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.S);    
    }

    private bool IsTargetMoving()
    {
        return targetMoveController != null && targetMoveController.isMoving;
    }

    private void AssignTargetTransform()
    {
        if (targetTransform != null) return;

        if (PlayerStatus.Instance != null)
        {
            targetTransform = PlayerStatus.Instance.transform;
            return;
        }

        targetTransform = transform;
    }

    private void RestartFromInitialState()
    {
        if (MapManager.Instance != null)
        {
            isRestarting = true;
            MapManager.Instance.RestartCurrentMap();
        }
    }
}
