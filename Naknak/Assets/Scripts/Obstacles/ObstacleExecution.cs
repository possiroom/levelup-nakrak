using UnityEngine;

public abstract class ObstacleExecution : MonoBehaviour
{
    protected abstract void FirstExecute();

    private void Start()
    {
        if (!TemporaryFeatureSwitches.ObstaclesEnabled) return;
        Debug.Log("[ObstacleExecution] FirstExecuted");
        this.FirstExecute();
    }
}
