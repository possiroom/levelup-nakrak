public class NextDialogEvent : GameEventBase
{
    public override void Execute()
    {
        DialogManager.Instance.AdvanceFromInput();
    }
}
