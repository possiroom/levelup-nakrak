using CitrioN.Common;
using UnityEngine;
using UnityEngine.Events;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Base listener for input rebinding started/ended/canceled events.
  /// </summary>
  [AddTooltips]
  [HeaderInfo("Listens to events of the 'Input Action Rebinder' script to react to different aspects " +
              "of the rebinding process such as the starting and ending.")]
  public class InputActionRebindingListener : MonoBehaviour
  {
    [SerializeField]
    [Tooltip("Action(s) to invoke when the rebinding process was started.")]
    protected UnityEvent onRebindStarted = new UnityEvent();

    [SerializeField]
    [Tooltip("Action(s) to invoke when the rebinding process has ended.")]
    protected UnityEvent onRebindEnded = new UnityEvent();

    [SerializeField]
    [Tooltip("Action(s) to invoke when the rebinding process was canceled.")]
    protected UnityEvent onRebindCanceled = new UnityEvent();

    protected virtual void OnEnable()
    {
      InputActionRebinder.OnRebindStarted += OnRebindingStarted;
      InputActionRebinder.OnRebindEnded += OnRebindingEnded;
      InputActionRebinder.OnRebindCanceled += OnRebindingCanceled;
    }

    protected virtual void OnDisable()
    {
      InputActionRebinder.OnRebindStarted -= OnRebindingStarted;
      InputActionRebinder.OnRebindEnded -= OnRebindingEnded;
      InputActionRebinder.OnRebindCanceled -= OnRebindingCanceled;
    }

    protected virtual void OnRebindingStarted()
    {
      onRebindStarted?.Invoke();
      ConsoleLogger.Log("Rebinding Started", Common.LogType.Debug);
    }

    protected virtual void OnRebindingEnded()
    {
      onRebindEnded?.Invoke();
      ConsoleLogger.Log("Rebinding Ended", Common.LogType.Debug);
    }

    protected virtual void OnRebindingCanceled()
    {
      onRebindCanceled?.Invoke();
      ConsoleLogger.Log("Rebinding Canceled", Common.LogType.Debug);
    }
  }
}