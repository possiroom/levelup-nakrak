using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyController : MonoBehaviour
{
	[Header("Move")]
	// 한 칸을 이동할 때의 실제 이동 속도.
	[SerializeField]
	private float moveSpeed = 3f;

	// player와 동일하게 한 번에 1타일씩 이동한다.
	[SerializeField]
	private float moveDistance = 1f;

	// player collider 기준처럼 발밑 지점에서 충돌을 검사하기 위한 보정값.
	[SerializeField]
	private Vector2 collisionProbeOffset = new Vector2(0f, -0.5f);

	// 문 Transform 자체가 막혀 있을 수 있어 주변 몇 칸까지 도착 가능 지점을 찾을지 정한다.
	[SerializeField]
	private int doorReachCellRadius = 2;

	// GridChase에서 쫓아갈 player 참조.
	private Transform player;

	// player가 가진 층별 TilemapCollider LayerMask를 재사용한다.
	private PlayerMoveController3 playerMove;

	// 같은 방 추격/문까지 이동에서 사용하는 타일 BFS 경로 탐색기.
	private GridPathfinder pathfinder;

	// 월드 좌표와 타일 셀 좌표를 변환하는 Unity Grid.
	private GridLayout grid;

	// 현재 1칸 이동 코루틴이 진행 중인지 여부.
	private bool isMoving;

	// AIManager가 같은 방 추격을 켰을 때 true.
	private bool canGridChase;

	// 방 이동 코루틴(RoomMoveRoutine)이 진행 중인지 여부.
	private bool isRoomMoving;

	// 이번 방 이동의 출발 방, 도착 방, 문 위치, 도착 스폰 위치 정보.
	private RoomMoveInfo currentRoomMoveInfo;

	// 방 단위 이동 코루틴. GridChase가 시작되면 즉시 끊는다.
	private Coroutine roomMoveCoroutine;

	// 실제 1칸 이동 코루틴. 방 이동 중 추격으로 전환될 때 같이 끊기 위해 따로 보관한다.
	private Coroutine tileMoveCoroutine;

	// player를 잡았을 때 Kill/Restart가 중복 호출되지 않게 막는다.
	private bool hasTriggeredRestart;

	public bool IsMoving => isMoving;

	public bool IsRoomMoving => isRoomMoving;

	public void SetGridChase(bool value)
	{
		// GridChase가 켜지면 배회/방 이동을 즉시 멈추고 같은 방 타일 추격으로 넘긴다.
		canGridChase = value;
		if (value)
		{
			CancelRoomMove();
		}
	}

	public void CancelRoomMove()
	{
		// 방 이동 코루틴과 그 안에서 돌던 1칸 이동 코루틴을 모두 중단한다.
		bool wasRoomMoving = isRoomMoving;

		if (roomMoveCoroutine != null)
		{
			StopCoroutine(roomMoveCoroutine);
			roomMoveCoroutine = null;
		}

		if (wasRoomMoving && tileMoveCoroutine != null)
		{
			StopCoroutine(tileMoveCoroutine);
			tileMoveCoroutine = null;
		}

		if (wasRoomMoving)
			isMoving = false;

		isRoomMoving = false;
	}

    public void MoveToRoom(RoomMoveInfo info)
    {
        // Patrol/RoomChase가 선택한 방 이동 정보를 받아 문까지 걸어가는 코루틴을 시작한다.
        if (isRoomMoving || info == null || info.doorTrigger == null || info.spawnPoint == null)
        {
            return;
        }

        canGridChase = false;
        currentRoomMoveInfo = info;
        roomMoveCoroutine = StartCoroutine(RoomMoveRoutine());
    }

    private IEnumerator RoomMoveRoutine()
    {
        // 방 이동은 먼저 현재 방의 실제 문 근처까지 1칸씩 걸어간 뒤,
        // 도착 방의 spawnPoint로 이동 완료 처리한다.
        isRoomMoving = true;
        isMoving = false;

		SyncGrid();

		if (!TryGetReachableDoorCell(out Vector2Int doorReachCell))
		{
			// 문 주변에서 갈 수 있는 셀을 찾지 못하면 방 이동을 취소한다.
			CancelRoomMove();
			yield break;
		}

		while (GetCell(transform.position) != doorReachCell)
		{
			if (!isRoomMoving || canGridChase)
			{
				// 방 이동 도중 같은 방 추격이 켜지면 즉시 중단한다.
				CancelRoomMove();
				yield break;
			}

			if (!TryGetNextStep(GetCell(transform.position), doorReachCell, out Vector2 direction))
			{
				// TilemapCollider 때문에 문까지 갈 수 없으면 순간이동하지 않고 목표 방을 스킵한다.
				Debug.LogWarning($"[EnemyController] Door path blocked by TilemapCollider. Room move cancelled: {currentRoomMoveInfo.fromRoomId} -> {currentRoomMoveInfo.toRoomId}");
				int failedTargetRoomId = currentRoomMoveInfo.toRoomId;
				isRoomMoving = false;
				roomMoveCoroutine = null;

				if (AIManager.Instance != null)
					AIManager.Instance.EnemyRoomMoveFailed(failedTargetRoomId);

				yield break;
			}
			
			tileMoveCoroutine = StartCoroutine(MoveOneTile(direction, false));
			yield return tileMoveCoroutine;
			tileMoveCoroutine = null;
		}

        transform.position = currentRoomMoveInfo.spawnPoint.position;
        isRoomMoving = false;
		roomMoveCoroutine = null;

        if (AIManager.Instance != null)
        {
            // 방 이동 완료 후 AIManager의 enemyRoomID를 갱신한다.
            AIManager.Instance.EnemyArrived(currentRoomMoveInfo.toRoomId);
        }
    }

	public void Initialize()
	{
		// enemy clone 생성/재사용 시 player, pathfinder, grid 참조를 다시 잡는다.
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		if (gameObject != null)
		{
			player = gameObject.transform;
			playerMove = gameObject.GetComponent<PlayerMoveController3>();
		}
		pathfinder = new GridPathfinder();
		grid = ((TriggerExecutor.Instance != null) ? TriggerExecutor.Instance.grid : null);
	}

	private void Update()
	{
		// GridChase가 켜진 상태에서만 실제 타일 추격을 수행한다.
		if (!canGridChase || isRoomMoving || player == null)
		{
			return;
		}
		CheckPlayerCatch();
		if (isMoving)
		{
			return;
		}
		SyncGrid();
		if (GetCell(base.transform.position) == GetCell(player.position))
		{
			RestartFromCatch();
			return;
		}

		if (TryGetNextStep(GetCell(base.transform.position), GetCell(player.position), out Vector2 direction))
		{
			// 한 번에 한 칸만 이동한다. 대각선 이동은 TryGetNextStep/MoveOneTile에서 막는다.
			tileMoveCoroutine = StartCoroutine(MoveOneTile(direction, true));
		}
	}

	private void SyncGrid()
	{
		// TriggerExecutor가 늦게 준비되는 경우를 대비해 grid 참조를 지연 복구한다.
		if (grid == null && TriggerExecutor.Instance != null)
		{
			grid = TriggerExecutor.Instance.grid;
		}
	}

	private bool TryGetNextStep(Vector2Int start, Vector2Int goal, out Vector2 direction)
	{
		// 타일 BFS로 다음 1칸을 찾는다. 실패하면 막히지 않는 방향 후보를 한 번 더 시도한다.
		direction = Vector2.zero;

		if (pathfinder == null)
			pathfinder = new GridPathfinder();

		List<Vector2Int> path = pathfinder.FindPath(start, goal, IsWalkableCell);
		if (path.Count > 1)
		{
			Vector2Int step = path[1] - start;
			direction = new Vector2(step.x, step.y);
			return IsCardinalDirection(direction) && !CheckCollider(base.transform.position, direction);
		}

		return TryGetGreedyStep(start, goal, out direction);
	}

	private bool TryGetGreedyStep(Vector2Int start, Vector2Int goal, out Vector2 direction)
	{
		// BFS 경로가 없을 때 player/목표 쪽으로 가까워지는 축부터 1칸 후보를 검사한다.
		direction = Vector2.zero;

		Vector2Int delta = goal - start;
		List<Vector2Int> candidates = new List<Vector2Int>();

		if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
		{
			AddHorizontalCandidate(candidates, delta.x);
			AddVerticalCandidate(candidates, delta.y);
		}
		else
		{
			AddVerticalCandidate(candidates, delta.y);
			AddHorizontalCandidate(candidates, delta.x);
		}

		AddMissingCandidate(candidates, Vector2Int.up);
		AddMissingCandidate(candidates, Vector2Int.down);
		AddMissingCandidate(candidates, Vector2Int.left);
		AddMissingCandidate(candidates, Vector2Int.right);

		foreach (Vector2Int candidate in candidates)
		{
			Vector2 candidateDirection = new Vector2(candidate.x, candidate.y);
			Vector2Int nextCell = start + candidate;

			if (!IsWalkableCell(nextCell))
				continue;

			if (CheckCollider(base.transform.position, candidateDirection))
				continue;

			direction = candidateDirection;
			return true;
		}

		return false;
	}

	private void AddHorizontalCandidate(List<Vector2Int> candidates, int deltaX)
	{
		// 목표가 오른쪽/왼쪽에 있으면 그 방향을 우선 후보로 넣는다.
		if (deltaX > 0)
			AddMissingCandidate(candidates, Vector2Int.right);
		else if (deltaX < 0)
			AddMissingCandidate(candidates, Vector2Int.left);
	}

	private void AddVerticalCandidate(List<Vector2Int> candidates, int deltaY)
	{
		// 목표가 위/아래에 있으면 그 방향을 우선 후보로 넣는다.
		if (deltaY > 0)
			AddMissingCandidate(candidates, Vector2Int.up);
		else if (deltaY < 0)
			AddMissingCandidate(candidates, Vector2Int.down);
	}

	private void AddMissingCandidate(List<Vector2Int> candidates, Vector2Int candidate)
	{
		// 같은 방향 후보가 중복으로 들어가지 않게 한다.
		if (!candidates.Contains(candidate))
			candidates.Add(candidate);
	}

	private bool TryGetReachableDoorCell(out Vector2Int reachableCell)
	{
		// doorTrigger의 셀이 막혀 있을 수 있으므로 주변 셀 중 실제로 도달 가능한 셀을 찾는다.
		reachableCell = GetCell(currentRoomMoveInfo.doorTrigger.position);
		Vector2Int start = GetCell(transform.position);
		Vector2Int doorCell = reachableCell;

		if (CanReachCell(start, doorCell))
			return true;

		for (int radius = 1; radius <= doorReachCellRadius; radius++)
		{
			for (int x = -radius; x <= radius; x++)
			{
				for (int y = -radius; y <= radius; y++)
				{
					if (Mathf.Abs(x) + Mathf.Abs(y) != radius)
						continue;

					Vector2Int candidate = doorCell + new Vector2Int(x, y);
					if (CanReachCell(start, candidate))
					{
						reachableCell = candidate;
						return true;
					}
				}
			}
		}

		return false;
	}

	private bool CanReachCell(Vector2Int start, Vector2Int goal)
	{
		// 목표 셀이 walkable이고 BFS 경로가 있으면 도달 가능하다고 본다.
		if (!IsWalkableCell(goal))
			return false;

		if (pathfinder == null)
			pathfinder = new GridPathfinder();

		List<Vector2Int> path = pathfinder.FindPath(start, goal, IsWalkableCell);
		return path.Count > 0;
	}

	private IEnumerator MoveOneTile(Vector2 direction, bool checkCatchAfterMove)
	{
		// 모든 enemy 이동의 최소 단위. 한 번에 cardinal 방향 1칸만 이동한다.
		if (!IsCardinalDirection(direction))
		{
			isMoving = false;
			yield break;
		}

		if (CheckCollider(base.transform.position, direction))
		{
			// 이동 직전 레이캐스트로 TilemapCollider를 한 번 더 확인한다.
			isMoving = false;
			yield break;
		}

		isMoving = true;
		Vector3 target = base.transform.position + (Vector3)(direction * moveDistance);
		target.z = base.transform.position.z;

		while (Vector2.Distance(base.transform.position, target) > 0.01f)
		{
			base.transform.position = Vector2.MoveTowards(base.transform.position, target, moveSpeed * Time.deltaTime);
			yield return null;
		}
		base.transform.position = target;
		if (checkCatchAfterMove)
			CheckPlayerCatch();
		isMoving = false;
		tileMoveCoroutine = null;
	}

	private Vector2Int GetCell(Vector3 worldPosition)
	{
		// 월드 좌표를 grid cell 좌표로 변환한다. grid가 없으면 반올림으로 대체한다.
		if (grid == null)
		{
			return Vector2Int.RoundToInt(worldPosition);
		}
		Vector3Int vector3Int = grid.WorldToCell(worldPosition);
		return new Vector2Int(vector3Int.x, vector3Int.y);
	}

	private Vector3 GetCellWorldPosition(Vector2Int cell)
	{
		// cell 좌표의 월드 중심점을 구한다. TilemapCollider 검사 기준으로 사용한다.
		if (grid == null)
		{
			return new Vector3(cell.x, cell.y, base.transform.position.z);
		}
		Vector3 result = grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + grid.cellSize * 0.5f;
		result.z = base.transform.position.z;
		return result;
	}

	private bool IsWalkableCell(Vector2Int cell)
	{
		// 해당 셀 중심 발밑 위치에 TilemapCollider가 없으면 이동 가능하다.
		return !HasTilemapCollider(GetCellWorldPosition(cell) + (Vector3)collisionProbeOffset);
	}

	private bool IsCardinalDirection(Vector2 direction)
	{
		// player와 동일하게 상하좌우만 허용하고 대각선 이동을 막는다.
		return direction == Vector2.up ||
			direction == Vector2.down ||
			direction == Vector2.left ||
			direction == Vector2.right;
	}

	private bool CheckCollider(Vector3 from, Vector2 direction)
	{
		// 현재 위치 발밑에서 이동 방향으로 1칸 raycast하여 벽/타일맵 충돌을 검사한다.
		if (direction == Vector2.zero)
			return false;

		RaycastHit2D hit = Physics2D.Raycast(
			(Vector2)from + collisionProbeOffset,
			direction,
			moveDistance,
			GetCollisionLayerMask());

		Debug.DrawRay((Vector2)from + collisionProbeOffset, direction * moveDistance, Color.red);

		return hit.collider != null;
	}

	private int GetCollisionLayerMask()
	{
		// player가 현재 사용하는 floor collision layer를 우선 사용하고, 없으면 모든 층 콜라이더를 본다.
		if (playerMove != null && playerMove.floorLayer.value != 0)
			return playerMove.floorLayer.value;

		int mask = LayerMask.GetMask("Col 1F", "Col 2F", "Col 3F");
		return mask != 0 ? mask : Physics2D.DefaultRaycastLayers;
	}

	private bool HasTilemapCollider(Vector3 worldPos)
	{
		// 특정 위치가 TilemapCollider2D 위인지 검사한다.
		Collider2D[] array = Physics2D.OverlapPointAll(worldPos, GetCollisionLayerMask());
		foreach (Collider2D collider2D in array)
		{
			if (!(collider2D == null) && collider2D.GetComponent<TilemapCollider2D>() != null)
				return true;
		}

		return false;
	}

	private void CheckPlayerCatch()
	{
		// enemy와 player가 같은 셀이 되면 catch 처리한다.
		if (!(player == null) && GetCell(base.transform.position) == GetCell(player.position))
		{
			RestartFromCatch();
		}
	}

	private void RestartFromCatch()
	{
		// catch 처리는 한 번만 실행한다.
		if (!hasTriggeredRestart)
		{
			hasTriggeredRestart = true;
			Debug.Log("Enemy caught player. Restart.");
			if (PlayerStatus.Instance != null)
			{
				PlayerStatus.Instance.Kill();
			}
			else if (MapManager.Instance != null)
			{
				MapManager.Instance.RestartCurrentMap();
			}
		}
	}
}
