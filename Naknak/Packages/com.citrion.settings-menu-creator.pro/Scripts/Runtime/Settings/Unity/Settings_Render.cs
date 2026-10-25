using CitrioN.Common;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace CitrioN.SettingsMenuCreator
{
  // TODO Enable once color support is added
  //[System.Serializable]
  //[DisplayName("Ambient Equator Color (Builtin & URP)")]
  //public class Setting_AmbientEquatorColor : Setting_Render<Color>
  //{
  //  public override string PropertyName => nameof(RenderSettings.ambientEquatorColor);
  //}

  // TODO Enable once color support is added
  //[DisplayName("Ambient Ground Color (Builtin & URP)")]
  //public class Setting_AmbientGroundColor : Setting_Render<Color>
  //{
  //  public override string PropertyName => nameof(Setting_Render.ambientGroundColor);
  //}

  [System.Serializable]
  [DisplayName("Ambient Intensity (Builtin & URP)")]
  public class Setting_AmbientIntensity : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.ambientIntensity);

    public Setting_AmbientIntensity()
    {
      options.AddMinMaxRangeValues("0", "8");
      options.AddStepSize("0.1");

      defaultValue = 1;
    }
  }

  // TODO Enable once color support is added
  //[System.Serializable]
  //[DisplayName("Ambient Light Color (Builtin & URP)")]
  //public class Setting_AmbientLightColor : RenderSetting<Color>
  //{
  //  public override string PropertyName => nameof(RenderSettings.ambientLight);
  //}

  [System.Serializable]
  [DisplayName("Ambient Mode (Builtin & URP)")]
  public class Setting_AmbientMode : Setting_Render<AmbientMode>
  {
    public override string PropertyName => nameof(RenderSettings.ambientMode);

    public Setting_AmbientMode()
    {
      defaultValue = AmbientMode.Skybox;
    }
  }

  // TODO Enable once color support is added
  //[System.Serializable]
  //[DisplayName("Ambient Sky Color (Builtin & URP)")]
  //public class Setting_AmbientSkyColor : RenderSetting<Color>
  //{
  //  public override string PropertyName => nameof(RenderSettings.ambientSkyColor);
  //}

  [System.Serializable]
  [DisplayName("Default Reflection Mode (Builtin & URP)")]
  public class Setting_DefaultReflectionMode : Setting_Render<DefaultReflectionMode>
  {
    public override string PropertyName => nameof(RenderSettings.defaultReflectionMode);

    public Setting_DefaultReflectionMode()
    {
      defaultValue = DefaultReflectionMode.Skybox;
    }
  }

  [System.Serializable]
  [DisplayName("Default Reflection Resolution (Builtin & URP)")]
  public class Setting_DefaultReflectionResolution : Setting_Render<int>
  {
    public override string PropertyName => nameof(RenderSettings.defaultReflectionResolution);

    public Setting_DefaultReflectionResolution()
    {
      options.Add(new StringToStringRelation("16", "16"));
      options.Add(new StringToStringRelation("32", "32"));
      options.Add(new StringToStringRelation("64", "64"));
      options.Add(new StringToStringRelation("128", "128"));
      options.Add(new StringToStringRelation("256", "256"));
      options.Add(new StringToStringRelation("512", "512"));
      options.Add(new StringToStringRelation("1024", "1024"));
      options.Add(new StringToStringRelation("2048", "2048"));

      defaultValue = 128;
    }
  }

  [System.Serializable]
  [DisplayName("Flare Fade Speed (Builtin & URP)")]
  public class Setting_FlareFadeSpeed : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.flareFadeSpeed);

    public Setting_FlareFadeSpeed()
    {
      options.AddMinMaxRangeValues("0", "10");
      options.AddStepSize("0.25");

      defaultValue = 3;
    }
  }

  [System.Serializable]
  [DisplayName("Flare Strength (Builtin & URP)")]
  public class Setting_FlareStrength : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.flareStrength);

    public Setting_FlareStrength()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 1;
    }
  }

  #region Fog
  [System.Serializable]
  [MenuPath("Render/Fog")]
  [DisplayName("Fog (Builtin & URP)")]
  public class Setting_Fog : Setting_Render<bool>
  {
    public override string PropertyName => nameof(RenderSettings.fog);

    public Setting_Fog()
    {
      defaultValue = false;
    }
  }

  // TODO Enable once color support is added
  //[System.Serializable]
  //[DisplayName("Fog Color (Builtin & URP)")]
  //public class Setting_FogColor : Setting_Render<Color>
  //{
  //  public override string PropertyName => nameof(RenderSettings.fogColor);
  //}

  [System.Serializable]
  [MenuPath("Render/Fog")]
  [DisplayName("Fog Mode (Builtin & URP)")]
  public class Setting_FogMode : Setting_Render<FogMode>
  {
    public override string PropertyName => nameof(RenderSettings.fogMode);

    public Setting_FogMode()
    {
      defaultValue = FogMode.ExponentialSquared;
    }
  }

  [System.Serializable]
  [MenuPath("Render/Fog")]
  [DisplayName("Fog Density (Builtin & URP)")]
  public class Setting_FogDensity : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.fogDensity);

    public Setting_FogDensity()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 0.01f;
    }
  }

  [System.Serializable]
  [MenuPath("Render/Fog")]
  [DisplayName("Fog Start Distance (Builtin & URP)")]
  public class Setting_FogStartDistance : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.fogStartDistance);

    public Setting_FogStartDistance()
    {
      options.AddMinMaxRangeValues("0", "100");
      options.AddStepSize("5");

      defaultValue = 0;
    }
  }

  [System.Serializable]
  [MenuPath("Render/Fog")]
  [DisplayName("Fog End Distance (Builtin & URP)")]
  public class Setting_FogEndDistance : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.fogEndDistance);

    public Setting_FogEndDistance()
    {
      options.AddMinMaxRangeValues("0", "1000");
      options.AddStepSize("50");

      defaultValue = 300;
    }
  }
  #endregion

  [System.Serializable]
  [DisplayName("Halo Strength (Builtin & URP)")]
  public class Setting_HaloStrength : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.haloStrength);

    public Setting_HaloStrength()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 0.5f;
    }
  }

  [System.Serializable]
  [DisplayName("Reflection Bounces (Builtin & URP)")]
  public class Setting_ReflectionBounces : Setting_Render<int>
  {
    public override string PropertyName => nameof(RenderSettings.reflectionBounces);

    public Setting_ReflectionBounces()
    {
      options.Add(new StringToStringRelation("1", "One"));
      options.Add(new StringToStringRelation("2", "Two"));
      options.Add(new StringToStringRelation("3", "Three"));
      options.Add(new StringToStringRelation("4", "Four"));
      options.Add(new StringToStringRelation("5", "Five"));

      defaultValue = 1;
    }
  }

  [System.Serializable]
  [DisplayName("Reflection Intensity (Builtin & URP)")]
  public class Setting_ReflectionIntensity : Setting_Render<float>
  {
    public override string PropertyName => nameof(RenderSettings.reflectionIntensity);

    public Setting_ReflectionIntensity()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 1;
    }
  }

  // TODO Enable once color support is added
  //[System.Serializable]
  //[DisplayName("Subtractive Shadow Color (Builtin & URP)")]
  //public class Setting_SubtractiveShadowColor : Setting_Render<Color>
  //{
  //  public override string PropertyName => nameof(RenderSettings.subtractiveShadowColor);
  //}
}