using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder
{
	private readonly Vector2Int[] directions = new Vector2Int[4]
	{
		Vector2Int.up,
		Vector2Int.down,
		Vector2Int.left,
		Vector2Int.right
	};

	public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> canMove = null)
	{
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		Dictionary<Vector2Int, Vector2Int> dictionary = new Dictionary<Vector2Int, Vector2Int>();
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		queue.Enqueue(start);
		hashSet.Add(start);
		while (queue.Count > 0)
		{
			Vector2Int vector2Int = queue.Dequeue();
			if (vector2Int == goal)
			{
				break;
			}
			Vector2Int[] array = directions;
			foreach (Vector2Int vector2Int2 in array)
			{
				Vector2Int vector2Int3 = vector2Int + vector2Int2;
				if (!hashSet.Contains(vector2Int3) && (canMove == null || canMove(vector2Int3)))
				{
					hashSet.Add(vector2Int3);
					dictionary[vector2Int3] = vector2Int;
					queue.Enqueue(vector2Int3);
				}
			}
		}
		List<Vector2Int> list = new List<Vector2Int>();
		if (!hashSet.Contains(goal))
		{
			return list;
		}
		Vector2Int vector2Int4 = goal;
		list.Add(vector2Int4);
		while (vector2Int4 != start)
		{
			vector2Int4 = dictionary[vector2Int4];
			list.Add(vector2Int4);
		}
		list.Reverse();
		return list;
	}
}
