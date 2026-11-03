using UnityEngine;

public class RoomMoveInfo
{
	// AI가 현재 어디서 출발하는지 기록한다.
	public int fromRoomId;

	// AI가 이동 완료 후 들어간 것으로 처리할 방 ID.
	public int toRoomId;

	// 현재 방에서 enemy가 걸어가야 하는 실제 문 위치.
	public Transform doorTrigger;

	// 문에 도착한 뒤 enemy를 놓을 다음 방의 도착 위치.
	public Transform spawnPoint;

	public RoomMoveInfo(int fromRoomId, int toRoomId, Transform doorTrigger, Transform spawnPoint)
	{
		// 방 단위 이동에 필요한 출발/도착 방과 실제 Transform 기준점을 한 묶음으로 전달한다.
		this.fromRoomId = fromRoomId;
		this.toRoomId = toRoomId;
		this.doorTrigger = doorTrigger;
		this.spawnPoint = spawnPoint;
	}
}
