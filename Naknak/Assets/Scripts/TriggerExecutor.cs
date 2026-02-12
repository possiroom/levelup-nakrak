using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class TriggerExecutor : MonoBehaviour
{
    public static TriggerExecutor Instance { get; private set; }

    // 추후 접근 권한 수정 예정
    public GridLayout grid;
    public GameObject trigTilemap;
    private Dictionary<Vector2Int, TriggerBlock> map = new();

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void ChangeMap(GridLayout grid, GameObject trigTilemap)
    {
        this.grid = grid;
        this.trigTilemap = trigTilemap;
        BuildIndex();
    }

    public void ChangeTileMap(GameObject trigTilemap)
    {
        this.trigTilemap = trigTilemap;
        BuildIndex();
    }

    public void BuildIndex()
    {
        map.Clear();
        if (trigTilemap == null) return;

        TriggerBlock[] blocks = trigTilemap.GetComponentsInChildren<TriggerBlock>();
        for (int i = 0; i < blocks.Length; i++)
        {
            TriggerBlock b      = blocks[i];
            Vector3Int   cell3  = grid.WorldToCell(b.transform.position);
            Vector2Int   cell   = new(cell3.x, cell3.y);

            map.Add(cell, b);
        }
        Debug.Log("[TriggerExecutor] Build Index Count: " + map.Count);
    }

    public void RemoveIndex(TriggerBlock triggerBlock)
    {
        if (map.Values.Contains(triggerBlock))
        {
            Vector2Int key = map.First(x => x.Value == triggerBlock).Key;
            map.Remove(key);
        }
        Debug.Log("[TriggerExecutor] Remove Index. Count: " + map.Count);
    }

    public bool OnStepStarted(Vector2 pos)
    {
        bool pause = false;
        Vector2Int cell = CellTo2Int(grid.WorldToCell(pos));
        if (map.TryGetValue(cell, out TriggerBlock block))
        {
            pause = block.DepartTrigger();
        }
        return pause;
    }

    public bool OnStepCompleted(Vector2 pos)
    {
        bool pause = false;
        Vector2Int cell = CellTo2Int(grid.WorldToCell(pos));
        if (map.TryGetValue(cell, out TriggerBlock block))
        {
            pause = block.ArrivedTrigger();
        }
        return pause;
    }

    private Vector2Int CellTo2Int(Vector3Int cell3)
    {
        return new(cell3.x, cell3.y);
    }
}
