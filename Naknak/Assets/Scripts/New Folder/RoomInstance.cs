using UnityEngine;

public class RoomInstance : MonoBehaviour
{
	private RoomData roomData;

	public RoomData RoomData => roomData;

	public void Initialize(RoomData data)
	{
		roomData = data;
		DoorTrigger[] componentsInChildren = GetComponentsInChildren<DoorTrigger>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Initialize(this);
		}
	}
}
