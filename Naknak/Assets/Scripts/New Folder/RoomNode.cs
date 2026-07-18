using System.Collections.Generic;

public class RoomNode
{
    public int roomID;

    public List<int> neighbors = new List<int>();

    public RoomNode(int id)
    {
        roomID = id;
    }
}