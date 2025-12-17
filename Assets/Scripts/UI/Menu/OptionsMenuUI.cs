using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Required for List<>

/// <summary>
/// OptionsMenuUI - Controller for Options Menu canvas.
/// Manages settings UI elements (sliders, dropdowns, toggles).
/// Epic 5 Story 5.1 - Options Menu.
/// Pattern 2: Scene-specific (no DontDestroyOnLoad).
/// Pattern 7: Canvas switching via MenuManager.
/// </summary>
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Audio Settings UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Graphics Settings UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    // Working copy of settings (modified by UI, not saved until Apply)
    private SettingsData workingSettings;

    /// <summary>
    /// OnEnable - Setup UI when canvas is shown (AC3).
    /// Always sets up buttons and loads current settings.
    /// </summary>
    private void OnEnable()
    {
        Debug.Log($"[OptionsMenuUI] OnEnable called. SettingsManager={(SettingsManager.Instance != null ? "exists" : "null")}");

        // Always setup buttons (needed for clicks to work)
        SetupButtons();

        // Load current settings if SettingsManager is ready
        if (SettingsManager.Instance != null)
        {
            LoadCurrentSettings();
        }
        else
        {
            Debug.LogWarning("[OptionsMenuUI] SettingsManager not ready in OnEnable, will try in Start()");
        }
    }

    /// <summary>
    /// Start - Fallback to load settings if OnEnable ran before SettingsManager was ready.
    /// </summary>
    private void Start()
    {
        Debug.Log($"[OptionsMenuUI] Start called. workingSettings={(workingSettings != null ? "exists" : "null")}");

        // If settings weren't loaded in OnEnable (because SettingsManager wasn't ready), load them now
        if (workingSettings == null && SettingsManager.Instance != null)
        {
            Debug.Log("[OptionsMenuUI] Loading settings in Start() as fallback");
            LoadCurrentSettings();
        }
    }

    /// <summary>
    /// Load current settings from SettingsManager and populate UI (AC3).
    /// </summary>
    private void LoadCurrentSettings()
    {
        // Validate SettingsManager exists
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[OptionsMenuUI] SettingsManager.Instance is null - cannot load settings");
            return;
        }

        // Check if CurrentSettings is null
        if (SettingsManager.Instance.CurrentSettings == null)
        {
            Debug.LogError("[OptionsMenuUI] SettingsManager.CurrentSettings is null - cannot clone");
            return;
        }

        // Clone current settings to working copy (so we can cancel changes)
        workingSettings = SettingsManager.Instance.CurrentSettings.Clone();

        if (workingSettings == null)
        {
            Debug.LogError("[OptionsMenuUI] workingSettings is null after Clone() - something went wrong");
            return;
        }

        Debug.Log($"[OptionsMenuUI] Settings cloned: Master={workingSettings.masterVolume:F2}, ResolutionIndex={workingSettings.resolutionIndex}");

        // Populate UI with current values
        PopulateUI();

        Debug.Log("[OptionsMenuUI] Settings loaded into UI");
    }

    /// <summary>
    /// Populate UI elements with settings values (AC3, AC4, AC5).
    /// Includes Auto-Detection for Resolution on first run.
    /// </summary>
    private void PopulateUI()
    {
        // Audio sliders (AC4) - Map 0.0-1.0
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = workingSettings.masterVolume;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = workingSettings.musicVolume;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = workingSettings.sfxVolume;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.value = workingSettings.voiceVolume;
            voiceVolumeSlider.onValueChanged.RemoveAllListeners();
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        // Resolution dropdown (AC5) with Auto-Detect Logic
        if (resolutionDropdown != null)
        {
            // 1. Get available resolution strings from manager
            resolutionDropdown.ClearOptions();
            string[] resolutions = SettingsManager.Instance.GetAvailableResolutions();
            resolutionDropdown.AddOptions(new List<string>(resolutions));

            // 2. Check for "First Run" flag (-1)
            if (workingSettings.resolutionIndex == -1)
            {
                // Auto-detect based on current screen hardware
                int nativeIndex = GetAutoDetectedResolutionIndex(resolutions);

                // Update working settings immediately
                workingSettings.resolutionIndex = nativeIndex;
                Debug.Log($"[OptionsMenuUI] Auto-detected resolution index: {nativeIndex} ({resolutions[nativeIndex]})");
            }

            // 3. Safety Clamp to ensure we don't crash if index is out of bounds
            workingSettings.resolutionIndex = Mathf.Clamp(workingSettings.resolutionIndex, 0, resolutions.Length - 1);

            // 4. Set UI value
            resolutionDropdown.value = workingSettings.resolutionIndex;
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        // Fullscreen toggle (AC5)
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = workingSettings.fullscreen;
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    /// <summary>
    /// Helper to find the index of the user's current screen resolution in the available list.
    /// </summary>
    private int GetAutoDetectedResolutionIndex(string[] availableResolutions)
    {
        // Get the current screen resolution (native monitor resolution)
        Resolution currentRes = Screen.currentResolution;

        // Construct the expected string format (Width x Height)
        string targetResString = $"{currentRes.width} x {currentRes.height}";

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            // Check if the dropdown option contains the width and height
            if (availableResolutions[i].Contains(targetResString))
            {
                return i;
            }
        }

        // Fallback: If exact match not found, return the last one (usually highest/best res)
        return Mathf.Max(0, availableResolutions.Length - 1);
    }

    /// <summary>
    /// Setup button click listeners (AC8).
    /// </summary>
    private void SetupButtons()
    {
        if (applyButton != null)
        {
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(OnApplyClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    // ============================================================================
    // UI Event Handlers - Update working settings when user changes values
    // ============================================================================

    /// <summary>
    /// Master volume slider changed (AC4).
    /// Applies live to AudioManager for immediate feedback.
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        workingSettings.masterVolume = value;

        // Apply live
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }

    /// <summary>
    /// Music volume slider changed (AC4).
    /// Applies live to AudioManager for immediate feedback.
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        workingSettings.musicVolume = value;

        // Apply live
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    /// <summary>
    /// SFX volume slider changed (AC4).
    /// Applies live to AudioManager for immediate feedback.
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        workingSettings.sfxVolume = value;

        // Apply live
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    /// <summary>
    /// Voice volume slider changed.
    /// Applies live to AudioManager for immediate feedback.
    /// </summary>
    private void OnVoiceVolumeChanged(float value)
    {
        workingSettings.voiceVolume = value;

        // Apply live
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVoiceVolume(value);
    }

    /// <summary>
    /// Resolution dropdown changed (AC5).
    /// </summary>
    private void OnResolutionChanged(int index)
    {
        workingSettings.resolutionIndex = index;
        Debug.Log($"[OptionsMenuUI] Resolution changed: Index {index}");
    }

    /// <summary>
    /// Fullscreen toggle changed (AC5).
    /// </summary>
    private void OnFullscreenChanged(bool isFullscreen)
    {
        workingSettings.fullscreen = isFullscreen;
        Debug.Log($"[OptionsMenuUI] Fullscreen changed: {isFullscreen}");
    }

    // ============================================================================
    // Button Click Handlers (AC8)
    // ============================================================================

    /// <summary>
    /// Apply button clicked - Save and apply settings (AC8).
    /// </summary>
    private void OnApplyClicked()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[OptionsMenuUI] SettingsManager.Instance is null - cannot apply settings");
            return;
        }

        Debug.Log("[OptionsMenuUI] Apply clicked - Saving and applying settings");

        // Update SettingsManager with working settings
        SettingsManager.Instance.UpdateSettings(workingSettings);

        // Save to file
        SettingsManager.Instance.SaveSettings();

        // Apply to Unity systems
        SettingsManager.Instance.ApplySettings();

        // Return to main menu
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowMainMenuCanvas();
        }
    }

    /// <summary>
    /// Back button clicked - Discard changes and return to main menu (AC8).
    /// </summary>
    private void OnBackClicked()
    {
        Debug.Log("[OptionsMenuUI] Back clicked - Discarding changes");

        // Discard working settings (do not save)
        workingSettings = null;

        // Return to main menu
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowMainMenuCanvas();
        }
    }
}