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
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Toggle modeToggle;

    [Header("COOP Panel")]
    [SerializeField] private GameObject coopServerPanel;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationDialog;

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
    private Difficulty currentDifficulty = Difficulty.Medium;
    private GameMode currentMode = GameMode.Singleplayer;

    // Form state tracking
    private string initialBaseName;
    private Difficulty initialDifficulty = Difficulty.Medium;
    private GameMode initialMode = GameMode.Singleplayer;
    private bool isFormDirty = false;

    // ============================================================================
    // LIFECYCLE METHODS
    // ============================================================================

    private void OnEnable()
    {
        InitializeForm();
    }

    private void OnDisable()
    {
        // Clean up listeners
        if (baseNameInputField != null)
        {
            baseNameInputField.onValueChanged.RemoveListener(OnBaseNameChanged);
        }
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
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

        // Set difficulty dropdown to Medium (index 1)
        currentDifficulty = Difficulty.Medium;
        initialDifficulty = Difficulty.Medium;

        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = 1; // Medium
            difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged); // Prevent duplicates
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
        Debug.Log($"[NewBaseUI] Difficulty initialized: {currentDifficulty}");

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

        // Hide confirmation dialog initially
        if (confirmationDialog != null)
        {
            confirmationDialog.SetActive(false);
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
    /// </summary>
    public Difficulty GetDifficulty()
    {
        return currentDifficulty;
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
        //Debug.Log($"[NewBaseUI] Base name changed: {currentBaseName}");
    }

    /// <summary>
    /// Called when difficulty dropdown value changes.
    /// Maps dropdown index (0/1/2) to Difficulty enum (Easy/Medium/Hard).
    /// </summary>
    /// <param name="dropdownIndex">Dropdown selected index</param>
    private void OnDifficultyChanged(int dropdownIndex)
    {
        currentDifficulty = (Difficulty)dropdownIndex;
        CheckFormDirty();
        Debug.Log($"[NewBaseUI] Difficulty selected: {currentDifficulty}");
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
                      currentDifficulty != initialDifficulty ||
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
    /// Dialog has Yes/No buttons (wired in Inspector to OnConfirmBack/OnCancelBack).
    /// </summary>
    public void ShowConfirmationDialog()
    {
        if (confirmationDialog != null)
        {
            confirmationDialog.SetActive(true);
            Debug.Log("[NewBaseUI] Confirmation dialog displayed");
        }
    }

    /// <summary>
    /// Called when user confirms going back (Yes button).
    /// Returns to main menu without saving changes.
    /// </summary>
    public void OnConfirmBack()
    {
        Debug.Log("[NewBaseUI] User confirmed going back, discarding changes");
        if (confirmationDialog != null)
        {
            confirmationDialog.SetActive(false);
        }
        MenuManager.Instance.ShowMainMenuCanvas();
    }

    /// <summary>
    /// Called when user cancels going back (No button).
    /// Hides confirmation dialog and stays on NewBaseCanvas.
    /// </summary>
    public void OnCancelBack()
    {
        Debug.Log("[NewBaseUI] User cancelled going back, staying on NewBaseCanvas");
        if (confirmationDialog != null)
        {
            confirmationDialog.SetActive(false);
        }
    }
}
