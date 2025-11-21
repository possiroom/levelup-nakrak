public class DialogEvent : GameEventBase
{
    public string storyID;
    public StoryTextData storyTextData;

    // 생성자
    public DialogEvent(string storyID){
        this.storyID = storyID;
    }
    
    public override void Execute()
    {
        if (!DialogManager.Instance.IsDialogActivate) DialogManager.Instance.ShowDialog(storyID);
    }
}
