using CitrioN.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace CitrioN.SettingsMenuCreator
{
  [HeaderInfo("\n\nSaves the setting values to a json file.")]
  [CreateAssetMenu(fileName = "SettingsSaver_Json_",
                   menuName = "CitrioN/Settings Menu Creator/Settings Saver/Json",
                   order = 22)]
  public class SettingsSaver_Json : SettingsSaver_File
  {
    protected static bool canShowJsonInstallWindow = true;

    protected override string FileExtension => "json";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
      canShowJsonInstallWindow = true;
    }

    protected override string GetSaveString(SettingsCollection collection, Dictionary<string, object> settingValues)
    {
#if NEWTONSOFT_JSON
      if (settingValues == null) { settingValues = new Dictionary<string, object>(); }

      List<string> identifiers = new List<string>();

      if (collection != null)
      {
        foreach (var item in collection.activeSettingValues)
        {
          var identifier = item.Key;
          identifiers.AddIfNotContains(identifier);
          var value = item.Value;

          if (value == null) { continue; }

          settingValues.AddOrUpdateDictionaryItem(identifier, item.Value);
        }

        foreach (var setting in collection.Settings)
        {
          var identifier = setting.Identifier;
          if (identifiers.Contains(identifier)) { continue; }
          //if (isDefault && settingValues.ContainsKey(identifier)) { continue; }
          identifiers.Add(identifier);
          var values = setting.Setting.GetCurrentValues(collection);
          if (values == null || values.Count < 1) { continue; }
          var value = values[0];
          if (value == null) { continue; }

          settingValues.AddOrUpdateDictionaryItem(identifier, value);
        }
      }

      List<SerializedSetting> serializedSettings = new List<SerializedSetting>();

      foreach (var item in settingValues)
      {
        var identifier = item.Key;
        var value = item.Value;

        var type = value.GetType();
        string valueString = JsonConvert.SerializeObject(value, Formatting.Indented);

        var serializedSetting = new SerializedSetting(identifier, type, valueString);
        serializedSettings.Add(serializedSetting);
        ConsoleLogger.Log($"Json SettingsSaver: Saving '{identifier}' with value '{value}'",
                          Common.LogType.Debug);
      }

      return JsonConvert.SerializeObject(serializedSettings, Formatting.Indented);
#else
      ConsoleLogger.LogError("To save settings to json the 'Newtonsoft Json' package is required! " +
                             "You can install it from Tools/CitrioN/Settings Menu Creator/Dependencies/Import Newtonsoft Json Package");

      ShowJsonPackageInstallWindow();
      return string.Empty;
#endif
    }

    private void ShowJsonPackageInstallWindow()
    {
#if UNITY_EDITOR
      if (!canShowJsonInstallWindow) { return; }
      DialogUtilities.DisplayDialog($"Install Newtonsoft Json Package?",
    $"To save and load settings with json the 'Newtonsoft Json' package is required! " +
    $"Would you like to add it?",
    "Add", "Don't Add", AddJsonPackage, null);
      canShowJsonInstallWindow = false;
#endif
    }

    private void AddJsonPackage()
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.ExitPlaymode();
      UnityEditor.PackageManager.Client.Add("com.unity.nuget.newtonsoft-json");
#endif
    }

    protected override Dictionary<string, object> LoadFromText(string text)
    {
      var dict = new Dictionary<string, object>();
#if NEWTONSOFT_JSON
      var settingsList = JsonConvert.DeserializeObject<List<SerializedSetting>>(text);

      if (settingsList?.Count > 0)
      {
        foreach (var setting in settingsList)
        {
          var identifier = setting.Key;
          var typeString = setting.Type;
          var type = Type.GetType(typeString);
          var valueString = setting.Value;
          var value = JsonConvert.DeserializeObject(valueString, type);

          //collection.ApplySettingChange(identifier, true, true, value);
          dict.AddOrUpdateDictionaryItem(identifier, value);
        }
      }
#else
      ConsoleLogger.LogError("To load settings from json the 'Newtonsoft Json' package is required! " +
                             "You can install it from Tools/CitrioN/Settings Menu Creator/Dependencies/Import Newtonsoft Json Package");
      ShowJsonPackageInstallWindow();
#endif
      return dict;
    }
  }
}