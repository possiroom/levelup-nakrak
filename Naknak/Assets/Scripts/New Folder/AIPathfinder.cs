using System.Collections.Generic;
using UnityEngine;

public class AIPathfinder
{
	public List<int> FindPath(int start, int goal)
	{
		Debug.Log("<<< AIPathfinder.cs 실행 >>>");
		Debug.Log($"FindPath : {start} -> {goal}");
		Queue<int> queue = new Queue<int>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		HashSet<int> hashSet = new HashSet<int>();
		queue.Enqueue(start);
		hashSet.Add(start);
		while (queue.Count > 0)
		{
			int num = queue.Dequeue();
			if (num == goal)
			{
				break;
			}
			foreach (int neighbor in MansionRoomSystem.Instance.GetNeighbors(num))
			{
				if (!hashSet.Contains(neighbor))
				{
					hashSet.Add(neighbor);
					dictionary[neighbor] = num;
					queue.Enqueue(neighbor);
				}
			}
		}
		List<int> list = new List<int>();
		if (!hashSet.Contains(goal))
		{
			return list;
		}
		int num2 = goal;
		list.Add(num2);
		while (num2 != start)
		{
			num2 = dictionary[num2];
			list.Add(num2);
		}
		list.Reverse();
		return list;
	}
}
