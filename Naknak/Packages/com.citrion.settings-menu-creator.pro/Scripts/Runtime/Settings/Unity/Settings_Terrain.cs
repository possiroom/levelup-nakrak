using System.ComponentModel;
using UnityEngine;

namespace CitrioN.SettingsMenuCreator
{
  [System.Serializable]
  [DisplayName("Terrain Basemap Distance (2022+)")]
  public class Setting_TerrainBasemapDistance : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainBasemapDistance);
#else
    public override string PropertyName => "terrainBasemapDistance";
#endif

    public Setting_TerrainBasemapDistance()
    {
      options.AddMinMaxRangeValues("0", "20000");
      options.AddStepSize("20");

      defaultValue = 1000;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Billboard Start (2022+)")]
  public class Setting_TerrainBillboardStart : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainBillboardStart);
#else
    public override string PropertyName => "terrainBillboardStart";
#endif

    public Setting_TerrainBillboardStart()
    {
      options.AddMinMaxRangeValues("5", "2000");
      options.AddStepSize("5");

      defaultValue = 50;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Detail Density Scale (2022+)")]
  public class Setting_TerrainDetailDensityScale : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainDetailDensityScale);
#else
    public override string PropertyName => "terrainDetailDensityScale";
#endif

    public Setting_TerrainDetailDensityScale()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 1;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Detail Distance (2022+)")]
  public class Setting_TerrainDetailDistance : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainDetailDistance);
#else
    public override string PropertyName => "terrainDetailDistance";
#endif

    public Setting_TerrainDetailDistance()
    {
      options.AddMinMaxRangeValues("0", "1000");
      options.AddStepSize("5");

      defaultValue = 80;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Fade Length (2022+)")]
  public class Setting_TerrainFadeLength : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainFadeLength);
#else
    public override string PropertyName => "terrainFadeLength";
#endif

    public Setting_TerrainFadeLength()
    {
      options.AddMinMaxRangeValues("0", "200");
      options.AddStepSize("5");

      defaultValue = 5;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Max Trees (2022+)")]
  public class Setting_TerrainMaxTrees : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainMaxTrees);
#else
    public override string PropertyName => "terrainMaxTrees";
#endif

    public Setting_TerrainMaxTrees()
    {
      options.AddMinMaxRangeValues("0", "10000");
      options.AddStepSize("10");

      defaultValue = 50;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Tree Distance (2022+)")]
  public class Setting_TerrainTreeDistance : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainTreeDistance);
#else
    public override string PropertyName => "terrainTreeDistance";
#endif

    public Setting_TerrainTreeDistance()
    {
      options.AddMinMaxRangeValues("0", "5000");
      options.AddStepSize("10");

      defaultValue = 5000;
    }
  }

  [System.Serializable]
  [DisplayName("Terrain Pixel Error (2022+)")]
  public class Setting_TerrainPixelError : Setting_Terrain<float>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.terrainPixelError);
#else
    public override string PropertyName => "terrainPixelError";
#endif

    public Setting_TerrainPixelError()
    {
      options.AddMinMaxRangeValues("1", "200");
      options.AddStepSize("0.1");

      defaultValue = 1;
    }
  }

  //  // TODO Requires an input element with multiselect option
  //#if UNITY_2022_1_OR_NEWER
  //  [System.Serializable]
  //  public class TerrainQualityOverridesSetting : Setting_Terrain<TerrainQualityOverrides>
  //  {
  //    public override string PropertyName => nameof(QualitySettings.terrainQualityOverrides);
  //  }
  //#else
  //  [System.Serializable]
  //  public class TerrainQualityOverridesSetting : Setting_Terrain<bool>
  //  {
  //    public override string PropertyName => "terrainQualityOverrides";
  //  }
  //#endif

  [System.Serializable]
  [DisplayName("Use Legacy Detail Distribution (2022+)")]
  public class Setting_UseLegacyDetailDistribution : Setting_Terrain<bool>
  {
#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.useLegacyDetailDistribution);
#else
    public override string PropertyName => "useLegacyDetailDistribution";
#endif
  }
}