using CitrioN.Common;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// UI Element handling the rebinding of an <see cref="InputAction"/>.
  /// </summary>
  public class InputActionRebindElement : MonoBehaviour
  {
    #region Fields & Properties
    [SerializeField]
    [Tooltip("The button to initiate the rebinding process")]
    protected Button changeBindingButton;

    [SerializeField]
    [Tooltip("The text component used to display the current binding or rebinding information text")]
    protected TextMeshProUGUI bindingTextComponent;

    [SerializeField]
    [Tooltip("The text to display on the 'bindingTextComponent' while waiting for a binding input\n" +
             "If left empty the current binding will be displayed. Useful if you have an overlay " +
             "handling the information display.")]
    protected string rebindInfoText;

    [SerializeField]
    [Tooltip("The button to reset the binding")]
    protected Button resetToDefaultBindingButton;

    [SerializeField]
    [Tooltip("The action to rebind")]
    protected InputActionBinding inputActionBinding;

    [SerializeField]
    [Tooltip("Whether the buton navigation should be updated")]
    protected bool updateButtonNavigation = true;

    [SerializeField]
    [Tooltip("Whether the reset buton is on the right or left side of the rebinding button." +
             "Only relevant if the navigation should be updated.")]
    protected bool resetButtonOnRightSide = true;

    [SerializeField]
    [Tooltip("A list of InputControlPaths that should be excluded during rebinding. " +
             "Any path in this list will not trigger a rebinding.")]
    protected List<string> excludedPaths = new List<string>();

    // TODO Enable once working
    [SerializeField]
    [Tooltip("A list of InputControlPaths that should be matched during rebinding. " +
             "If any path in this list is matched the binding is processed.")]
    protected List<string> matchingPaths = new List<string>();

    [SerializeField]
    [Tooltip("Optional input path to cancel the rebinding process. " +
         "If not set only the escape button will reset the process.\n" +
         "Example usage:\n" +
         "*/{Cancel}\n" +
         "This will cancel the rebinding process if any input with the " +
         "usage of Cancel is performed on any device")]
    protected List<string> cancelPaths = new List<string>();

    [SerializeField]
    [HideInInspector]
    protected string displayedValue = string.Empty;

    [Tooltip("Invoked with the new value as the parameter when the value of the selector changes")]
    public UnityEvent<InputActionRebindElement, InputActionBinding, string> onValueChanged =
       new UnityEvent<InputActionRebindElement, InputActionBinding, string>();

    private InputActionReference lastInputActionReference;

    public InputAction InputAction =>
      inputActionBinding != null && inputActionBinding.InputActionReference != null ?
      inputActionBinding.InputActionReference.action : null;

    public List<string> ExcludedPaths { get => excludedPaths; set => excludedPaths = value; }

    public List<string> MatchingPaths { get => matchingPaths; set => matchingPaths = value; }

    public List<string> CancelPaths { get => cancelPaths; set => cancelPaths = value; }

    #endregion

    protected virtual void OnValidate()
    {
      // Check if the reference changed
      if (lastInputActionReference != inputActionBinding?.InputActionReference)
      {
        lastInputActionReference = inputActionBinding?.InputActionReference;
        UpdateVisuals();
      }
    }

    protected virtual void Start()
    {
      CacheButtonReferences();
      CacheBindingDisplayReference();

      if (Application.isPlaying)
      {
        RegisterButtonCallbacks();
        UpdateVisuals();
        InputSystem.onActionChange += OnActionChanged;
      }
    }

    protected virtual void OnDestroy()
    {
      UnregisterButtonCallbacks();
      InputSystem.onActionChange -= OnActionChanged;
    }

    private void RegisterButtonCallbacks()
    {
      if (changeBindingButton != null)
      {
        changeBindingButton.onClick.RemoveListener(OnChangeBindingButtonClicked);
        changeBindingButton.onClick.AddListener(OnChangeBindingButtonClicked);
      }

      if (resetToDefaultBindingButton != null)
      {
        resetToDefaultBindingButton.onClick.RemoveListener(OnResetBindingButtonClicked);
        resetToDefaultBindingButton.onClick.AddListener(OnResetBindingButtonClicked);
      }
    }

    private void UnregisterButtonCallbacks()
    {
      if (changeBindingButton != null)
      {
        changeBindingButton.onClick.RemoveListener(OnChangeBindingButtonClicked);
      }

      if (resetToDefaultBindingButton != null)
      {
        resetToDefaultBindingButton.onClick.RemoveListener(OnResetBindingButtonClicked);
      }
    }

    protected virtual void CacheBindingDisplayReference()
    {
      if (bindingTextComponent == null && changeBindingButton)
      {
        bindingTextComponent = changeBindingButton.GetComponentInChildren<TextMeshProUGUI>(includeInactive: false);
      }
    }

    protected virtual void CacheButtonReferences()
    {
      var needsChangeBindingButton = changeBindingButton == null;
      var needsResetBindingButton = resetToDefaultBindingButton == null;

      if (needsChangeBindingButton || needsResetBindingButton)
      {
        var buttons = GetComponentsInChildren<Button>(includeInactive: false);
        if (buttons != null)
        {
          for (int i = 0; i < buttons.Length; i++)
          {
            var button = buttons[i];
            if (button == null) { continue; }
            if (needsChangeBindingButton)
            {
              if (button != resetToDefaultBindingButton)
              {
                changeBindingButton = button;
                needsChangeBindingButton = false;
              }
            }

            if (needsResetBindingButton)
            {
              if (button != changeBindingButton)
              {
                resetToDefaultBindingButton = button;
                needsResetBindingButton = false;
              }
            }

            if (!needsChangeBindingButton && !needsResetBindingButton) { break; }
          }
        }
      }

      UpdateButtonNavigation();
    }

    protected virtual void UpdateButtonNavigation()
    {
      if (!updateButtonNavigation || changeBindingButton == null ||
          resetToDefaultBindingButton == null) { return; }

      var changeBindingButtonNavigation = changeBindingButton.navigation;
      var resetToDefaultButtonNavigation = resetToDefaultBindingButton.navigation;
      changeBindingButtonNavigation.mode = Navigation.Mode.Explicit;
      resetToDefaultButtonNavigation.mode = Navigation.Mode.Explicit;

      if (resetButtonOnRightSide)
      {
        changeBindingButtonNavigation.selectOnRight = resetToDefaultBindingButton;
        resetToDefaultButtonNavigation.selectOnLeft = changeBindingButton;
      }
      else
      {
        changeBindingButtonNavigation.selectOnLeft = resetToDefaultBindingButton;
        resetToDefaultButtonNavigation.selectOnRight = changeBindingButton;
      }

      resetToDefaultButtonNavigation.selectOnUp = changeBindingButtonNavigation.selectOnUp;
      resetToDefaultButtonNavigation.selectOnDown = changeBindingButtonNavigation.selectOnDown;

      changeBindingButton.navigation = changeBindingButtonNavigation;
      resetToDefaultBindingButton.navigation = resetToDefaultButtonNavigation;
    }

    /// <summary>
    /// Set the internal action binding to the provided one
    /// </summary>
    public void SetInputActionBinding(InputActionBinding inputActionBinding)
    {
      this.inputActionBinding = inputActionBinding;
      //OnValidate();
    }

    /// <summary>
    /// <para>Action invoked when any <see cref="InputActionChange"/> occurs.</para>
    /// Used to set the internal value with <see cref="SetValue(string)"/> 
    /// as the new binding path if a rebind occurred.
    /// </summary>
    private void OnActionChanged(object value, InputActionChange change)
    {
      if (change != InputActionChange.BoundControlsChanged) { return; }

      ConsoleLogger.Log($"{GetType().Name} InputAction controls change detected", Common.LogType.Debug);

      var actionAsset = value as InputActionAsset;
      if (actionAsset == null) { return; }

      var action = InputAction;
      if (action == null) { return; }

      if (!actionAsset.Contains(action)) { return; }

      var bindingId = inputActionBinding.BindingId;
      var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == bindingId);
      var binding = action.bindings[bindingIndex];
      var bindingPath = binding.effectivePath;

      SetValue(bindingPath);
    }

    protected virtual void OnChangeBindingButtonClicked()
    {
      // TODO Do we need to call Select?
      //Select();

      StartInteractiveRebind();
    }

    /// <summary>
    /// <para>Starts the interactive rebinding process if possible.</para>
    /// Uses the <see cref="InputActionRebinder.StartInteractiveRebind(InputActionReference, int, System.Action{string}, string)"/>
    /// method to handle the rebinding process.
    /// </summary>
    protected virtual void StartInteractiveRebind()
    {
      if (inputActionBinding == null) { return; }

      var reference = inputActionBinding.InputActionReference;
      if (reference == null) { return; }

      var bindingIndex = GetBindingIndex();
      if (bindingIndex < 0) { return; }

      // Update the information text if applicable
      if (!string.IsNullOrEmpty(rebindInfoText))
      {
        bindingTextComponent.SetText(rebindInfoText);
      }

      // Disable the binding button so it can not be triggered again during the rebinding process.
      // It will be enabled again after the rebinding process ended.
      changeBindingButton.enabled = false;

      // Delay the interactive rebind so the submit input is not accidentally bound to the action
      ScheduleUtility.InvokeDelayedByFrames(() =>
      {
        InputActionRebinder.StartInteractiveRebind(reference, bindingIndex, OnRebindPerformed, excludedPaths, matchingPaths, cancelPaths);
      }, 5);



    }

    protected virtual void OnResetBindingButtonClicked()
    {
      ResetBinding();
    }

    /// <summary>
    /// Resets the binding for the referenced <see cref="InputAction"/>.
    /// </summary>
    private void ResetBinding()
    {
      // TODO For the settings menu creator this should trigger a settings update by forwarding the information via event!

      if (inputActionBinding == null) { return; }

      var action = InputAction;
      if (action == null) { return; }

      var bindingIndex = GetBindingIndex();
      if (bindingIndex < 0) { return; }

      var binding = action.bindings[bindingIndex];

      if (binding.isComposite)
      {
        var bindings = action.bindings;

        // Remove the binding for all composite bindings
        for (var i = bindingIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
        {
          action.RemoveBindingOverride(i);
        }
      }
      else
      {
        action.RemoveBindingOverride(bindingIndex);
      }

      var bindingPath = action.bindings[bindingIndex].effectivePath;
      SetValue(bindingPath);
    }

    /// <summary>
    /// Invoked when a rebind was performed or canceled
    /// </summary>
    protected virtual void OnRebindPerformed(string value)
    {
      if (!string.IsNullOrEmpty(value))
      {
        SetValue(value);
      }

      // Enable the rebinding button next frame so it will not be triggered again in
      // case the submit input was used to rebind to the current action.
      ScheduleUtility.InvokeDelayedByFrames(() =>
      {
        if (changeBindingButton != null)
        {
          changeBindingButton.enabled = true;
          changeBindingButton.Select();
        }
      });
    }

    /// <summary>
    /// Finds the binding index for the referenced <see cref="InputAction"/>.
    /// </summary>
    private int GetBindingIndex()
    {
      var action = InputAction;
      if (action == null) { return -1; }

      var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == inputActionBinding.BindingId);
      if (bindingIndex >= 0) { return bindingIndex; }

      return -1;
    }

    public virtual void UpdateVisuals()
    {
      if (bindingTextComponent != null)
      {
        bindingTextComponent.SetText(GetBindingDisplayString());
      }
    }

    /// <summary>
    /// Fetches the binding display string for the referenced InputAction.
    /// </summary>
    protected virtual string GetBindingDisplayString()
    {
      var action = InputAction;
      if (action == null) { return string.Empty; }

      var bindingIndex = GetBindingIndex();
      if (bindingIndex < 0) { return string.Empty; }

      return action.GetBindingDisplayString(bindingIndex);
    }

    public void SetValue(string value)
    {
      UpdateVisuals();
      if (displayedValue != value)
      {
        onValueChanged?.Invoke(this, inputActionBinding, value);
        displayedValue = value;
      }
    }

    public void SetValueWithoutNotify(string value)
    {
      UpdateVisuals();
    }

    public string GetValue()
    {
      var action = InputAction;
      if (action == null) { return string.Empty; }

      var bindingIndex = GetBindingIndex();
      if (bindingIndex < 0) { return string.Empty; }
      #region TODO Check for removal
      //var bindingId = inputActionBinding.BindingId;
      //if (string.IsNullOrEmpty(bindingId)) { return string.Empty; }

      //var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == bindingId); 
      #endregion
      var binding = action.bindings[bindingIndex];
      if (binding == null) { return string.Empty; }

      return binding.effectivePath;
    }
  }
}