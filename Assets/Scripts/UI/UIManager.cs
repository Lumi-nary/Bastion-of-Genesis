using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private BuildingInfoPanel buildingInfoPanel;
    [SerializeField] private BuildingSelectionPanel buildingSelectionPanel;

    [Header("Pause Menu")]
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private float pausedMusicVolume = 0.3f;

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
        // Block ESC during dialogue
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            Debug.Log("[UIManager] ESC blocked - dialogue is active");
            return;
        }

        // Block ESC during modal dialog (modal handles its own ESC)
        if (ModalDialog.Instance != null && ModalDialog.Instance.IsModalActive())
        {
            // Modal handles ESC itself, don't toggle pause
            return;
        }

        TogglePause();
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

        // Only stop time in singleplayer
        if (NetworkGameManager.Instance == null || !NetworkGameManager.Instance.IsOnline)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Debug.Log("[UIManager] Multiplayer active - Time.timeScale NOT set to 0");
        }

        // Lower music volume (don't stop it)
        if (AudioManager.Instance != null)
        {
            // Store current volume and lower it
            AudioManager.Instance.SetMusicVolume(pausedMusicVolume);
        }

        // Stop voice and ambience
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopVoice();
            AudioManager.Instance.StopAmbience();
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

        // Restore music volume
        if (AudioManager.Instance != null && SettingsManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(SettingsManager.Instance.CurrentSettings.musicVolume);
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
        pendingTooltipHeader = header;
        pendingTooltipDescription = description;
        isTooltipHovering = true;
        tooltipHoverTimer = 0f;
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
