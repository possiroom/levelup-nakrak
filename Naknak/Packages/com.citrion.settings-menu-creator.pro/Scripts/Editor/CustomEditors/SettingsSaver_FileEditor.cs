using CitrioN.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CitrioN.SettingsMenuCreator.Editor
{
  [CustomEditor(typeof(SettingsSaver_File), editorForChildClasses: true)]
  public class SettingsSaver_FileEditor : UnityEditor.Editor
  {
    public override VisualElement CreateInspectorGUI()
    {
      var root = new VisualElement();

      // Add the default inspector
      InspectorElement.FillDefaultInspector(root, serializedObject, this);
      ClassAttribute.ApplyClassAttributesToHierarchy(root, serializedObject);

      // Add some spacing
      var spacer = new VisualElement();
      spacer.style.height = 10;
      root.Add(spacer);

      var deleteSaveFileButton = new Button(DeleteSaveFile);
      deleteSaveFileButton.text = "Delete Save File";
      root.Add(deleteSaveFileButton);

      var openSaveFileDirectoryButton = new Button(OpenSaveFileDirectory);
      openSaveFileDirectoryButton.text = "Open Save File Directory";
      root.Add(openSaveFileDirectoryButton);

      return root;
    }

    private void DeleteSaveFile()
    {
      var targetObject = serializedObject.targetObject;
      if (targetObject != null && targetObject is SettingsSaver_File saver)
      {
        saver.DeleteSave();
      }
    }

    private void OpenSaveFileDirectory()
    {
      var targetObject = serializedObject.targetObject;
      if (targetObject != null && targetObject is SettingsSaver_File saver)
      {
        saver.OpenSaveFileDirectory();
      }
    }
  }
}