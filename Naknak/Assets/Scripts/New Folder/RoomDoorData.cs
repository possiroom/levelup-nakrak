using System;

[Serializable]
public class ImportedRoomDoorData
{
	public int roomId;

	public string roomName;

	public int upDoor;

	public int downDoor;

	public int leftDoor;

	public int rightDoor;

	public int GetTargetRoom(DoorDirection dir)
	{
		return dir switch
		{
			DoorDirection.Up => upDoor, 
			DoorDirection.Down => downDoor, 
			DoorDirection.Left => leftDoor, 
			DoorDirection.Right => rightDoor, 
			_ => 13, 
		};
	}

	public void SetTargetRoom(DoorDirection dir, int targetRoomId)
	{
		switch (dir)
		{
		case DoorDirection.Up:
			upDoor = targetRoomId;
			break;
		case DoorDirection.Down:
			downDoor = targetRoomId;
			break;
		case DoorDirection.Left:
			leftDoor = targetRoomId;
			break;
		case DoorDirection.Right:
			rightDoor = targetRoomId;
			break;
		}
	}

	public void ReplaceTarget(int oldTargetId, int newTargetId)
	{
		if (upDoor == oldTargetId)
		{
			upDoor = newTargetId;
		}
		if (downDoor == oldTargetId)
		{
			downDoor = newTargetId;
		}
		if (leftDoor == oldTargetId)
		{
			leftDoor = newTargetId;
		}
		if (rightDoor == oldTargetId)
		{
			rightDoor = newTargetId;
		}
	}

	public void ClearDoors()
	{
		upDoor = 13;
		downDoor = 13;
		leftDoor = 13;
		rightDoor = 13;
	}

	public ImportedRoomDoorData Clone()
	{
		return new ImportedRoomDoorData
		{
			roomId = roomId,
			roomName = roomName,
			upDoor = upDoor,
			downDoor = downDoor,
			leftDoor = leftDoor,
			rightDoor = rightDoor
		};
	}
}
