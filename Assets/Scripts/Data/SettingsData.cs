using System;

/// <summary>
/// SettingsData - Serializable data structure for game settings.
/// Saved/loaded as JSON in Application.persistentDataPath/settings.json.
/// Epic 5 Story 5.1 - Options Menu.
/// </summary>
[Serializable]
public class SettingsData
{
    public const string DefaultPlayerName = "Player";
    public const int MaxPlayerNameLength = 24;

    // Audio Settings
    public float masterVolume = 1.0f;  // 0.0 to 1.0 (0% to 100%)
    public float musicVolume = 0.7f;   // 0.0 to 1.0 (0% to 100%)
    public float sfxVolume = 0.8f;     // 0.0 to 1.0 (0% to 100%)
    public float voiceVolume = 1.0f;   // 0.0 to 1.0 (0% to 100%)

    // Graphics Settings
    public int resolutionWidth = 0;
    public int resolutionHeight = 0;
    public WindowMode windowMode = WindowMode.BorderlessWindow;
    public bool fullscreen = true;

    // Gameplay Settings
    public bool tutorialEnabled = true;

    // Multiplayer Settings
    public string playerName = DefaultPlayerName;

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
    public SettingsData(float master, float music, float sfx, float voice, int width, int height, WindowMode mode, bool isFullscreen, bool enableTutorial = true, string multiplayerPlayerName = DefaultPlayerName)
    {
        masterVolume = master;
        musicVolume = music;
        sfxVolume = sfx;
        voiceVolume = voice;
        resolutionWidth = width;
        resolutionHeight = height;
        windowMode = mode;
        fullscreen = isFullscreen;
        tutorialEnabled = enableTutorial;
        playerName = NormalizePlayerName(multiplayerPlayerName);
    }

    public SettingsData(float master, float music, float sfx, float voice, int width, int height, bool isFullscreen)
        : this(master, music, sfx, voice, width, height, isFullscreen ? WindowMode.Fullscreen : WindowMode.Windowed, isFullscreen)
    {
    }

    /// <summary>
    /// Clone settings data (for Cancel/Revert functionality).
    /// </summary>
    public SettingsData Clone()
    {
        return new SettingsData(masterVolume, musicVolume, sfxVolume, voiceVolume, resolutionWidth, resolutionHeight, windowMode, fullscreen, tutorialEnabled, playerName);
    }

    public static string NormalizePlayerName(string value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim();
        return normalized.Length <= MaxPlayerNameLength
            ? normalized
            : normalized.Substring(0, MaxPlayerNameLength);
    }
}

public enum WindowMode
{
    Fullscreen = 0,
    BorderlessWindow = 1,
    Windowed = 2
}
