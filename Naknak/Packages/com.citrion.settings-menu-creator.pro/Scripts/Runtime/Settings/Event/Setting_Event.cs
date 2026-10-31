using CitrioN.Common;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace CitrioN.SettingsMenuCreator
{
  [MenuPath("Event")]
  [MenuOrder(101)]
  [System.Serializable]
  public class Setting_Event : Setting
  {
    [SerializeField]
    protected UnityEvent action = new UnityEvent();

    private const int MAX_EVENT_DISPLAY_NAME_COUNT = 2;

    public override List<string> ParameterTypes => null;

    public override string EditorNamePrefix => "[Event]";

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

    public override bool StoreValueInternally => false;

    public override object ApplySettingChange(SettingsCollection settings, params object[] args)
    {
      action?.Invoke();
      return null;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings) => null;

    public override object GetDefaultValue(SettingsCollection settings) => null;
  }
}