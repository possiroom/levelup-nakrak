using CitrioN.Common;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputBinding;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Setting for rebinding an InputAction.
  /// </summary>
  [MenuOrder(170)]
  [MenuPath("Input/")]
  [System.Serializable]
  //[DisplayName("Rebind")]
  public class Setting_InputActionRebind : Setting_Generic<SerializedInputBinding>
  {
    #region Fields & Properties

    [SerializeField]
    [Tooltip("The InputAction to rebind")]
    protected InputActionBinding inputActionBinding;

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
    [Tooltip("Optional path to cancel an ongoing rebind process.\n\n" +
             "Example: */{Cancel}\n" +
             "Will cancel rebinding when any input specified as Cancel is performed.\n" +
             "This allows that multiple inputs can cancel the rebinding process.")]
    protected List<string> cancelPaths = new List<string>() { "<Keyboard>/escape", "<Gamepad>/buttonEast" };

    protected InputAction InputAction =>
      inputActionBinding != null && inputActionBinding.InputActionReference != null ?
      inputActionBinding.InputActionReference.action : null;

    public int BindingIndex
    {
      get
      {
        var action = InputAction;
        if (action == null) { return -1; }
        var index = action.bindings.IndexOf(i => i.id.ToString() == inputActionBinding.BindingId);
        return index;
      }
    }

    public override string EditorNamePrefix => "[Input]";

    public override string RuntimeName
    {
      get
      {
        var inputReference = inputActionBinding?.InputActionReference;
        if (inputReference != null)
        {
          var action = inputReference.action;
          var bindingId = inputActionBinding.BindingId;
          var bindingIndex = action.bindings.IndexOf(i => i.id.ToString() == bindingId);
          string displayString = "";

          #region TODO Check for removal
          //var binding = inputReference.action.bindings[bindingIndex];
          //var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

          //var displayOptions = /*InputBinding.DisplayStringOptions.DontUseShortDisplayNames | */InputBinding.DisplayStringOptions.IgnoreBindingOverrides
          //  | InputBinding.DisplayStringOptions.DontIncludeInteractions;
          //if (!haveBindingGroups)
          //  displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

          //var displayString = action.GetBindingDisplayString(bindingIndex, displayOptions);

          // OLD
          //var displayStringOptions = InputBinding.DisplayStringOptions.DontUseShortDisplayNames;
          //displayString = action.bindings[bindingIndex].name;
          //displayString = action.GetBindingDisplayString(bindingIndex, out var deviceLayoutName, out var controlPath, displayStringOptions); 
          #endregion

          displayString = action.bindings[bindingIndex].name;

          #region Groups
          var groups = action.bindings[bindingIndex].groups;
          var splitGroups = groups.Split(";");
          var devices = new StringBuilder();
          foreach (var group in splitGroups)
          {
            if (!string.IsNullOrEmpty(group))
            {
              devices.Append(group.SplitCamelCase().Replace("&", " &"));
            }
          }
          var devicesString = devices.ToString();
          #endregion

          var strings = displayString.Split(" ");
          var capitalizedStrings = new StringBuilder();
          if (strings.Length == 1)
          {
            if (!string.IsNullOrEmpty(strings[0]))
            {
              capitalizedStrings.Append(strings[0].ToUpperFirstCharacter());
            }
          }
          else if (strings.Length > 1)
          {
            for (int i = 0; i < strings.Length; i++)
            {
              var s = strings[i];
              if (string.IsNullOrEmpty(s)) { continue; }
              bool isLast = i == strings.Length - 1;
              capitalizedStrings.Append($"{s.ToUpperFirstCharacter()}{(isLast ? "" : " ")}");
            }
          }
          displayString = capitalizedStrings.ToString();

          var actionName = inputReference.action.name;

          bool hasDisplayString = !string.IsNullOrEmpty(displayString);
          bool hasDevicesString = !string.IsNullOrEmpty(devicesString);

          if (!hasDisplayString && !hasDevicesString)
          {
            return actionName;
          }
          else if (hasDisplayString)
          {
            return $"{actionName} ({displayString})";
          }
          else if (!hasDisplayString && hasDevicesString)
          {
            return $"{actionName} ({devicesString})";
          }
        }
        return base.RuntimeName;
      }
    }

    public override string EditorName
    {
      get
      {
        return $"Action Rebind | {GetDisplayName(InputAction, BindingIndex, " &", "/")}";
        //var actionName = InputAction?.name;
        //return $"Action Binding: {(string.IsNullOrEmpty(actionName) ? "" : actionName)} | {BindingName}";
      }
    }

    public string BindingName
    {
      get
      {
        var action = InputAction;
        if (action == null) { return string.Empty; }

        var bindingIndex = BindingIndex;
        if (bindingIndex < 0) { return string.Empty; }

        var bindings = action.bindings;
        if (bindingIndex >= bindings.Count) { return string.Empty; }

        return bindings[bindingIndex].name;
      }
    }

    public string GetDisplayName(InputAction action, int bindingIndex,
                                 string andReplacement = "And",
                                 string forwardSlashReplacement = " - ")
    {
      if (action == null) { return string.Empty; }
      if (bindingIndex < 0) { return string.Empty; }

      var bindings = action.bindings;
      var binding = bindings[bindingIndex];

      #region Name
      var sb = new StringBuilder();
      var name = binding.name.Split(" ");
      int stringsAdded = 0;
      for (int j = 0; j < name.Length; j++)
      {
        var s = name[j];
        if (string.IsNullOrEmpty(s)) { continue; }
        //var subGroup = s.SplitCamelCase();
        if (stringsAdded > 0) { sb.Append(" "); }
        sb.Append($"{s.ToUpperFirstCharacter()}");
        stringsAdded++;
      }
      var nameString = sb.ToString();
      var addNameString = !string.IsNullOrEmpty(nameString);
      #endregion

      #region Group
      var sb1 = new StringBuilder();
      var groups = binding.groups.Split(";");
      if (binding.isComposite)
      {
        var b = bindings[bindingIndex + 1];
        if (b.isPartOfComposite)
        {
          groups = b.groups.Split(";");
        }
      }
      stringsAdded = 0;
      for (int j = 0; j < groups.Length; j++)
      {
        var group = groups[j];
        group = group.Replace("&", andReplacement);
        if (string.IsNullOrEmpty(group)) { continue; }
        var subGroup = group.SplitCamelCase();
        if (stringsAdded > 0) { sb1.Append(" "); }
        sb1.Append($"{subGroup.ToUpperFirstCharacter()}");
        stringsAdded++;
      }
      var groupsString = sb1.ToString();
      bool addGroups = !string.IsNullOrEmpty(groupsString);
      #endregion

      string displayString = string.Empty;

      // Add Name
      if (addNameString) { displayString += $"{nameString}: "; }

      // Add Binding
      var displayOptions = InputBinding.DisplayStringOptions.DontUseShortDisplayNames |
                           InputBinding.DisplayStringOptions.DontIncludeInteractions;
      string bindingDisplayString = string.Empty;

      try
      {
        bindingDisplayString = action.GetBindingDisplayString(bindingIndex, displayOptions);
      }
      catch (System.Exception e)
      {
        ConsoleLogger.Log(e);
#if UNITY_EDITOR
        // We request a script recompilation so any issues with modified input action assets
        // can be resolved.
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation
          (UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
#endif
      }

      displayString += $"{bindingDisplayString}";

      // Add Groups
      if (addGroups) { displayString += $" ({groupsString})"; }

      // Replace forward slash of composite bindings
      displayString = displayString.Replace("/", forwardSlashReplacement);

      return displayString;
    }

    public InputActionBinding InputActionBinding { get => inputActionBinding; protected set => inputActionBinding = value; }

    public List<string> ExcludedPaths { get => excludedPaths; protected set => excludedPaths = value; }

    public List<string> MatchingPaths { get => matchingPaths; protected set => matchingPaths = value; }

    public List<string> CancelPaths { get => cancelPaths; protected set => cancelPaths = value; }

    #endregion

    public override object ApplySettingChange(SettingsCollection settings, params object[] args)
    {
      if (args?.Length > 0)
      {
        var value = args[0];
        bool validType = false;
        SerializedInputBinding serializedInputBinding = null;
        if (value is SerializedInputBinding)
        {
          serializedInputBinding = value as SerializedInputBinding;
          validType = true;
        }
        else if (value is string stringValue)
        {
          serializedInputBinding = new SerializedInputBinding(stringValue);
          validType = true;
        }

        if (validType)
        {
          var newValue = ApplySettingChangeWithValue(settings, serializedInputBinding);
          base.ApplySettingChange(settings, newValue);
          return newValue;
        }
      }

      base.ApplySettingChange(settings, null);
      return null;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, SerializedInputBinding serializedBinding)
    {
      string value = serializedBinding.Binding;
      if (string.IsNullOrEmpty(value)) { return null; }

      var action = InputAction;
      if (action == null) { return null; }

      var bindingIndex = BindingIndex;
      if (bindingIndex < 0) { return null; }

      var bindings = action.bindings;

      if (value == "-1")
      {
        if (action.bindings[bindingIndex].isComposite)
        {
          for (var i = bindingIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
          {
            action.RemoveBindingOverride(i);
          }
        }
        else
        {
          action.RemoveBindingOverride(bindingIndex);
        }

        //if (!action.enabled) { action.Enable(); }

        var bindingPath = action.bindings[bindingIndex].effectivePath;
        return bindingPath;
      }

      var values = value.Split(" && ");
      int offset = 0;
      int valueIndex = 0;
      var length = values.Length > 1 ? values.Length + 1 : values.Length;

      for (int i = 0; i < length; i++)
      {
        var binding = bindings[bindingIndex + offset];
        if (!binding.isComposite)
        {
          var bindingValue = values[valueIndex++];
          if (binding.effectivePath != bindingValue)
          {
            action.ApplyBindingOverride(bindingIndex + offset, bindingValue);
          }
        }
        offset++;
      }

      // Return the original value instead of bindings[bindingIndex].effectivePath
      // TODO Make this actually check the current effectPaths for all the bindings controlled by this setting? composite?
      return value;
    }

    public override object GetDefaultValue(SettingsCollection settings) => new SerializedInputBinding("-1");

    protected string GetBindingDisplayName()
    {
      var action = InputAction;
      if (action == null) { return null; }

      var bindingIndex = BindingIndex;
      if (bindingIndex == -1) { return null; }

      var bindings = action.bindings;
      if (bindings.Count <= bindingIndex) { return null; }

      var actionBinding = bindings[bindingIndex];

      var hasBindingGroups = !string.IsNullOrEmpty(actionBinding.groups);

      var displayOptions = DisplayStringOptions.DontUseShortDisplayNames |
                           DisplayStringOptions.IgnoreBindingOverrides;
      if (!hasBindingGroups) { displayOptions |= DisplayStringOptions.DontOmitDevice; }

      return action.GetBindingDisplayString(bindingIndex, displayOptions);
    }

    protected override void OnApplicationQuit()
    {
      base.OnApplicationQuit();

#if UNITY_EDITOR
      var action = InputAction;
      if (action == null) { return; }

      var bindingIndex = BindingIndex;
      if (bindingIndex < 0) { return; }

      // We remove the binding overrides when we exit playmode so the original input bindings are restored
      // and used when entering play mode again. Only if the settings menu is initialized again the overrides
      // of the settings menu will be applied again. This ensures sessions that don't use the settings menu
      // will use the input bindings of the InputActionAsset.
      UnityEditor.EditorApplication.delayCall += () => RemoveBindingOverrides(action, bindingIndex);
#endif
    }

    protected void RemoveBindingOverrides(InputAction action, int bindingIndex)
    {
      var bindings = action.bindings;

      if (action.bindings[bindingIndex].isComposite)
      {
        for (var i = bindingIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
        {
          action.RemoveBindingOverride(i);
        }
      }
      else
      {
        action.RemoveBindingOverride(bindingIndex);
      }
    }
  }
}