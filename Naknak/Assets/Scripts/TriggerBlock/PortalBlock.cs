using UnityEngine;

public sealed class PortalBlock : TriggerBlock
{
    [Header("Portal Attributes")]
    [SerializeField] private GameObject targetMap;
    [SerializeField] private Vector2Int arrivePos;
    [SerializeField] private int floor = 1;
    [SerializeField] private bool destroyMap = false;

    protected override void OnTriggered()
    {
        StartCoroutine(MapManager.Instance.ChangeMap(targetMap, arrivePos, destroyMap));
        MapManager.Instance.ChangePlayerFloor(floor);
    }
}
