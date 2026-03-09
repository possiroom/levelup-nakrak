using UnityEngine;

public abstract class ObstacleExecution : MonoBehaviour
{
    protected abstract void FirstExecute();

    private void Start()
    {
        Debug.Log("[ObstacleExecution] FirstExecuted");
        this.FirstExecute();
    }
}
