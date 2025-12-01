using UnityEngine;

public class StairManager : MonoBehaviour
{
    public LayerMask stairLayer1;
    public LayerMask stairLayer2;

    public PlayerMoveController3 player;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (player.floorLayer.Equals(stairLayer1))
            {
                player.floorLayer = stairLayer2;
            }
            else if (player.floorLayer.Equals(stairLayer2))
            {
                player.floorLayer = stairLayer1;
            }
        }
    }
}