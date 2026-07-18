using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    private RoomData roomData;

    public RoomData RoomData => roomData;

    public void Initialize(RoomData data)
    {
        roomData = data;

        DoorTrigger[] doors = GetComponentsInChildren<DoorTrigger>();

        foreach (DoorTrigger door in doors)
        {
            door.Initialize(this);
        }
    }
}