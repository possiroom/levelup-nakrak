using UnityEngine;

public class AIPathfinder : MonoBehaviour
{
    public Vector2Int GetNextTile(Vector2Int current, Vector2Int target)
    {
        if (current.x < target.x) return new Vector2Int(current.x + 1, current.y);
        if (current.x > target.x) return new Vector2Int(current.x - 1, current.y);
        if (current.y < target.y) return new Vector2Int(current.x, current.y + 1);
        if (current.y > target.y) return new Vector2Int(current.x, current.y - 1);
        return current;
    }
}
