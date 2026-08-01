using UnityEngine;

public class MansionRoom : MonoBehaviour
{
    [Header("Room Info")]
    [SerializeField] private int roomId;
    [SerializeField] private string roomName;

    [Header("Spawn Points")]
    [SerializeField] private Transform upSpawnPoint;
    [SerializeField] private Transform downSpawnPoint;
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private Transform rightSpawnPoint;

    [Header("Visual Point")]
    [SerializeField] private Transform visualPoint;

    public int RoomId => roomId;
    public string RoomName => roomName;

    public Transform GetSpawnPoint(DoorDirection enterDirection)
    {
        switch (enterDirection)
        {
            case DoorDirection.Up:
                return upSpawnPoint;

            case DoorDirection.Down:
                return downSpawnPoint;

            case DoorDirection.Left:
                return leftSpawnPoint;

            case DoorDirection.Right:
                return rightSpawnPoint;

            default:
                return downSpawnPoint;
        }
    }

    public Vector3 GetVisualPosition()
    {
        if (visualPoint != null)
            return visualPoint.position;

        Vector3 sum = Vector3.zero;
        int count = 0;

        AddSpawnPosition(upSpawnPoint, ref sum, ref count);
        AddSpawnPosition(downSpawnPoint, ref sum, ref count);
        AddSpawnPosition(leftSpawnPoint, ref sum, ref count);
        AddSpawnPosition(rightSpawnPoint, ref sum, ref count);

        if (count > 0)
            return sum / count;

        return transform.position;
    }

    private void AddSpawnPosition(Transform point, ref Vector3 sum, ref int count)
    {
        if (point == null)
            return;

        sum += point.position;
        count++;
    }
}