using UnityEngine;

public class Map_Manager : MonoBehaviour
{
    public static Map_Manager Instance;

    [SerializeField]
    private RoomData[] rooms;

    [SerializeField]
    private Transform roomRoot;

    private GameObject currentRoomObject;

    private RoomData currentRoom;
    public RoomData CurrentRoom => currentRoom;

    private RoomGraph graph;
    public RoomGraph Graph => graph;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        graph = new RoomGraph();

        foreach (RoomData room in rooms)
            graph.AddRoom(room);

        LoadRoom(rooms[0]);

        AIManager.Instance.PlayerEnteredRoom(0);
    }

    public void LoadRoom(RoomData roomData)
    {
        if (currentRoomObject != null)
            Destroy(currentRoomObject);

        currentRoomObject =
            Instantiate(roomData.roomPrefab, roomRoot);

        RoomInstance instance =
            currentRoomObject.GetComponent<RoomInstance>();

        instance.Initialize(roomData);

        currentRoom = roomData;
    }

    public void MoveToNextRoom(RoomData room, DoorDirection direction)
    {
        int nextRoomID = -1;

        switch (direction)
        {
            case DoorDirection.Up:
                nextRoomID = room.upRoom;
                break;

            case DoorDirection.Down:
                nextRoomID = room.downRoom;
                break;

            case DoorDirection.Left:
                nextRoomID = room.leftRoom;
                break;

            case DoorDirection.Right:
                nextRoomID = room.rightRoom;
                break;
        }

        if (nextRoomID == -1)
            return;

        LoadRoom(rooms[nextRoomID]);

        AIManager.Instance.PlayerEnteredRoom(nextRoomID);
    }
}