using CitrioN.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace CitrioN.SettingsMenuCreator.Input.UIToolkit
{
  [SkipObfuscation]
#if UNITY_2023_2_OR_NEWER
  [UxmlElement]
#endif
  public partial class InputActionRebindElement : VisualElement
  {
#if !UNITY_2023_2_OR_NEWER
    public new class UxmlFactory : UxmlFactory<InputActionRebindElement, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
      UxmlStringAttributeDescription label =
        new UxmlStringAttributeDescription { name = "label", defaultValue = "Input Action Rebind Element" };
      UxmlBoolAttributeDescription makeResetButtonSquare =
        new UxmlBoolAttributeDescription { name = "make-reset-button-square", defaultValue = false };
      UxmlStringAttributeDescription resetButtonText =
        new UxmlStringAttributeDescription { name = "reset-button-text", defaultValue = "Reset" };
      UxmlStringAttributeDescription rebindInfoText =
        new UxmlStringAttributeDescription { name = "rebind-info-text", defaultValue = "" };
      UxmlStringAttributeDescription excludedPaths =
        new UxmlStringAttributeDescription { name = "excluded-paths", defaultValue = "" };
      UxmlStringAttributeDescription matchingPaths =
        new UxmlStringAttributeDescription { name = "matching-paths", defaultValue = "" };
      UxmlStringAttributeDescription cancelPaths =
        new UxmlStringAttributeDescription { name = "cancel-paths", defaultValue = "<Keyboard>/escape,<Gamepad>/buttonEast" };

      public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
      {
        get { yield break; }
      }

      public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
      {
        base.Init(ve, bag, cc);
        var inputActionBindingElement = ve as InputActionRebindElement;
        inputActionBindingElement.makeResetButtonSquare = makeResetButtonSquare.GetValueFromBag(bag, cc);
        inputActionBindingElement.resetButtonText = resetButtonText.GetValueFromBag(bag, cc);
        inputActionBindingElement.rebindInfoText = rebindInfoText.GetValueFromBag(bag, cc);
        inputActionBindingElement.excludedPaths = excludedPaths.GetValueFromBag(bag, cc).Split(",").ToList();
        inputActionBindingElement.matchingPaths = matchingPaths.GetValueFromBag(bag, cc).Split(",").ToList();
        inputActionBindingElement.cancelPaths = cancelPaths.GetValueFromBag(bag, cc).Split(",").ToList();
        var labelText = label.GetValueFromBag(bag, cc);
        inputActionBindingElement.labelText = labelText;
        UIToolkitUtilities.SetText(inputActionBindingElement.label, labelText);
      }
    }
#endif

    #region Statics
    private const string resourcesSubFolders = "UI Toolkit/USS/";
    private const string styleSheetResourceName = "InputActionRebindElement";

    public const string rootClassName = "input-action-rebind-element";

    // TODO Move
    // Base Field
    public const string baseFieldClassName = "unity-base-field";

    // Label
    public const string unityBaseFieldLabelClassName = "unity-base-field__label";
    public const string inputActionRebindElementLabelClassName = "input-action-rebind-element__label";

    // Input
    public const string unityBaseFieldInputClassName = "unity-base-field__input";
    public const string inputActionRebindElementInputClassName = "input-action-rebind-element__input";

    // Spacer
    public const string spacerClassName = "input-action-rebind-element__spacer";

    // Buttons
    public const string rebindButtonClassName = "input-action-rebind-element__rebind-button";
    public const string rebindButtonHighlightImageClassName = "input-action-rebind-element__rebind-button__highlight-image";

    public const string resetButtonClassName = "input-action-rebind-element__reset-button";
    public const string resetButtonHighlightImageClassName = "input-action-rebind-element__reset-button__highlight-image";
    public const string resetButtonImageClassName = "input-action-rebind-element__reset-button__image";
    #endregion

    #region Fields
    private StyleSheet styleSheet;

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute]
#endif
    [Tooltip("Should the width of the reset button be matching its height as best as possible?" +
             "This can be useful if an image is used on the reset button")]
    public bool makeResetButtonSquare = false;

    [SerializeField]
    public InputActionBinding inputActionBinding;

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute]
#endif
    [Tooltip("The text to display on the 'bindingTextComponent' while waiting for a binding input\n" +
             "If left empty the current binding will be displayed. Useful if you have an overlay " +
             "handling the information display.")]
    protected string rebindInfoText;

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute]
#endif
    [Tooltip("The text to display on the reset button")]
    protected string resetButtonText = "Reset";

    [SerializeField]
    protected Button rebindButton;

    [SerializeField]
    protected Button resetButton;

    [SerializeField]
    protected VisualElement spacer;

    [SerializeField]
    protected VisualElement resetButtonImage;

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute]
#endif
    [Tooltip("A list of InputControlPaths that should be excluded during rebinding. " +
             "Any path in this list will not trigger a rebinding.")]
    protected List<string> excludedPaths = new List<string>();

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute]
#endif
    [Tooltip("A list of InputControlPaths that should be matched during rebinding. " +
             "If any path in this list is matched the binding is processed.")]
    protected List<string> matchingPaths = new List<string>();

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute]
#endif
    [Tooltip("Optional input path to cancel the rebinding process. " +
         "If not set only the escape button will reset the process.\n" +
         "Example usage:\n" +
         "*/{Cancel}\n" +
         "This will cancel the rebinding process if any input with the " +
         "usage of Cancel is performed on any device")]
    protected List<string> cancelPaths = new List<string>() 
                                         { "<Keyboard>/escape", "<Gamepad>/buttonEast" };

    [SerializeField]
    public Label label;

    [SerializeField]
