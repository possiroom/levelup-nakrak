using CitrioN.Common;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(180)]
  [MenuPath("Event")]
  [System.Serializable]
  [ExcludeFromMenuSelection]
  public class Setting_Generic_Event<T> : Setting_Generic<T>
  {
    [SerializeField]
    protected UnityEvent<T> action = new UnityEvent<T>();

    [SerializeField]
    protected UnityEvent<SettingsCollection, T> actionWithCollection = new UnityEvent<SettingsCollection, T>();

    private const int MAX_EVENT_DISPLAY_NAME_COUNT = 2;

    public override string EditorNamePrefix => $"[Event {typeof(T).Name}]";

    public override string EditorName
    {
      get
      {
        var eventCount = action.GetPersistentEventCount();
        if (eventCount > 0)
        {
          var sb = new StringBuilder();
          for (int i = 0; i < eventCount; i++)
          {
            if (i >= MAX_EVENT_DISPLAY_NAME_COUNT)
            {
              sb.Append("\n");
              sb.Append($"+ {eventCount - i} more");
              break;
            }


            var target = action.GetPersistentTarget(i);
            if (target != null)
            {
              var targetName = target.name;
              var methodName = action.GetPersistentMethodName(i);
              if (string.IsNullOrEmpty(methodName))
              {
                methodName = "[No function]";
              }
              var fullName = $"{targetName}.{methodName}";
              sb.Append("\n");
              sb.Append(fullName);
            }

            //if (i == 0) { sb.Append(fullName); }
            //else { sb.AppendLine(fullName); }
            //bool isLastItem = i == eventCount - 1;
            ////if (!isLastItem) { sb.Append(", "); }
            //if (!isLastItem) { sb.Append("\n"); }
          }
          return $"{sb}";
        }
        else
        {
          return $"None";
        }
      }
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, T value)
    {
      action?.Invoke(value);

      actionWithCollection?.Invoke(settings, value);
      return value;
    }
  }
}