using UnityEngine;

public class ObsWater : ObstacleBlock
{
    protected override void RunObstacleAction()
    {
        Debug.Log("냠");
        GameEventBase evt = GameEventFactory.CreateAdjustHealthEvent(5);
        GameEventManager.Instance.Submit(evt);
    }

    public override void LoadOnCamera()
    {
        Debug.Log("물 자연 소멸");
        TriggerExecutor.Instance.RemoveIndex(this);
        Destroy(gameObject);
    }
}
