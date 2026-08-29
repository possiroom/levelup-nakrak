using System.Collections.Generic;
using UnityEngine;

public class AIPathfinder
{
	public List<int> FindPath(int start, int goal)
	{
		// 방 ID 그래프에서 start -> goal까지의 최단 방 경로를 BFS로 찾는다.
		// MansionRoomSystem.GetAINeighbors는 삭제된 방을 제외하거나, 삭제방 내부에서는 탈출용 base door를 제공한다.
		Debug.Log($"FindPath : {start} -> {goal}");
		Queue<int> queue = new Queue<int>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		HashSet<int> hashSet = new HashSet<int>();

		// BFS frontier 초기화.
		queue.Enqueue(start);
		hashSet.Add(start);
		while (queue.Count > 0)
		{
			int num = queue.Dequeue();
			if (num == goal)
			{
				break;
			}

			// 현재 방에서 AI가 이동 가능한 이웃 방을 모두 탐색한다.
			foreach (int neighbor in MansionRoomSystem.Instance.GetAINeighbors(num))
			{
				if (!hashSet.Contains(neighbor))
				{
					hashSet.Add(neighbor);
					// neighbor의 이전 방을 저장해 두었다가 마지막에 경로를 역추적한다.
					dictionary[neighbor] = num;
					queue.Enqueue(neighbor);
				}
			}
		}

		// goal을 찾지 못했으면 빈 경로를 반환한다.
		List<int> list = new List<int>();
		if (!hashSet.Contains(goal))
		{
			return list;
		}

		// goal부터 start까지 이전 방을 따라가며 경로를 복원한 뒤 뒤집는다.
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
