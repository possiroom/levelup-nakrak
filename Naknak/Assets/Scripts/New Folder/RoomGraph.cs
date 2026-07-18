using System.Collections.Generic;
using UnityEngine;

public class RoomGraph
{
    Dictionary<int, RoomNode> nodes = new();

    public void AddRoom(RoomData room)
    {
        if (!nodes.ContainsKey(room.roomID))
            nodes.Add(room.roomID, new RoomNode(room.roomID));

        RoomNode node = nodes[room.roomID];

        AddNeighbor(node, room.upRoom);
        AddNeighbor(node, room.downRoom);
        AddNeighbor(node, room.leftRoom);
        AddNeighbor(node, room.rightRoom);
    }

    void AddNeighbor(RoomNode node, int id)
    {
        if (id == -1)
            return;

        node.neighbors.Add(id);
    }

    public List<int> GetNeighbors(int roomID)
    {
        return nodes[roomID].neighbors;
    }
}