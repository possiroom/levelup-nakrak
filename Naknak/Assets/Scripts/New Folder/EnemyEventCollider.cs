using UnityEngine;

public class EnemyEventCollider : MonoBehaviour
{
    public bool IsPlayerInRange()
    {
        Collider2D[] hit = Physics2D.OverlapCircleAll((Vector2)transform.position, 5f);

        foreach(Collider2D col in hit)
        {
            if(col.CompareTag("Player"))
                return true;
        }

        return false;
    }
}
