public class DialogEvent : GameEventBase
{
    public StoryData storyID;

    // 생성자
    public DialogEvent(StoryData storyID){
        this.storyID = storyID;
    }
    
    public override void Execute()
    {
        DialogManager.Instance.ShowDialog(storyID);
    }
}
