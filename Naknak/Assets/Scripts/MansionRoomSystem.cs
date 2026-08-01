using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MansionRoomSystem : MonoBehaviour
{
    public static MansionRoomSystem Instance { get; private set; }

    private const int NoRoom = 13;
    private const int MainRoomId = 11;
    private const int StudyRoomId = 8;

    [Header("Player")]
    [SerializeField] private PlayerMoveController3 playerCtrl;

    [Header("Current Room")]
    [SerializeField] private int currentRoomId = 1;

    [Header("Door Table")]
    [SerializeField] private RoomDoorData[] roomDoorTable;

    [Header("Transition")]
    [SerializeField] private float teleportDelay = 0.05f;
    [SerializeField] private bool showOnlyCurrentRoom = false;

    [Header("Space Delete")]
    [SerializeField] private bool debugPhaseHotkeys = true;
    [SerializeField] private int[] fireRoomOrder = { 1, 2, 3, 4, 5, 6 };

    [Header("Space Delete Visual")]
    [SerializeField] private GameObject fireVisualPrefab;
    [SerializeField] private GameObject deletedVisualPrefab;
    [SerializeField] private Transform visualRoot;

    private bool isTransitioning = false;

    private Dictionary<int, MansionRoom> roomMap;
    private Dictionary<int, RoomDoorData> baseDoorMap;
    private Dictionary<int, RoomDoorData> doorMap;

    private HashSet<int> deletedRooms = new HashSet<int>();

    private int currentFirePhase = 0;
    private int burningRoomId = 0;
    private bool burningRoomSeen = false;

    private GameObject fireVisualInstance;
    private Dictionary<int, GameObject> deletedVisuals = new Dictionary<int, GameObject>();

    private void Awake()
    {
        Instance = this;

        if (playerCtrl == null)
            playerCtrl = FindFirstObjectByType<PlayerMoveController3>();

        InitRoomMap();
        InitDoorMap();

        RefreshRoomVisible();
    }

    private void Update()
    {
        if (!debugPhaseHotkeys)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            StartFirePhase(1);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            StartFirePhase(2);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            StartFirePhase(3);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            StartFirePhase(4);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            StartFirePhase(5);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            StartFirePhase(6);
    }

    private void InitRoomMap()
    {
        roomMap = new Dictionary<int, MansionRoom>();

        MapContainer mapContainer = GetComponentInParent<MapContainer>();
        MansionRoom[] foundRooms;

        if (mapContainer != null)
            foundRooms = mapContainer.GetComponentsInChildren<MansionRoom>(true);
        else
            foundRooms = transform.root.GetComponentsInChildren<MansionRoom>(true);

        foreach (MansionRoom room in foundRooms)
        {
            if (room == null)
                continue;

            int id = room.RoomId;

            if (roomMap.ContainsKey(id))
            {
                Debug.LogWarning("[MansionRoomSystem] 중복 Room Id 발견: " + id + " / " + room.name);
                continue;
            }

            roomMap.Add(id, room);
            Debug.Log("[MansionRoomSystem] Room 등록됨: " + id + " / " + room.name);
        }

        Debug.Log("[MansionRoomSystem] 등록된 방 개수: " + roomMap.Count);
    }

    private void InitDoorMap()
    {
        baseDoorMap = new Dictionary<int, RoomDoorData>();
        doorMap = new Dictionary<int, RoomDoorData>();

        foreach (RoomDoorData data in roomDoorTable)
        {
            if (data == null)
                continue;

            if (!baseDoorMap.ContainsKey(data.roomId))
                baseDoorMap.Add(data.roomId, data.Clone());

            if (!doorMap.ContainsKey(data.roomId))
                doorMap.Add(data.roomId, data.Clone());
        }

        Debug.Log("[MansionRoomSystem] Door Table 등록 개수: " + doorMap.Count);
    }

    public void EnterDoor(int fromRoomId, DoorDirection exitDirection)
    {
        if (isTransitioning)
            return;

        StartCoroutine(EnterDoorRoutine(fromRoomId, exitDirection));
    }

    private IEnumerator EnterDoorRoutine(int fromRoomId, DoorDirection exitDirection)
    {
        isTransitioning = true;

        int previousRoomId = currentRoomId;

        if (!doorMap.ContainsKey(fromRoomId))
        {
            Debug.LogError("[MansionRoomSystem] Door Table에 없는 방 ID: " + fromRoomId);
            isTransitioning = false;
            yield break;
        }

        int targetRoomId = doorMap[fromRoomId].GetTargetRoom(exitDirection);

        if (targetRoomId == NoRoom)
        {
            Debug.Log("[MansionRoomSystem] 연결되지 않은 문입니다.");
            isTransitioning = false;
            yield break;
        }

        if (deletedRooms.Contains(targetRoomId))
        {
            Debug.LogError("[MansionRoomSystem] 삭제된 방으로 이동하려고 함: " + targetRoomId);
            isTransitioning = false;
            yield break;
        }

        if (!roomMap.ContainsKey(targetRoomId))
        {
            Debug.LogError("[MansionRoomSystem] Rooms에 등록되지 않은 목적지 방 ID: " + targetRoomId);
            isTransitioning = false;
            yield break;
        }

        DoorDirection enterDirection = GetOppositeDirection(exitDirection);
        MansionRoom targetRoom = roomMap[targetRoomId];
        Transform spawnPoint = targetRoom.GetSpawnPoint(enterDirection);

        if (spawnPoint == null)
        {
            Debug.LogError("[MansionRoomSystem] SpawnPoint 없음: " + targetRoomId + " / " + enterDirection);
            isTransitioning = false;
            yield break;
        }

        yield return new WaitForSeconds(teleportDelay);

        currentRoomId = targetRoomId;
        RefreshRoomVisible();

        playerCtrl.TeleportToWorld(spawnPoint.position);

        Debug.Log(
            "[RoomMove] " +
            fromRoomId + " " + exitDirection +
            " → " +
            targetRoomId + " " + enterDirection
        );

        MarkBurningRoomSeen(targetRoomId);
        TryDeleteBurningRoomAfterExit(previousRoomId);

        yield return new WaitForSeconds(0.1f);

        isTransitioning = false;
    }

    public void StartFirePhase(int phase)
    {
        if (phase < 1 || phase > fireRoomOrder.Length)
        {
            Debug.LogWarning("[MansionRoomSystem] 없는 화재 페이즈: " + phase);
            return;
        }

        int targetFireRoomId = fireRoomOrder[phase - 1];

        if (deletedRooms.Contains(targetFireRoomId))
        {
            Debug.LogWarning("[MansionRoomSystem] 이미 삭제된 방에는 화재를 발생시킬 수 없음: " + targetFireRoomId);
            return;
        }

        currentFirePhase = phase;
        burningRoomId = targetFireRoomId;
        burningRoomSeen = currentRoomId == burningRoomId;

        ShowFireVisual(burningRoomId);

        Debug.Log("[MansionRoomSystem] Phase " + phase + " 화재 발생 방: " + burningRoomId);

        if (burningRoomSeen)
            Debug.Log("[MansionRoomSystem] 현재 방이 화재 방이라 즉시 목격 처리됨: " + burningRoomId);
    }

    public void StartNextFirePhase()
    {
        StartFirePhase(currentFirePhase + 1);
    }

    private void MarkBurningRoomSeen(int roomId)
    {
        if (burningRoomId == 0)
            return;

        if (roomId != burningRoomId)
            return;

        burningRoomSeen = true;
        Debug.Log("[MansionRoomSystem] 화재 방 목격: " + roomId);
    }

    private void TryDeleteBurningRoomAfterExit(int previousRoomId)
    {
        if (burningRoomId == 0)
            return;

        if (previousRoomId != burningRoomId)
            return;

        if (!burningRoomSeen)
            return;

        DeleteRoom(burningRoomId);

        burningRoomId = 0;
        burningRoomSeen = false;
    }

    public void DeleteRoom(int roomId)
    {
        if (deletedRooms.Contains(roomId))
            return;

        deletedRooms.Add(roomId);

        HideFireVisual();
        ShowDeletedVisual(roomId);

        RebuildDoorMapByDeletedRooms();

        Debug.Log("[MansionRoomSystem] 방 삭제 완료: " + roomId);
    }

    private void RebuildDoorMapByDeletedRooms()
    {
        doorMap.Clear();

        foreach (var pair in baseDoorMap)
        {
            doorMap.Add(pair.Key, pair.Value.Clone());
        }

        List<int> sortedDeletedRooms = new List<int>(deletedRooms);
        sortedDeletedRooms.Sort();

        foreach (int deletedRoomId in sortedDeletedRooms)
        {
            ApplyDeletedRoomRule(deletedRoomId);
        }

        PrintCurrentDoorMap();
    }

    private void ApplyDeletedRoomRule(int deletedRoomId)
    {
        // 삭제된 방 자체의 문은 전부 막음
        if (doorMap.ContainsKey(deletedRoomId))
            doorMap[deletedRoomId].ClearDoors();

        ApplyPlusFourRule(deletedRoomId);
        ApplyStudyBridgeRule(deletedRoomId);
        ApplyMainExceptionRule(deletedRoomId);
    }

    private void ApplyPlusFourRule(int deletedRoomId)
    {
        int plusFourRoomId = deletedRoomId + 4;

        if (!doorMap.ContainsKey(plusFourRoomId))
            return;

        if (deletedRooms.Contains(plusFourRoomId))
            return;

        doorMap[plusFourRoomId].ReplaceTarget(deletedRoomId, MainRoomId);

        Debug.Log("[SpaceDelete] " + plusFourRoomId + "번 방의 " + deletedRoomId + " 연결을 Main으로 변경");
    }

    private void ApplyStudyBridgeRule(int deletedRoomId)
    {
        int nextAliveRoomId = FindNextAliveRoomId(deletedRoomId);

        if (nextAliveRoomId == NoRoom)
            return;

        if (!doorMap.ContainsKey(nextAliveRoomId))
            return;

        if (!doorMap.ContainsKey(StudyRoomId))
            return;

        if (deletedRooms.Contains(StudyRoomId))
            return;

        doorMap[nextAliveRoomId].ReplaceTarget(deletedRoomId, StudyRoomId);
        doorMap[StudyRoomId].ReplaceTarget(deletedRoomId, nextAliveRoomId);

        Debug.Log("[SpaceDelete] " + nextAliveRoomId + "번 방 ↔ Study(8) 연결 처리");
    }

    private int FindNextAliveRoomId(int deletedRoomId)
    {
        int[] candidates = { 1, 2, 3, 4, 5, 6, 7, 8 };

        foreach (int id in candidates)
        {
            if (id <= deletedRoomId)
                continue;

            if (deletedRooms.Contains(id))
                continue;

            return id;
        }

        return NoRoom;
    }

    private void ApplyMainExceptionRule(int deletedRoomId)
    {
        if (!doorMap.ContainsKey(MainRoomId))
            return;

        if (deletedRoomId == 1)
        {
            doorMap[MainRoomId].SetTargetRoom(DoorDirection.Up, StudyRoomId);
            Debug.Log("[SpaceDelete] Main Up: Entrance → Study");
        }
        else if (deletedRoomId == 3)
        {
            doorMap[MainRoomId].SetTargetRoom(DoorDirection.Right, 7);
            Debug.Log("[SpaceDelete] Main Right: Dining → Reception");
        }
        else if (deletedRoomId == 5)
        {
            doorMap[MainRoomId].SetTargetRoom(DoorDirection.Down, StudyRoomId);
            Debug.Log("[SpaceDelete] Main Down: Stair → Study");
        }
    }

    private void ShowFireVisual(int roomId)
    {
        if (fireVisualPrefab == null)
        {
            Debug.LogWarning("[MansionRoomSystem] Fire Visual Prefab이 연결되지 않았습니다.");
            return;
        }

        if (!roomMap.ContainsKey(roomId))
            return;

        if (fireVisualInstance == null)
        {
            Transform parent = visualRoot != null ? visualRoot : transform;
            fireVisualInstance = Instantiate(fireVisualPrefab, parent);
        }

        fireVisualInstance.transform.position = roomMap[roomId].GetVisualPosition();
        fireVisualInstance.SetActive(true);
    }

    private void HideFireVisual()
    {
        if (fireVisualInstance != null)
            fireVisualInstance.SetActive(false);
    }

    private void ShowDeletedVisual(int roomId)
    {
        if (deletedVisualPrefab == null)
        {
            Debug.LogWarning("[MansionRoomSystem] Deleted Visual Prefab이 연결되지 않았습니다.");
            return;
        }

        if (!roomMap.ContainsKey(roomId))
            return;

        if (deletedVisuals.ContainsKey(roomId))
            return;

        Transform parent = visualRoot != null ? visualRoot : transform;

        GameObject visual = Instantiate(deletedVisualPrefab, parent);
        visual.transform.position = roomMap[roomId].GetVisualPosition();

        deletedVisuals.Add(roomId, visual);
    }

    private DoorDirection GetOppositeDirection(DoorDirection dir)
    {
        switch (dir)
        {
            case DoorDirection.Up:
                return DoorDirection.Down;

            case DoorDirection.Down:
                return DoorDirection.Up;

            case DoorDirection.Left:
                return DoorDirection.Right;

            case DoorDirection.Right:
                return DoorDirection.Left;

            default:
                return DoorDirection.Down;
        }
    }

    private void RefreshRoomVisible()
    {
        if (!showOnlyCurrentRoom)
            return;

        if (roomMap == null)
            return;

        foreach (MansionRoom room in roomMap.Values)
        {
            if (room == null)
                continue;

            room.gameObject.SetActive(room.RoomId == currentRoomId);
        }
    }

    private void PrintCurrentDoorMap()
    {
        foreach (var pair in doorMap)
        {
            RoomDoorData data = pair.Value;

            Debug.Log(
                "[DoorMap] " +
                data.roomId + " / " + data.roomName +
                " / U:" + data.upDoor +
                " D:" + data.downDoor +
                " L:" + data.leftDoor +
                " R:" + data.rightDoor
            );
        }
    }

    public int GetCurrentRoomId()
    {
        return currentRoomId;
    }

    public bool IsRoomDeleted(int roomId)
    {
        return deletedRooms.Contains(roomId);
    }

    public int GetBurningRoomId()
    {
        return burningRoomId;
    }
}

