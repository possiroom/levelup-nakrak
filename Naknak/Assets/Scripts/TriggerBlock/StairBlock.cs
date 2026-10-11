using UnityEngine;

public class StairBlock : TriggerBlock
{
    [Header("Stair Attributes")]
    [SerializeField] private int floor = 1;

    protected override void OnTriggered()
    {
        MapManager.Instance.ChangePlayerFloor(floor);
    }
}
