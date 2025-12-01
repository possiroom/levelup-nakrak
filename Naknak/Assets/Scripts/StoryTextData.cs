using System.Collections.Generic;

public class StoryTextData
{
    public string   storyID         { get; private set; }
    public string   text            { get; private set; }
    public string   name            { get; private set; }
    public int      activatedImage  { get; private set; }
    public bool     isSelect        { get; private set; }
    public string   nextStoryID     { get; private set; }
    public StoryTextData nextData   { get; private set; }
    public List<StorySelectData> selects { get; private set; }

    public StoryTextData(
        string storyID      = "<NULL>", 
        string text         = "", 
        string name         = "",
        int activatedImage  = 0, 
        bool isSelect       = false, 
        string nextStoryID  = "<END>",
        StoryTextData nextData = null,
        List<StorySelectData> selects = null
    ) {
        this.storyID        = storyID;          // 스토리 ID
        this.text           = text;             // 대사
        this.name           = name;             // 캐릭터 이름
        this.activatedImage = activatedImage;   // 선택 이미지
        this.isSelect       = isSelect;         // 사용자의 선택을 요구하는 대사인지
        this.nextStoryID    = nextStoryID;      // 상호작용 키를 누르면 재생할 다음 스토리(대사) ID
        this.nextData       = nextData;         // 재생할 다음 스토리 인스턴스 주소
        this.selects        = selects;          // 선택지 주소
    }


    /// <summary>
    /// 추후 Handler로 분리할 예정
    /// </summary>
    /// <param name="nextData"></param>
    public void SetNextData(StoryTextData nextData)
    {
        this.nextData = nextData;
    }

    public void AddSelect(StorySelectData select)
    {
        selects.Add(select);
    }
}
