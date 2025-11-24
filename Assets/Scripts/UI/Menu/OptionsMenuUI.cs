using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Graphics Settings UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    // Working copy of settings (modified by UI, not saved until Apply)
    private SettingsData workingSettings;

    /// <summary>
    /// OnEnable - Load current settings when canvas is shown (AC3).
    /// Pattern 2: Initialize in OnEnable() for canvas that may be disabled on scene load.
    /// </summary>
    private void OnEnable()
    {
        LoadCurrentSettings();
        SetupButtons();
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

        // Clone current settings to working copy (so we can cancel changes)
        workingSettings = SettingsManager.Instance.CurrentSettings.Clone();

        // Populate UI with current values
        PopulateUI();

        Debug.Log("[OptionsMenuUI] Settings loaded into UI");
    }

    /// <summary>
    /// Populate UI elements with settings values (AC3, AC4, AC5).
    /// </summary>
    private void PopulateUI()
    {
        // Audio sliders (AC4) - Map 0.0-1.0 to 0-100
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

        // Resolution dropdown (AC5)
        if (resolutionDropdown != null)
        {
            // Populate dropdown with available resolutions
            resolutionDropdown.ClearOptions();
            string[] resolutions = SettingsManager.Instance.GetAvailableResolutions();
            resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(resolutions));

            // Set current value
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
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        workingSettings.masterVolume = value;
        Debug.Log($"[OptionsMenuUI] Master Volume changed: {value:F2}");
    }

    /// <summary>
    /// Music volume slider changed (AC4).
    /// </summary>
    private void OnMusicVolumeChanged(float value)
    {
        workingSettings.musicVolume = value;
        Debug.Log($"[OptionsMenuUI] Music Volume changed: {value:F2}");
    }

    /// <summary>
    /// SFX volume slider changed (AC4).
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        workingSettings.sfxVolume = value;
        Debug.Log($"[OptionsMenuUI] SFX Volume changed: {value:F2}");
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
