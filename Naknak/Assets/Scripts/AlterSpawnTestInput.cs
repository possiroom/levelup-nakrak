using UnityEngine;

public class AlterSpawnTestInput : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            if (AlterSpawnManager.Instance != null)
            {
                AlterSpawnManager.Instance.TriggerBlindDebuff();
            }
            else
            {
                Debug.LogWarning("[AlterSpawnTestInput] AlterSpawnManager.Instance is NULL.");
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (AlterSpawnManager.Instance != null)
            {
                AlterSpawnManager.Instance.TriggerBlindDebuff(4f);
            }
            else
            {
                Debug.LogWarning("[AlterSpawnTestInput] AlterSpawnManager.Instance is NULL.");
            }
        }
    }
}