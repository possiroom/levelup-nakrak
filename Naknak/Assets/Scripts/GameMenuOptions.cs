using System;
using System.Collections.Generic;
using CitrioN.SettingsMenuCreator;
using UnityEngine;

/// <summary>Uses the purchased Pro XML saver; shares one options file between title and game menus.</summary>
public sealed class GameMenuOptions : IDisposable
{
    private readonly SettingsCollection collection;
    private readonly SettingsSaver_Xml saver;
    public float Volume { get; private set; } = 0.8f;
    public bool Fullscreen { get; private set; }
    public int ResolutionIndex { get; private set; }
    public readonly List<Vector2Int> Resolutions = new List<Vector2Int>();

    public GameMenuOptions()
    {
        foreach (var resolution in Screen.resolutions)
        {
            var size = new Vector2Int(resolution.width, resolution.height);
            if (size.x >= 960 && size.y >= 540 && !Resolutions.Contains(size)) Resolutions.Add(size);
        }
        if (Resolutions.Count == 0) Resolutions.Add(new Vector2Int(Mathf.Max(960, Screen.width), Mathf.Max(540, Screen.height)));
        Resolutions.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
        ResolutionIndex = Resolutions.Count - 1;
        Fullscreen = Screen.fullScreen;
        collection = ScriptableObject.CreateInstance<SettingsCollection>();
        saver = ScriptableObject.CreateInstance<SettingsSaver_Xml>();
        // Dedicated saver avoids collisions with other CitrioN settings collections.
        JsonUtility.FromJsonOverwrite("{\"fileName\":\"NaknakOptions\",\"directoryName\":\"Naknak\"}", saver);
        collection.SettingsSaver = saver;
        Dictionary<string, object> values = null;
        try { values = saver.LoadSettings(); }
        catch (Exception exception) { Debug.LogWarning("[Options] 기본 설정을 사용합니다: " + exception.Message); }
        if (values != null)
        {
            if (values.TryGetValue("MasterVolume", out var volume) && volume is float savedVolume && !float.IsNaN(savedVolume)) Volume = Mathf.Clamp01(savedVolume);
            if (values.TryGetValue("Fullscreen", out var fullscreen) && fullscreen is bool savedFullscreen) Fullscreen = savedFullscreen;
            if (values.TryGetValue("Width", out var width) && width is int savedWidth && values.TryGetValue("Height", out var height) && height is int savedHeight)
            {
                int index = Resolutions.IndexOf(new Vector2Int(savedWidth, savedHeight));
                if (index >= 0) ResolutionIndex = index;
            }
        }
        AudioListener.volume = Volume;
        // Do not resize the editor Game view or switch display modes during startup.
        if (!Application.isEditor) ApplyDisplay();
    }

    public void AdjustVolume(int step) { Volume = Mathf.Clamp01(Volume + step * 0.05f); AudioListener.volume = Volume; }
    public void AdjustResolution(int step) { ResolutionIndex = (ResolutionIndex + step + Resolutions.Count) % Resolutions.Count; }
    public void ToggleFullscreen() => Fullscreen = !Fullscreen;
    public void Reset() { Volume = 0.8f; Fullscreen = true; ResolutionIndex = Resolutions.Count - 1; AudioListener.volume = Volume; }

    public void Save()
    {
        var size = Resolutions[ResolutionIndex];
        collection.activeSettingValues["MasterVolume"] = Volume;
        collection.activeSettingValues["Fullscreen"] = Fullscreen;
        collection.activeSettingValues["Width"] = size.x;
        collection.activeSettingValues["Height"] = size.y;
        saver.SaveSettings(collection);
        if (!Application.isEditor) ApplyDisplay();
    }

    private void ApplyDisplay()
    {
        var size = Resolutions[ResolutionIndex];
        Screen.SetResolution(size.x, size.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public void Dispose() { UnityEngine.Object.Destroy(collection); UnityEngine.Object.Destroy(saver); }
}
