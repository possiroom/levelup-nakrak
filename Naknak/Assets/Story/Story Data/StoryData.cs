using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryData", menuName = "Scriptable Objects/Create New Story Data")]
public class StoryData : ScriptableObject
{
    public string charName;       // 캐릭터 이름
    
    [TextArea(3, 10)]
    public string text;
    
    [Header("설정")]
    public int activatedImage;    // 이미지 번호
    public bool isSelect;         // 선택지 여부
    public bool isEnd;            // 종료 여부

    
    [Header("연결")]
    public List<StorySelectData> selects;
    public StoryData nextData; 
}
