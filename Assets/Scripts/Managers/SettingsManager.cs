using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// SettingsManager - Persistent singleton for game settings management.
/// Handles loading, saving, and applying settings.
/// Epic 5 Story 5.1 - Options Menu.
/// Pattern 2: Persistent singleton with DontDestroyOnLoad (unlike scene-specific managers).
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const int MinResolutionWidth = 800;
    private const int MinResolutionHeight = 600;

    [Serializable]
    private class LegacySettingsData
    {
        public int resolutionIndex = -1;
    }

    public static SettingsManager Instance { get; private set; }

    // Current settings data
    private SettingsData currentSettings;

    // Settings file path
    private string settingsFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void ApplySavedGraphicsBeforeSplash()
    {
        ApplyStartupGraphicsSettings();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedGraphicsBeforeFirstScene()
    {
        ApplyStartupGraphicsSettings();
    }

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
        Resolution nativeResolution = GetFallbackAllowedResolution();
        defaults.resolutionWidth = nativeResolution.width;
        defaults.resolutionHeight = nativeResolution.height;

        // Set fullscreen to match current state
        defaults.fullscreen = Screen.fullScreen;
        defaults.windowMode = GetCurrentWindowMode();
        defaults.fullscreen = defaults.windowMode == WindowMode.Fullscreen;

        Debug.Log($"[SettingsManager] Default resolution set to: {defaults.resolutionWidth}x{defaults.resolutionHeight}");
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
                    SaveSettings();
                    return;
                }

                // Deserialize to SettingsData
                currentSettings = JsonUtility.FromJson<SettingsData>(json);

                // JsonUtility can return null on invalid JSON
                if (currentSettings == null)
                {
                    Debug.LogWarning($"[SettingsManager] Settings file invalid, using defaults");
                    currentSettings = CreateDefaultSettings();
                    SaveSettings();
                    return;
                }

                if (NormalizeLoadedSettings(json))
                {
                    SaveSettings();
                }

                Debug.Log($"[SettingsManager] Settings loaded from: {settingsFilePath}");
            }
            else
            {
                // No settings file found, use defaults
                currentSettings = CreateDefaultSettings();
                SaveSettings();

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

    private static void ApplyStartupGraphicsSettings()
    {
#if UNITY_EDITOR
        return;
#else
        SettingsData startupSettings = LoadStartupSettings();
        if (startupSettings == null)
        {
            return;
        }

        if (!HasAllowedResolution(startupSettings))
        {
            Resolution fallbackResolution = GetFallbackAllowedResolutionStatic();
            startupSettings.resolutionWidth = fallbackResolution.width;
            startupSettings.resolutionHeight = fallbackResolution.height;
        }

        startupSettings.fullscreen = startupSettings.windowMode == WindowMode.Fullscreen;
        Screen.SetResolution(
            startupSettings.resolutionWidth,
            startupSettings.resolutionHeight,
            ToUnityFullScreenMode(startupSettings.windowMode));

        if (startupSettings.windowMode == WindowMode.BorderlessWindow)
        {
            ApplyBorderlessWindow(startupSettings.resolutionWidth, startupSettings.resolutionHeight);
        }
        else if (startupSettings.windowMode == WindowMode.Windowed)
        {
            ApplyDecoratedWindow(startupSettings.resolutionWidth, startupSettings.resolutionHeight);
        }
#endif
    }

    private static SettingsData LoadStartupSettings()
    {
        try
        {
            string startupSettingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
            if (!File.Exists(startupSettingsPath))
            {
                return null;
            }

            string json = File.ReadAllText(startupSettingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            SettingsData startupSettings = JsonUtility.FromJson<SettingsData>(json);
            if (startupSettings == null)
            {
                return null;
            }

            NormalizeStartupWindowMode(startupSettings, json);
            return startupSettings;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SettingsManager] Startup graphics settings could not be loaded: {ex.Message}");
            return null;
        }
    }

    private static void NormalizeStartupWindowMode(SettingsData settings, string json)
    {
        if (settings == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(json) || !json.Contains("windowMode") || !Enum.IsDefined(typeof(WindowMode), settings.windowMode))
        {
            settings.windowMode = settings.fullscreen ? WindowMode.BorderlessWindow : WindowMode.Windowed;
        }

        settings.fullscreen = settings.windowMode == WindowMode.Fullscreen;
    }

    /// <summary>
    /// Apply audio settings via AudioManager (uses AudioMixer).
    /// Falls back to AudioListener.volume if AudioManager not available.
    /// </summary>
    private void ApplyAudioSettings()
    {
        if (AudioManager.Instance != null)
        {
            // Use AudioManager (preferred - uses AudioMixer)
            AudioManager.Instance.ApplySettingsVolumes();
            Debug.Log($"[SettingsManager] Audio applied via AudioManager: Master={currentSettings.masterVolume:F2}, Music={currentSettings.musicVolume:F2}, SFX={currentSettings.sfxVolume:F2}");
        }
        else
        {
            // Fallback: Master Volume only via AudioListener
            AudioListener.volume = currentSettings.masterVolume;
            Debug.Log($"[SettingsManager] Audio applied (fallback): Master={currentSettings.masterVolume:F2}");
        }
    }

    /// <summary>
    /// Apply graphics settings (AC5).
    /// Sets resolution and fullscreen mode.
    /// </summary>
    private void ApplyGraphicsSettings()
    {
        if (!HasAllowedResolution(currentSettings))
        {
            Resolution defaultResolution = GetFallbackAllowedResolution();
            currentSettings.resolutionWidth = defaultResolution.width;
            currentSettings.resolutionHeight = defaultResolution.height;
            Debug.LogWarning($"[SettingsManager] Invalid saved resolution. Using default {defaultResolution.width}x{defaultResolution.height}.");
        }

        currentSettings.fullscreen = currentSettings.windowMode == WindowMode.Fullscreen;
        FullScreenMode fullscreenMode = ToUnityFullScreenMode(currentSettings.windowMode);

        // Apply resolution and window mode
        Screen.SetResolution(currentSettings.resolutionWidth, currentSettings.resolutionHeight, fullscreenMode);
        if (currentSettings.windowMode == WindowMode.BorderlessWindow)
        {
            StartCoroutine(ApplyBorderlessWindowNextFrame(currentSettings.resolutionWidth, currentSettings.resolutionHeight));
        }
        else if (currentSettings.windowMode == WindowMode.Windowed)
        {
            StartCoroutine(ApplyDecoratedWindowNextFrame(currentSettings.resolutionWidth, currentSettings.resolutionHeight));
        }

        Debug.Log($"[SettingsManager] Graphics applied: {currentSettings.resolutionWidth}x{currentSettings.resolutionHeight} WindowMode={currentSettings.windowMode}");
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

        NormalizeSettings(newSettings, JsonUtility.ToJson(newSettings));
        currentSettings = newSettings;
        Debug.Log($"[SettingsManager] Settings updated: Master={newSettings.masterVolume:F2}, Resolution={newSettings.resolutionWidth}x{newSettings.resolutionHeight}, WindowMode={newSettings.windowMode}");
    }

    /// <summary>
    /// Get available resolutions for dropdown (AC5).
    /// Returns array of resolution strings like "1920x1080".
    /// </summary>
    public string[] GetAvailableResolutions()
    {
        Resolution[] resolutions = GetDistinctAvailableResolutions();
        string[] resolutionStrings = new string[resolutions.Length];

        for (int i = 0; i < resolutions.Length; i++)
        {
            resolutionStrings[i] = $"{resolutions[i].width}x{resolutions[i].height}";
        }

        return resolutionStrings;
    }

    public Resolution[] GetDistinctAvailableResolutions()
    {
        Resolution[] screenResolutions = Screen.resolutions;
        List<Resolution> uniqueResolutions = new List<Resolution>();

        for (int i = 0; i < screenResolutions.Length; i++)
        {
            Resolution resolution = screenResolutions[i];
            if (!IsAllowedResolution(resolution.width, resolution.height))
            {
                continue;
            }

            if (!ContainsResolution(uniqueResolutions, resolution.width, resolution.height))
            {
                uniqueResolutions.Add(resolution);
            }
        }

        if (uniqueResolutions.Count == 0)
        {
            uniqueResolutions.Add(GetMinimumResolution());
        }

        uniqueResolutions.Sort((a, b) =>
        {
            int areaComparison = (a.width * a.height).CompareTo(b.width * b.height);
            return areaComparison != 0 ? areaComparison : a.width.CompareTo(b.width);
        });

        return uniqueResolutions.ToArray();
    }

    public bool TryGetResolutionAtIndex(int index, out Resolution resolution)
    {
        Resolution[] resolutions = GetDistinctAvailableResolutions();
        if (index < 0 || index >= resolutions.Length)
        {
            resolution = GetFallbackAllowedResolution();
            return false;
        }

        resolution = resolutions[index];
        return true;
    }

    public bool IsDefaultDeviceResolution(int width, int height)
    {
        Resolution defaultResolution = GetDefaultDeviceResolution();
        return width >= defaultResolution.width && height >= defaultResolution.height;
    }

    public static string[] GetWindowModeOptions()
    {
        return new[]
        {
            "Fullscreen",
            "Borderless Window",
            "Windowed"
        };
    }

    public static int GetWindowModeIndex(WindowMode windowMode)
    {
        return Mathf.Clamp((int)windowMode, 0, GetWindowModeOptions().Length - 1);
    }

    public static WindowMode GetWindowModeAtIndex(int index)
    {
        if (index < 0 || index >= GetWindowModeOptions().Length)
        {
            return WindowMode.BorderlessWindow;
        }

        return (WindowMode)index;
    }

    public static bool NormalizeSettings(SettingsData settings, string json)
    {
        if (settings == null)
        {
            return false;
        }

        bool changed = false;
        if (string.IsNullOrEmpty(json) || !json.Contains("tutorialEnabled"))
        {
            settings.tutorialEnabled = true;
            changed = true;
        }

        string normalizedPlayerName = SettingsData.NormalizePlayerName(settings.playerName);
        if (string.IsNullOrEmpty(json) || !json.Contains("playerName") || settings.playerName != normalizedPlayerName)
        {
            settings.playerName = normalizedPlayerName;
            changed = true;
        }

        return changed;
    }

    public int GetResolutionIndex(int width, int height)
    {
        Resolution[] resolutions = GetDistinctAvailableResolutions();
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                return i;
            }
        }

        return GetResolutionIndex(GetDefaultDeviceResolution());
    }

    private int GetResolutionIndex(Resolution targetResolution)
    {
        Resolution[] resolutions = GetDistinctAvailableResolutions();
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == targetResolution.width && resolutions[i].height == targetResolution.height)
            {
                return i;
            }
        }

        return Mathf.Max(0, resolutions.Length - 1);
    }

    private bool NormalizeLoadedSettings(string json)
    {
        bool changed = NormalizeLoadedWindowMode(json);
        changed |= NormalizeSettings(currentSettings, json);

        if (HasAllowedResolution(currentSettings))
        {
            return changed;
        }

        LegacySettingsData legacySettings = JsonUtility.FromJson<LegacySettingsData>(json);
        if (legacySettings != null &&
            TryGetLegacyResolutionAtIndex(legacySettings.resolutionIndex, out Resolution legacyResolution) &&
            IsAllowedResolution(legacyResolution.width, legacyResolution.height))
        {
            currentSettings.resolutionWidth = legacyResolution.width;
            currentSettings.resolutionHeight = legacyResolution.height;
            Debug.Log($"[SettingsManager] Migrated legacy resolution index {legacySettings.resolutionIndex} to {legacyResolution.width}x{legacyResolution.height}");
            return true;
        }

        Resolution defaultResolution = GetFallbackAllowedResolution();
        currentSettings.resolutionWidth = defaultResolution.width;
        currentSettings.resolutionHeight = defaultResolution.height;
        Debug.Log($"[SettingsManager] Filled missing resolution with default {defaultResolution.width}x{defaultResolution.height}");
        return true;
    }

    private bool NormalizeLoadedWindowMode(string json)
    {
        WindowMode originalMode = currentSettings.windowMode;
        bool originalFullscreen = currentSettings.fullscreen;

        if (string.IsNullOrEmpty(json) || !json.Contains("windowMode") || !Enum.IsDefined(typeof(WindowMode), currentSettings.windowMode))
        {
            currentSettings.windowMode = currentSettings.fullscreen ? WindowMode.BorderlessWindow : WindowMode.Windowed;
        }

        currentSettings.fullscreen = currentSettings.windowMode == WindowMode.Fullscreen;
        return originalMode != currentSettings.windowMode || originalFullscreen != currentSettings.fullscreen;
    }

    private bool TryGetLegacyResolutionAtIndex(int index, out Resolution resolution)
    {
        Resolution[] resolutions = Screen.resolutions;
        if (index < 0 || index >= resolutions.Length)
        {
            resolution = GetDefaultDeviceResolution();
            return false;
        }

        resolution = resolutions[index];
        return resolution.width > 0 && resolution.height > 0;
    }

    private static bool HasAllowedResolution(SettingsData settings)
    {
        return settings != null && IsAllowedResolution(settings.resolutionWidth, settings.resolutionHeight);
    }

    private static bool IsAllowedResolution(int width, int height)
    {
        return width >= MinResolutionWidth && height >= MinResolutionHeight;
    }

    private static bool ContainsResolution(List<Resolution> resolutions, int width, int height)
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                return true;
            }
        }

        return false;
    }

    private Resolution GetDefaultDeviceResolution()
    {
        Resolution currentResolution = Screen.currentResolution;
        if (currentResolution.width > 0 && currentResolution.height > 0)
        {
            return currentResolution;
        }

        Resolution[] resolutions = Screen.resolutions;
        Resolution largestResolution = default;
        int largestArea = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int area = resolutions[i].width * resolutions[i].height;
            if (area > largestArea)
            {
                largestResolution = resolutions[i];
                largestArea = area;
            }
        }

        if (largestResolution.width > 0 && largestResolution.height > 0)
        {
            return largestResolution;
        }

        largestResolution.width = Mathf.Max(Screen.width, 1);
        largestResolution.height = Mathf.Max(Screen.height, 1);
        return largestResolution;
    }

    private Resolution GetFallbackAllowedResolution()
    {
        Resolution defaultResolution = GetDefaultDeviceResolution();
        if (IsAllowedResolution(defaultResolution.width, defaultResolution.height))
        {
            return defaultResolution;
        }

        Resolution[] resolutions = GetDistinctAvailableResolutions();
        return resolutions.Length > 0 ? resolutions[0] : GetMinimumResolution();
    }

    private static Resolution GetFallbackAllowedResolutionStatic()
    {
        Resolution defaultResolution = GetDefaultDeviceResolutionStatic();
        if (IsAllowedResolution(defaultResolution.width, defaultResolution.height))
        {
            return defaultResolution;
        }

        Resolution[] resolutions = Screen.resolutions;
        Resolution smallestAllowedResolution = default;
        int smallestAllowedArea = int.MaxValue;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];
            if (!IsAllowedResolution(resolution.width, resolution.height))
            {
                continue;
            }

            int area = resolution.width * resolution.height;
            if (area < smallestAllowedArea)
            {
                smallestAllowedResolution = resolution;
                smallestAllowedArea = area;
            }
        }

        return smallestAllowedResolution.width > 0 ? smallestAllowedResolution : GetMinimumResolution();
    }

    private static Resolution GetDefaultDeviceResolutionStatic()
    {
        Resolution currentResolution = Screen.currentResolution;
        if (currentResolution.width > 0 && currentResolution.height > 0)
        {
            return currentResolution;
        }

        Resolution[] resolutions = Screen.resolutions;
        Resolution largestResolution = default;
        int largestArea = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int area = resolutions[i].width * resolutions[i].height;
            if (area > largestArea)
            {
                largestResolution = resolutions[i];
                largestArea = area;
            }
        }

        if (largestResolution.width > 0 && largestResolution.height > 0)
        {
            return largestResolution;
        }

        largestResolution.width = Mathf.Max(Screen.width, 1);
        largestResolution.height = Mathf.Max(Screen.height, 1);
        return largestResolution;
    }

    private static Resolution GetMinimumResolution()
    {
        Resolution minimumResolution = default;
        minimumResolution.width = MinResolutionWidth;
        minimumResolution.height = MinResolutionHeight;
        return minimumResolution;
    }

    private static FullScreenMode ToUnityFullScreenMode(WindowMode windowMode)
    {
        switch (windowMode)
        {
            case WindowMode.Fullscreen:
                return FullScreenMode.ExclusiveFullScreen;
            case WindowMode.Windowed:
                return FullScreenMode.Windowed;
            default:
                return FullScreenMode.Windowed;
        }
    }

    private static WindowMode GetCurrentWindowMode()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return WindowMode.Fullscreen;
            case FullScreenMode.Windowed:
                return WindowMode.Windowed;
            default:
                return WindowMode.BorderlessWindow;
        }
    }

    private IEnumerator ApplyBorderlessWindowNextFrame(int width, int height)
    {
        yield return null;
        ApplyBorderlessWindow(width, height);
    }

    private IEnumerator ApplyDecoratedWindowNextFrame(int width, int height)
    {
        yield return null;
        ApplyDecoratedWindow(width, height);
    }

    private static void ApplyBorderlessWindow(int width, int height)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        NativeBorderlessWindow.Apply(width, height);
