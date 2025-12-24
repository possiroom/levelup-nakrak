using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogContainer", menuName = "Scriptable Objects/Create New Dialog Container")]
public class DialogContainer : ScriptableObject
{
    [System.Serializable]
    public struct DialogSlot
    {
        // 개발자 메모 (예: 퀘스트 완료 후)
        public string memo;

        public DialogCondition condition;
        public StoryData headStoryData;
    }

    public List<DialogSlot> dialogSlots; // 슬롯 리스트

    /// <summary>
    /// 조건에 맞는 대사를 찾는 함수 
    /// </summary>
    public StoryData GetMatchingDialog()
    {
        foreach (var slot in dialogSlots)
        {
            // 조건이 없거나(null), 조건이 참(True)이면 해당 대사 반환
            if (slot.condition == null || slot.condition.IsMet())
            {
                return slot.headStoryData;
            }
        }
        return null;
    }
}
