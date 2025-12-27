using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Story/Dialog Container")]
public class DialogContainer : ScriptableObject
{
    [System.Serializable]
    public class DialogSlot
    {
        public string memo;
        
        // 이 슬롯을 선택할 조건 리스트
        public List<DialogCondition> conditions = new List<DialogCondition>(); 
        
        public StoryData headStoryData;
    }

    public List<DialogSlot> dialogSlots;

    // 조건 검사 매소드
    public StoryData GetMatchingDialog()
    {
        foreach (var slot in dialogSlots)
        {
            if (ConditionManager.Instance.CheckConditions(slot.conditions))
            {
                return slot.headStoryData;
            }
        }
        return null;
    }
}