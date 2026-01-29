using UnityEngine;

public sealed class DebugLogBlock : TriggerBlock
{
    [Header("Log Text")]
    [TextArea]
    [SerializeField] private string logMessage = "";


    protected override void OnTriggered()
    {
        Debug.Log(logMessage);
    }
}
