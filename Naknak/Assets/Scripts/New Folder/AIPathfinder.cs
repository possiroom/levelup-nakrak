using System.Collections.Generic;

public class AIPathfinder
{
    RoomGraph graph;

    public AIPathfinder(RoomGraph graph)
    {
        this.graph = graph;
    }

    public List<int> FindPath(int start, int goal)
    {
        Queue<int> queue = new();

        Dictionary<int, int> parent = new();

        HashSet<int> visited = new();

        queue.Enqueue(start);

        visited.Add(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == goal)
                break;

            foreach (int next in graph.GetNeighbors(current))
            {
                if (visited.Contains(next))
                    continue;

                visited.Add(next);

                parent[next] = current;

                queue.Enqueue(next);
            }
        }

        List<int> path = new();

        if (!visited.Contains(goal))
            return path;

        int node = goal;

        path.Add(node);

        while (node != start)
        {
            node = parent[node];

            path.Add(node);
        }

        path.Reverse();

        return path;
    }
}