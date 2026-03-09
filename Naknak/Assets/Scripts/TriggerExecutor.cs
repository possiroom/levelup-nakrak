using System.Collections.Generic;
using UnityEngine;

public sealed class TriggerExecutor : MonoBehaviour
{
    public static TriggerExecutor Instance { get; private set; }

    // 추후 접근 권한 수정 예정
    public GridLayout grid;
    public GameObject trigTilemap;
    [SerializeField] private ConditionToEvent conditionToEvent;
    private Dictionary<Vector2Int, List<TriggerBlock>> map = new();

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

    public void ChangeC2E(ConditionToEvent conditionToEvent)
    {
        this.conditionToEvent = conditionToEvent;
    }

    public void BuildIndex()
    {
        map.Clear();
        if (trigTilemap == null) return;

        TriggerBlock[] blocks = trigTilemap.GetComponentsInChildren<TriggerBlock>();
        for (int i = 0; i < blocks.Length; i++)
        {
            TriggerBlock b = blocks[i];
            Vector2Int cell = (Vector2Int)grid.WorldToCell(b.transform.position);

            if (!map.TryGetValue(cell, out List<TriggerBlock> blockList))
            {
                blockList = new List<TriggerBlock>();
                map.Add(cell, blockList);
            }
            blockList.Add(b);
        }
        Debug.Log("[TriggerExecutor] Build Index. Cell Count: " + map.Count + ", Trigger Count: " + blocks.Length);
    }

    public void RemoveIndex(TriggerBlock triggerBlock)
    {
        if (triggerBlock == null) return;

        List<Vector2Int> emptyCells = null;
        foreach (var pair in map)
        {
            if (pair.Value.Remove(triggerBlock) && pair.Value.Count == 0)
            {
                if (emptyCells == null) emptyCells = new List<Vector2Int>();
                emptyCells.Add(pair.Key);
            }
        }

        if (emptyCells != null)
        {
            for (int i = 0; i < emptyCells.Count; i++)
            {
                map.Remove(emptyCells[i]);
            }
        }

        Debug.Log("[TriggerExecutor] Remove Index. Cell Count: " + map.Count);
    }

    public bool OnStepStarted(Vector2 pos)
    {
        bool pause = false;
        Vector2Int cell = CellTo2Int(grid.WorldToCell(pos));
        if (map.TryGetValue(cell, out List<TriggerBlock> blocks))
        {
            // 실행 중 리스트가 변경될 수 있어 스냅샷으로 순회
            TriggerBlock[] snapshots = blocks.ToArray();
            for (int i = 0; i < snapshots.Length; i++)
            {
                TriggerBlock block = snapshots[i];
                if (block == null) continue;
                pause = block.DepartTrigger() || pause;
            }
        }
        return pause;
    }

    public bool OnStepCompleted(Vector2 pos)
    {
        bool pause = false;
        Vector2Int cell = CellTo2Int(grid.WorldToCell(pos));
        if (map.TryGetValue(cell, out List<TriggerBlock> blocks))
        {
            // 실행 중 리스트가 변경될 수 있어 스냅샷으로 순회
            TriggerBlock[] snapshots = blocks.ToArray();
            for (int i = 0; i < snapshots.Length; i++)
            {
                TriggerBlock block = snapshots[i];
                if (block == null) continue;
                pause = block.ArrivedTrigger() || pause;
            }
        }
        return pause;
    }

    public void AfterDialogEvent(StoryData dataSO)
    {
        conditionToEvent.Execute(dataSO);
    }

    private Vector2Int CellTo2Int(Vector3Int cell3)
    {
        return new(cell3.x, cell3.y);
    }
}
