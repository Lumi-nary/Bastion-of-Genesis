using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private BuildingInfoPanel buildingInfoPanel;
    [SerializeField] private BuildingSelectionPanel buildingSelectionPanel;
    [SerializeField] private ResearchPanel researchPanel;
    [SerializeField] private WorkerAssemblyPanel workerAssemblyPanel;
    [SerializeField] private WorkerAssignPanel workerAssignPanel;

    [Header("Pause Menu")]
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private float pausedMusicVolume = 0.3f;

    [Header("Pause Input Block")]
    [SerializeField] private CanvasGroup gameplayCanvasGroup;

    [Header("Tooltip Settings")]
    [SerializeField] private TooltipUI tooltipUI;
    [SerializeField] private float tooltipShowDelay = 0.5f;
    [SerializeField] private float tooltipHideDelay = 0.1f;

    // Pause state
    private bool isPaused;
    private float previousMusicVolume = 1f;

    // Tooltip state
    private float tooltipHoverTimer;
    private float tooltipHideTimer;
    private bool isTooltipHovering;
    private bool isTooltipPendingHide;
    private string pendingTooltipHeader;
    private string pendingTooltipDescription;

    // Properties
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Handle ESC key for pause menu
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }

        // Only update tooltip when not paused
        if (!isPaused)
        {
            UpdateTooltip();
        }
    }

    #region Pause System

    private void HandleEscapeKey()
    {
        // Layer 1: Block ESC during dialogue
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        // Layer 2: Block ESC during modal dialog (modal handles its own ESC)
        if (ModalDialog.Instance != null && ModalDialog.Instance.IsModalActive())
        {
            return;
        }

        // Layer 3: If paused with Options sub-menu open, go back to pause menu
        if (isPaused && pauseMenuUI != null && pauseMenuUI.IsOptionsVisible)
        {
            pauseMenuUI.ReturnFromOptions();
            return;
        }

        // Layer 4: If paused, unpause
        if (isPaused)
        {
            Unpause();
            return;
        }

        // Layer 5: Close any open game panel
        if (CloseTopmostPanel())
        {
            return;
        }

        // Layer 6: Nothing open — pause the game
        Pause();
    }

    /// <summary>
    /// Close the topmost open game panel. Returns true if a panel was closed.
    /// </summary>
    private bool CloseTopmostPanel()
    {
        // Close in reverse-priority order (most overlay-like first)
        if (buildingInfoPanel != null && buildingInfoPanel.IsVisible)
        {
            buildingInfoPanel.HidePanel();
            return true;
        }
        if (researchPanel != null && researchPanel.IsVisible)
        {
            researchPanel.HidePanel();
            return true;
        }
        if (MissionPanel.Instance != null && MissionPanel.Instance.IsVisible)
        {
            MissionPanel.Instance.HidePanel();
            return true;
        }
        if (workerAssemblyPanel != null && workerAssemblyPanel.IsVisible)
        {
            workerAssemblyPanel.HidePanel();
            return true;
        }
        if (workerAssignPanel != null && workerAssignPanel.IsVisible)
        {
            workerAssignPanel.HidePanel();
            return true;
        }
        if (buildingSelectionPanel != null && buildingSelectionPanel.IsVisible)
        {
            buildingSelectionPanel.HidePanel();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Close all open game panels (called when pausing)
    /// </summary>
    private void CloseAllGamePanels()
    {
        if (buildingInfoPanel != null && buildingInfoPanel.IsVisible) buildingInfoPanel.HidePanel();
        if (researchPanel != null && researchPanel.IsVisible) researchPanel.HidePanel();
        if (MissionPanel.Instance != null && MissionPanel.Instance.IsVisible) MissionPanel.Instance.HidePanel();
        if (workerAssemblyPanel != null && workerAssemblyPanel.IsVisible) workerAssemblyPanel.HidePanel();
        if (workerAssignPanel != null && workerAssignPanel.IsVisible) workerAssignPanel.HidePanel();
        if (buildingSelectionPanel != null && buildingSelectionPanel.IsVisible) buildingSelectionPanel.HidePanel();
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;

        // Close all game panels before showing pause menu
        CloseAllGamePanels();

        // Only stop time in singleplayer (check GameMode, not IsOnline, since SP uses local host)
        bool isCoop = SaveManager.Instance != null && SaveManager.Instance.pendingMode == GameMode.COOP;
        if (!isCoop)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Debug.Log("[UIManager] COOP active - Time.timeScale NOT set to 0");
        }

        // Lower music volume relative to user's saved setting
        if (AudioManager.Instance != null)
        {
            float userVolume = SettingsManager.Instance != null
                ? SettingsManager.Instance.CurrentSettings.musicVolume
                : 1f;
            previousMusicVolume = userVolume;
            AudioManager.Instance.SetMusicVolume(userVolume * pausedMusicVolume);
        }

        // Stop voice and ambience
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopVoice();
            AudioManager.Instance.StopAmbience();
        }

        // Block gameplay UI interaction
        if (gameplayCanvasGroup != null)
        {
            gameplayCanvasGroup.interactable = false;
        }

        // Show pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.Show();
        }

        Debug.Log("[UIManager] Game paused");
    }

    /// <summary>
    /// Unpause the game
    /// </summary>
    public void Unpause()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        // Restore music volume to user's saved setting
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(previousMusicVolume);
        }

        // Restore gameplay UI interaction
        if (gameplayCanvasGroup != null)
        {
            gameplayCanvasGroup.interactable = true;
        }

        // Hide pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.Hide();
        }

        Debug.Log("[UIManager] Game unpaused");
    }

    #endregion

    #region Building Panels

    public void ToggleBuildingSelectionPanel()
    {
        if (buildingSelectionPanel != null)
        {
            if (buildingSelectionPanel.gameObject.activeSelf)
            {
                buildingSelectionPanel.HidePanel();
            }
            else
            {
                buildingSelectionPanel.ShowPanel();
            }
        }
    }

    public void ShowBuildingInfoPanel(Building building)
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.ShowPanel(building);
        }
    }

    public void HideBuildingInfoPanel()
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.HidePanel();
        }
    }

    #endregion

    #region Tooltip System

    private void UpdateTooltip()
    {
        if (tooltipUI == null) return;

        if (isTooltipHovering)
        {
            tooltipHoverTimer += Time.deltaTime;
            isTooltipPendingHide = false;
            tooltipHideTimer = 0f;

            if (tooltipHoverTimer >= tooltipShowDelay && !tooltipUI.gameObject.activeSelf)
            {
                ShowTooltipImmediate();
            }

            // Update tooltip position to follow mouse
            if (tooltipUI.gameObject.activeSelf && Mouse.current != null)
            {
                tooltipUI.UpdatePosition(Mouse.current.position.ReadValue());
            }
        }
        else if (isTooltipPendingHide)
        {
            tooltipHideTimer += Time.deltaTime;

            if (tooltipHideTimer >= tooltipHideDelay)
            {
                HideTooltipImmediate();
            }
        }
    }

    public void ShowTooltip(string header, string description)
    {
        // If content changed while tooltip is already visible, update immediately
        bool contentChanged = header != pendingTooltipHeader || description != pendingTooltipDescription;

        pendingTooltipHeader = header;
        pendingTooltipDescription = description;
        isTooltipHovering = true;
        isTooltipPendingHide = false;

        if (contentChanged && tooltipUI != null && tooltipUI.gameObject.activeSelf)
        {
            // Already showing — update content and position instantly
            ShowTooltipImmediate();
        }
        else if (!tooltipUI.gameObject.activeSelf)
        {
            // Not showing yet — reset delay timer
            tooltipHoverTimer = 0f;
        }
    }

    private void ShowTooltipImmediate()
    {
        if (tooltipUI != null && Mouse.current != null)
        {
            tooltipUI.Show(pendingTooltipHeader, pendingTooltipDescription);
            tooltipUI.UpdatePosition(Mouse.current.position.ReadValue());
        }
    }

    public void HideTooltip()
    {
        isTooltipHovering = false;
        tooltipHoverTimer = 0f;
        isTooltipPendingHide = true;
        tooltipHideTimer = 0f;
    }

    private void HideTooltipImmediate()
    {
        isTooltipPendingHide = false;
        tooltipHideTimer = 0f;

        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }
    }

    public void ShowTooltipFromProvider(ITooltipProvider provider)
    {
        if (provider != null)
        {
            ShowTooltip(provider.GetTooltipHeader(), provider.GetTooltipDescription());
        }
    }

    #endregion
}
