using UnityEngine;

public class AlterSpawnTestInput : MonoBehaviour
{
    [SerializeField] private AlterSpawnManager spawnManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (spawnManager != null)
            {
                spawnManager.TriggerBlindDebuff();
            }
        }
    }
}
