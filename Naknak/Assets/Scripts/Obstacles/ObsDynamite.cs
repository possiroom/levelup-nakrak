using UnityEngine;

public class ObsDynamite : ObstacleBlock
{
    protected override void RunObstacleAction()
    {
        Debug.Log("펑!!!!!");
        GameEventBase evt = GameEventFactory.CreateAdjustHealthEvent(-10);
        GameEventManager.Instance.Submit(evt);
    }

    public override void LoadOnCamera()
    {
        Debug.Log("다이너마이트 자연 소멸");
        TriggerExecutor.Instance.RemoveIndex(this);
        Destroy(gameObject);
    }
}
