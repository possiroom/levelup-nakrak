using CitrioN.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CitrioN.SettingsMenuCreator
{
  #region Basic
  [MenuOrder(900)]
  [MenuPath("Audio")]
  [System.Serializable]
  public class Setting_GlobalAudioListenerVolume : Setting_Generic_Unity<float>
  {
    public override bool StoreValueInternally => true;

    public override string EditorNamePrefix => "[Audio]";

    public Setting_GlobalAudioListenerVolume()
    {
      options.AddMinMaxRangeValues("0", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 1;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, float value)
    {
      AudioListener.volume = Mathf.Clamp01(value);
      return AudioListener.volume;
    }

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      return new List<object> { AudioListener.volume };
    }
  }
  #endregion

  #region Audio Configuration
  [MenuOrder(900)]
  [MenuPath("Audio")]
  [System.Serializable]
  public abstract class Setting_Audio<T1> : Setting_Generic_Reflection_Field_Unity<T1, AudioConfiguration>
  {
    [SerializeField]
    [Tooltip("Should the current audio system be cached and\n" +
             "then reapplied after the setting change?\n\n" +
             "Can be a heavy process if a lot of AudioSources are\n" +
             "in the game.\n\n" +
             "If disabled all AudioSources currently playing will stop!\n\n" +
             "Default: true")]
    protected bool keepAudioSystemState = true;

    public override string EditorNamePrefix => "[Audio]";

    //protected override object ApplySettingChangeWithValue(SettingsCollection settings, T1 value)
    //{
    //  AudioConfiguration config = AudioSettings.GetConfiguration();
    //  //var newValue = ApplySettingChangeWithValueInternal(config, settings, value);

    //  var newValue = ApplyDelayed(config, settings, value);
    //  ScheduleUtility.InvokeNextFrame(() => ApplyDelayed(config, settings, newValue));

    //  return newValue;
    //}

    //private T1 ApplyDelayed(AudioConfiguration config, SettingsCollection settings, T1 value)
    //{
    //  var newValue = ApplySettingChangeWithValueInternal(config, settings, value);
    //  if (keepAudioSystemState)
    //  {
    //    AudioSourceUtility.PausePlayingAudioSources();
    //  }
    //  AudioSettings.Reset(config);
    //  if (keepAudioSystemState)
    //  {
    //    AudioSourceUtility.UnpausePlayingAudioSources();
    //  }
    //  return (T1)newValue;
    //}

    //protected abstract object ApplySettingChangeWithValueInternal(AudioConfiguration config, SettingsCollection settings, T1 value);

    public override object GetObject(SettingsCollection settings)
    {
      var config = AudioSettings.GetConfiguration();
      return config;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, T1 value)
    {
      var field = FieldInfo;
      if (field == null) { return null; }

      var obj = GetObject(settings);
      if (obj == null) { return null; }

      field.SetValue(obj, value);
      if (obj is AudioConfiguration config)
      {
        if (keepAudioSystemState)
        {
          AudioSourceUtility.PausePlayingAudioSources();
        }
        AudioSettings.Reset(config);
        if (keepAudioSystemState)
        {
          AudioSourceUtility.UnpausePlayingAudioSources();
        }
      }
      var newValue = field.GetValue(obj);
      return newValue;
    }
  }

  [System.Serializable]
  public class Setting_SpeakerMode : Setting_Audio<AudioSpeakerMode>
  {
    public override string FieldName => nameof(AudioConfiguration.speakerMode);

    public Setting_SpeakerMode()
    {
      options.Clear();
      //options.Add(new StringToStringRelation("Raw", "Raw"));
      options.Add(new StringToStringRelation("Mono", "Mono"));
      options.Add(new StringToStringRelation("Stereo", "Stereo"));
      options.Add(new StringToStringRelation("Quad", "Quad"));
      options.Add(new StringToStringRelation("Surround", "Surround"));
      options.Add(new StringToStringRelation("Mode5point1", "Surround 5.1"));
      options.Add(new StringToStringRelation("Mode7point1", "Surround 7.1"));

      // Disabled because it doesn't work and falls back to Stereo
      // and (re)sets the sample rate to 48hz with the following warning:
      // FMOD could not set speaker mode to the one specified in the project settings (7). Falling back to stereo.
      // AudioSettings.driverCapabilities a possible help?
      //options.Add(new StringToStringRelation("Prologic", "Prologic DST"));

      defaultValue = AudioSpeakerMode.Stereo;
    }

    //protected override object ApplySettingChangeWithValueInternal(AudioConfiguration config, SettingsCollection settings, AudioSpeakerMode value)
    //{
    //  config.speakerMode = value;
    //  return config.speakerMode;
    //}

    //public override List<object> GetCurrentValues(SettingsCollection settings)
    //{
    //  return new List<object> { AudioSettings.GetConfiguration().speakerMode };
    //}
  }

  [System.Serializable]
  public class Setting_SampleRate : Setting_Audio<int>
  {
    public override string FieldName => nameof(AudioConfiguration.sampleRate);

    public Setting_SampleRate()
    {
      options.Add(new StringToStringRelation("11025", "11 kHz"));
      options.Add(new StringToStringRelation("22050", "22 kHz"));
      options.Add(new StringToStringRelation("44100", "44.1 kHz"));
      options.Add(new StringToStringRelation("48000", "48 kHz"));
      options.Add(new StringToStringRelation("88200", "88.2 kHz"));
      options.Add(new StringToStringRelation("96000", "96 kHz"));

      defaultValue = 44100;
    }

    //protected override object ApplySettingChangeWithValueInternal(AudioConfiguration config, SettingsCollection settings, int value)
    //{
    //  config.sampleRate = value;

    //  return config.sampleRate;
    //}

    //public override List<object> GetCurrentValues(SettingsCollection settings)
    //{
    //  return new List<object> { AudioSettings.GetConfiguration().sampleRate };
    //}
  }

  //TODO Enable later
  //32, 64, 128, 256, 340, 480, 512, 1024, 2048, 4096, 8192
  //[DisplayName("DSP Buffer Size")]
  //public class Setting_DSPBufferSize : Setting_Audio<int>
  //{
  //  public override string RuntimeName => "DSP Buffer Size";

  //  public override string EditorName => "DSP Buffer Size";

  //  protected override object ApplySettingChangeWithValueInternal(AudioConfiguration config, SettingsCollection settings, int value)
  //  {
  //    config.dspBufferSize = value;
  //    return config.dspBufferSize;
  //  }

  //  public override List<object> GetCurrentValues(SettingsCollection settings)
  //  {
  //    return new List<object> { AudioSettings.GetConfiguration().dspBufferSize };
  //  }
  //}

  [System.Serializable]
  public class Setting_VirtualVoicesAmount : Setting_Audio<int>
  {
    public override string FieldName => nameof(AudioConfiguration.numVirtualVoices);

    public Setting_VirtualVoicesAmount()
    {
      options.Add(new StringToStringRelation("1", "1"));
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
      options.Add(new StringToStringRelation("2048", "2048"));
      options.Add(new StringToStringRelation("4095", "Max"));

      defaultValue = 512;
    }

    //protected override object ApplySettingChangeWithValueInternal(AudioConfiguration config, SettingsCollection settings, int value)
    //{
    //  config.numVirtualVoices = value;
    //  return config.numVirtualVoices;
    //}

    //public override List<object> GetCurrentValues(SettingsCollection settings)
    //{
    //  return new List<object> { AudioSettings.GetConfiguration().numVirtualVoices };
    //}
  }

  [System.Serializable]
  public class Setting_RealVoicesAmount : Setting_Audio<int>
  {
    public override string FieldName => nameof(AudioConfiguration.numRealVoices);

    public Setting_RealVoicesAmount()
    {
      options.Add(new StringToStringRelation("1", "1"));
      options.Add(new StringToStringRelation("2", "2"));
      options.Add(new StringToStringRelation("4", "4"));
      options.Add(new StringToStringRelation("8", "8"));
      options.Add(new StringToStringRelation("16", "16"));
      options.Add(new StringToStringRelation("32", "32"));
      options.Add(new StringToStringRelation("64", "64"));
      options.Add(new StringToStringRelation("128", "128"));
      options.Add(new StringToStringRelation("255", "Max"));

      defaultValue = 32;
    }

    //protected override object ApplySettingChangeWithValueInternal(AudioConfiguration config, SettingsCollection settings, int value)
    //{
    //  config.numRealVoices = value;
    //  return config.numRealVoices;
    //}

    //public override List<object> GetCurrentValues(SettingsCollection settings)
    //{
    //  return new List<object> { AudioSettings.GetConfiguration().numRealVoices };
    //}
  }
  #endregion

  #region Audio Mixer
  [MenuOrder(900)]
  [MenuPath("Audio")]
  [System.Serializable]
  public abstract class Setting_AudioMixer : Setting_Generic_Unity<float>
  {
    [SerializeField]
    [Tooltip("Reference to the AudioMixer to manage.\n\n" +
             "If left blank the AudioMixer specified in\n" +
             "the SettingsCollection will be used.")]
    protected AudioMixer audioMixerOverride;

    [SerializeField]
    [Tooltip("The AudioMixer variable to manage.\n" +
             "Needs to be specified in the AudioMixer")]
    protected string variableName = string.Empty;

    public override string EditorNamePrefix => "[Audio]";

    // Required to store it or it won't load and apply automatically
    //public override bool StoreValueInternally => false;

    protected AudioMixer GetAudioMixer(SettingsCollection settings)
    {
      if (audioMixerOverride != null) { return audioMixerOverride; }
      if (settings != null && settings.AudioMixer != null)
      {
        return settings.AudioMixer;
      }
      ConsoleLogger.LogWarning($"No Audio Mixer referenced for '{GetType().Name}'");
      return null;
    }

    /// <summary>
    /// Gets the value from the mixer and processes it 
    /// to have a proper value for the input element
    /// </summary>
    protected virtual bool GetModifiedValueFromMixer(SettingsCollection settings, out float value)
    {
      var audioMixer = GetAudioMixer(settings);
      if (audioMixer == null || string.IsNullOrEmpty(variableName) ||
         !audioMixer.GetFloat(variableName, out value))
      {
        value = 0f;
        return false;
      }
      return true;
    }

    /// <summary>
    /// Modifies the value the mixer will be provided with
    /// </summary>
    protected virtual float GetMixerValueToSet(SettingsCollection settings, float value) => value;

    public override List<object> GetCurrentValues(SettingsCollection settings)
    {
      if (GetModifiedValueFromMixer(settings, out var value))
      {
        return new List<object>() { value };
      }
      return null;
    }

    protected override object ApplySettingChangeWithValue(SettingsCollection settings, float value)
    {
      var audioMixer = GetAudioMixer(settings);
      if (audioMixer != null)
      {
        // Check if the variable is available
        if (audioMixer.GetFloat(variableName, out var currentValue))
        {
          var newValue = GetMixerValueToSet(settings, value);
          newValue = Mathf.Clamp(newValue, -200f, 20f);
          audioMixer.SetFloat(variableName, newValue);

          CoroutineRunner.Instance.InvokeDelayedByFrames(
            () => { audioMixer.SetFloat(variableName, newValue); }, 1);
        }
      }
      GetModifiedValueFromMixer(settings, out value);
      return value;
    }
  }

  [System.Serializable]
  public class Setting_AudioMixerVolume : Setting_AudioMixer
  {
    public Setting_AudioMixerVolume()
    {
      options.AddMinMaxRangeValues("0.01", "1");
      options.AddStepSize("0.01");
      options.AddOneHundredMultiplierAndPercent();

      defaultValue = 1;
    }

    protected override bool GetModifiedValueFromMixer(SettingsCollection settings, out float value)
    {
      if (!base.GetModifiedValueFromMixer(settings, out value))
      {
        return false;
      }
      value = Mathf.Pow(10, value / 65);
      return true;
    }

    protected override float GetMixerValueToSet(SettingsCollection settings, float value)
    {
      return Mathf.Log10(value) * 65;
    }
  }

  [System.Serializable]
  public class Setting_AudioMixerPitch : Setting_AudioMixer
  {
    public Setting_AudioMixerPitch()
    {
      options.AddMinMaxRangeValues("0.1", "10");
      options.AddStepSize("0.1");

      defaultValue = 1;
    }
  }
  #endregion
}