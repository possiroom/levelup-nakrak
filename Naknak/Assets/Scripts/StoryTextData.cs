public class StoryTextData
{
    public string   storyID         { get; private set; }
    public string   text            { get; private set; }
    public string   name            { get; private set; }
    public int      activatedImage  { get; private set; }
    public bool     isSelect        { get; private set; }
    public string   nextStoryID     { get; private set; }

    public StoryTextData(
        string storyID, 
        string text, 
        string name,
        int activatedImage, 
        bool isSelect, 
        string nextStoryID
    ) {
        this.storyID        = storyID;          // 스토리 ID
        this.text           = text;             // 대사
        this.name           = name;             // 캐릭터 이름
        this.activatedImage = activatedImage;   // 선택 이미지
        this.isSelect       = isSelect;         // 사용자의 선택을 요구하는 대사인지
        this.nextStoryID    = nextStoryID;      // 상호작용 키를 누르면 재생할 다음 스토리(대사) ID
    }
}
