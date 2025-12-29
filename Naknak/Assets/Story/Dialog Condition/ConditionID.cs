using UnityEngine;

[CreateAssetMenu(fileName = "ConditionID", menuName = "Scriptable Objects/Create New ConditionID")]
public class ConditionID : ScriptableObject
{
    [TextArea] public string description;
}
