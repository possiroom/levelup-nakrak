using UnityEngine;

public class RoomDoorTrigger : TriggerBlock
{
    [Header("Door Info")]
    [SerializeField] private int roomId;
    [SerializeField] private DoorDirection exitDirection;

    protected override void OnTriggered()
    {
        Debug.Log("[RoomDoorTriggerBlock] 문 트리거 실행: Room " + roomId + " / " + exitDirection);

        if (MansionRoomSystem.Instance == null)
        {
            Debug.LogError("[RoomDoorTriggerBlock] MansionRoomSystem.Instance가 없습니다.");
            return;
        }

        MansionRoomSystem.Instance.EnterDoor(roomId, exitDirection);
    }
}