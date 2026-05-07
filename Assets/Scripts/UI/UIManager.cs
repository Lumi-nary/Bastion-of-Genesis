using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public enum PanelKind { None, BuildingSelection, WorkerAssembly, WorkerAssign, Research, Mission }

    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private BuildingInfoPanel buildingInfoPanel;
    [SerializeField] private BuildingHoverHealthUI buildingHoverHealthUI;
    [SerializeField] private BuildingHoverPopupUI buildingHoverPopupUI;
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
    public PanelKind ActivePanel { get; private set; } = PanelKind.None;

    /// <summary>
    /// Fires whenever the active nav panel changes (open, close, switch).
    /// NavButtons subscribe to update their highlight state.
    /// </summary>
    public event System.Action<PanelKind> OnActivePanelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureGameplayCanvasEnabled();
        EnsureBuildingHoverHealthUI();
        EnsureBuildingHoverPopupUI();
    }

    private void OnEnable()
    {
        SubscribeTutorialGuide();
    }

    private void Start()
    {
        SubscribeTutorialGuide();
    }

    private void OnDisable()
    {
        if (TutorialGuideManager.Instance != null)
            TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
    }

    private void SubscribeTutorialGuide()
    {
        if (TutorialGuideManager.Instance == null)
            return;

        TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
        TutorialGuideManager.Instance.OnTutorialObjectiveChanged += OnTutorialObjectiveChanged;
    }

    private void OnTutorialObjectiveChanged(MissionObjective objective)
    {
        if (ActivePanel != PanelKind.None &&
            TutorialGuideManager.Instance != null &&
            !TutorialGuideManager.Instance.CanOpenPanel(ActivePanel))
        {
            HideNavPanel(ActivePanel);
            SetActivePanel(PanelKind.None);
        }
    }

    private void EnsureGameplayCanvasEnabled()
    {
        if (gameplayCanvasGroup != null)
        {
            gameplayCanvasGroup.alpha = 1f;
            gameplayCanvasGroup.interactable = true;
            gameplayCanvasGroup.blocksRaycasts = true;

            Canvas gameplayCanvas = gameplayCanvasGroup.GetComponent<Canvas>();
            if (gameplayCanvas == null)
            {
                gameplayCanvas = gameplayCanvasGroup.GetComponentInParent<Canvas>();
            }

            if (gameplayCanvas != null)
            {
                gameplayCanvas.enabled = true;
            }

            return;
        }

        Canvas fallbackCanvas = GetComponentInParent<Canvas>();
        if (fallbackCanvas == null)
        {
            fallbackCanvas = FindFirstObjectByType<Canvas>();
        }

        if (fallbackCanvas != null)
        {
            fallbackCanvas.enabled = true;
        }
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
            SetActivePanel(PanelKind.None);
            return true;
        }
        if (MissionPanel.Instance != null && MissionPanel.Instance.IsVisible)
        {
            MissionPanel.Instance.HidePanel();
            SetActivePanel(PanelKind.None);
            return true;
        }
        if (workerAssemblyPanel != null && workerAssemblyPanel.IsVisible)
        {
            workerAssemblyPanel.HidePanel();
            SetActivePanel(PanelKind.None);
            return true;
        }
        if (workerAssignPanel != null && workerAssignPanel.IsVisible)
        {
            workerAssignPanel.HidePanel();
            SetActivePanel(PanelKind.None);
            return true;
        }
        if (buildingSelectionPanel != null && buildingSelectionPanel.IsVisible)
        {
            buildingSelectionPanel.HidePanel();
            SetActivePanel(PanelKind.None);
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
        SetActivePanel(PanelKind.None);
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

    #region Nav Panels

    /// <summary>
    /// Open a nav panel, closing any other nav panel that is currently open.
    /// </summary>
    public void ShowPanel(PanelKind kind) { ShowPanel(kind, null); }

    public void ShowPanel(PanelKind kind, RectTransform anchor)
    {
        if (kind == PanelKind.None) return;

        if (TutorialGuideManager.Instance != null && !TutorialGuideManager.Instance.CanOpenPanel(kind))
            return;

        CloseOtherNavPanels(kind);
        Canvas.ForceUpdateCanvases();

        switch (kind)
        {
            case PanelKind.BuildingSelection:
                if (buildingSelectionPanel != null && !buildingSelectionPanel.IsVisible)
                    buildingSelectionPanel.ShowPanel();
                break;
            case PanelKind.WorkerAssembly:
                if (workerAssemblyPanel != null && !workerAssemblyPanel.IsVisible)
                    workerAssemblyPanel.ShowPanel(anchor);
                break;
            case PanelKind.WorkerAssign:
                if (workerAssignPanel != null && !workerAssignPanel.IsVisible)
                    workerAssignPanel.ShowPanel(anchor);
                break;
            case PanelKind.Research:
                if (researchPanel != null && !researchPanel.IsVisible)
                    researchPanel.ShowPanel();
                break;
            case PanelKind.Mission:
                if (MissionPanel.Instance != null && !MissionPanel.Instance.IsVisible)
                    MissionPanel.Instance.ShowPanel();
                break;
        }

        SetActivePanel(kind);
    }

    /// <summary>
    /// Toggle a nav panel. If target is already open, closes it; otherwise opens it (closing any other nav panel).
    /// </summary>
    public void TogglePanel(PanelKind kind) { TogglePanel(kind, null); }

    public void TogglePanel(PanelKind kind, RectTransform anchor)
    {
        if (kind == PanelKind.None) return;

        if (TutorialGuideManager.Instance != null && !TutorialGuideManager.Instance.CanOpenPanel(kind))
            return;

        if (ActivePanel == kind)
        {
            HideNavPanel(kind);
            SetActivePanel(PanelKind.None);
        }
        else
        {
            ShowPanel(kind, anchor);
        }
    }

    /// <summary>
    /// Close every nav panel except the one we're about to open.
    /// </summary>
    private void CloseOtherNavPanels(PanelKind keepOpen)
    {
        if (keepOpen != PanelKind.BuildingSelection && buildingSelectionPanel != null && buildingSelectionPanel.IsVisible)
            buildingSelectionPanel.HidePanelImmediate();
        if (keepOpen != PanelKind.WorkerAssembly && workerAssemblyPanel != null && workerAssemblyPanel.IsVisible)
            workerAssemblyPanel.HidePanel();
        if (keepOpen != PanelKind.WorkerAssign && workerAssignPanel != null && workerAssignPanel.IsVisible)
            workerAssignPanel.HidePanel();
        if (keepOpen != PanelKind.Research && researchPanel != null && researchPanel.IsVisible)
            researchPanel.HidePanel();
        if (keepOpen != PanelKind.Mission && MissionPanel.Instance != null && MissionPanel.Instance.IsVisible)
            MissionPanel.Instance.HidePanel();
    }

    private void HideNavPanel(PanelKind kind)
    {
        switch (kind)
        {
            case PanelKind.BuildingSelection:
                if (buildingSelectionPanel != null) buildingSelectionPanel.HidePanel();
                break;
            case PanelKind.WorkerAssembly:
                if (workerAssemblyPanel != null) workerAssemblyPanel.HidePanel();
                break;
            case PanelKind.WorkerAssign:
                if (workerAssignPanel != null) workerAssignPanel.HidePanel();
                break;
            case PanelKind.Research:
                if (researchPanel != null) researchPanel.HidePanel();
                break;
            case PanelKind.Mission:
                if (MissionPanel.Instance != null) MissionPanel.Instance.HidePanel();
                break;
        }
    }

    private void SetActivePanel(PanelKind kind)
    {
        if (ActivePanel == kind) return;
        ActivePanel = kind;
        OnActivePanelChanged?.Invoke(kind);
    }

    public bool HasVisibleNavPanel()
    {
        return (buildingSelectionPanel != null && buildingSelectionPanel.IsVisible)
            || (workerAssemblyPanel != null && workerAssemblyPanel.IsVisible)
            || (workerAssignPanel != null && workerAssignPanel.IsVisible)
            || (researchPanel != null && researchPanel.IsVisible)
            || (MissionPanel.Instance != null && MissionPanel.Instance.IsVisible);
    }

    #endregion

    #region Building Panels (legacy wrappers)

    public void ToggleBuildingSelectionPanel()
    {
        TogglePanel(PanelKind.BuildingSelection);
    }

    public void ShowBuildingInfoPanel(Building building)
    {
        if (buildingInfoPanel != null)
        {
            if (!BuildingInfoPanel.ShouldShowFullInfoPanel(building))
            {
                buildingInfoPanel.HidePanel();
                return;
            }

            buildingInfoPanel.ShowPanel(building);
        }
    }

    public void HideBuildingInfoPanel()
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.HidePanel();
        }

        if (buildingHoverHealthUI != null)
        {
            buildingHoverHealthUI.Hide();
        }

        if (buildingHoverPopupUI != null)
        {
            buildingHoverPopupUI.Hide(this);
        }
    }

    private void EnsureBuildingHoverHealthUI()
    {
        if (buildingHoverHealthUI == null)
        {
            buildingHoverHealthUI = GetComponent<BuildingHoverHealthUI>();
        }

        if (buildingHoverHealthUI == null)
        {
            buildingHoverHealthUI = gameObject.AddComponent<BuildingHoverHealthUI>();
        }

        Canvas canvas = gameplayCanvasGroup != null
            ? gameplayCanvasGroup.GetComponent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        if (canvas == null && gameplayCanvasGroup != null)
        {
            canvas = gameplayCanvasGroup.GetComponentInParent<Canvas>();
        }

        buildingHoverHealthUI.Initialize(canvas);
    }

    private void EnsureBuildingHoverPopupUI()
    {
        if (buildingHoverPopupUI == null)
        {
            buildingHoverPopupUI = GetComponent<BuildingHoverPopupUI>();
        }

        if (buildingHoverPopupUI == null)
        {
            buildingHoverPopupUI = gameObject.AddComponent<BuildingHoverPopupUI>();
        }

        Canvas canvas = gameplayCanvasGroup != null
            ? gameplayCanvasGroup.GetComponent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        if (canvas == null && gameplayCanvasGroup != null)
        {
            canvas = gameplayCanvasGroup.GetComponentInParent<Canvas>();
        }

        buildingHoverPopupUI.Initialize(canvas);
    }

    public void ShowBuildingRequirementPopup(BuildingData data, float durationSeconds, object source)
    {
        EnsureBuildingHoverPopupUI();
        if (buildingHoverPopupUI != null)
        {
            buildingHoverPopupUI.ShowBuildingRequirements(data, durationSeconds, source);
        }
    }

    public void ShowBuiltBuildingStatusPopup(Building building, float durationSeconds, object source)
    {
        EnsureBuildingHoverPopupUI();
        if (buildingHoverPopupUI != null)
        {
            buildingHoverPopupUI.ShowBuiltBuildingStatus(building, durationSeconds, source);
        }
    }

    public void ShowBuiltBuildingHealthPopup(Building building, float durationSeconds, object source)
    {
        EnsureBuildingHoverPopupUI();
        if (buildingHoverPopupUI != null)
        {
            buildingHoverPopupUI.ShowBuiltBuildingHealth(building, durationSeconds, source);
        }
    }

    public void HideBuildingHoverPopup(object source)
    {
        if (buildingHoverPopupUI != null)
        {
            buildingHoverPopupUI.Hide(source);
        }
    }

    public void HideBuildingHoverPopup()
    {
        if (buildingHoverPopupUI != null)
        {
            buildingHoverPopupUI.Hide();
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