#if UNITY_2023_2_OR_NEWER
    [UxmlAttribute()]
#endif
    public string labelText = "Input Action Rebind Element";

    [SerializeField]
    [HideInInspector]
    protected string displayedValue = string.Empty;

    [Tooltip("Invoked with the new value as the parameter when the value of the selector changes")]
    public UnityEvent<InputActionRebindElement, InputActionBinding, string> onValueChanged =
       new UnityEvent<InputActionRebindElement, InputActionBinding, string>();

    [Tooltip("Invoked when the value is either changed or set with no change notification." +
         "This event can be useful to register callbacks that update visuals.")]
    public UnityEvent onValueDirty = new UnityEvent();

    [Tooltip("Invoked when the visuals have been updated.")]
    public UnityEvent onVisualsUpdated = new UnityEvent();

    private bool rebindingInProgress = false;
    #endregion

    #region Properties
    public InputAction InputAction =>
      inputActionBinding != null && inputActionBinding.InputActionReference != null ?
      inputActionBinding.InputActionReference.action : null;

    public List<string> ExcludedPaths { get => excludedPaths; set => excludedPaths = value; }

    public List<string> MatchingPaths { get => matchingPaths; set => matchingPaths = value; }

    public List<string> CancelPaths { get => cancelPaths; set => cancelPaths = value; }

    public Button ResetButton { get => resetButton; set => resetButton = value; }

    public string ResetButtonText { get => resetButtonText; set => resetButtonText = value; }

    public override bool canGrabFocus => false;
    #endregion

    public InputActionRebindElement()
    {
      focusable = false;
      SetupHierarchy();
      UpdateVisuals();
      RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
      RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
      ScheduleUtility.InvokeDelayedByFrames(UpdateVisuals);
      if (Application.isPlaying)
      {
        InputSystem.onActionChange += OnActionChanged;
      }
    }

    // TODO OnDestroy?

    /// <summary>
    /// <para>Action invoked when any <see cref="InputActionChange"/> occurs.</para>
    /// Used to set the internal value with <see cref="SetValue(string)"/> 
    /// as the new binding path if a rebind occurred.
    /// </summary>
    private void OnActionChanged(object value, InputActionChange change)
    {
      if (change != InputActionChange.BoundControlsChanged) { return; }

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

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
      if (!makeResetButtonSquare) { return; }
      resetButton?.UpdateAspectRatio(matchWidthToHeight: true, aspect: 1);
    }

    /// <summary> Set the internal action binding to the provided one </summary>
    public void SetInputActionBinding(InputActionBinding inputActionBinding)
    {
      this.inputActionBinding = inputActionBinding;
    }

    protected virtual void SetupHierarchy()
    {
      if (styleSheet == null)
      {
        styleSheet = Resources.Load<StyleSheet>(resourcesSubFolders + styleSheetResourceName);
        this.AddStyleSheet(styleSheet);
      }

      AddToClassList(rootClassName);
      AddToClassList(baseFieldClassName);

      #region Label
      if (label == null)
      {
        label = new Label();
        label.text = labelText;
        Add(label);

        label.AddToClassList(unityBaseFieldLabelClassName);
        label.AddToClassList(inputActionRebindElementLabelClassName);
      }
      #endregion

      #region Input
      VisualElement inputContainer = new VisualElement();
      Add(inputContainer);
      inputContainer.AddToClassList(unityBaseFieldInputClassName);
      inputContainer.AddToClassList(inputActionRebindElementInputClassName);

      if (rebindButton == null)
      {
        rebindButton = new Button(OnChangeBindingButtonClicked);
        inputContainer.Add(rebindButton);
        rebindButton.AddToClassList(rebindButtonClassName);

        var rebindButtonHighlightImage = new Image();
        rebindButtonHighlightImage.name = "Selected Overlay";
        rebindButtonHighlightImage.AddToClassList(rebindButtonHighlightImageClassName);
        rebindButton.Add(rebindButtonHighlightImage);
      }

      if (spacer == null)
      {
        spacer = new VisualElement();
        inputContainer.Add(spacer);
        spacer.AddToClassList(spacerClassName);
      }

      if (resetButton == null)
      {
        resetButton = new Button(OnResetButtonClicked);
        inputContainer.Add(resetButton);
        resetButton.AddToClassList(resetButtonClassName);
        UIToolkitUtilities.SetText(resetButton, resetButtonText);

        if (resetButtonImage == null)
        {
          resetButtonImage = new VisualElement();
          resetButton.Add(resetButtonImage);
          resetButtonImage.AddToClassList(resetButtonImageClassName);
        }

        var resetButtonHighlightImage = new Image();
        resetButtonHighlightImage.name = "Selected Overlay";
        resetButtonHighlightImage.AddToClassList(resetButtonHighlightImageClassName);
        resetButton.Add(resetButtonHighlightImage);
      }
      #endregion
    }

    protected virtual void OnChangeBindingButtonClicked()
    {
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
        UIToolkitUtilities.SetText(rebindButton, rebindInfoText);
      }

      // Disable the binding button so it can not be triggered again during the rebinding process.
      // It will be enabled again after the rebinding process ended.
      rebindButton.SetEnabled(false);

      //InputSystem.DisableAllEnabledActions();
      //var actions = InputSystem.ListEnabledActions();

      rebindingInProgress = true;

      // Delay the interactive rebind so the submit input is not accidentally bound to the action
      ScheduleUtility.InvokeDelayedByFrames(() =>
      {
        InputActionRebinder.StartInteractiveRebind(reference, bindingIndex, OnRebindPerformed, excludedPaths, matchingPaths, cancelPaths);
      }, 5);

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
        rebindButton?.SetEnabled(true);
        rebindingInProgress = false;
      }, 1);
      SelectSelf();
    }

    /// <summary>
    /// Prevents unwanted UI navigation during the rebinding process.
    /// </summary>
    private void OnNavigationMove(NavigationMoveEvent evt)
    {
      switch (evt.direction)
      {
        case NavigationMoveEvent.Direction.None:
          if (rebindingInProgress) { SelectSelf(); }
          break;
        case NavigationMoveEvent.Direction.Left:
          if (rebindingInProgress) { SelectSelf(); }
          break;
        case NavigationMoveEvent.Direction.Up:
          if (rebindingInProgress) { SelectSelf(); }
          break;
        case NavigationMoveEvent.Direction.Right:
          if (rebindingInProgress) { SelectSelf(); }
          break;
        case NavigationMoveEvent.Direction.Down:
          if (rebindingInProgress) { SelectSelf(); }
          break;
#if UNITY_2022_2_OR_NEWER
        case NavigationMoveEvent.Direction.Next:
          if (rebindingInProgress) { SelectSelf(); }
          break;
        case NavigationMoveEvent.Direction.Previous:
          if (rebindingInProgress) { SelectSelf(); }
          break;
#endif
      }
    }

    private void SelectSelf()
    {
      ScheduleUtility.InvokeDelayedByFrames(() => { rebindButton?.Focus(); }, 1);
    }

    private void UpdateVisuals()
    {
      UIToolkitUtilities.SetText(rebindButton, GetBindingDisplayString());
      UIToolkitUtilities.SetText(resetButton, resetButtonText);
      onVisualsUpdated?.Invoke();
    }

    private void UpdateButtonVisuals()
    {
      // TODO show/hide the reset button with USS?
    }

    private void OnResetButtonClicked() => ResetBinding();

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

    public void SetValue(string value)
    {
      UpdateVisuals();
      if (displayedValue != value)
      {
        onValueChanged?.Invoke(this, inputActionBinding, value);
        displayedValue = value;
      }
      onValueDirty?.Invoke();
    }

    public void SetValueWithoutNotify(string value)
    {
      UpdateVisuals();
      if (displayedValue != value)
      {
        displayedValue = value;
      }
      onValueDirty?.Invoke();
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

    protected virtual string GetBindingDisplayString()
    {
      var action = InputAction;
      if (action == null) { return string.Empty; }

      var bindingIndex = GetBindingIndex();
      if (bindingIndex < 0) { return string.Empty; }

      return action.GetBindingDisplayString(bindingIndex);
    }
  }
}