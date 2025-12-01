public class StorySelectData
{
    public string   text            { get; private set; }
    public string   nextStoryID     { get; private set; }
    public StoryTextData nextData   { get; private set; }

    public StorySelectData(
        string text         = "", 
        string nextStoryID  = "<SKIP>",
        StoryTextData nextData = null
    ) {
        this.text           = text;
        this.nextStoryID    = nextStoryID;
        this.nextData       = nextData;
    }

    /// <summary>
    /// 추후 Handler로 분리할 예정
    /// </summary>
    /// <param name="nextData"></param>
    public void SetNextData(StoryTextData nextData)
    {
        this.nextData = nextData;
    }
}
