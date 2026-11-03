using CitrioN.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Helper behaviour to invoke functionality when an <see cref="InputAction"/> is performed.
  /// </summary>
  [AddTooltips]
  [HeaderInfo("Reacts to a specified 'Input Action' being performed.")]
  public class InputActionPerformedListener : MonoBehaviour
  {
    [SerializeField]
    [Tooltip("The input action to listen to being performed.")]
    protected InputActionReference actionReference;

    [SerializeField]
    [Tooltip("Action(s) to invoke when the matching input action was performed.")]
    protected UnityEvent onActionPerformed;

    protected virtual void OnEnable() => AddCallback();

    protected virtual void OnDisable() => RemoveCallback();

    private void AddCallback()
    {
      RemoveCallback();
      if (actionReference != null && actionReference.action != null)
      {
        actionReference.action.Enable();
        actionReference.action.performed += OnActionPerformed;
      }
    }

    private void RemoveCallback()
    {
      if (actionReference != null && actionReference.action != null)
      {
        actionReference.action.performed -= OnActionPerformed;
      }
    }

    protected virtual void OnActionPerformed(InputAction.CallbackContext context)
    {
      onActionPerformed?.Invoke();
      ConsoleLogger.Log($"{context.action.name} performed: {actionReference.action}", Common.LogType.Debug);
    }
  }
}