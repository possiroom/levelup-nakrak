using CitrioN.Common;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;

namespace CitrioN.SettingsMenuCreator.Input
{
  [System.Serializable]
  [DisplayName("Input Action Processor (Float)")]
  public class Setting_InputActionProcessor_Float : Setting_InputActionProcessor<float>
  {
    [SerializeField]
    protected InputActionProcessorType_Float processorType;

    public Setting_InputActionProcessor_Float()
    {
      options.AddMinMaxRangeValues("0", "2");
      options.AddStepSize("0.01");
      defaultValue = 1;
    }

    public override string EditorName => $"Action Processor (Float / {processorType.ToString().SplitCamelCase().Replace("_", " ")}) | {InputAction?.name}";

    protected override object ApplyParameterOverride(SettingsCollection settings, float value)
    {
      var action = InputAction;
      if (action == null) { return null; }

      //var bindingIndex = BindingIndex;
      //if (bindingIndex < 0) { return null; }

      //var bindings = action.bindings;
      //var binding = bindings[bindingIndex];

      //binding.processors = $"ScaleVector2(x={value})";
      //InputAction.ApplyBindingOverride(binding);

      switch (processorType)
      {
        case InputActionProcessorType_Float.ScaleVector2_XY:
          action.ApplyParameterOverride<ScaleVector2Processor, float>((p) => p.x, value);
          action.ApplyParameterOverride<ScaleVector2Processor, float>((p) => p.y, value);
          break;
        case InputActionProcessorType_Float.ScaleVector2_X:
          action.ApplyParameterOverride<ScaleVector2Processor, float>((p) => p.x, value);
          break;
        case InputActionProcessorType_Float.ScaleVector2_Y:
          action.ApplyParameterOverride<ScaleVector2Processor, float>((p) => p.y, value);
          break;
        case InputActionProcessorType_Float.Scale:
          action.ApplyParameterOverride<ScaleProcessor, float>((p) => p.factor, value);
          break;
        case InputActionProcessorType_Float.Clamp_Min:
          action.ApplyParameterOverride<ClampProcessor, float>((p) => p.min, value);
          break;
        case InputActionProcessorType_Float.Clamp_Max:
          action.ApplyParameterOverride<ClampProcessor, float>((p) => p.max, value);
          break;
        case InputActionProcessorType_Float.Normalize_Min:
          action.ApplyParameterOverride<NormalizeProcessor, float>((p) => p.min, value);
          break;
        case InputActionProcessorType_Float.Normalize_Max:
          action.ApplyParameterOverride<NormalizeProcessor, float>((p) => p.max, value);
          break;
        case InputActionProcessorType_Float.Normalize_Zero:
          action.ApplyParameterOverride<NormalizeProcessor, float>((p) => p.zero, value);
          break;
        default:
          break;
      }

      return value;
    }
  }
}
