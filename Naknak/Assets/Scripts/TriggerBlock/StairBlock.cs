using UnityEngine;

public class StairBlock : TriggerBlock
{
    [Header("Stair Attributes")]
    [SerializeField] private int floor = 1;

    protected override void OnTriggered()
    {
        if (MapManager.Instance != null) MapManager.Instance.ChangePlayerFloor(floor);
        else FindFirstObjectByType<PlayerMoveController3>()?.SetFloor(floor);
    }
}
