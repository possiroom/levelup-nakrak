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
        DialogManager.Instance.ShowDialog(storyID);
    }
}
