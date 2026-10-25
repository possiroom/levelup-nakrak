using CitrioN.Common;
using System.ComponentModel;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(800)]
  [System.Serializable]
  public abstract class Setting_Application<T> : Setting_Generic_Reflection_Property_Unity_Static<T, Application>
  {
    public override string EditorNamePrefix => "[Application]";
  }

  [System.Serializable]
  [MenuPath("Application")]
  [DisplayName("Target Framerate")]
  public class Setting_TargetFramerate : Setting_Application<int>
  {
    public Setting_TargetFramerate()
    {
      options.Add(new StringToStringRelation("30", "30"));
      options.Add(new StringToStringRelation("60", "60"));
      options.Add(new StringToStringRelation("90", "90"));
      options.Add(new StringToStringRelation("120", "120"));
      options.Add(new StringToStringRelation("150", "150"));
      options.Add(new StringToStringRelation("200", "200"));
      options.Add(new StringToStringRelation("300", "300"));
      options.Add(new StringToStringRelation("-1", "Max"));

      defaultValue = -1;
    }

    public override string PropertyName => nameof(Application.targetFrameRate);
  }

  [System.Serializable]
  [MenuPath("Application")]
  [DisplayName("Run In Background")]
  public class Setting_RunInBackground : Setting_Application<bool>
  {
    public override string PropertyName => nameof(Application.runInBackground);
  }

  [System.Serializable]
  [MenuPath("Application")]
  [DisplayName("Background Loading Priority")]
  public class Setting_BackgroundLoadingPriority : Setting_Application<ThreadPriority>
  {
    public override string PropertyName => nameof(Application.backgroundLoadingPriority);

    public Setting_BackgroundLoadingPriority()
    {
      defaultValue = ThreadPriority.Normal;
    }
  }
}

