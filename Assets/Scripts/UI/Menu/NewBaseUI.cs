using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// NewBaseUI controls the New Base configuration form.
/// Handles auto-generated base names, difficulty selection, and SP/COOP mode toggle.
/// Pattern 7: All canvas switching via MenuManager, no direct canvas manipulation.
/// ADR-7: Sets SaveManager pending data before scene transition.
/// </summary>
public class NewBaseUI : MonoBehaviour
{
    // ============================================================================
    // SERIALIZED FIELDS (Assigned in Unity Inspector)
    // ============================================================================

    [Header("Form Elements")]
    [SerializeField] private TMP_InputField baseNameInputField;
    [SerializeField] private TMP_Dropdown difficultyDropdown; // Currently unused - defaults to Medium
    [SerializeField] private Toggle modeToggle;

    [Header("COOP Panel")]
    [SerializeField] private GameObject coopServerPanel;

    // ============================================================================
    // PRIVATE FIELDS
    // ============================================================================

    /// <summary>
    /// Predefined base names for random selection (10 names for MVP).
    /// Epic 7: Can expand to 50+ names or add procedural generation.
    /// </summary>
    private readonly string[] baseNames = new string[]
    {
        "Colony Alpha",
        "Genesis Base",
        "Outpost Prime",
        "New Eden",
        "Frontier Station",
        "Pioneer Camp",
        "Sanctuary One",
        "Haven Outpost",
        "Terra Nova Base",
        "Horizon Colony"
    };

    private string currentBaseName;
    private GameMode currentMode = GameMode.Singleplayer;

    // Form state tracking
    private string initialBaseName;
    private GameMode initialMode = GameMode.Singleplayer;
    private bool isFormDirty = false;
    private bool hasInitialized = false;

    // ============================================================================
    // LIFECYCLE METHODS
    // ============================================================================

