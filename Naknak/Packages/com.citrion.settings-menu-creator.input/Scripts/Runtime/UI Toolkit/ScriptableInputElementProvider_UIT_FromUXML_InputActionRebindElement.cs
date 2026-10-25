using CitrioN.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace CitrioN.SettingsMenuCreator.Input.UIToolkit
{
  /// <summary>
  /// Input element provider for rebinding and InputAction through the <see cref="Setting_InputActionRebind"/> setting.
  /// </summary>
  [CreateAssetMenu(fileName = "Provider_UIT_FromUXML_InputActionRebindElement_",
                   menuName = "CitrioN/Settings Menu Creator/Input Element Provider/UI Toolkit/Input Action Rebind Element",
                   order = 58)]
  public class ScriptableInputElementProvider_UIT_FromUXML_InputActionRebindElement : ScriptableInputElementProvider_UIT_FromUXML<InputActionRebindElement>
  {
    protected static Dictionary<InputActionRebindElement, UnityEngine.Events.UnityAction<InputActionRebindElement, InputActionBinding, string>> rebindDelegates = new();

    private static Dictionary<InputActionRebindElement, UnityAction> onVisualsUpdatedListeners = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    protected static void Init()
    {
      foreach (var item in rebindDelegates)
      {
        item.Key.onValueChanged.RemoveListener(item.Value);
      }

      onVisualsUpdatedListeners.Clear();
    }

    public override Type GetInputFieldType(SettingsCollection settings) => typeof(InputActionRebindElement);

    public override bool UpdateInputElement(VisualElement elem, string settingIdentifier,
                                            string labelText, SettingsCollection settings,
                                            List<object> values, bool initialize)
    {
      var success = base.UpdateInputElement(elem, settingIdentifier, labelText, settings, values, initialize);

      if (elem == null) { return false; }

      if (!ProviderUtility_UIT.IsCorrectInputElementType<InputActionRebindElement>(elem))
      {
        return false;
      }

      var rebindElement = elem.Q<InputActionRebindElement>();
      if (rebindElement == null) { return false; }

      // TODO Add localization
      UIToolkitUtilities.SetText(rebindElement.label, labelText);

      if (initialize)
      {
        var rebindButton = rebindElement.Q<Button>(className: InputActionRebindElement.rebindButtonClassName);
        rebindButton?.AddToClassList(ProviderUtility_UIT.INPUT_ELEMENT_SELECTABLE_CLASS);
        ProviderUtility_UIT.UpdateInputElementBase(rebindButton, labelText, settings, initialize);

        var resetButton = rebindElement.Q<Button>(className: InputActionRebindElement.resetButtonClassName);
        resetButton?.AddToClassList(ProviderUtility_UIT.INPUT_ELEMENT_SELECTABLE_CLASS);
        ProviderUtility_UIT.UpdateInputElementBase(resetButton, labelText, settings, initialize);

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

          #region Localization
          if (Application.isPlaying)
          {
            bool listenerExists = onVisualsUpdatedListeners.TryGetValue(rebindElement, out var listener);

            if (listenerExists && listener != null)
            {
              rebindElement.onVisualsUpdated.RemoveListener(listener);
            }

            listener = () =>
            {
              if (LocalizationManipulator_Button.localizers.TryGetValue(resetButton, out var localizer))
              {
                localizer.Localize();
              }
            };
            onVisualsUpdatedListeners.AddOrUpdateDictionaryItem(rebindElement, listener);
            rebindElement.onVisualsUpdated.AddListener(listener);

            if (!LocalizationManipulator_Button.localizers.TryGetValue(resetButton, out var localizer))
            {
              localizer = new LocalizationManipulator_Button(settings.LocalizationProvider);
              resetButton.AddManipulator(localizer);
              LocalizationManipulator_Button.localizers.Add(resetButton, localizer);
            }

            localizer.AssignProvider(settings.LocalizationProvider, true);
            localizer.LocalizationKey = rebindElement.ResetButtonText;
            localizer.Localize();
          }
          #endregion

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

