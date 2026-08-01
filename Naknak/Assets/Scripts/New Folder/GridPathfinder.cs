using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder
{
    private readonly LayerMask wallLayer;

    private readonly Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };



    public GridPathfinder(LayerMask wallLayer)
    {
        this.wallLayer = wallLayer;
    }

    public bool TryGetNextStep(
        Vector2Int start,
        Vector2Int goal,
        out Vector2Int nextStep)
    {
        nextStep = start;

        Queue<Vector2Int> queue = new();
        Dictionary<Vector2Int, Vector2Int> parent = new();
        HashSet<Vector2Int> visited = new();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == goal)
                break;

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;

                if (visited.Contains(next))
                    continue;

                //if (IsWall(next))
                //    continue;

                visited.Add(next);
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        if (start == goal)
            return false;

        if (!visited.Contains(goal))
            return false;

        Vector2Int node = goal;

        while (parent.ContainsKey(node) && parent[node] != start)
        {
            node = parent[node];
        }

        nextStep = node;
        return true;
    }

    private bool IsWall(Vector2Int cell)
    {
        return false;

        return Physics2D.OverlapBox(
            cell,
            Vector2.one * 0.8f,
            0f,
            wallLayer);
    }
}