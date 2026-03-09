public class AdjustHealthEvent : GameEventBase
{
    public int delta;

    // 생성자
    public AdjustHealthEvent(int delta){
        this.delta = delta;
    }

    public override void Execute()
    {
        PlayerStatus.Instance.AddHealth(delta);
    }
}
