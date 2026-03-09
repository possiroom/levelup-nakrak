using UnityEngine;

public class ObsThornyVine : ObstacleBlock
{
    protected override void RunObstacleAction()
    {
        Debug.Log("악");
        GameEventBase evt = GameEventFactory.CreateAdjustHealthEvent(-10);
        GameEventManager.Instance.Submit(evt);
    }

    public override void LoadOnCamera()
    {
        return;
    }
}
