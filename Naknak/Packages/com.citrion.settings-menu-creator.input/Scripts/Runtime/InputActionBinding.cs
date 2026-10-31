using CitrioN.Common;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Stores an <see cref="InputActionReference"/> and a binding ID to determine
  /// the binding for the referenced <see cref="InputAction"/>.
  /// </summary>
  [System.Serializable]
  public class InputActionBinding
  {
    public event Action OnInputActionReferenceChanged;

    [SerializeField]
    private InputActionReference inputActionReference;

    [SerializeField]
    protected string bindingId = string.Empty;

    public InputActionReference InputActionReference
    {
      get => inputActionReference;
      set
      {
        var resetId = value != InputActionReference;
        inputActionReference = value;

        if (resetId)
        {
          AssignDefaultBindingId(true);
          OnInputActionReferenceChanged?.Invoke();
        }
      }
    }

    public string BindingId
    {
      get
      {
        AssignDefaultBindingId();
        return bindingId;
      }
      set
      {
        bindingId = value;
      }
    }

    public InputActionBinding() { }

    public InputActionBinding(InputActionReference inputActionReference, string bindingId)
    {
      InputActionReference = inputActionReference;
      BindingId = bindingId;
    }

    public void AssignDefaultBindingId(bool overrideExisting = false)
    {
      if (overrideExisting) { bindingId = string.Empty; }

      if (string.IsNullOrEmpty(bindingId))
      {
        bindingId = GetDefaultBindingId();
      }
      else if (InputActionReference == null)
      {
        bindingId = string.Empty;
      }
    }

    /// <summary>
    /// Determines the default binding id by getting the first binding id for the action.
    /// </summary>
    private string GetDefaultBindingId()
    {
      if (InputActionReference == null || InputActionReference.action == null)
      {
        return string.Empty;
      }
      var bindings = InputActionReference.action.bindings;
      var type = bindings.GetType();
      var startIndexField = type?.GetPrivateField("m_StartIndex");
      var startIndexValue = startIndexField?.GetValue(bindings);
      if (startIndexValue is int startIndex)
      {
        var arrayField = type.GetPrivateField("m_Array");
        var arrayValue = arrayField?.GetValue(bindings);
        if (arrayValue is InputBinding[] bindingsArray)
        {
          if (bindingsArray.Length > startIndex)
          {
            var binding = bindingsArray[startIndex];
            return binding.id.ToString();
          }
        }
      }
      return string.Empty;
    }
  }
}