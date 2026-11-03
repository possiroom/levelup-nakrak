using CitrioN.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace CitrioN.SettingsMenuCreator.Input
{
  /// <summary>
  /// Manages input rebinding for an <see cref="InputAction"/>.
  /// </summary>
  public static class InputActionRebinder
  {
    #region Fields & Properties
    private static InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private static int bindingIndex = -1;

    public static event Action OnRebindStarted;

    public static event Action OnRebindEnded;

    public static event Action OnRebindCanceled;

    public static InputActionRebindingExtensions.RebindingOperation RebindingOperation { get { return rebindingOperation; } }

    public static Action<InputEventPtr, InputDevice> OnEventDelegate;

    public static List<string> cancelBindings = new List<string>();

    public static int BindingIndex { get { return bindingIndex; } private set { bindingIndex = value; } }
    #endregion

    /// <summary>
    /// Static initialization method to reset the events.
    /// Required for 'Enter Play Mode Settings' to work propertly.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
      OnRebindStarted = null;
      OnRebindEnded = null;
      OnRebindCanceled = null;
      cancelBindings = new List<string>();
      GlobalEventHandler.RemoveEventListener("OnApplicationQuit", OnApplicationQuit);
      GlobalEventHandler.AddEventListener("OnApplicationQuit", OnApplicationQuit);
    }

    private static void OnApplicationQuit()
    {
      if (OnEventDelegate != null)
      {
        InputSystem.onEvent -= OnEventDelegate;
      }
      GlobalEventHandler.RemoveEventListener("OnApplicationQuit", OnApplicationQuit);
    }

    public static void StartInteractiveRebind(InputActionReference inputActionReference, int bindingIndex, Action<string> onRebindCallback,
                                              List<string> excludedPaths = null, List<string> matchingPaths = null, List<string> cancelPaths = null)
    {
      if (inputActionReference == null || bindingIndex < 0)
      {
        onRebindCallback?.Invoke(null);
        return;
      }

      var action = inputActionReference.action;
      if (action == null)
      {
        onRebindCallback?.Invoke(null);
        return;
      }

      BindingIndex = bindingIndex;

      if (action.bindings[BindingIndex].isComposite)
      {
        var index = BindingIndex + 1;
        if (index < action.bindings.Count && action.bindings[index].isPartOfComposite)
        {
          PerformInteractiveRebind(action, index, onRebindCallback, "", allCompositeParts: true, excludedPaths, matchingPaths, cancelPaths);
        }
      }
      else
      {
        PerformInteractiveRebind(action, BindingIndex, onRebindCallback, "", false, excludedPaths, matchingPaths, cancelPaths);
      }
    }

    private static void PerformInteractiveRebind(InputAction action, int bindingIndex, Action<string> onRebindCallback,
                                                 string bindingPath, bool allCompositeParts = false, List<string> excludedPaths = null,
                                                 List<string> matchingPaths = null, List<string> cancelPaths = null)
    {
      if (cancelPaths == null) { cancelPaths = new List<string>(); }
      if (cancelPaths.Count == 0)
      {
        cancelPaths.Add("<Keyboard>/escape");
        cancelPaths.Add("<Gamepad>/buttonEast");
      }

      cancelBindings.Clear();

      foreach (var p in cancelPaths)
      {
        cancelBindings.Add(p);
      }

      BindingIndex = bindingIndex;
      string currentBindingPath = action.bindings[BindingIndex].effectivePath;

      var actions = action.actionMap.actions;
      //foreach (var a in actions)
      //{
      //  a.Disable();
      //}
      // TODO Is it sufficient to only disable the current action?
      action.Disable();
      rebindingOperation?.Cancel();
      rebindingOperation?.Reset();

      void CleanUp()
      {
        rebindingOperation?.Dispose();
        rebindingOperation = null;

        var actions = action.actionMap.actions;
        foreach (var a in actions)
        {
          a.Enable();
        }
        // TODO Should we only enable the current action?
        //action.Enable();
      }

      rebindingOperation = action.PerformInteractiveRebinding(BindingIndex);

      if (matchingPaths?.Count > 0)
      {
        bool isReset = false;
        string[] includedPathsArray = null;

        foreach (var matchingPath in matchingPaths)
        {
          if (string.IsNullOrEmpty(matchingPath)) { continue; }

          if (!isReset)
          {
            var includedPathsObject = rebindingOperation.GetType().GetPrivateField("m_IncludePaths")?.GetValue(rebindingOperation);
            var includedPathsCountField = rebindingOperation.GetType().GetPrivateField("m_IncludePathCount");

            if (includedPathsObject != null)
            {
              includedPathsArray = (string[])includedPathsObject;
              Array.Clear(includedPathsArray, 0, 0);
            }

            if (includedPathsCountField != null)
            {
              includedPathsCountField.SetValue(rebindingOperation, 0);
            }
          }
          isReset = true;


          rebindingOperation.WithControlsHavingToMatchPath(matchingPath);
        }
      }

      rebindingOperation.OnCancel(operation =>
      {
        try
        {
          ScheduleUtility.InvokeDelayedByFrames(RemoveEventListener, 1);
          OnRebindCanceled?.Invoke();
          ConsoleLogger.Log("Rebinding operation was canceled", Common.LogType.Debug);
          string bindingPath = action.bindings[BindingIndex].effectivePath;
          onRebindCallback?.Invoke(bindingPath);
          CleanUp();
        }
        catch (Exception)
        {
          // TODO Display this to the user?
          //ConsoleLogger.LogError(e);
        }
      });

      rebindingOperation.OnComplete(operation =>
      {
        string path = action.bindings[BindingIndex].effectivePath;
        // Adding the path to the existing binding path for composites to work
        bindingPath += path;

        CleanUp();

        bool rebindCompleted = true;

        if (allCompositeParts)
        {
          var nextBindingIndex = BindingIndex + 1;
          if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
          {
            bindingPath += " && ";
            PerformInteractiveRebind(action, nextBindingIndex, onRebindCallback, bindingPath, true, excludedPaths, matchingPaths, cancelPaths);
            rebindCompleted = false;
          }
        }

        if (rebindCompleted)
        {
          onRebindCallback?.Invoke(bindingPath);
          OnRebindEnded?.Invoke();
          ScheduleUtility.InvokeDelayedByFrames(RemoveEventListener, 1);
        }
      });

      // Old approach using WithCancelingThrough.
      // This however does not work with multiple paths and
      // since using a InputControlPath like */{Cancel} with
      // wildcards or actions do not work with Unity's internal
      // path matching we can't use that method.
      //if (!string.IsNullOrEmpty(cancelPath))
      //{
      //  //rebindingOperation.WithCancelingThrough(cancelPath);
      //}

      rebindingOperation.WithCancelingThrough(cancelBindings[0]);

      rebindingOperation.OnPotentialMatch(operation =>
      {
        var selectedControl = operation?.selectedControl;

        if (selectedControl != null)
        {
          foreach (var b in cancelBindings)
          {
            if (string.IsNullOrEmpty(b)) { continue; }

            if (InputControlPath.Matches(b, selectedControl))
            {
              RebindingOperation?.Cancel();
            }
          }
        }
      });

      if (excludedPaths?.Count > 0)
      {
        foreach (var excludedPath in excludedPaths)
        {
          if (string.IsNullOrEmpty(excludedPath)) { continue; }
          RebindingOperation?.WithControlsExcluding(excludedPath);
        }
      }

      // TODO Enable once its working
      if (matchingPaths?.Count > 0)
      {
        foreach (var matchingPath in matchingPaths)
        {
          if (string.IsNullOrEmpty(matchingPath)) { continue; }
          RebindingOperation?.WithControlsHavingToMatchPath(matchingPath);
        }
      }

      // TODO Add support for additional filtering/checking of rebinding validity

      if (OnEventDelegate == null) { OnEventDelegate = OnEventTriggered; }

      InputSystem.onEvent -= OnEventDelegate;
      InputSystem.onEvent += OnEventDelegate;

      OnRebindStarted?.Invoke();
      rebindingOperation.Start();
    }

    private static void RemoveEventListener()
    {
      if (OnEventDelegate != null)
      {
        InputSystem.onEvent -= OnEventDelegate;
      }
    }

    private static void OnEventTriggered(InputEventPtr eventPtr, InputDevice device)
    {
      var eventType = eventPtr.type;
      if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type) { return; }

      var flags = InputControlExtensions.Enumerate.IncludeNonLeafControls |
                  InputControlExtensions.Enumerate.IncludeSyntheticControls;

      foreach (var control in eventPtr.EnumerateControls(flags, device))
      {
        if (control == null) { continue; }

        if (control.IsPressed())
        {
          foreach (var cancelBinding in cancelBindings)
          {
            if (string.IsNullOrEmpty(cancelBinding)) { continue; }

            if (InputControlPath.Matches(cancelBinding, control))
            {
              RebindingOperation?.Cancel();
              break;
            }
          }
        }
      }
    }
  }
}