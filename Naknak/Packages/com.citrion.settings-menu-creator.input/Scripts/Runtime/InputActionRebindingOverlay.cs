using CitrioN.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Handles an overlay for <see cref="InputAction"/> rebinding through the <see cref="InputActionRebinder"/> class.
  /// </summary>
  [HeaderInfo("\n\nA UI overlay to display information about an ongoing input action rebinding process. " +
              "It can show the user what is currently being rebound or how to cancel the process.")]
  [HelpURL("https://docs.google.com/document/d/12zlt3ItOtv34QoLhjmKd49UKVWitEBLYMgahhmbodf0/edit#bookmark=id.8lll0nutt6qa")]
  public class InputActionRebindingOverlay : InputActionRebindingListener
  {
    [SerializeField]
    [Tooltip("The overlay's root object. " +
             "Will be enabled/disabled based on the InputActionRebinder events.")]
    protected GameObject overlayRoot;

    [SerializeField]
    [Tooltip("The text component to display rebinding related information.")]
    protected TextMeshProUGUI overlayTextComponent;

    [SerializeField]
    [Tooltip("The placeholder text for the overlay text.\n" +
             "- Use %PART-NAME% as a placeholder for the part name\n" +
             "- Use %CONTROL-TYPE% as a placeholder for the control type\n" +
             "- Use %STOP-BINDING% as a placeholder for the cancel binding\n" +
             "- Use %STOP-BINDINGS% as a placeholder for all the cancel bindings\n" +
             "Example: Binding %PART-NAME% ...")]
    [TextArea(1, 99)]
    protected string overlayTextTemplate = $"Binding {PART_NAME_PLACEHOLDER}\n\n" +
                                           $"Waiting for {CONTROL_TYPE_PLACEHOLDER} input\n\n" +
                                           $"Press {STOP_BINDING_PLACEHOLDER} to stop";

    [SerializeField]
    [Tooltip("Options to customize how the cancel binding should be displayed.")]
    protected InputControlPath.HumanReadableStringOptions cancelPathDisplayOptions =
              InputControlPath.HumanReadableStringOptions.UseShortNames |
              InputControlPath.HumanReadableStringOptions.OmitDevice;


    [SerializeField]
    [Tooltip("The text to display between bindings that cancel the rebinding process. " +
             "Used in combination with the %STOP-BINDINGS% placeholder text.\n" +
             "Default: ' or '")]
    protected string bindingsSeparator = " or ";

    /// <summary> Placeholder string to be replaced with the input action part name</summary>
    protected const string PART_NAME_PLACEHOLDER = "%PART-NAME%";
    /// <summary> Placeholder string to be replaced with the input action control type</summary>
    protected const string CONTROL_TYPE_PLACEHOLDER = "%CONTROL-TYPE%";
    /// <summary> Placeholder string to be replaced with the binding to stop the rebinding process</summary>
    protected const string STOP_BINDING_PLACEHOLDER = "%STOP-BINDING%";
    /// <summary> Placeholder string to be replaced with the list of bindings to stop the rebinding process</summary>
    protected const string STOP_BINDINGS_PLACEHOLDER = "%STOP-BINDINGS%";

    protected virtual void Awake()
    {
      CacheReferences();
      if (overlayRoot != null)
      {
        overlayRoot.SetActive(false);
      }
    }

    private void CacheReferences()
    {
      if (overlayRoot == null)
      {
        // If not specified use this object as the root for the overlay
        overlayRoot = gameObject;
      }

      if (overlayTextComponent == null)
      {
        // If not specified use the first TMP text component as the overlay text component
        overlayTextComponent = overlayRoot.GetComponentInChildren<TextMeshProUGUI>();
      }
    }

    protected override void OnRebindingStarted()
    {
      base.OnRebindingStarted();

      if (overlayRoot != null)
      {
        overlayRoot.SetActive(true);
      }

      var operation = InputActionRebinder.RebindingOperation;
      var bindingIndex = InputActionRebinder.BindingIndex;
      string partName = string.Empty;
      string controlType = string.Empty;

      if (operation != null)
      {
        var action = operation.action;
        partName = action.bindings[bindingIndex].name;

        controlType = operation.expectedControlType;
      }

      #region Old
      //var displayStringOptions = InputBinding.DisplayStringOptions.DontUseShortDisplayNames;
      //var displayString = action.GetBindingDisplayString(bindingIndex, out var deviceLayoutName, out var controlPath, displayStringOptions);

      //var bindingText = partNameTextTemplate.Replace(PART_NAME_PLACEHOLDER, !string.IsNullOrEmpty(partName) ? partName.ToUpperFirstCharacter().Bold() : string.Empty);
      //string overlayText = controlTypeTextTemplate.Replace(CONTROL_TYPE_PLACEHOLDER,
      //                     !string.IsNullOrEmpty(controlType) ? controlType.Bold() : string.Empty);

      //var sb = new StringBuilder();
      //sb.AppendLine(bindingText);
      //sb.AppendLine();
      //sb.AppendLine(overlayText);
      #endregion

      string text = overlayTextTemplate.Replace(PART_NAME_PLACEHOLDER, !string.IsNullOrEmpty(partName) ?
                                                                       partName.ToUpperFirstCharacter().Bold() : string.Empty);
      text = text.Replace(CONTROL_TYPE_PLACEHOLDER, !string.IsNullOrEmpty(controlType) ?
                                                    controlType.Bold() : string.Empty);

      string cancelBinding = string.Empty;
      string cancelBindings = string.Empty;
      bool isFirstBinding = true;

      for (int i = 0; i < InputActionRebinder.cancelBindings.Count; i++)
      {
        var binding = InputActionRebinder.cancelBindings[i];

        if (!string.IsNullOrEmpty(binding))
        {
          string currentBinding = InputControlPath.ToHumanReadableString(binding, cancelPathDisplayOptions);
          if (!string.IsNullOrEmpty(cancelBindings))
          {
            cancelBindings += " or ";
          }
          cancelBindings += currentBinding;
          
          if (isFirstBinding)
          {
            cancelBinding = currentBinding;
            isFirstBinding = false;
          }
        }
      }

      text = text.Replace(STOP_BINDING_PLACEHOLDER, !string.IsNullOrEmpty(cancelBinding) ? cancelBinding.Bold() : string.Empty);
      text = text.Replace(STOP_BINDINGS_PLACEHOLDER, !string.IsNullOrEmpty(cancelBindings) ? cancelBindings.Bold() : string.Empty);

      UpdateOverlayText(text);
    }

    protected override void OnRebindingEnded()
    {
      base.OnRebindingEnded();

      if (overlayRoot != null)
      {
        overlayRoot.SetActive(false);
      }
      UpdateOverlayText(string.Empty);
    }

    protected override void OnRebindingCanceled()
    {
      base.OnRebindingCanceled();

      if (overlayRoot != null)
      {
        overlayRoot.SetActive(false);
      }
      UpdateOverlayText(string.Empty);
    }

    protected virtual void UpdateOverlayText(string overlayText)
    {
      if (overlayTextComponent != null)
      {
        overlayTextComponent.SetText(overlayText);
      }
    }
  }
}