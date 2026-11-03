using CitrioN.Common;
using CitrioN.Common.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CitrioN.SettingsMenuCreator.Input.Editor
{
  [CustomPropertyDrawer(typeof(InputActionBinding))]
  public class InputActionBindingDrawer : PropertyDrawerFromTemplate
  {
    protected List<string> bindingIds = new List<string>();

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
      var root = base.CreatePropertyGUI(property);

      SetupVisualElements(property, root);

      return root;
    }

    protected void SetupVisualElements(SerializedProperty property, VisualElement root)
    {
      EditorUtilities.GetPropertyValue<InputActionBinding>(property, out var inputActionBinding);

      var inputActionReference = inputActionBinding.InputActionReference;
      var bindingId = inputActionBinding.BindingId;

      var inputActionReferenceProperty = property.FindPropertyRelative("inputActionReference");
      var inputActionReferencePropertyField = UIToolkitEditorExtensions.SetupPropertyField(inputActionReferenceProperty, root, "inputActionReference");

      //var bindingIdProperty = property.FindPropertyRelative("bindingId");
      //var bindingIdPropertyField = UIToolkitEditorExtensions.SetupPropertyField(bindingIdProperty, root, "bindingId");

      bool hasReference = inputActionReferenceProperty.objectReferenceValue;

      var popupField = CreateBindingPopup(root);
      popupField.Show(hasReference);
      popupField.RegisterValueChangedCallback((evt) => OnPopupValueChanged(evt, property.serializedObject, popupField, inputActionBinding));
      UpdateBindingPopup(popupField, inputActionBinding);

      inputActionReferencePropertyField.RegisterValueChangeCallback((evt) => OnInputActionReferencePropertyChanged(evt, property.serializedObject, inputActionBinding, popupField));
    }

    private void OnPopupValueChanged(ChangeEvent<string> evt, SerializedObject so, PopupField<string> popupField, InputActionBinding inputActionBinding)
    {
      var index = popupField.index;

      if (bindingIds.Count > index)
      {
        if (index >= 0)
        {
          inputActionBinding.BindingId = bindingIds[index];
        }
        else
        {
          inputActionBinding.BindingId = string.Empty;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(so.targetObject);
      }
    }

    private void OnInputActionReferencePropertyChanged(SerializedPropertyChangeEvent evt, SerializedObject so, InputActionBinding inputActionBinding, PopupField<string> popupField)
    {
      if (inputActionBinding == null) { return; }
      var reference = evt.changedProperty.objectReferenceValue;
      popupField.Show(reference != null);
      var actualReference = inputActionBinding.InputActionReference;

      var currentBindingId = inputActionBinding.BindingId;
      var action = actualReference != null ? actualReference.action : null;
      var actionName = action?.name;
      var bindings = action?.bindings.Where(b => b.id.ToString() == currentBindingId).ToList();
      if (bindings?.Count > 0)
      {
        var bindingActionName = bindings[0].action;
      }
      else
      {
        inputActionBinding.AssignDefaultBindingId(true);
        if (so != null)
        {
          so.ApplyModifiedProperties();
          EditorUtility.SetDirty(so.targetObject);
        }
      }

      UpdateBindingPopup(popupField, inputActionBinding);
    }

    public PopupField<string> CreateBindingPopup(VisualElement root)
    {
      var popupField = root.Q<PopupField<string>>();

      if (popupField == null)
      {
        popupField = new PopupField<string>();
        root.Add(popupField);
      }

      popupField.labelElement.style.paddingRight = 15;
      popupField.labelElement.style.marginRight = -13;
      popupField.labelElement.style.overflow = Overflow.Hidden;

      popupField.label = "Binding";

      return popupField;
    }

    public void UpdateBindingPopup(PopupField<string> popupField, InputActionBinding inputActionBinding)
    {
      if (popupField == null || inputActionBinding == null) { return; }
      if (inputActionBinding.InputActionReference == null)
      {
        popupField.choices = new List<string>();
        popupField.value = string.Empty;
        return;
      }

      InputAction action = inputActionBinding.InputActionReference.action;
      var bindingId = inputActionBinding.BindingId;

      if (action != null)
      {
        var bindingIndex = action.bindings.IndexOf(i => i.id.ToString() == bindingId);
        popupField.choices = GetBindingNames(action, bindingId, out var selectedIndex, out var selectedValue);
        //if (bindingIndex >= 0)
        {
          popupField.index = bindingIndex;
        }

        //popupField.value = selectedValue;
      }
    }

    private List<string> GetBindingNames(InputAction action, string selectedBindingId, out int selectedIndex, out string selectedValue)
    {
      selectedIndex = -1;
      selectedValue = string.Empty;

      bindingIds.Clear();
      List<string> bindingNames = new List<string>();

      var bindings = action.bindings;
      var bindingsCount = bindings.Count;

      for (var i = 0; i < bindingsCount; i++)
      {
        var binding = bindings[i];

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
          var b = bindings[i + 1];
          if (b.isPartOfComposite)
          {
            groups = b.groups.Split(";");
          }
        }
        stringsAdded = 0;
        for (int j = 0; j < groups.Length; j++)
        {
          var group = groups[j];
          group = group.Replace("&", "And");
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
        displayString += $"{action.GetBindingDisplayString(i, displayOptions)}";

        // Add Groups
        if (addGroups) { displayString += $" ({groupsString})"; }

        // Replace forward slash of composite bindings
        displayString = displayString.Replace("/", " - ");

        bindingNames.Add(displayString);
        var bindingID = binding.id.ToString();
        bindingIds.Add(bindingID);

        if (selectedBindingId == bindingID)
        {
          selectedIndex = i;
          selectedValue = displayString;
        }

      }
      return bindingNames;
    }

    //private void UpdateBindingId(InputActionBinding inputActionBinding)
    //{
    //  return;
    //  var reference = inputActionBinding.InputActionReference;
    //  if (reference == null)
    //  {
    //    inputActionBinding.BindingId = string.Empty;
    //    return;
    //  }

    //  var bindings = reference.action.bindings;
    //  var type = bindings.GetType();
    //  var startIndexField = type?.GetPrivateField("m_StartIndex");
    //  var startIndexValue = startIndexField?.GetValue(bindings);
    //  if (startIndexValue is int startIndex)
    //  {
    //    var arrayField = type?.GetPrivateField("m_Array");
    //    var arrayValue = arrayField?.GetValue(bindings);
    //    if (arrayValue is InputBinding[] bindingsArray)
    //    {
    //      if (bindingsArray.Length > startIndex)
    //      {
    //        var binding = bindingsArray[startIndex];
    //        inputActionBinding.BindingId = binding.id.ToString();
    //      }
    //    }
    //  }
    //}
  }
}