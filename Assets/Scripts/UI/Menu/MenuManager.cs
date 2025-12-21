using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// MenuManager handles canvas-based navigation for the main menu system.
/// Scene-specific singleton (NO DontDestroyOnLoad) - destroyed when leaving MenuScene.
/// Follows Pattern 2 (Manager Pattern Consistency) and Pattern 7 (UI Canvas Management).
/// </summary>
public class MenuManager : MonoBehaviour
{
    // Singleton pattern - scene-specific
    public static MenuManager Instance { get; private set; }

    [Header("Canvas References")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private Canvas newBaseCanvas;
    [SerializeField] private Canvas loadGameCanvas;
    [SerializeField] private Canvas joinGameCanvas;
    [SerializeField] private Canvas multiplayerCanvas;
    [SerializeField] private Canvas optionsCanvas;
    [SerializeField] private Canvas creditsCanvas;

    // Track currently active canvas
    private Canvas currentCanvas;

    /// <summary>
    /// Get the currently active canvas (Story 3.3 fix).
    /// Used by UI controllers to check if they should initialize.
    /// </summary>
    public Canvas CurrentCanvas => currentCanvas;

    // Unity Input System actions for ESC key
    private InputAction escapeAction;

    /// <summary>
    /// Awake - Initialize singleton (Pattern 2: Scene-specific singleton).
    /// NO DontDestroyOnLoad - MenuManager only exists in MenuScene.
    /// </summary>
    private void Awake()
    {
        // Singleton self-destruct pattern for duplicate instances
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MenuManager] Duplicate MenuManager detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[MenuManager] MenuManager singleton initialized");
    }

    /// <summary>
    /// Start - Validate canvas references and set initial state.
    /// </summary>
    private void Start()
    {
        // Validate canvas references (AC4.5: No NullReferenceExceptions)
        ValidateCanvasReferences();

        // Set main menu as default active canvas
        if (mainMenuCanvas != null)
        {
            ShowMainMenuCanvas();
        }
    }

    /// <summary>
    /// OnEnable - Subscribe to ESC key input for back navigation (AC4.4).
    /// </summary>
    private void OnEnable()
    {
        // Setup ESC key listener using Unity Input System
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
        escapeAction.performed += OnEscapePressed;
        escapeAction.Enable();
    }

    /// <summary>
    /// OnDisable - Unsubscribe from ESC key input.
    /// </summary>
    private void OnDisable()
    {
        if (escapeAction != null)
        {
            escapeAction.performed -= OnEscapePressed;
            escapeAction.Disable();
            escapeAction.Dispose();
        }
    }

    /// <summary>
    /// Validate all canvas SerializeField references (AC4.5).
    /// </summary>
    private void ValidateCanvasReferences()
    {
        if (mainMenuCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: mainMenuCanvas");
        if (newBaseCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: newBaseCanvas");
        if (loadGameCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: loadGameCanvas");
        if (joinGameCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: joinGameCanvas");
        if (multiplayerCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: multiplayerCanvas");
        if (optionsCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: optionsCanvas");
        if (creditsCanvas == null)
            Debug.LogError("[MenuManager] Canvas reference missing: creditsCanvas");
    }

    /// <summary>
    /// ESC key callback - Return to main menu from any subscreen (AC4.4).
    /// Story 3.3: Do not process ESC if ModalDialog is active or just handled ESC.
    /// </summary>
    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        // Story 3.3: Check if modal is active OR just handled ESC this frame
        // Both handlers fire on same frame, so ModalDialog sets a flag when it handles ESC
        bool modalActive = ModalDialog.Instance != null && ModalDialog.Instance.IsModalActive();
        bool modalJustHandledEsc = ModalDialog.Instance != null && ModalDialog.Instance.JustHandledEscThisFrame();

        Debug.Log($"[MenuManager] ESC pressed. Modal active: {modalActive}, just handled: {modalJustHandledEsc}");

        if (modalActive || modalJustHandledEsc)
        {
            Debug.Log("[MenuManager] Modal is handling ESC, skipping menu navigation");
            return; // Modal has priority over MenuManager ESC handling
        }

        // Only navigate back if not already on main menu
        if (currentCanvas != mainMenuCanvas && mainMenuCanvas != null)
        {
            Debug.Log("[MenuManager] Navigating back to main menu");
            ShowMainMenuCanvas();
        }
        // If already on main menu, do nothing (no quit confirmation)
    }

    /// <summary>
    /// Core canvas switching logic (AC4.2, AC4.6).
    /// Achieves <100ms transition by using Canvas.enabled (no GameObject destruction).
    /// </summary>
    private void SwitchCanvas(Canvas targetCanvas)
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[MenuManager] Cannot switch to null canvas");
            return;
        }

        // Disable current canvas
        if (currentCanvas != null)
        {
            currentCanvas.enabled = false;
        }

        // Enable target canvas
        targetCanvas.enabled = true;

        // Update current canvas reference
        currentCanvas = targetCanvas;

        // Log canvas switch (AC4.6: Pattern 4 - Logging Strategy)
        Debug.Log($"[MenuManager] Canvas switched: {targetCanvas.name}");
    }

    // ============================================================================
    // Public Canvas Switching API (AC2.1) - Called by button scripts
    // ============================================================================

    /// <summary>
    /// Show Main Menu canvas (main navigation hub).
    /// </summary>
    public void ShowMainMenuCanvas()
    {
        SwitchCanvas(mainMenuCanvas);
    }

    /// <summary>
    /// Show New Base creation canvas (Epic 2).
    /// Resets form to ensure clean state with new auto-generated base name.
    /// </summary>
    public void ShowNewBaseCanvas()
    {
        SwitchCanvas(newBaseCanvas);

        // Reset NewBaseUI form to generate new name and clear any changes
        if (newBaseCanvas != null)
        {
            NewBaseUI newBaseUI = newBaseCanvas.GetComponentInChildren<NewBaseUI>();
            if (newBaseUI != null)
            {
                newBaseUI.ResetForm();
            }
        }
    }

    /// <summary>
    /// Show Load Game browser canvas (Epic 3).
    /// </summary>
    public void ShowLoadGameCanvas()
    {
        SwitchCanvas(loadGameCanvas);
    }

    /// <summary>
    /// Show Join Game LAN browser canvas (Epic 4).
    /// </summary>
    public void ShowJoinGameCanvas()
    {
        SwitchCanvas(joinGameCanvas);
    }

    /// <summary>
    /// Show Multiplayer Lobby canvas (for both host and client).
    /// </summary>
    public void ShowMultiplayerCanvas()
    {
        SwitchCanvas(multiplayerCanvas);

        // Initialize the lobby UI when shown
        if (multiplayerCanvas != null)
        {
            MultiplayerLobbyUI lobbyUI = multiplayerCanvas.GetComponentInChildren<MultiplayerLobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.InitializeLobby();
            }
        }
    }

    /// <summary>
    /// Show Options menu canvas (Epic 5).
    /// </summary>
    public void ShowOptionsCanvas()
    {
        SwitchCanvas(optionsCanvas);
    }

    /// <summary>
    /// Show Credits screen canvas (Epic 6).
    /// </summary>
    public void ShowCreditsCanvas()
    {
        SwitchCanvas(creditsCanvas);
    }
}
