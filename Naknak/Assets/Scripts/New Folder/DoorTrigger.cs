using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField]
    private DoorDirection direction;

    private RoomInstance room;

    public void Initialize(RoomInstance instance)
    {
        room = instance;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Map_Manager.Instance.MoveToNextRoom(room.RoomData, direction);
    }
}