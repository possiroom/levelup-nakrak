using CitrioN.Common;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(1000)]
  [MenuPath("Quality/Terrain/")]
  [ExcludeFromMenuSelection]
  [System.Serializable]
  public abstract class Setting_Terrain<T> : Setting_Quality<T>
  {
    public override string EditorNamePrefix => "[Terrain]";

    public override string RuntimeName => base.RuntimeName.Replace("Terrain ", "");

    public override string EditorName => $"{base.EditorName} (2022+)";
  }
}

