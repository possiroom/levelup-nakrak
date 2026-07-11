using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private DoorDirection direction;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Door triggered: {direction}");
        }
    }
}
