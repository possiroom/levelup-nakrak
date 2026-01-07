using UnityEngine;

[CreateAssetMenu(fileName = "ConditionID", menuName = "Scriptable Objects/Create New ConditionID")]
public class ConditionID : ScriptableObject
{
    [ReadOnly] public int sortOrder = 0;
    [TextArea] public string description;
}
