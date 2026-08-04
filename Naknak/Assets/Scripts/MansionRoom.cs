using UnityEngine;

public class MansionRoom : MonoBehaviour
{
	[Header("Room Info")]
	[SerializeField]
	private int roomId;

	[SerializeField]
	private string roomName;

	[Header("Spawn Points")]
	[SerializeField]
	private Transform upSpawnPoint;

	[SerializeField]
	private Transform downSpawnPoint;

	[SerializeField]
	private Transform leftSpawnPoint;

	[SerializeField]
	private Transform rightSpawnPoint;

	[Header("Visual Point")]
	[SerializeField]
	private Transform visualPoint;

	[Header("Door Points")]
	[SerializeField]
	private Transform upDoorPoint;

	[SerializeField]
	private Transform downDoorPoint;

	[SerializeField]
	private Transform leftDoorPoint;

	[SerializeField]
	private Transform rightDoorPoint;

	public int RoomId => roomId;

	public string RoomName => roomName;

	public Transform GetDoorPoint(DoorDirection exitDirection)
	{
		switch (exitDirection)
		{
		case DoorDirection.Up:
			if (!(upDoorPoint != null))
			{
				return upSpawnPoint;
			}
			return upDoorPoint;
		case DoorDirection.Down:
			if (!(downDoorPoint != null))
			{
				return downSpawnPoint;
			}
			return downDoorPoint;
		case DoorDirection.Left:
			if (!(leftDoorPoint != null))
			{
				return leftSpawnPoint;
			}
			return leftDoorPoint;
		case DoorDirection.Right:
			if (!(rightDoorPoint != null))
			{
				return rightSpawnPoint;
			}
			return rightDoorPoint;
		default:
			if (!(downDoorPoint != null))
			{
				return downSpawnPoint;
			}
			return downDoorPoint;
		}
	}

	public Transform GetSpawnPoint(DoorDirection enterDirection)
	{
		return enterDirection switch
		{
			DoorDirection.Up => upSpawnPoint, 
			DoorDirection.Down => downSpawnPoint, 
			DoorDirection.Left => leftSpawnPoint, 
			DoorDirection.Right => rightSpawnPoint, 
			_ => downSpawnPoint, 
		};
	}

	public Vector3 GetVisualPosition()
	{
		if (visualPoint != null)
		{
			return visualPoint.position;
		}
		Vector3 sum = Vector3.zero;
		int count = 0;
		AddSpawnPosition(upSpawnPoint, ref sum, ref count);
		AddSpawnPosition(downSpawnPoint, ref sum, ref count);
		AddSpawnPosition(leftSpawnPoint, ref sum, ref count);
		AddSpawnPosition(rightSpawnPoint, ref sum, ref count);
		if (count > 0)
		{
			return sum / count;
		}
		return base.transform.position;
	}

	private void AddSpawnPosition(Transform point, ref Vector3 sum, ref int count)
	{
		if (!(point == null))
		{
			sum += point.position;
			count++;
		}
	}
}