#endif
    }

    private static void ApplyDecoratedWindow(int width, int height)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        NativeBorderlessWindow.Restore(width, height);
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private static class NativeBorderlessWindow
    {
        private const int GWL_STYLE = -16;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public static void Apply(int width, int height)
        {
            IntPtr window = GetActiveWindow();
            if (window == IntPtr.Zero)
            {
                return;
            }

            SetWindowLongPtr(window, GWL_STYLE, new IntPtr(unchecked((int)(WS_POPUP | WS_VISIBLE))));
            SetWindowPos(window, IntPtr.Zero, GetCenteredX(width), GetCenteredY(height), width, height, SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        public static void Restore(int width, int height)
        {
            IntPtr window = GetActiveWindow();
            if (window == IntPtr.Zero)
            {
                return;
            }

            SetWindowLongPtr(window, GWL_STYLE, new IntPtr(unchecked((int)(WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_VISIBLE))));
            SetWindowPos(window, IntPtr.Zero, GetCenteredX(width), GetCenteredY(height), width, height, SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        private static int GetCenteredX(int width)
        {
            return Math.Max(0, (GetSystemMetrics(0) - width) / 2);
        }

        private static int GetCenteredY(int height)
        {
            return Math.Max(0, (GetSystemMetrics(1) - height) / 2);
        }
    }
#endif
}
