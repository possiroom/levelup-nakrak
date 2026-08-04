using UnityEngine;

public class RoomMoveInfo
{
	public int fromRoomId;

	public int toRoomId;

	public Transform doorTrigger;

	public Transform spawnPoint;

	public RoomMoveInfo(int fromRoomId, int toRoomId, Transform doorTrigger, Transform spawnPoint)
	{
		this.fromRoomId = fromRoomId;
		this.toRoomId = toRoomId;
		this.doorTrigger = doorTrigger;
		this.spawnPoint = spawnPoint;
	}
}
