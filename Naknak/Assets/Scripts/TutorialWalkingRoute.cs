using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Four-direction walking routes through the current shop, avoiding furniture and people.</summary>
public static class TutorialWalkingRoute
{
    private static readonly Vector2Int[] Directions =
        { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

    public static bool TryBuild(Transform actor, Vector2 from, Vector2 target, out Vector3[] route,
        bool includeInactivePeople = false)
    {
        route = System.Array.Empty<Vector3>();
        if (actor == null) return false;
        var scene = actor.gameObject.scene;
        var nodes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var maps = nodes.Select(t => t.GetComponent<Tilemap>()).Where(m => m != null && m.gameObject.activeInHierarchy).ToArray();
        var walls = maps.Where(m => m.GetComponent<Collider2D>() != null).ToArray();
        var floor = maps.FirstOrDefault(m => m.name == "Tilemap" && m.GetComponent<Collider2D>() == null);
        var people = nodes.Where(t => t.GetComponent<NPC>() != null).ToList();
        people.AddRange(Object.FindObjectsByType<PlayerMoveController3>(FindObjectsSortMode.None).Select(p => p.transform));
        people = people.Where(p => p != actor && !p.IsChildOf(actor) &&
            (includeInactivePeople || p.gameObject.activeInHierarchy)).Distinct().ToList();
        var obstacles = nodes.SelectMany(t => t.GetComponents<Collider2D>()).Where(c =>
            c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && c.GetComponent<Tilemap>() == null &&
            !c.transform.IsChildOf(actor) && c.GetComponentInParent<NPC>() == null &&
            c.GetComponentInParent<PlayerMoveController3>() == null).ToArray();
        var start = Vector2Int.RoundToInt(from);
        var goal = Vector2Int.RoundToInt(target);
        var pending = new Queue<Vector2Int>();
        var previous = new Dictionary<Vector2Int, Vector2Int>();
        var distance = new Dictionary<Vector2Int, int> { [start] = 0 };
        pending.Enqueue(start);
        Vector2Int? closest = null;
        int bestOffset = int.MaxValue;
        while (pending.Count > 0 && distance.Count < 512)
        {
            var cell = pending.Dequeue();
            int offset = (cell - goal).sqrMagnitude;
            if (offset <= 4 && offset < bestOffset && IsOpen(cell, floor, walls, people, obstacles, start))
            {
                closest = cell;
                bestOffset = offset;
                if (offset == 0) break;
            }
            foreach (var direction in Directions)
            {
                var next = cell + direction;
                if (distance.ContainsKey(next) || !IsOpen(next, floor, walls, people, obstacles, start)) continue;
                previous[next] = cell;
                distance[next] = distance[cell] + 1;
                pending.Enqueue(next);
            }
        }
        if (!closest.HasValue) return false;
        var result = new List<Vector3>();
        var current = closest.Value;
        while (current != start)
        {
            result.Add(new Vector3(current.x, current.y, actor.position.z));
            current = previous[current];
        }
        result.Reverse();
        route = result.ToArray();
        return true;
    }

    private static bool IsOpen(Vector2Int cell, Tilemap floor, Tilemap[] walls,
        List<Transform> people, Collider2D[] obstacles, Vector2Int start)
    {
        var foot = new Vector3(cell.x, cell.y - .5f, 0);
        if (floor != null && !floor.HasTile(floor.WorldToCell(foot))) return false;
        if (floor == null && (Mathf.Abs(cell.x - start.x) > 20 || Mathf.Abs(cell.y - start.y) > 20)) return false;
        foreach (var wall in walls)
        {
            var tile = wall.WorldToCell(foot);
            if (wall.HasTile(tile) && wall.GetColliderType(tile) != Tile.ColliderType.None) return false;
        }
        foreach (var person in people)
            if (Mathf.Abs(cell.x - person.position.x) < .8f && Mathf.Abs(cell.y - person.position.y) < 1.1f) return false;
        foreach (var obstacle in obstacles)
        {
            var bounds = obstacle.bounds;
            // A small foot clearance prevents cutting through props at tile boundaries.
            if (foot.x > bounds.min.x - .15f && foot.x < bounds.max.x + .15f &&
                foot.y > bounds.min.y - .15f && foot.y < bounds.max.y + .15f) return false;
        }
        return true;
    }
}
