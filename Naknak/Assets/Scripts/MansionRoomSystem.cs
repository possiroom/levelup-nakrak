using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MansionRoomSystem : MonoBehaviour
{
	private const int NoRoom = 13;

	private const int MainRoomId = 11;

	private const int StudyRoomId = 8;

	[Header("Player")]
	[SerializeField]
	private PlayerMoveController3 playerCtrl;

	[Header("Current Room")]
	[SerializeField]
	private int currentRoomId = 1;

	[Header("Door Table")]
	[SerializeField]
	private RoomDoorData[] roomDoorTable;

	[Header("Transition")]
	[SerializeField]
	private float teleportDelay = 0.05f;

	[SerializeField]
	private bool showOnlyCurrentRoom;

	[Header("Space Delete")]
	[SerializeField]
	private bool debugPhaseHotkeys = true;

	[SerializeField]
	private int[] fireRoomOrder = new int[6] { 1, 2, 3, 4, 5, 6 };

	[Header("Space Delete Visual")]
	[SerializeField]
	private GameObject fireVisualPrefab;

	[SerializeField]
	private GameObject deletedVisualPrefab;

	[SerializeField]
	private Transform visualRoot;

	private bool isTransitioning;

	private Dictionary<int, MansionRoom> roomMap;

	private Dictionary<int, RoomDoorData> baseDoorMap;

	private Dictionary<int, RoomDoorData> doorMap;

	private HashSet<int> deletedRooms = new HashSet<int>();

	private int currentFirePhase;

	private int burningRoomId;

	private bool burningRoomSeen;

	private GameObject fireVisualInstance;

	private Dictionary<int, GameObject> deletedVisuals = new Dictionary<int, GameObject>();

	[SerializeField]
	private Transform upDoorTrigger;

	[SerializeField]
	private Transform downDoorTrigger;

	[SerializeField]
	private Transform leftDoorTrigger;

	[SerializeField]
	private Transform rightDoorTrigger;

	public static MansionRoomSystem Instance { get; private set; }

	public Transform GetDoorTrigger(DoorDirection dir)
	{
		return dir switch
		{
			DoorDirection.Up => upDoorTrigger, 
			DoorDirection.Down => downDoorTrigger, 
			DoorDirection.Left => leftDoorTrigger, 
			DoorDirection.Right => rightDoorTrigger, 
			_ => null, 
		};
	}

	private void Awake()
	{
		Instance = this;
		if (playerCtrl == null)
		{
			playerCtrl = Object.FindFirstObjectByType<PlayerMoveController3>();
		}
		InitRoomMap();
		InitDoorMap();
		RefreshRoomVisible();
	}

	private void Start()
	{
		if (AIManager.Instance != null)
		{
			AIManager.Instance.PlayerEnteredRoom(currentRoomId);
		}
	}

	private void Update()
	{
		if (debugPhaseHotkeys)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				StartFirePhase(1);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				StartFirePhase(2);
			}
			if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				StartFirePhase(3);
			}
			if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				StartFirePhase(4);
			}
			if (Input.GetKeyDown(KeyCode.Alpha5))
			{
				StartFirePhase(5);
			}
			if (Input.GetKeyDown(KeyCode.Alpha6))
			{
				StartFirePhase(6);
			}
		}
	}

	private void InitRoomMap()
	{
		roomMap = new Dictionary<int, MansionRoom>();
		MapContainer componentInParent = GetComponentInParent<MapContainer>();
		MansionRoom[] array = ((!(componentInParent != null)) ? base.transform.root.GetComponentsInChildren<MansionRoom>(includeInactive: true) : componentInParent.GetComponentsInChildren<MansionRoom>(includeInactive: true));
		MansionRoom[] array2 = array;
		foreach (MansionRoom mansionRoom in array2)
		{
			if (!(mansionRoom == null))
			{
				int roomId = mansionRoom.RoomId;
				if (roomMap.ContainsKey(roomId))
				{
					Debug.LogWarning("[MansionRoomSystem] 중복 Room Id 발견: " + roomId + " / " + mansionRoom.name);
					continue;
				}
				roomMap.Add(roomId, mansionRoom);
				Debug.Log("[MansionRoomSystem] Room 등록됨: " + roomId + " / " + mansionRoom.name);
			}
		}
		Debug.Log("[MansionRoomSystem] 등록된 방 개수: " + roomMap.Count);
	}

	private void InitDoorMap()
	{
		baseDoorMap = new Dictionary<int, RoomDoorData>();
		doorMap = new Dictionary<int, RoomDoorData>();
		RoomDoorData[] array = roomDoorTable;
		foreach (RoomDoorData roomDoorData in array)
		{
			if (roomDoorData != null)
			{
				if (!baseDoorMap.ContainsKey(roomDoorData.roomId))
				{
					baseDoorMap.Add(roomDoorData.roomId, roomDoorData.Clone());
				}
				if (!doorMap.ContainsKey(roomDoorData.roomId))
				{
					doorMap.Add(roomDoorData.roomId, roomDoorData.Clone());
				}
			}
		}
		Debug.Log("[MansionRoomSystem] Door Table 등록 개수: " + doorMap.Count);
	}

	public void EnterDoor(int fromRoomId, DoorDirection exitDirection)
	{
		if (!isTransitioning)
		{
			StartCoroutine(EnterDoorRoutine(fromRoomId, exitDirection));
		}
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
		if (targetRoomId == 13)
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
		MansionRoom mansionRoom = roomMap[targetRoomId];
		Transform spawnPoint = mansionRoom.GetSpawnPoint(enterDirection);
		if (spawnPoint == null)
		{
			Debug.LogError("[MansionRoomSystem] SpawnPoint 없음: " + targetRoomId + " / " + enterDirection);
			isTransitioning = false;
			yield break;
		}
		yield return new WaitForSeconds(teleportDelay);
		currentRoomId = targetRoomId;
		AIManager.Instance.PlayerEnteredRoom(currentRoomId);
		RefreshRoomVisible();
		playerCtrl.TeleportToWorld(spawnPoint.position);
		Debug.Log("[RoomMove] " + fromRoomId + " " + exitDirection.ToString() + " → " + targetRoomId + " " + enterDirection);
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
		int item = fireRoomOrder[phase - 1];
		if (deletedRooms.Contains(item))
		{
			Debug.LogWarning("[MansionRoomSystem] 이미 삭제된 방에는 화재를 발생시킬 수 없음: " + item);
			return;
		}
		currentFirePhase = phase;
		burningRoomId = item;
		burningRoomSeen = currentRoomId == burningRoomId;
		ShowFireVisual(burningRoomId);
		Debug.Log("[MansionRoomSystem] Phase " + phase + " 화재 발생 방: " + burningRoomId);
		if (burningRoomSeen)
		{
			Debug.Log("[MansionRoomSystem] 현재 방이 화재 방이라 즉시 목격 처리됨: " + burningRoomId);
		}
	}

	public void StartNextFirePhase()
	{
		StartFirePhase(currentFirePhase + 1);
	}

	private void MarkBurningRoomSeen(int roomId)
	{
		if (burningRoomId != 0 && roomId == burningRoomId)
		{
			burningRoomSeen = true;
			Debug.Log("[MansionRoomSystem] 화재 방 목격: " + roomId);
		}
	}

	private void TryDeleteBurningRoomAfterExit(int previousRoomId)
	{
		if (burningRoomId != 0 && previousRoomId == burningRoomId && burningRoomSeen)
		{
			DeleteRoom(burningRoomId);
			burningRoomId = 0;
			burningRoomSeen = false;
		}
	}

	public void DeleteRoom(int roomId)
	{
		if (!deletedRooms.Contains(roomId))
		{
			deletedRooms.Add(roomId);
			HideFireVisual();
			ShowDeletedVisual(roomId);
			RebuildDoorMapByDeletedRooms();
			Debug.Log("[MansionRoomSystem] 방 삭제 완료: " + roomId);
		}
	}

	private void RebuildDoorMapByDeletedRooms()
	{
		doorMap.Clear();
		foreach (KeyValuePair<int, RoomDoorData> item in baseDoorMap)
		{
			doorMap.Add(item.Key, item.Value.Clone());
		}
		List<int> list = new List<int>(deletedRooms);
		list.Sort();
		foreach (int item2 in list)
		{
			ApplyDeletedRoomRule(item2);
		}
		PrintCurrentDoorMap();
	}

	private void ApplyDeletedRoomRule(int deletedRoomId)
	{
		if (doorMap.ContainsKey(deletedRoomId))
		{
			doorMap[deletedRoomId].ClearDoors();
		}
		ApplyPlusFourRule(deletedRoomId);
		ApplyStudyBridgeRule(deletedRoomId);
		ApplyMainExceptionRule(deletedRoomId);
	}

	private void ApplyPlusFourRule(int deletedRoomId)
	{
		int num = deletedRoomId + 4;
		if (doorMap.ContainsKey(num) && !deletedRooms.Contains(num))
		{
			doorMap[num].ReplaceTarget(deletedRoomId, 11);
			Debug.Log("[SpaceDelete] " + num + "번 방의 " + deletedRoomId + " 연결을 Main으로 변경");
		}
	}

	private void ApplyStudyBridgeRule(int deletedRoomId)
	{
		int num = FindNextAliveRoomId(deletedRoomId);
		if (num != 13 && doorMap.ContainsKey(num) && doorMap.ContainsKey(8) && !deletedRooms.Contains(8))
		{
			doorMap[num].ReplaceTarget(deletedRoomId, 8);
			doorMap[8].ReplaceTarget(deletedRoomId, num);
			Debug.Log("[SpaceDelete] " + num + "번 방 ↔ Study(8) 연결 처리");
		}
	}

	private int FindNextAliveRoomId(int deletedRoomId)
	{
		int[] array = new int[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
		foreach (int num in array)
		{
			if (num > deletedRoomId && !deletedRooms.Contains(num))
			{
				return num;
			}
		}
		return 13;
	}

	private void ApplyMainExceptionRule(int deletedRoomId)
	{
		if (doorMap.ContainsKey(11))
		{
			switch (deletedRoomId)
			{
			case 1:
				doorMap[11].SetTargetRoom(DoorDirection.Up, 8);
				Debug.Log("[SpaceDelete] Main Up: Entrance → Study");
				break;
			case 3:
				doorMap[11].SetTargetRoom(DoorDirection.Right, 7);
				Debug.Log("[SpaceDelete] Main Right: Dining → Reception");
				break;
			case 5:
				doorMap[11].SetTargetRoom(DoorDirection.Down, 8);
				Debug.Log("[SpaceDelete] Main Down: Stair → Study");
				break;
			}
		}
	}

	private void ShowFireVisual(int roomId)
	{
		if (fireVisualPrefab == null)
		{
			Debug.LogWarning("[MansionRoomSystem] Fire Visual Prefab이 연결되지 않았습니다.");
		}
		else if (roomMap.ContainsKey(roomId))
		{
			if (fireVisualInstance == null)
			{
				Transform parent = ((visualRoot != null) ? visualRoot : base.transform);
				fireVisualInstance = Object.Instantiate(fireVisualPrefab, parent);
			}
			fireVisualInstance.transform.position = roomMap[roomId].GetVisualPosition();
			fireVisualInstance.SetActive(value: true);
		}
	}

	private void HideFireVisual()
	{
		if (fireVisualInstance != null)
		{
			fireVisualInstance.SetActive(value: false);
		}
	}

	private void ShowDeletedVisual(int roomId)
	{
		if (deletedVisualPrefab == null)
		{
			Debug.LogWarning("[MansionRoomSystem] Deleted Visual Prefab이 연결되지 않았습니다.");
		}
		else if (roomMap.ContainsKey(roomId) && !deletedVisuals.ContainsKey(roomId))
		{
			Transform parent = ((visualRoot != null) ? visualRoot : base.transform);
			GameObject gameObject = Object.Instantiate(deletedVisualPrefab, parent);
			gameObject.transform.position = roomMap[roomId].GetVisualPosition();
			deletedVisuals.Add(roomId, gameObject);
		}
	}

	private DoorDirection GetOppositeDirection(DoorDirection dir)
	{
		return dir switch
		{
			DoorDirection.Up => DoorDirection.Down, 
			DoorDirection.Down => DoorDirection.Up, 
			DoorDirection.Left => DoorDirection.Right, 
			DoorDirection.Right => DoorDirection.Left, 
			_ => DoorDirection.Down, 
		};
	}

	private void RefreshRoomVisible()
	{
		if (!showOnlyCurrentRoom || roomMap == null)
		{
			return;
		}
		foreach (MansionRoom value in roomMap.Values)
		{
			if (!(value == null))
			{
				value.gameObject.SetActive(value.RoomId == currentRoomId);
			}
		}
	}

	private void PrintCurrentDoorMap()
	{
		foreach (KeyValuePair<int, RoomDoorData> item in doorMap)
		{
			RoomDoorData value = item.Value;
			Debug.Log("[DoorMap] " + value.roomId + " / " + value.roomName + " / U:" + value.upDoor + " D:" + value.downDoor + " L:" + value.leftDoor + " R:" + value.rightDoor);
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

	public List<int> GetNeighbors(int roomId)
	{
		List<int> list = new List<int>();
		if (!doorMap.ContainsKey(roomId))
		{
			return list;
		}
		RoomDoorData roomDoorData = doorMap[roomId];
		if (roomDoorData.upDoor != 13)
		{
			list.Add(roomDoorData.upDoor);
		}
		if (roomDoorData.downDoor != 13)
		{
			list.Add(roomDoorData.downDoor);
		}
		if (roomDoorData.leftDoor != 13)
		{
			list.Add(roomDoorData.leftDoor);
		}
		if (roomDoorData.rightDoor != 13)
		{
			list.Add(roomDoorData.rightDoor);
		}
		Debug.Log(string.Format("Room {0} → [{1}]", roomId, string.Join(", ", list)));
		return list;
	}

	public void MoveEnemyToRoom(GameObject enemy, int roomId)
	{
		if (!(enemy == null) && roomMap.ContainsKey(roomId))
		{
			Transform spawnPoint = roomMap[roomId].GetSpawnPoint(DoorDirection.Down);
			if (spawnPoint == null)
			{
				Debug.LogError($"Room {roomId} SpawnPoint 없음");
				return;
			}
			enemy.transform.position = spawnPoint.position;
			Debug.Log($"Enemy Teleport : {roomId}");
		}
	}

	private DoorDirection GetExitDirection(int currentRoomId, int nextRoomId)
	{
		if (!doorMap.ContainsKey(currentRoomId))
		{
			return DoorDirection.Down;
		}
		RoomDoorData roomDoorData = doorMap[currentRoomId];
		if (roomDoorData.upDoor == nextRoomId)
		{
			return DoorDirection.Up;
		}
		if (roomDoorData.downDoor == nextRoomId)
		{
			return DoorDirection.Down;
		}
		if (roomDoorData.leftDoor == nextRoomId)
		{
			return DoorDirection.Left;
		}
		if (roomDoorData.rightDoor == nextRoomId)
		{
			return DoorDirection.Right;
		}
		return DoorDirection.Down;
	}

	public RoomMoveInfo GetRoomMoveInfo(int currentRoomId, int nextRoomId)
	{
		if (!roomMap.ContainsKey(currentRoomId))
		{
			Debug.LogError($"현재 방 없음 : {currentRoomId}");
			return null;
		}
		if (!roomMap.ContainsKey(nextRoomId))
		{
			Debug.LogError($"이동할 방 없음 : {nextRoomId}");
			return null;
		}
		DoorDirection exitDirection = GetExitDirection(currentRoomId, nextRoomId);
		DoorDirection oppositeDirection = GetOppositeDirection(exitDirection);
		MansionRoom mansionRoom = roomMap[currentRoomId];
		MansionRoom mansionRoom2 = roomMap[nextRoomId];
		Transform doorPoint = mansionRoom.GetDoorPoint(exitDirection);
		Transform spawnPoint = mansionRoom2.GetSpawnPoint(oppositeDirection);
		if (doorPoint == null)
		{
			Debug.LogError($"DoorPoint 없음 : Room {currentRoomId}");
			return null;
		}
		if (spawnPoint == null)
		{
			Debug.LogError($"SpawnPoint 없음 : Room {nextRoomId}");
			return null;
		}
		return new RoomMoveInfo(currentRoomId, nextRoomId, doorPoint, spawnPoint);
	}
}
