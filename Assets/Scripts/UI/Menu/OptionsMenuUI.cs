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
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Gameplay Settings UI")]
    [SerializeField] private Toggle tutorialEnabledToggle;

    [Header("Multiplayer Settings UI")]
    [SerializeField] private TMP_InputField playerNameInput;

    [Header("Category Navigation")]
    [SerializeField] private Button audioCategoryButton;
    [SerializeField] private Button graphicsCategoryButton;
    [SerializeField] private Button gameplayCategoryButton;
    [SerializeField] private Button multiplayerCategoryButton;
    [SerializeField] private GameObject audioSection;
    [SerializeField] private GameObject graphicsSection;
    [SerializeField] private GameObject gameplaySection;
    [SerializeField] private GameObject multiplayerSection;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    // Working copy of settings (modified by UI, not saved until Apply)
    private SettingsData workingSettings;
    private GameObject activeSection;

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

        Debug.Log($"[OptionsMenuUI] Settings cloned: Master={workingSettings.masterVolume:F2}, Resolution={workingSettings.resolutionWidth}x{workingSettings.resolutionHeight}, WindowMode={workingSettings.windowMode}");

        // Populate UI with current values
        PopulateUI();
        ShowCategory(audioSection);

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

        // Resolution dropdown (AC5)
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            string[] resolutions = SettingsManager.Instance.GetAvailableResolutions();
            resolutionDropdown.AddOptions(new List<string>(resolutions));

            int selectedIndex = SettingsManager.Instance.GetResolutionIndex(
                workingSettings.resolutionWidth,
                workingSettings.resolutionHeight);

            resolutionDropdown.value = selectedIndex;
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (windowModeDropdown != null)
        {
            windowModeDropdown.ClearOptions();
            windowModeDropdown.AddOptions(new List<string>(SettingsManager.GetWindowModeOptions()));
            windowModeDropdown.value = SettingsManager.GetWindowModeIndex(workingSettings.windowMode);
            windowModeDropdown.onValueChanged.RemoveAllListeners();
            windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.gameObject.SetActive(windowModeDropdown == null);
            fullscreenToggle.isOn = workingSettings.windowMode == WindowMode.Fullscreen;
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (tutorialEnabledToggle != null)
        {
            tutorialEnabledToggle.SetIsOnWithoutNotify(workingSettings.tutorialEnabled);
            tutorialEnabledToggle.onValueChanged.RemoveAllListeners();
            tutorialEnabledToggle.onValueChanged.AddListener(OnTutorialEnabledChanged);
        }

        if (playerNameInput != null)
        {
            playerNameInput.characterLimit = SettingsData.MaxPlayerNameLength;
            playerNameInput.SetTextWithoutNotify(SettingsData.NormalizePlayerName(workingSettings.playerName));
            playerNameInput.onValueChanged.RemoveAllListeners();
            playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
            playerNameInput.onEndEdit.RemoveAllListeners();
            playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
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
            backButton.onClick.AddListener(RequestBack);
        }

        SetupCategoryButton(audioCategoryButton, audioSection);
        SetupCategoryButton(graphicsCategoryButton, graphicsSection);
        SetupCategoryButton(gameplayCategoryButton, gameplaySection);
        SetupCategoryButton(multiplayerCategoryButton, multiplayerSection);
    }

    private void SetupCategoryButton(Button button, GameObject section)
    {
        if (button == null || section == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowCategory(section));
    }

    private void ShowCategory(GameObject section)
    {
        if (section == null)
        {
            return;
        }

        SetSectionActive(audioSection, section);
        SetSectionActive(graphicsSection, section);
        SetSectionActive(gameplaySection, section);
        SetSectionActive(multiplayerSection, section);
        activeSection = section;

        UpdateCategoryButtonState(audioCategoryButton, audioSection);
        UpdateCategoryButtonState(graphicsCategoryButton, graphicsSection);
        UpdateCategoryButtonState(gameplayCategoryButton, gameplaySection);
        UpdateCategoryButtonState(multiplayerCategoryButton, multiplayerSection);
    }

    private void SetSectionActive(GameObject section, GameObject active)
    {
        if (section != null)
        {
            section.SetActive(section == active);
        }
    }

    private void UpdateCategoryButtonState(Button button, GameObject section)
    {
        if (button != null)
        {
            button.interactable = section != activeSection;
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
        if (workingSettings == null) return;
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
        if (workingSettings == null) return;
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
        if (workingSettings == null) return;
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
        if (workingSettings == null) return;
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
        if (workingSettings == null) return;

        if (SettingsManager.Instance != null &&
            SettingsManager.Instance.TryGetResolutionAtIndex(index, out Resolution selectedResolution))
        {
            workingSettings.resolutionWidth = selectedResolution.width;
            workingSettings.resolutionHeight = selectedResolution.height;
            if (workingSettings.windowMode == WindowMode.Fullscreen &&
                !SettingsManager.Instance.IsDefaultDeviceResolution(selectedResolution.width, selectedResolution.height))
            {
                workingSettings.windowMode = WindowMode.Windowed;
                workingSettings.fullscreen = false;
                if (windowModeDropdown != null)
                {
                    windowModeDropdown.SetValueWithoutNotify(SettingsManager.GetWindowModeIndex(WindowMode.Windowed));
                }
            }

            Debug.Log($"[OptionsMenuUI] Resolution changed: {selectedResolution.width}x{selectedResolution.height}");
        }
    }

    private void OnWindowModeChanged(int index)
    {
        if (workingSettings == null) return;

        workingSettings.windowMode = SettingsManager.GetWindowModeAtIndex(index);
        if (workingSettings.windowMode == WindowMode.Fullscreen &&
            SettingsManager.Instance != null &&
            !SettingsManager.Instance.IsDefaultDeviceResolution(workingSettings.resolutionWidth, workingSettings.resolutionHeight))
        {
            workingSettings.windowMode = WindowMode.Windowed;
            workingSettings.fullscreen = false;
            if (windowModeDropdown != null)
            {
                windowModeDropdown.SetValueWithoutNotify(SettingsManager.GetWindowModeIndex(WindowMode.Windowed));
            }
        }

        workingSettings.fullscreen = workingSettings.windowMode == WindowMode.Fullscreen;
        Debug.Log($"[OptionsMenuUI] Window mode changed: {workingSettings.windowMode}");
    }

    /// <summary>
    /// Fullscreen toggle changed (AC5).
    /// </summary>
    private void OnFullscreenChanged(bool isFullscreen)
    {
        if (workingSettings == null) return;

        workingSettings.windowMode = isFullscreen ? WindowMode.Fullscreen : WindowMode.Windowed;
        workingSettings.fullscreen = isFullscreen;
        Debug.Log($"[OptionsMenuUI] Fullscreen changed: {isFullscreen}");
    }

    private void OnTutorialEnabledChanged(bool isEnabled)
    {
        if (workingSettings == null) return;

        workingSettings.tutorialEnabled = isEnabled;
    }

    private void OnPlayerNameChanged(string value)
    {
        if (workingSettings == null) return;

        workingSettings.playerName = value;
    }

    private void OnPlayerNameEndEdit(string value)
    {
        if (workingSettings == null) return;

        workingSettings.playerName = SettingsData.NormalizePlayerName(value);
        if (playerNameInput != null)
        {
            playerNameInput.SetTextWithoutNotify(workingSettings.playerName);
        }
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
        ApplySettingsAndReturnToMainMenu();
    }

    private void ApplySettingsAndReturnToMainMenu()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[OptionsMenuUI] SettingsManager.Instance is null - cannot apply settings");
            return;
        }

        if (workingSettings == null)
        {
            Debug.LogWarning("[OptionsMenuUI] No working settings to apply");
            return;
        }

        workingSettings.playerName = SettingsData.NormalizePlayerName(workingSettings.playerName);
        if (playerNameInput != null)
        {
            playerNameInput.SetTextWithoutNotify(workingSettings.playerName);
        }

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
    /// Called when the Options canvas becomes visible.
    /// Since MenuManager uses Canvas.enabled (not SetActive), OnEnable doesn't re-fire,
    /// so this must be called explicitly to reinitialize the UI.
    /// </summary>
    public void OnShow()
    {
        if (SettingsManager.Instance != null)
        {
            LoadCurrentSettings();
        }
    }

    /// <summary>
    /// Back button clicked - Discard changes and return to main menu (AC8).
    /// </summary>
    public void RequestBack()
    {
        if (HasUnsavedChanges())
        {
            Debug.Log("[OptionsMenuUI] Back clicked with unsaved changes - showing confirmation modal");
            ShowUnsavedSettingsModal();
            return;
        }

        Debug.Log("[OptionsMenuUI] Back clicked - no unsaved changes");
        DiscardChangesAndReturnToMainMenu();
    }

    private void ShowUnsavedSettingsModal()
    {
        if (ModalDialog.Instance == null)
        {
            Debug.LogWarning("[OptionsMenuUI] ModalDialog missing; discarding settings and returning to main menu");
            DiscardChangesAndReturnToMainMenu();
            return;
        }

        ModalDialog.Instance.Show(
            "Unsaved Settings",
            "Apply settings before returning, or cancel changes?",
            new[] { "Apply", "Cancel" },
            buttonIndex =>
            {
                if (buttonIndex == 0)
                {
                    ApplySettingsAndReturnToMainMenu();
                }
                else
                {
                    DiscardChangesAndReturnToMainMenu();
                }
            });
    }

    private bool HasUnsavedChanges()
    {
        if (workingSettings == null || SettingsManager.Instance == null || SettingsManager.Instance.CurrentSettings == null)
        {
            return false;
        }

        SettingsData saved = SettingsManager.Instance.CurrentSettings;
        return !Mathf.Approximately(workingSettings.masterVolume, saved.masterVolume) ||
            !Mathf.Approximately(workingSettings.musicVolume, saved.musicVolume) ||
            !Mathf.Approximately(workingSettings.sfxVolume, saved.sfxVolume) ||
            !Mathf.Approximately(workingSettings.voiceVolume, saved.voiceVolume) ||
            workingSettings.resolutionWidth != saved.resolutionWidth ||
            workingSettings.resolutionHeight != saved.resolutionHeight ||
            workingSettings.windowMode != saved.windowMode ||
            workingSettings.fullscreen != saved.fullscreen ||
            workingSettings.tutorialEnabled != saved.tutorialEnabled ||
            SettingsData.NormalizePlayerName(workingSettings.playerName) != SettingsData.NormalizePlayerName(saved.playerName);
    }

    private void DiscardChangesAndReturnToMainMenu()
    {
        Debug.Log("[OptionsMenuUI] Discarding unapplied settings");

        // Remove slider listeners first so reverting values doesn't trigger callbacks
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        if (voiceVolumeSlider != null) voiceVolumeSlider.onValueChanged.RemoveAllListeners();
        if (tutorialEnabledToggle != null) tutorialEnabledToggle.onValueChanged.RemoveAllListeners();
        if (playerNameInput != null)
        {
            playerNameInput.onValueChanged.RemoveAllListeners();
            playerNameInput.onEndEdit.RemoveAllListeners();
        }

        // Revert audio and sliders to saved settings
        if (SettingsManager.Instance != null)
        {
            SettingsData saved = SettingsManager.Instance.CurrentSettings;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(saved.masterVolume);
                AudioManager.Instance.SetMusicVolume(saved.musicVolume);
                AudioManager.Instance.SetSFXVolume(saved.sfxVolume);
                AudioManager.Instance.SetVoiceVolume(saved.voiceVolume);
            }

            // Revert slider positions to saved values
            if (masterVolumeSlider != null) masterVolumeSlider.value = saved.masterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = saved.musicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = saved.sfxVolume;
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = saved.voiceVolume;
            if (tutorialEnabledToggle != null) tutorialEnabledToggle.SetIsOnWithoutNotify(saved.tutorialEnabled);
            if (playerNameInput != null) playerNameInput.SetTextWithoutNotify(SettingsData.NormalizePlayerName(saved.playerName));
        }

        // Discard working settings (do not save)
        workingSettings = null;

        // Return to main menu
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowMainMenuCanvas();
        }
    }
}
