using CitrioN.Common.Editor;
using UnityEditor;

namespace CitrioN.SettingsMenuCreator.Input.Editor
{
  [CanEditMultipleObjects]
#if !UNITY_2022_1_OR_NEWER
  [CustomEditor(typeof(InputActionRebindElement), editorForChildClasses: true/*, isFallback = true*/)]
#endif
  public class InputActionRebindElementDrawer: UIToolkitInspectorWindowEditor { }
}