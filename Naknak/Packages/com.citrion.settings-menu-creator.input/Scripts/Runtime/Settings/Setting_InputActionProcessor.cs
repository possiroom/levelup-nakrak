using CitrioN.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Setting for applying a processor to an <see cref="InputActionReference"/>
  /// </summary>
  [MenuOrder(170)]
  [MenuPath("Input/")]
  [System.Serializable]
  public abstract class Setting_InputActionProcessor<T1> : Setting_Generic<T1>
  {
    #region Fields & Properties

    [SerializeField]
    [Tooltip("The InputActionReference for which to apply the processor for.")]
    protected InputActionReference inputActionReference;

    protected InputAction InputAction 
      => inputActionReference != null ? inputActionReference.action : null;

    public override string EditorNamePrefix => "[Input]";

    public override string RuntimeName => $"{InputAction?.name} Processor";

    public override string EditorName
    {
      get
      {
        return $"Action Processor ({typeof(T1).Name}) | {InputAction?.name}";
      }
    }

    #endregion

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, T1 value)
    {
      ApplyParameterOverride(settings, value);
      return value;
    }

    protected abstract object ApplyParameterOverride(SettingsCollection settings, T1 value);
  }
}