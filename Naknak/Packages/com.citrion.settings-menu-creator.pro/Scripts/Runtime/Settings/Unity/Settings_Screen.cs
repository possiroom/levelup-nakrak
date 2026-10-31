using CitrioN.Common;
using System.Collections.Generic;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [MenuPath("Display")]
  [System.Serializable]
  public class Setting_Fullscreen : Setting_Screen<bool>
  {
    public override string PropertyName => nameof(Screen.fullScreen);

    public Setting_Fullscreen()
    {
      defaultValue = true;
    }
  }

  // Old
  // TODO Remove later
  //[MenuPath("Display")]
  //public class Setting_FullScreenMode : Setting_Screen<FullScreenMode>
  //{
  //  public override string PropertyName => nameof(Screen.fullScreenMode);

  //  public Setting_FullScreenMode()
  //  {
  //    defaultValue = FullScreenMode.FullScreenWindow;
  //  }
  //}

  [MenuOrder(700)]
  [MenuPath("Display")]
  [System.Serializable]
  public class Setting_FullScreenMode : Setting_Generic<FullScreenMode>
  {
    public override string EditorNamePrefix => "[Screen]";

    public Setting_FullScreenMode()
    {
      defaultValue = FullScreenMode.FullScreenWindow;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, FullScreenMode value)
    {
      //var fullScreenMode = value;
      if (Screen.fullScreenMode != value)
      {
        Screen.fullScreenMode = value;
      }
      //ScheduleUtility.InvokeDelayedByFrames(() =>
      //{
      var isFullScreen = Screen.fullScreen;
      isFullScreen = !(value == FullScreenMode.Windowed);

      int width = ScreenUtility.activeScreenResolution != Vector2Int.zero ? 
                  ScreenUtility.activeScreenResolution.x : Screen.currentResolution.width;
      int height = ScreenUtility.activeScreenResolution != Vector2Int.zero ? 
                   ScreenUtility.activeScreenResolution.y : Screen.currentResolution.height;

      ScreenUtility.SetScreenResolution(width, height, isFullScreen);

      //Screen.fullScreenMode = value;
      //});
      // We need to apply it again if the game just started/scene just loaded.
      // TODO Is there a way to improve this?
      //if (Time.timeSinceLevelLoad == 0)
      //{
      //ScheduleUtility.InvokeDelayedByFrames(() => { Screen.fullScreenMode = value; }, 3);
      //}
      return value;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object>() { Screen.fullScreenMode };
    }
  }

  [MenuPath("Display")]
  [System.Serializable]
  public class Setting_ScreenSleepTimeout : Setting_Screen<int>
  {
    public override string PropertyName => nameof(Screen.sleepTimeout);

    public Setting_ScreenSleepTimeout()
    {
      options.Add(new StringToStringRelation("-1", "Never"));
      options.Add(new StringToStringRelation("-2", "System Setting"));

      defaultValue = -1;
    }
  }

  #region Mobile Settings
  // TODO Enable once tested

  // TODO Only works on iOS?
  //[MenuPath("Display")]
  //[System.Serializable]
  //public class Setting_Brightness : Setting_Screen<float>
  //{
  //  public override string PropertyName => nameof(Screen.brightness);

  //  public Setting_Brightness()
  //  {
  //    options.AddMinMaxRangeValues("0", "1");

  //    assignUnityValueAsDefault = true;
  //  }
  //}

  //[MenuPath("Display")]
  //[System.Serializable]
  //public class Setting_ScreenOrientation : Setting_Screen<ScreenOrientation>
  //{
  //  public override string PropertyName => nameof(Screen.orientation);

  //  public Setting_ScreenOrientation()
  //  {
  //    assignUnityValueAsDefault = true;
  //  }
  //}

  //[MenuPath("Display")]
  //[System.Serializable]
  //public class Setting_AutorotateToLandscapeLeft : Setting_Screen<bool>
  //{
  //  public override string PropertyName => nameof(Screen.autorotateToLandscapeLeft);

  //  public Setting_AutorotateToLandscapeLeft()
  //  {
  //    assignUnityValueAsDefault = true;
  //  }
  //}

  //[MenuPath("Display")]
  //[System.Serializable]
  //public class Setting_AutorotateToLandscapeRight : Setting_Screen<bool>
  //{
  //  public override string PropertyName => nameof(Screen.autorotateToLandscapeRight);

  //  public Setting_AutorotateToLandscapeRight()
  //  {
  //    assignUnityValueAsDefault = true;
  //  }
  //}

  //[MenuPath("Display")]
  //[System.Serializable]
  //public class Setting_AutorotateToPortrait : Setting_Screen<bool>
  //{
  //  public override string PropertyName => nameof(Screen.autorotateToPortrait);

  //  public Setting_AutorotateToPortrait()
  //  {
  //    assignUnityValueAsDefault = true;
  //  }
  //}

  //[MenuPath("Display")]
  //[System.Serializable]
  //public class Setting_AutorotateToPortraitUpsideDown : Setting_Screen<bool>
  //{
  //  public override string PropertyName => nameof(Screen.autorotateToPortraitUpsideDown);

  //  public Setting_AutorotateToPortraitUpsideDown()
  //  {
  //    assignUnityValueAsDefault = true;
  //  }
  //}
  #endregion
}