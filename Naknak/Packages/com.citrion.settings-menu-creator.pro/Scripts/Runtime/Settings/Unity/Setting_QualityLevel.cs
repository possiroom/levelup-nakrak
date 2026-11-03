using CitrioN.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(999)]
  [MenuPath("Quality")]
  [System.Serializable]
  // TODO Add support to rename the different options?
  public class Setting_QualityLevel : Setting_Generic_Unity<int>
  {
    public override string EditorNamePrefix => "[Quality]";

    //public override bool StoreValueInternally => false;

    public Setting_QualityLevel()
    {
      try
      {
        options = new List<StringToStringRelation>();
        var names = QualitySettings.names;

        for (int i = 0; i < names.Length; i++)
        {
          options.Add(new StringToStringRelation($"{i}", names[i]));
        }

        var currentLevel = QualitySettings.GetQualityLevel();
        defaultValue = currentLevel;
      }
      catch (System.Exception)
      {
        // We catch the error that Unity throws
        // when deserializing the setting object
        // which calls this constructor:
        // UnityException: get_names is not allowed to be called during serialization, call it from OnEnable instead.
        //throw;
      }
    }

    protected void InitializeDefaultOptions()
    {

    }

    public override List<StringToStringRelation> Options
    {
      get
      {
        bool hasOptions = options != null && options.Count > 0;

        List<StringToStringRelation> runtimeOptions = new List<StringToStringRelation>();

        var names = QualitySettings.names;

        for (int i = 0; i < names.Length; i++)
        {
          runtimeOptions.Add(new StringToStringRelation($"{i}", names[i]));
        }

        if (!hasOptions)
        {
          return runtimeOptions;
        }

        var optionsDuplicate = new List<StringToStringRelation>(options);
        // Remove all entries that are not available at runtime
        optionsDuplicate.RemoveAll(o => runtimeOptions.Find(ro => o.Key == ro.Key) == null);
        return optionsDuplicate;
      }
    }

    public override List<object> GetCurrentValues(SettingsCollection settings) =>
      new List<object>() { QualitySettings.GetQualityLevel() };

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, int value)
    {
      QualitySettings.SetQualityLevel(value);
      return QualitySettings.GetQualityLevel();
    }
  }
}