using UnityEngine;

public class StairBlock : TriggerBlock
{
    [Header("Stair Attributes")]
    [SerializeField] private int floor = 1;

    protected override void OnTriggered()
    {
        if (MapStreamingManager.Instance != null)
            MapStreamingManager.Instance.ChangePlayerFloor(floor);
    }
}
