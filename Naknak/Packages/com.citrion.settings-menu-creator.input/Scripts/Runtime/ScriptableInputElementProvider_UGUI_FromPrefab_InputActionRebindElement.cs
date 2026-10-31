using CitrioN.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Input element provider for rebinding and InputAction through the <see cref="Setting_InputActionRebind"/> setting.
  /// </summary>
  [CreateAssetMenu(fileName = "Provider_UGUI_InputActionRebindElement_",
                   menuName = "CitrioN/Settings Menu Creator/Input Element Provider/UGUI/Input Action Rebind Element",
                   order = 58)]
  public class ScriptableInputElementProvider_UGUI_FromPrefab_InputActionRebindElement : ScriptableInputElementProvider_UGUI_FromPrefab
  {
    protected static Dictionary<InputActionRebindElement, UnityEngine.Events.UnityAction<InputActionRebindElement, InputActionBinding, string>> rebindDelegates = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    protected static void Init()
    {
      foreach (var item in rebindDelegates)
      {
        item.Key.onValueChanged.RemoveListener(item.Value);
      }
    }

    public override Type GetInputFieldType(SettingsCollection settings) => typeof(InputActionRebindElement);

    public override bool UpdateInputElement(RectTransform elem, string settingIdentifier,
                                            string labelText, SettingsCollection settings,
                                            List<object> values, bool initialize)
    {
      var success = base.UpdateInputElement(elem, settingIdentifier, labelText, settings, values, initialize);

      if (initialize)
      {
        var rebindElement = elem.GetComponentInChildren<InputActionRebindElement>(true);
        if (rebindElement == null) { return false; }

        // Find the corresponding setting
        var setting = settings.Settings.Find(s => s.Identifier == settingIdentifier);

        // Check if the setting is for rebinding
        if (setting != null && setting.Setting is Setting_InputActionRebind rebindSetting)
        {
          var inputActionReference = rebindSetting.InputActionBinding?.InputActionReference;
          var bindingIndex = rebindSetting.BindingIndex;

          // Assign the InputActionBinding to the rebind element
          rebindElement.SetInputActionBinding(rebindSetting.InputActionBinding);
          // Assign the cancel path for the rebinding process
          rebindElement.CancelPaths = rebindSetting.CancelPaths;
          rebindElement.ExcludedPaths = rebindSetting.ExcludedPaths;
          rebindElement.MatchingPaths = rebindSetting.MatchingPaths;

          // Remove any existing delegate for the rebind element.
          // This ensures only one listener is subscribed at all times.
          if (rebindDelegates != null && rebindDelegates.TryGetValue(rebindElement, out var rebindDelegate))
          {
            rebindElement.onValueChanged.RemoveListener(rebindDelegate);
          }

          var collection = settings;
          var identifier = settingIdentifier;

          UnityEngine.Events.UnityAction<InputActionRebindElement, InputActionBinding, string> onRebindDelegate =
            delegate (InputActionRebindElement inputActionRebindElement, InputActionBinding inputActionBinding, string binding)
          {
            if (rebindElement != inputActionRebindElement || inputActionRebindElement == null) { return; }
            OnRebind(collection, identifier, binding);
          };

          rebindDelegates.AddOrUpdateDictionaryItem(rebindElement, onRebindDelegate);

          rebindElement.onValueChanged.AddListener(onRebindDelegate);

          //rebindElement.onValueChanged.AddListener(binding => OnRebind(settings, settingIdentifier, binding));
          return true;
        }
        return false;
      }
      return success;
    }

    private void OnRebind(SettingsCollection settings, string settingIdentifier, string val)
    {
      if (settings == null) { return; }
      settings.ApplySettingChange(settingIdentifier, forceApply: true, true, val);
    }
  }
}