[System.Serializable]
public class RoomDoorData
{
    public int roomId;
    public string roomName;

    public int upDoor;
    public int downDoor;
    public int leftDoor;
    public int rightDoor;

    public int GetTargetRoom(DoorDirection dir)
    {
        switch (dir)
        {
            case DoorDirection.Up:
                return upDoor;

            case DoorDirection.Down:
                return downDoor;

            case DoorDirection.Left:
                return leftDoor;

            case DoorDirection.Right:
                return rightDoor;

            default:
                return 13;
        }
    }

    public void SetTargetRoom(DoorDirection dir, int targetRoomId)
    {
        switch (dir)
        {
            case DoorDirection.Up:
                upDoor = targetRoomId;
                break;

            case DoorDirection.Down:
                downDoor = targetRoomId;
                break;

            case DoorDirection.Left:
                leftDoor = targetRoomId;
                break;

            case DoorDirection.Right:
                rightDoor = targetRoomId;
                break;
        }
    }

    public void ReplaceTarget(int oldTargetId, int newTargetId)
    {
        if (upDoor == oldTargetId)
            upDoor = newTargetId;

        if (downDoor == oldTargetId)
            downDoor = newTargetId;

        if (leftDoor == oldTargetId)
            leftDoor = newTargetId;

        if (rightDoor == oldTargetId)
            rightDoor = newTargetId;
    }

    public void ClearDoors()
    {
        upDoor = 13;
        downDoor = 13;
        leftDoor = 13;
        rightDoor = 13;
    }

    public RoomDoorData Clone()
    {
        return new RoomDoorData
        {
            roomId = roomId,
            roomName = roomName,
            upDoor = upDoor,
            downDoor = downDoor,
            leftDoor = leftDoor,
            rightDoor = rightDoor
        };
    }
}