using UnityEngine;

public class RoomDoorTrigger : TriggerBlock
{
    [Header("Door Info")]
    [SerializeField] private int roomId;
    [SerializeField] private DoorDirection exitDirection;
    [SerializeField] private float keepReadyDistance = 1.2f;

    public int RoomId => roomId;
    public DoorDirection ExitDirection => exitDirection;

    private static RoomDoorTrigger pendingDoor;
    private Transform playerTransform;

    private void Update()
    {
        if (pendingDoor != this)
            return;

        if (!IsPlayerNearThisDoor())
        {
            pendingDoor = null;
            return;
        }

        if (!CanUseDoorInput())
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || IsExitDirectionKeyDown())
            EnterPendingDoor();
    }

    protected override void OnTriggered()
    {
        pendingDoor = this;
        Debug.Log("[RoomDoorTriggerBlock] Door ready. Press Enter: Room " + roomId + " / " + exitDirection);
    }

    private void EnterPendingDoor()
    {
        if (MansionRoomSystem.Instance == null)
        {
            Debug.LogError("[RoomDoorTriggerBlock] MansionRoomSystem.Instance is null.");
            return;
        }

        Debug.Log("[RoomDoorTriggerBlock] Enter pressed. Move room: Room " + roomId + " / " + exitDirection);
        pendingDoor = null;
        MansionRoomSystem.Instance.EnterDoor(roomId, exitDirection);
    }

    private bool CanUseDoorInput()
    {
        return GameStateManager.Instance == null || GameStateManager.Instance.GameState == GameState.Gameplay;
    }

    private bool IsExitDirectionKeyDown()
    {
        switch (exitDirection)
        {
            case DoorDirection.Up:
                return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);

            case DoorDirection.Down:
                return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);

            case DoorDirection.Left:
                return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);

            case DoorDirection.Right:
                return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);

            default:
                return false;
        }
    }

    private bool IsPlayerNearThisDoor()
    {
        if (playerTransform == null)
        {
            PlayerMoveController3 player = FindFirstObjectByType<PlayerMoveController3>();
            if (player == null)
                return true;

            playerTransform = player.transform;
        }

        if (Vector2.Distance(playerTransform.position, transform.position) <= keepReadyDistance)
            return true;

        if (TriggerExecutor.Instance == null || TriggerExecutor.Instance.grid == null)
            return false;

        Vector3Int playerCell = TriggerExecutor.Instance.grid.WorldToCell(playerTransform.position - new Vector3(0f, 0.5f, 0f));
        Vector3Int doorCell = TriggerExecutor.Instance.grid.WorldToCell(transform.position);
        return playerCell == doorCell;
    }
}
