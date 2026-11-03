using CitrioN.Common;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(700)]
  [ExcludeFromMenuSelection]
  [System.Serializable]
  public abstract class Setting_Screen<T> : Setting_Generic_Reflection_Property_Unity_Static<T, Screen>
  {
    public override string EditorNamePrefix => "[Screen]";
  }
}
