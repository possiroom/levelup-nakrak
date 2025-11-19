using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    // Singleton Instance
    public static MapManager Instance { get; private set; }

    [SerializeField]
    private Tilemap collisionTilemap;

    // 추후 외부에서 Setter로 맵을 바꾸는 로직으로 수정
    [SerializeField]
    private Grid grid;

    // Singleton Pattern
    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 콜라이더가 있는지 검사, 있으면 true
    /// </summary>
    /// <param name="position">Vector3 Pos</param>
    /// <returns>bool: 콜라이더가 있는가?</returns>
    public bool IsCollision(Vector3 position) {
        if (!grid)
        {
            Debug.LogWarning("[MapManager] Grid가 지정되지 않았습니다.");
            return true; // 기본은 이동 가능
        }

        // translate position to Vector3Int
        Vector3Int gridPosition = World2Grid(position);

        return collisionTilemap.HasTile(gridPosition);
    }

    // Util
    public Vector3Int World2Grid(Vector3 worldPosition) {
        if (!grid)
        {
            Debug.LogWarning("[MapManager] Grid가 지정되지 않았습니다.");
            return Vector3Int.zero;
        }
        return grid.WorldToCell(worldPosition);
    }

    public Vector3 Grid2World(Vector3Int gridPosition) {
        if (!grid)
        {
            Debug.LogWarning("[MapManager] Grid가 지정되지 않았습니다.");
            return Vector3.zero;
        }
        return grid.GetCellCenterWorld(gridPosition);
    }
}
