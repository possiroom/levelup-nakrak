using System.Collections.Generic;
using UnityEngine;

public class ConditionBlock : TriggerBlock
{
    [Header("Condition Change")]
    [SerializeField] private List<DialogAction> actions;

    protected override void OnTriggered()
    {
        ConditionManager.Instance.ExecuteActions(actions);
    }
}
