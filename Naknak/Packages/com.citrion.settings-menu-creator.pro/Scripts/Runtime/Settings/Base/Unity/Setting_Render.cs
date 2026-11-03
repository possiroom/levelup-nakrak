using CitrioN.Common;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(500)]
  [MenuPath("Render")]
  [ExcludeFromMenuSelection]
  [System.Serializable]
  public abstract class Setting_Render<T> : Setting_Generic_Reflection_Property_Unity_Static<T, RenderSettings>
  {
    public override string EditorNamePrefix => "[Render]";

    public override string EditorName => base.EditorName + " (Builtin & URP)";
  }
}

