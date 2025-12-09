using UnityEngine;
using System.IO;

/// <summary>
/// SettingsManager - Persistent singleton for game settings management.
/// Handles loading, saving, and applying settings.
/// Epic 5 Story 5.1 - Options Menu.
/// Pattern 2: Persistent singleton with DontDestroyOnLoad (unlike scene-specific managers).
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // Current settings data
    private SettingsData currentSettings;

    // Settings file path
    private string settingsFilePath;

    /// <summary>
    /// Public accessor for current settings (read-only).
    /// </summary>
    public SettingsData CurrentSettings => currentSettings;

    /// <summary>
    /// Awake - Initialize singleton and load settings (AC2, AC7).
    /// Pattern 2: DontDestroyOnLoad for persistent manager.
    /// </summary>
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SettingsManager] Duplicate SettingsManager detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize settings file path
        settingsFilePath = Path.Combine(Application.persistentDataPath, "settings.json");

        // Auto-load settings on startup (AC2, AC7)
        LoadSettings();
        ApplySettings();

        Debug.Log($"[SettingsManager] SettingsManager initialized. Settings path: {settingsFilePath}");
    }

    /// <summary>
    /// Create default settings with current screen resolution detected.
    /// </summary>
    private SettingsData CreateDefaultSettings()
    {
        SettingsData defaults = new SettingsData();

        // Find current screen resolution in available resolutions
        Resolution currentRes = Screen.currentResolution;
        Resolution[] availableResolutions = Screen.resolutions;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == currentRes.width &&
                availableResolutions[i].height == currentRes.height)
            {
                defaults.resolutionIndex = i;
                Debug.Log($"[SettingsManager] Default resolution set to: {currentRes.width}x{currentRes.height} (index {i})");
                break;
            }
        }

        // Set fullscreen to match current state
        defaults.fullscreen = Screen.fullScreen;

        return defaults;
    }

    /// <summary>
    /// Load settings from JSON file (AC2, AC7).
    /// If file doesn't exist or is corrupted, use defaults.
    /// </summary>
    public void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFilePath))
            {
                // Read JSON file
                string json = File.ReadAllText(settingsFilePath);

                // Check if file is empty or whitespace
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning($"[SettingsManager] Settings file is empty, using defaults");
                    currentSettings = CreateDefaultSettings();
                    return;
                }

                // Deserialize to SettingsData
                currentSettings = JsonUtility.FromJson<SettingsData>(json);

                // JsonUtility can return null on invalid JSON
                if (currentSettings == null)
                {
                    Debug.LogWarning($"[SettingsManager] Settings file invalid, using defaults");
                    currentSettings = CreateDefaultSettings();
                    return;
                }

                Debug.Log($"[SettingsManager] Settings loaded from: {settingsFilePath}");
            }
            else
            {
                // No settings file found, use defaults
                currentSettings = CreateDefaultSettings();

                Debug.LogWarning($"[SettingsManager] No settings file found, using defaults");
            }
        }
        catch (System.Exception ex)
        {
            // File corrupted, use defaults
            Debug.LogError($"[SettingsManager] Failed to load settings: {ex.Message}. Using defaults.");
            currentSettings = CreateDefaultSettings();
        }
    }

    /// <summary>
    /// Save settings to JSON file (AC2, AC7).
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            // Serialize to JSON
            string json = JsonUtility.ToJson(currentSettings, true);

            // Write to file
            File.WriteAllText(settingsFilePath, json);

            Debug.Log($"[SettingsManager] Settings saved to: {settingsFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SettingsManager] Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply settings to Unity systems (AC2).
    /// Updates AudioListener volume and Screen resolution/fullscreen.
    /// </summary>
    public void ApplySettings()
    {
        if (currentSettings == null)
        {
            Debug.LogError("[SettingsManager] Cannot apply null settings");
            return;
        }

        // Apply audio settings (AC4)
        ApplyAudioSettings();

        // Apply graphics settings (AC5)
        ApplyGraphicsSettings();

        Debug.Log("[SettingsManager] Settings applied");
    }

    /// <summary>
    /// Apply audio settings (AC4).
    /// MVP: Only Master Volume uses AudioListener.volume (global).
    /// Music/SFX require AudioMixer (post-MVP).
    /// </summary>
    private void ApplyAudioSettings()
    {
        // Master Volume (MVP implementation using AudioListener)
        AudioListener.volume = currentSettings.masterVolume;

        // TODO: Music and SFX volumes require AudioMixer setup (post-MVP)
        // For now, they're stored but not applied

        Debug.Log($"[SettingsManager] Audio applied: Master={currentSettings.masterVolume:F2}");
    }

    /// <summary>
    /// Apply graphics settings (AC5).
    /// Sets resolution and fullscreen mode.
    /// </summary>
    private void ApplyGraphicsSettings()
    {
        // Get available resolutions
        Resolution[] resolutions = Screen.resolutions;

        // Validate resolution index
        if (currentSettings.resolutionIndex < 0 || currentSettings.resolutionIndex >= resolutions.Length)
        {
            Debug.LogWarning($"[SettingsManager] Invalid resolution index: {currentSettings.resolutionIndex}. Using current resolution.");
            return;
        }

        // Get target resolution
        Resolution targetResolution = resolutions[currentSettings.resolutionIndex];

        // Apply resolution and fullscreen
        Screen.SetResolution(targetResolution.width, targetResolution.height, currentSettings.fullscreen);

        Debug.Log($"[SettingsManager] Graphics applied: {targetResolution.width}x{targetResolution.height} Fullscreen={currentSettings.fullscreen}");
    }

    /// <summary>
    /// Update settings data (called by OptionsMenuUI when user changes values).
    /// Does NOT save or apply until explicitly called.
    /// </summary>
    public void UpdateSettings(SettingsData newSettings)
    {
        if (newSettings == null)
        {
            Debug.LogError("[SettingsManager] UpdateSettings called with null newSettings - aborting");
            return;
        }

        currentSettings = newSettings;
        Debug.Log($"[SettingsManager] Settings updated: Master={newSettings.masterVolume:F2}, Resolution={newSettings.resolutionIndex}");
    }

    /// <summary>
    /// Get available resolutions for dropdown (AC5).
    /// Returns array of resolution strings like "1920x1080".
    /// </summary>
    public string[] GetAvailableResolutions()
    {
        Resolution[] resolutions = Screen.resolutions;
        string[] resolutionStrings = new string[resolutions.Length];

        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionStrings[i] = $"{resolutions[i].width}x{resolutions[i].height}";
        }

        return resolutionStrings;
    }
}