    private void Start()
    {
        hasInitialized = true;

        // If canvas is currently active when Start runs, initialize now
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.enabled)
        {
            InitializeForm();
        }
    }

    private void OnEnable()
    {
        // Only initialize after Start() has run (hasInitialized = true)
        // This prevents initialization during scene load
        if (hasInitialized)
        {
            InitializeForm();
        }
    }

    private void OnDisable()
    {
        // Clean up listeners
        if (baseNameInputField != null)
        {
            baseNameInputField.onValueChanged.RemoveListener(OnBaseNameChanged);
        }
        if (modeToggle != null)
        {
            modeToggle.onValueChanged.RemoveListener(OnModeToggle);
        }
    }

    /// <summary>
    /// Initialize form with default values.
    /// Called on OnEnable() every time NewBaseCanvas is displayed - ensures clean state on return.
    /// </summary>
    private void InitializeForm()
    {
        // Generate random base name
        currentBaseName = GenerateBaseName();
        initialBaseName = currentBaseName;

        if (baseNameInputField != null)
        {
            baseNameInputField.text = currentBaseName;
            baseNameInputField.onValueChanged.RemoveListener(OnBaseNameChanged); // Prevent duplicates
            baseNameInputField.onValueChanged.AddListener(OnBaseNameChanged);
        }
        Debug.Log($"[NewBaseUI] Generated base name: {currentBaseName}");

        // Difficulty defaults to Medium (no selection needed for MVP)
        // Hide or disable the dropdown if it exists in the UI
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = 1; // Medium
            difficultyDropdown.interactable = false; // Disable selection
        }
        Debug.Log("[NewBaseUI] Difficulty defaulted to Medium");

        // Set mode toggle to Singleplayer (false = SP, true = COOP)
        currentMode = GameMode.Singleplayer;
        initialMode = GameMode.Singleplayer;

        if (modeToggle != null)
        {
            modeToggle.isOn = false; // Singleplayer
            modeToggle.onValueChanged.RemoveListener(OnModeToggle); // Prevent duplicates
            modeToggle.onValueChanged.AddListener(OnModeToggle);
        }
        Debug.Log($"[NewBaseUI] Mode initialized: {currentMode}");

        // Hide COOP panel initially (only visible when mode = COOP)
        if (coopServerPanel != null)
        {
            coopServerPanel.SetActive(false);
        }

        // Reset dirty flag
        isFormDirty = false;
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Reset form to default values with new auto-generated name.
    /// Called by MenuManager when showing NewBaseCanvas to ensure clean state.
    /// </summary>
    public void ResetForm()
    {
        InitializeForm();
    }

    /// <summary>
    /// Get current base name (called by CreateBaseButton).
    /// </summary>
    public string GetBaseName()
    {
        return currentBaseName;
    }

    /// <summary>
    /// Get current difficulty (called by CreateBaseButton).
    /// Always returns Medium for MVP - difficulty selection not implemented yet.
    /// </summary>
    public Difficulty GetDifficulty()
    {
        return Difficulty.Medium;
    }

    /// <summary>
    /// Get current game mode (called by CreateBaseButton).
    /// </summary>
    public GameMode GetMode()
    {
        return currentMode;
    }

    // ============================================================================
    // UI EVENT HANDLERS
    // ============================================================================

    /// <summary>
    /// Called when base name input field value changes.
    /// Updates current base name and marks form as dirty.
    /// </summary>
    /// <param name="newName">New base name from input field</param>
    private void OnBaseNameChanged(string newName)
    {
        currentBaseName = newName;
        CheckFormDirty();
    }

    /// <summary>
    /// Called when mode toggle value changes.
    /// Shows/hides COOP server panel based on mode selection.
    /// ADR-7: Mode will be stored in SaveManager.pendingMode by CreateBaseButton.
    /// </summary>
    /// <param name="isCoop">True if COOP mode selected, false for Singleplayer</param>
    private void OnModeToggle(bool isCoop)
    {
        currentMode = isCoop ? GameMode.COOP : GameMode.Singleplayer;
        CheckFormDirty();
        Debug.Log($"[NewBaseUI] Mode selected: {currentMode}");

        // Show/hide COOP server panel
        if (coopServerPanel != null)
        {
            coopServerPanel.SetActive(isCoop);

            if (isCoop)
            {
                Debug.Log("[NewBaseUI] COOP server panel displayed");
            }
        }
    }

    /// <summary>
    /// Check if current form values differ from initial values.
    /// Sets isFormDirty flag when any field changes.
    /// </summary>
    private void CheckFormDirty()
    {
        isFormDirty = currentBaseName != initialBaseName ||
                      currentMode != initialMode;

        if (isFormDirty)
        {
            Debug.Log("[NewBaseUI] Form marked as dirty (modified)");
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    /// <summary>
    /// Generate random base name from predefined list.
    /// Epic 7: Can enhance with procedural generation or user input.
    /// </summary>
    /// <returns>Random base name string</returns>
    private string GenerateBaseName()
    {
        int randomIndex = Random.Range(0, baseNames.Length);
        return baseNames[randomIndex];
    }

    /// <summary>
    /// Validate form (always valid with defaults, no empty fields possible).
    /// Kept for future validation logic (Epic 7: manual name editing).
    /// </summary>
    /// <returns>True if form is valid</returns>
    public bool ValidateForm()
    {
        // All fields have defaults, form is always valid in MVP
        return true;
    }

    /// <summary>
    /// Check if form has been modified from initial state.
    /// Used by BackButton to determine if confirmation dialog should be shown.
    /// </summary>
    /// <returns>True if any field has been changed</returns>
    public bool IsFormDirty()
    {
        return isFormDirty;
    }

    /// <summary>
    /// Show confirmation dialog when user tries to go back with unsaved changes.
    /// Story 3.3: Uses ModalDialog system instead of old confirmationDialog GameObject.
    /// </summary>
    public void ShowConfirmationDialog()
    {
        // Story 3.3: Use ModalDialog instead of old GameObject system
        if (ModalDialog.Instance != null)
        {
            ModalDialog.Instance.ShowConfirmation(
                message: "You have unsaved changes. Discard changes and return to main menu?",
                onConfirm: OnConfirmBack,
                onCancel: OnCancelBack
            );
            Debug.Log("[NewBaseUI] Confirmation dialog displayed via ModalDialog");
        }
        else
        {
            Debug.LogError("[NewBaseUI] ModalDialog.Instance is null - cannot show confirmation");
        }
    }

    /// <summary>
    /// Called when user confirms going back (Confirm button in ModalDialog).
    /// Returns to main menu without saving changes.
    /// Story 3.3: ModalDialog closes itself automatically.
    /// </summary>
    public void OnConfirmBack()
    {
        Debug.Log("[NewBaseUI] User confirmed going back, discarding changes");
        MenuManager.Instance.ShowMainMenuCanvas();
    }

    /// <summary>
    /// Called when user cancels going back (Cancel button in ModalDialog).
    /// Stays on NewBaseCanvas.
    /// Story 3.3: ModalDialog closes itself automatically.
    /// </summary>
    public void OnCancelBack()
    {
        Debug.Log("[NewBaseUI] User cancelled going back, staying on NewBaseCanvas");
        // Modal closes automatically, nothing else needed
    }
}
