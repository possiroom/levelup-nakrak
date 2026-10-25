using CitrioN.Common;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

namespace CitrioN.SettingsMenuCreator.Input
{
  [System.Serializable]
  [DisplayName("Input Action Processor (Bool)")]
  public class Setting_InputActionProcessor_Bool : Setting_InputActionProcessor<bool>
  {
    [SerializeField]
    protected InputActionProcessorType_Bool processorType;

    public Setting_InputActionProcessor_Bool()
    {
      defaultValue = false;
    }

    public override string EditorName => $"Action Processor (Bool / {processorType.ToString().SplitCamelCase().Replace("_", " ")}) | {InputAction?.name}";

    protected override object ApplyParameterOverride(SettingsCollection settings, bool value)
    {
      var action = InputAction;
      if (action == null) { return null; }

      switch (processorType)
      {
        case InputActionProcessorType_Bool.InvertVector2_XY:
          action.ApplyParameterOverride<InvertVector2Processor, bool>((p) => p.invertX, value);
          action.ApplyParameterOverride<InvertVector2Processor, bool>((p) => p.invertY, value);
          break;
        case InputActionProcessorType_Bool.InvertVector2_X:
          action.ApplyParameterOverride<InvertVector2Processor, bool>((p) => p.invertX, value);
          break;
        case InputActionProcessorType_Bool.InvertVector2_Y:
          action.ApplyParameterOverride<InvertVector2Processor, bool>((p) => p.invertY, value);
          break;
        default:
          break;
      }

      return value;
    }
  }
}
