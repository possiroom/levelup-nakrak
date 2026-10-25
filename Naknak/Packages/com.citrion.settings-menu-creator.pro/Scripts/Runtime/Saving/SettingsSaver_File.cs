using CitrioN.Common;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  public abstract class SettingsSaver_File : SettingsSaver
  {
    [SerializeField]
    [Tooltip("The name for the settings save file.")]
    protected string fileName = "Settings";

    [SerializeField]
    [Tooltip("The name for the directory the file will be saved in. " +
             "Will be combined with the path from the 'Directory Provider'. " +
             "Can be left empty.")]
    protected string directoryName = "Settings";

    protected abstract string FileExtension { get; } // Examples: json, xml

    [SerializeField]
    [Tooltip("The provider for the path to the main directory. " +
             "Will be combined with the directory name " +
             "to create the directory in which the " +
             "save file will stored in.")]
    protected DirectoryProvider directoryProvider = new DirectoryProvider();

    protected string DirectoryPath
    {
      get
      {
        var providerPath = directoryProvider?.Path;
        if (string.IsNullOrEmpty(providerPath))
        {
          return string.Empty;
        }
        if (string.IsNullOrEmpty(directoryName))
        {
          return providerPath;
        }
        //return Path.Combine(providerPath, directoryName);
        return $"{providerPath}/{directoryName}";
      }
    }

    public override void SaveSettings(SettingsCollection collection)
    {
      SaveSettingsInternal(collection);
    }

    protected virtual void SaveSettingsInternal(SettingsCollection collection)
    {
      if (collection == null)
      {
        ConsoleLogger.LogError($"A collection is required to save its settings!");
        return;
      }

      string directoryPath = DirectoryPath;

      if (string.IsNullOrEmpty(DirectoryPath))
      {
        ConsoleLogger.LogError("Invalid save path provided");
        return;
      }

      if (!Directory.Exists(DirectoryPath))
      {
        Directory.CreateDirectory(DirectoryPath);
        ConsoleLogger.Log($"Created new directory: {DirectoryPath}", Common.LogType.EditorAndDevelopmentBuildAndDebug);
      }

      var currentData = AppendData ? LoadSettingsInternal() : new Dictionary<string, object>();
      var saveString = GetSaveString(collection, currentData);

      string path = $"{DirectoryPath}/{fileName}.{FileExtension}";
      File.WriteAllText(path, saveString);
    }

    protected abstract string GetSaveString(SettingsCollection collection, Dictionary<string, object> settingValues);

    public override Dictionary<string, object> LoadSettings()
    {
      return LoadSettingsInternal();
    }

    protected virtual Dictionary<string, object> LoadSettingsInternal()
    {
      string path = $"{DirectoryPath}/{fileName}.{FileExtension}";

      if (File.Exists(path))
      {
        var text = File.ReadAllText(path);
        var settingsData = LoadFromText(text);

        return settingsData;
      }
      return null;
    }

    protected abstract Dictionary<string, object> LoadFromText(string text);

    [ContextMenu("Delete save file")]
    public override void DeleteSave()
    {
      string path = $"{DirectoryPath}/{fileName}.{FileExtension}";
      FileUtility.DeleteFile(path);
    }

    [ContextMenu("Show save directory")]
    public void OpenSaveFileDirectory()
    {
      var path = DirectoryPath;
      if (!Directory.Exists(path))
      {
        Directory.CreateDirectory(path);
      }
      FileUtility.OpenFileDirectory(path);
    }
  }
}