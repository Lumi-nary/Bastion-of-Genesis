using System;

/// <summary>
/// SettingsData - Serializable data structure for game settings.
/// Saved/loaded as JSON in Application.persistentDataPath/settings.json.
/// Epic 5 Story 5.1 - Options Menu.
/// </summary>
[Serializable]
public class SettingsData
{
    // Audio Settings
    public float masterVolume = 1.0f;  // 0.0 to 1.0 (0% to 100%)
    public float musicVolume = 0.7f;   // 0.0 to 1.0 (0% to 100%)
    public float sfxVolume = 0.8f;     // 0.0 to 1.0 (0% to 100%)

    // Graphics Settings
    public int resolutionIndex = -1;    // Index into Screen.resolutions array
    public bool fullscreen = true;

    /// <summary>
    /// Default constructor with sensible default values.
    /// Used when no settings file exists (first launch).
    /// </summary>
    public SettingsData()
    {
        // Defaults already set in field initializers
    }

    /// <summary>
    /// Constructor with explicit values (for testing or custom defaults).
    /// </summary>
    public SettingsData(float master, float music, float sfx, int resolution, bool isFullscreen)
    {
        masterVolume = master;
        musicVolume = music;
        sfxVolume = sfx;
        resolutionIndex = resolution;
        fullscreen = isFullscreen;
    }

    /// <summary>
    /// Clone settings data (for Cancel/Revert functionality).
    /// </summary>
    public SettingsData Clone()
    {
        return new SettingsData(masterVolume, musicVolume, sfxVolume, resolutionIndex, fullscreen);
    }
}
