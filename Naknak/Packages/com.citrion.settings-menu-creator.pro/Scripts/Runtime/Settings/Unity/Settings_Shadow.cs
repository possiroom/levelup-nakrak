using CitrioN.Common;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace CitrioN.SettingsMenuCreator
{
  [MenuOrder(1100)]
  [System.Serializable]
  public abstract class Setting_Shadow<T> : Setting_Generic_Unity<T>
  {
    public override string EditorNamePrefix => "[Shadow]";
  }

  [System.Serializable]
  public abstract class Setting_Shadow_Builtin<T> : Setting_Shadow<T>
  {
    public override string EditorName => base.EditorName + " (Builtin)";
  }

  [System.Serializable]
  public abstract class Setting_Shadow_Urp<T> : Setting_Shadow<T>
  {
    public override string RuntimeName => base.RuntimeName.Replace("Urp ", "");

    public override string EditorName => base.EditorName.Replace("Urp ", "") + " (URP)";
  }

  [System.Serializable]
  public abstract class Setting_Shadow_VolumeProfile<T> : Setting_Generic_VolumeProfile<T>
  {
    public override string EditorName => GetType().Name.Replace("Setting_", "").SplitCamelCase();

    public override string RuntimeName => GetType().Name.Replace("Setting_", "").SplitCamelCase();
  }

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Quality (Builtin)")]
  public class Setting_ShadowQuality : Setting_Shadow_Builtin<UnityEngine.ShadowQuality>
  {
    protected override object ApplySettingChangeWithValue(SettingsCollection settings, UnityEngine.ShadowQuality value)
    {
      QualitySettings.shadows = value;
      return QualitySettings.shadows;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object> { QualitySettings.shadows };
    }

    public Setting_ShadowQuality()
    {
      defaultValue = UnityEngine.ShadowQuality.All;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Projection (Builtin)")]
  public class Setting_ShadowProjection : Setting_Shadow_Builtin<ShadowProjection>
  {
    protected override object ApplySettingChangeWithValue(SettingsCollection settings, ShadowProjection value)
    {
      QualitySettings.shadowProjection = value;
      return QualitySettings.shadowProjection;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object> { QualitySettings.shadowProjection };
    }

    public Setting_ShadowProjection()
    {
      defaultValue = ShadowProjection.StableFit;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Resolution (Builtin)")]
  public class Setting_ShadowResolution : Setting_Shadow_Builtin<UnityEngine.ShadowResolution>
  {
    protected override object ApplySettingChangeWithValue(SettingsCollection settings, UnityEngine.ShadowResolution value)
    {
      QualitySettings.shadowResolution = value;
      return QualitySettings.shadowResolution;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object> { QualitySettings.shadowResolution };
    }

    public Setting_ShadowResolution()
    {
      defaultValue = UnityEngine.ShadowResolution.Medium;
    }
  }

  [System.Serializable]
  [MenuOrder(1100)]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Distance")]
  public class Setting_ShadowDistance : Setting_Shadow_VolumeProfile<float>
  {
    public override string EditorNamePrefix => "[Shadow]";

    //public override string RuntimeName => base.RuntimeName.Replace("Shadow ", "");

    public override bool StoreValueInternally => true;

    public Setting_ShadowDistance()
    {
      options.AddMinMaxRangeValues("0", "500");
      options.AddStepSize("10");

      defaultValue = 50;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, float value)
    {
#if UNITY_URP
      if (RenderPipelineUtility.GetCurrentRenderPipelineAsset<UniversalRenderPipelineAsset>(out var urp))
      {
        urp.shadowDistance = value;
        return urp.shadowDistance;
      }
#elif UNITY_HDRP
      var volumeProfile = GetVolumeProfile(settings);
      if (volumeProfile != null)
      {
        if (volumeProfile.TryGet<HDShadowSettings>(out var shadows))
        {
          var current = shadows.maxShadowDistance;
          current.value = value;
          current.overrideState = true;
          return current.value;
        }
      }
#endif
      QualitySettings.shadowDistance = value;
      return QualitySettings.shadowDistance;
    }
  }

  //[System.Serializable]
  //[MenuOrder(1100)]
  //[MenuPath("Quality/Shadow/Contact Shadows")]
  //[DisplayName("Contact Shadows Enabled")]
  //public class Setting_ContactShadows_Enabled : Setting_Generic_VolumeProfile_Extended<bool, ContactShadows>
  //{
  //  public override string RuntimeName => GetType().Name.Replace("Setting_", "").Replace("_", "").SplitCamelCase();

  //  public override string EditorNamePrefix => "[Contact Shadows]";

  //  public override string FieldName => "enable";

  //  public Setting_ContactShadows_Enabled()
  //  {
  //    //ContactShadows contactShadows;
  //    //contactShadows.enable
  //    defaultValue = true;
  //  }
  //}

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Mask Mode")]
  public class Setting_ShadowmaskMode : Setting_Shadow<ShadowmaskMode>
  {
    protected override object ApplySettingChangeWithValue(SettingsCollection settings, ShadowmaskMode value)
    {
      QualitySettings.shadowmaskMode = value;
      return QualitySettings.shadowmaskMode;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object>() { QualitySettings.shadowmaskMode };
    }
  }

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Near Plane Offset (Builtin)")]
  public class Setting_ShadowNearPlaneOffset : Setting_Shadow_Builtin<float>
  {
    public Setting_ShadowNearPlaneOffset()
    {
      options.AddMinMaxRangeValues("0", "5");
      options.AddStepSize("0.1");

      defaultValue = 3f;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, float value)
    {
      QualitySettings.shadowNearPlaneOffset = value;
      return QualitySettings.shadowNearPlaneOffset;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object>() { QualitySettings.shadowNearPlaneOffset };
    }
  }

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Cascades (Builtin)")]
  public class Setting_ShadowCascades : Setting_Shadow_Builtin<int>
  {
    public Setting_ShadowCascades()
    {
      options.Add(new StringToStringRelation("0", "No Cascades"));
      options.Add(new StringToStringRelation("2", "Two Cascades"));
      options.Add(new StringToStringRelation("4", "Four Cascades"));

      defaultValue = 2;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, int value)
    {
      QualitySettings.shadowCascades = value;
      return QualitySettings.shadowCascades;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object>() { QualitySettings.shadowCascades };
    }
  }

  [System.Serializable]
  [MenuPath("Quality/Shadow/")]
  [DisplayName("Shadow Cascade 2 Split (Builtin)")]
  public class Setting_ShadowCascade2Split : Setting_Shadow_Builtin<float>
  {
    public override string EditorName => "Shadow Cascade 2 Split (Builtin)";

    public override string RuntimeName => "Cascade 2 Split";

    public Setting_ShadowCascade2Split()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");

      defaultValue = 0.33f;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, float value)
    {
      QualitySettings.shadowCascade2Split = value;
      return QualitySettings.shadowCascade2Split;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object>() { QualitySettings.shadowCascade2Split };
    }
  }

  // TODO Enable when Vector3 support is added
  //[System.Serializable]
  //[MenuPath("Quality/Shadow/")]
  //public class Setting_ShadowCascade4Split : Setting_Shadow<Vector3>
  //{
  //  public override string PropertyName => nameof(QualitySettings.shadowCascade4Split);
  //}
}

