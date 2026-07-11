using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private RoomGraph graph;

    public void Initialize()
    {
        if (graph == null) return;
        Debug.Log($"Rooms initialized: {graph.nodes.Count}");
    }
}
