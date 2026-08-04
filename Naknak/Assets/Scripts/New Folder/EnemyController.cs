using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyController : MonoBehaviour
{
	[Header("Move")]
	[SerializeField]
	private float moveSpeed = 3f;

	private Transform player;

	private GridPathfinder pathfinder;

	private GridLayout grid;

	private bool isMoving;

	private bool canGridChase;

	private bool isRoomMoving;

	private RoomMoveInfo currentRoomMoveInfo;

	private bool hasTriggeredRestart;

	public bool IsMoving => isMoving;

	public bool IsRoomMoving => isRoomMoving;

	public void SetGridChase(bool value)
	{
		canGridChase = value;
	}

	public void MoveToRoom(RoomMoveInfo info)
	{
		if (info == null || info.spawnPoint == null)
		{
			return;
		}

		canGridChase = false;
		isMoving = false;
		isRoomMoving = false;
		currentRoomMoveInfo = info;
		base.transform.position = info.spawnPoint.position;

		if (AIManager.Instance != null)
		{
			AIManager.Instance.EnemyArrived(info.toRoomId);
		}
	}

	public void Initialize()
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		if (gameObject != null)
		{
			player = gameObject.transform;
		}
		pathfinder = new GridPathfinder();
		grid = ((TriggerExecutor.Instance != null) ? TriggerExecutor.Instance.grid : null);
	}

	private void Update()
	{
		if (!canGridChase || isRoomMoving || player == null)
		{
			return;
		}
		CheckPlayerCatch();
		if (isMoving)
		{
			return;
		}
		if (grid == null && TriggerExecutor.Instance != null)
		{
			grid = TriggerExecutor.Instance.grid;
		}
		Vector2Int cell = GetCell(base.transform.position);
		Vector2Int cell2 = GetCell(player.position);
		if (cell == cell2)
		{
			RestartFromCatch();
			return;
		}
		List<Vector2Int> list = pathfinder.FindPath(cell, cell2, IsWalkableCell);
		if (list.Count <= 1)
		{
			if (TryGetFallbackStep(cell, cell2, out var nextCell))
			{
				StartCoroutine(Move(nextCell));
			}
		}
		else
		{
			StartCoroutine(Move(list[1]));
		}
	}

	private bool TryGetFallbackStep(Vector2Int start, Vector2Int goal, out Vector2Int nextCell)
	{
		Vector2Int[] obj = new Vector2Int[4]
		{
			Vector2Int.up,
			Vector2Int.down,
			Vector2Int.left,
			Vector2Int.right
		};
		nextCell = start;
		float num = Vector2Int.Distance(start, goal);
		bool flag = false;
		Vector2Int[] array = obj;
		foreach (Vector2Int vector2Int in array)
		{
			Vector2Int vector2Int2 = start + vector2Int;
			if (IsWalkableCell(vector2Int2))
			{
				float num2 = Vector2Int.Distance(vector2Int2, goal);
				if (!flag || !(num2 >= num))
				{
					num = num2;
					nextCell = vector2Int2;
					flag = true;
				}
			}
		}
		return flag;
	}

	private IEnumerator Move(Vector2Int nextCell)
	{
		isMoving = true;
		Vector3 target = GetCellWorldPosition(nextCell);
		while (Vector2.Distance(base.transform.position, target) > 0.01f)
		{
			base.transform.position = Vector2.MoveTowards(base.transform.position, target, moveSpeed * Time.deltaTime);
			yield return null;
		}
		base.transform.position = target;
		CheckPlayerCatch();
		isMoving = false;
	}

	private Vector2Int GetCell(Vector3 worldPosition)
	{
		if (grid == null)
		{
			return Vector2Int.RoundToInt(worldPosition);
		}
		Vector3Int vector3Int = grid.WorldToCell(worldPosition);
		return new Vector2Int(vector3Int.x, vector3Int.y);
	}

	private Vector3 GetCellWorldPosition(Vector2Int cell)
	{
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
		Collider2D[] array = Physics2D.OverlapPointAll(GetCellWorldPosition(cell));
		foreach (Collider2D collider2D in array)
		{
			if (!(collider2D == null) && collider2D.GetComponent<TilemapCollider2D>() != null)
			{
				return false;
			}
		}
		return true;
	}

	private void CheckPlayerCatch()
	{
		if (!(player == null) && GetCell(base.transform.position) == GetCell(player.position))
		{
			RestartFromCatch();
		}
	}

	private void RestartFromCatch()
	{
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
