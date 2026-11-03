using CitrioN.Common;
using System.Collections.Generic;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  // TODO Notify screen resolution setting about the change of this setting
  [MenuOrder(700)]
  [MenuPath("Display")]
  [System.Serializable]
  public class Setting_Monitor : Setting
  {
    public override string EditorNamePrefix => "[Display]";

    public override string RuntimeName => "Monitor";

    public override string EditorName => "Monitor";

    public override List<string> ParameterTypes => new List<string>() { typeof(string).AssemblyQualifiedName };

    public override List<StringToStringRelation> Options
    {
      get
      {
        List<StringToStringRelation> options = new List<StringToStringRelation>();

        var displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);

        foreach (var display in displays)
        {
          var displayName = display.name;
          options.Add(new StringToStringRelation(displayName, displayName));
        }

        return options;
      }
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      var displays = new List<DisplayInfo>();
      Screen.GetDisplayLayout(displays);
      var displayIndex = displays.IndexOf(Screen.mainWindowDisplayInfo);
      if (displayIndex >= 0)
      {
        return new List<object>() { displays[displayIndex].name };
      }
      return null;
    }

    public override object ApplySettingChange(SettingsCollection settings, params object[] args)
    {
      if (args?.Length > 0)
      {
        if (args[0] is string stringValue)
        {
          var displays = new List<DisplayInfo>();
          Screen.GetDisplayLayout(displays);
          for (int i = 0; i < displays.Count; i++)
          {
            var display = displays[i];
            if (display.name == stringValue)
            {
              Screen.MoveMainWindowTo(display, new Vector2Int(display.width / 2, display.height / 2));
              base.ApplySettingChange(settings, stringValue);

              var isFullScreen = Screen.fullScreen;

              int width = ScreenUtility.activeScreenResolution != Vector2Int.zero ?
                          ScreenUtility.activeScreenResolution.x : Screen.currentResolution.width;
              int height = ScreenUtility.activeScreenResolution != Vector2Int.zero ?
                           ScreenUtility.activeScreenResolution.y : Screen.currentResolution.height;

              ScreenUtility.SetScreenResolution(width, height, isFullScreen);

              return stringValue;
            }
          }
        }
      }
      base.ApplySettingChange(settings, null);
      return null;
    }

    public override object GetDefaultValue(SettingsCollection settings)
    {
      var displays = new List<DisplayInfo>();
      Screen.GetDisplayLayout(displays);
      return displays[0].name;
    }
  }
}