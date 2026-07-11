using UnityEngine;

public class AIManager : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
}
