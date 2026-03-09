using System.Collections.Generic;
using UnityEngine;

public enum ConditionEventType {
    None,
    Destroy
} // etc.

[System.Serializable]
public struct ConditionEventMatch {
    public StoryData dataSO;
    public ConditionEventType eventType;
    public GameObject eventObject;
}

public class ConditionToEvent : MonoBehaviour
{
    [SerializeField] private List<ConditionEventMatch> conditionEventList = new();

    public void Execute(StoryData dataSO)
    {
        if (conditionEventList.Count == 0) return;

        foreach (var match in conditionEventList)
        {
            if (match.dataSO == dataSO)
            {
                switch (match.eventType)
                {
                    case ConditionEventType.Destroy:
                        if (match.eventObject != null) Destroy(match.eventObject);
                        else Debug.LogWarning("[ConditionToEvent] Event Object Is Null");
                        break;
                    default: break;
                }
            }
        }
    }

}
