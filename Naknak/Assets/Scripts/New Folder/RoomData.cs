using UnityEngine;

[CreateAssetMenu(menuName = "Game/Room Data")]
public class RoomData : ScriptableObject
{
    public int roomID;

    public GameObject roomPrefab;

    public int upRoom = -1;
    public int downRoom = -1;
    public int leftRoom = -1;
    public int rightRoom = -1;
}