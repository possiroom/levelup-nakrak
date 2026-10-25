using CitrioN.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
#endif
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace CitrioN.SettingsMenuCreator
{
  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("Anti Aliasing (Builtin & URP)")]
  public class Setting_AntiAliasing : Setting_Generic_Unity<int>
  {
    public override string EditorNamePrefix => "[Quality]";

    public override string EditorName => base.EditorName + " (Builtin & URP)";

    public Setting_AntiAliasing()
    {
      options.Add(new StringToStringRelation("0", "Disabled"));
      options.Add(new StringToStringRelation("2", "2x Multi Sampling"));
      options.Add(new StringToStringRelation("4", "4x Multi Sampling"));
      options.Add(new StringToStringRelation("8", "8x Multi Sampling"));

      defaultValue = 2;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, int value)
    {
#if UNITY_URP
      if (RenderPipelineUtility.GetCurrentRenderPipelineAsset<UniversalRenderPipelineAsset>(out var urp))
      {
        urp.msaaSampleCount = value;
        return urp.msaaSampleCount;
      }
#endif
      QualitySettings.antiAliasing = value;
      return QualitySettings.antiAliasing;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
#if UNITY_URP
      if (RenderPipelineUtility.GetCurrentRenderPipelineAsset<UniversalRenderPipelineAsset>(out var urp))
      {
        return new List<object>() { urp.msaaSampleCount };
      }
#endif
      return new List<object>() { QualitySettings.antiAliasing };
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_BillboardsFaceCameraPosition : Setting_Quality<bool>
  {
    public override string PropertyName => nameof(QualitySettings.billboardsFaceCameraPosition);
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("Enable LOD Cross Fade (2022+)")]
  public class Setting_EnableLODCrossFade : Setting_Generic_Unity<bool>
  {
    public override string EditorNamePrefix => "[Quality]";

    public override string RuntimeName => "Enable LOD Cross Fade";

    public override string EditorName => "Enable LOD Cross Fade (2022+)";

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, bool value)
    {
#if UNITY_URP
      if (RenderPipelineUtility.GetCurrentRenderPipelineAsset<UniversalRenderPipelineAsset>(out var urp))
      {
        var field = urp.GetType().GetPrivateField("m_EnableLODCrossFade");
        if (field != null)
        {
          field.SetValue(urp, value);
          return field.GetValue(urp);
        }
      }
#endif
#if UNITY_2022_1_OR_NEWER
      QualitySettings.enableLODCrossFade = value;
      return QualitySettings.enableLODCrossFade;
#else
      return null;
#endif
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
#if UNITY_URP
      if (RenderPipelineUtility.GetCurrentRenderPipelineAsset<UniversalRenderPipelineAsset>(out var urp))
      {
        var field = urp.GetType().GetPrivateField("m_EnableLODCrossFade");
        if (field != null)
        {
          return new List<object>() { field.GetValue(urp) };
        }
      }
#endif

#if UNITY_2022_1_OR_NEWER
      return new List<object>() { QualitySettings.enableLODCrossFade };
#else
      return null;
#endif
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_GlobalTextureMipmapLimit : Setting_Quality<int>
  {

#if UNITY_2022_1_OR_NEWER
    public override string PropertyName => nameof(QualitySettings.globalTextureMipmapLimit);
#else
    public override string PropertyName => nameof(QualitySettings.masterTextureLimit);
#endif

    public Setting_GlobalTextureMipmapLimit()
    {
      options.Add(new StringToStringRelation("0", "Full Resolution"));
      options.Add(new StringToStringRelation("1", "Half Resolution"));
      options.Add(new StringToStringRelation("2", "Quarter Resolution"));
      options.Add(new StringToStringRelation("3", "Eighth Resolution"));

      SetDefaultValue();
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("LOD Bias (Builtin & URP)")]
  public class Setting_LodBias : Setting_Quality<float>
  {
    public override string PropertyName => nameof(QualitySettings.lodBias);

    public override string EditorName => "LOD Bias (Builtin & URP)";

    public Setting_LodBias()
    {
      options.AddMinMaxRangeValues("0.01", "3");
      options.AddStepSize("0.01");

      defaultValue = 2;
    }
  }

  [System.Serializable]
  [DisplayName("Maximum LOD Level (Builtin & URP)")]
  [MenuPath("Quality/General/")]
  public class Setting_MaximumLODLevel : Setting_Quality<int>
  {
    public override string EditorName => "Maximum LOD Level (Builtin & URP)";

    public override string PropertyName => nameof(QualitySettings.maximumLODLevel);

    public Setting_MaximumLODLevel()
    {
      options.Add(new StringToStringRelation("0", "Best"));
      options.Add(new StringToStringRelation("1", "Balanced"));
      options.Add(new StringToStringRelation("2", "Worst"));

      defaultValue = 0;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_MaxQueuedFrames : Setting_Quality<int>
  {
    public override string PropertyName => nameof(QualitySettings.maxQueuedFrames);

    public Setting_MaxQueuedFrames()
    {
      options.Add(new StringToStringRelation("1", "1"));
      options.Add(new StringToStringRelation("2", "2"));
      options.Add(new StringToStringRelation("3", "3"));
      options.Add(new StringToStringRelation("4", "4"));
      options.Add(new StringToStringRelation("5", "5"));

      defaultValue = 2;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_ParticleRaycastBudget : Setting_Quality<int>
  {
    public override string PropertyName => nameof(QualitySettings.particleRaycastBudget);

    public Setting_ParticleRaycastBudget()
    {
      options.Add(new StringToStringRelation("4", "4"));
      options.Add(new StringToStringRelation("8", "8"));
      options.Add(new StringToStringRelation("16", "16"));
      options.Add(new StringToStringRelation("32", "32"));
      options.Add(new StringToStringRelation("64", "64"));
      options.Add(new StringToStringRelation("128", "128"));
      options.Add(new StringToStringRelation("256", "256"));
      options.Add(new StringToStringRelation("512", "512"));
      options.Add(new StringToStringRelation("1024", "1024"));
      options.Add(new StringToStringRelation("2048", "2048"));
      options.Add(new StringToStringRelation("4096", "4096"));

      defaultValue = 256;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("Pixel Light Count (Builtin)")]
  public class Setting_PixelLightCount : Setting_Quality_Builtin<int>
  {
    public override string PropertyName => nameof(QualitySettings.pixelLightCount);

    public Setting_PixelLightCount()
    {
      options.Add(new StringToStringRelation("1", "1"));
      options.Add(new StringToStringRelation("2", "2"));
      options.Add(new StringToStringRelation("3", "3"));
      options.Add(new StringToStringRelation("4", "4"));

      defaultValue = 2;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("Realtime Reflection Probes (Builtin & URP)")]
  public class Setting_RealtimeReflectionProbes : Setting_Quality<bool>
  {
    public override string PropertyName => nameof(QualitySettings.realtimeReflectionProbes);

    public override string EditorName => base.EditorName + " (Builtin & URP)";
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("Resolution Scaling Fixed DPI Factor")]
  public class Setting_ResolutionScalingFixedDPIFactor : Setting_Quality<float>
  {
    public override string RuntimeName => "Resolution Scaling Fixed DPI Factor";
    public override string EditorName => "Resolution Scaling Fixed DPI Factor";
    public override string PropertyName => nameof(QualitySettings.resolutionScalingFixedDPIFactor);

    public Setting_ResolutionScalingFixedDPIFactor()
    {
      options.AddMinMaxRangeValues("0.1", "2");
      options.AddStepSize("0.1");

      defaultValue = 1;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_SkinWeights : Setting_Quality<SkinWeights>
  {
    public override string PropertyName => nameof(QualitySettings.skinWeights);

    public Setting_SkinWeights()
    {
      defaultValue = SkinWeights.TwoBones;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  [DisplayName("Soft Particles (Builtin)")]
  public class Setting_SoftParticles : Setting_Quality_Builtin<bool>
  {
    public override string PropertyName => nameof(QualitySettings.softParticles);

    public Setting_SoftParticles()
    {
      defaultValue = false;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_SoftVegetation : Setting_Quality<bool>
  {
    public override string PropertyName => nameof(QualitySettings.softVegetation);
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_StreamingMipmapsActive : Setting_Quality<bool>
  {
    public override string PropertyName => nameof(QualitySettings.streamingMipmapsActive);

    public Setting_StreamingMipmapsActive()
    {
      defaultValue = false;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_StreamingMipmapsAddAllCameras : Setting_Quality<bool>
  {
    public override string PropertyName => nameof(QualitySettings.streamingMipmapsAddAllCameras);
  }

  [System.Serializable]
  [DisplayName("Streaming Mipmaps Max File IO Requests")]
  [MenuPath("Quality/General/")]
  public class Setting_StreamingMipmapsMaxFileIORequests : Setting_Quality<int>
  {
    public override string RuntimeName => "Streaming Mipmaps Max File IO Requests";
    public override string EditorName => "Streaming Mipmaps Max File IO Requests";

    public override string PropertyName => nameof(QualitySettings.streamingMipmapsMaxFileIORequests);

    public Setting_StreamingMipmapsMaxFileIORequests()
    {
      options.Add(new StringToStringRelation("4", "4"));
      options.Add(new StringToStringRelation("8", "8"));
      options.Add(new StringToStringRelation("16", "16"));
      options.Add(new StringToStringRelation("32", "32"));
      options.Add(new StringToStringRelation("64", "64"));
      options.Add(new StringToStringRelation("128", "128"));
      options.Add(new StringToStringRelation("256", "256"));
      options.Add(new StringToStringRelation("512", "512"));
      options.Add(new StringToStringRelation("1024", "1024"));
      options.Add(new StringToStringRelation("2048", "2048"));
      options.Add(new StringToStringRelation("4096", "4096"));

      defaultValue = 1024;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_StreamingMipmapsMaxLevelReduction : Setting_Quality<int>
  {
    public override string PropertyName => nameof(QualitySettings.streamingMipmapsMaxLevelReduction);

    public Setting_StreamingMipmapsMaxLevelReduction()
    {
      options.Add(new StringToStringRelation("1", "1"));
      options.Add(new StringToStringRelation("2", "2"));
      options.Add(new StringToStringRelation("3", "3"));
      options.Add(new StringToStringRelation("4", "4"));

      defaultValue = 2;
    }
  }

  // TODO Enable once float slider can support list of values
  //[System.Serializable]
  //[MenuPath("Quality/General/")]
  //public class StreamingMipmapsMemoryBudgetSetting : UnityQualitySettingsSetting<float>
  //{
  //  public override string PropertyName => nameof(QualitySettings.streamingMipmapsMemoryBudget);

  //  public StreamingMipmapsMemoryBudgetSetting()
  //  {
  //    options.Add(new StringToStringRelation("4", "4"));
  //    options.Add(new StringToStringRelation("8", "8"));
  //    options.Add(new StringToStringRelation("16", "16"));
  //    options.Add(new StringToStringRelation("32", "32"));
  //    options.Add(new StringToStringRelation("64", "64"));
  //    options.Add(new StringToStringRelation("128", "128"));
  //    options.Add(new StringToStringRelation("256", "256"));
  //    options.Add(new StringToStringRelation("512", "512"));
  //    options.Add(new StringToStringRelation("1024", "1024"));
  //    options.Add(new StringToStringRelation("2048", "2048"));
  //    options.Add(new StringToStringRelation("4096", "4096"));
  //  }
  //}

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_StreamingMipmapsRenderersPerFrame : Setting_Quality<int>
  {
    public override string PropertyName => nameof(QualitySettings.streamingMipmapsRenderersPerFrame);

    public Setting_StreamingMipmapsRenderersPerFrame()
    {
      options.Add(new StringToStringRelation("4", "4"));
      options.Add(new StringToStringRelation("8", "8"));
      options.Add(new StringToStringRelation("16", "16"));
      options.Add(new StringToStringRelation("32", "32"));
      options.Add(new StringToStringRelation("64", "64"));
      options.Add(new StringToStringRelation("128", "128"));
      options.Add(new StringToStringRelation("256", "256"));
      options.Add(new StringToStringRelation("512", "512"));
      options.Add(new StringToStringRelation("1024", "1024"));
      options.Add(new StringToStringRelation("2048", "2048"));
      options.Add(new StringToStringRelation("4096", "4096"));

      defaultValue = 512;
    }
  }

  [System.Serializable]
  [DisplayName("VSync Count")]
  [MenuPath("Quality/General/")]
  public class Setting_VSyncCount : Setting_Quality<int>
  {
    public override string RuntimeName => "VSync Count";

    public override string EditorName => "VSync Count";

    public override string PropertyName => nameof(QualitySettings.vSyncCount);

    public Setting_VSyncCount()
    {
      options.Add(new StringToStringRelation("0", "Disabled"));
      options.Add(new StringToStringRelation("1", "One"));
      options.Add(new StringToStringRelation("2", "Two"));

      defaultValue = 1;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_AsyncUploadTimeSlice : Setting_Quality<int>
  {
    public override string PropertyName => nameof(QualitySettings.asyncUploadTimeSlice);

    public Setting_AsyncUploadTimeSlice()
    {
      options.AddMinMaxRangeValues("1", "33");
      options.AddStepSize("1");

      defaultValue = 2;
    }
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_AsyncUploadPersistentBuffer : Setting_Quality<bool>
  {
    public override string PropertyName => nameof(QualitySettings.asyncUploadPersistentBuffer);
  }

  [System.Serializable]
  [MenuPath("Quality/General/")]
  public class Setting_AsyncUploadBufferSize : Setting_Quality<int>
  {
    public override string PropertyName => nameof(QualitySettings.asyncUploadBufferSize);

    public Setting_AsyncUploadBufferSize()
    {
      options.Add(new StringToStringRelation("2", "2"));
      options.Add(new StringToStringRelation("4", "4"));
      options.Add(new StringToStringRelation("8", "8"));
      options.Add(new StringToStringRelation("16", "16"));
      options.Add(new StringToStringRelation("32", "32"));
      options.Add(new StringToStringRelation("64", "64"));
      options.Add(new StringToStringRelation("128", "128"));
      options.Add(new StringToStringRelation("256", "256"));
      options.Add(new StringToStringRelation("512", "512"));
      options.Add(new StringToStringRelation("1024", "1024"));
      options.Add(new StringToStringRelation("2047", "2047"));

      defaultValue = 16;
    }
  }
}