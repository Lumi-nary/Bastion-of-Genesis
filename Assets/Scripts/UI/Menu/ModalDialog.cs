using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// ModalDialog - Reusable modal dialog system for confirmations, errors, and info messages.
/// Singleton pattern with modal queue for sequential display.
/// Epic 3 Story 3.3 - ModalDialog Component.
/// Pattern 2: Scene-specific singleton (MenuScene initially, optional DontDestroyOnLoad).
/// Pattern 3: All user-facing errors must show modal dialog.
/// Pattern 7: Modal rendered on top of all canvases with high Sort Order.
/// </summary>
public class ModalDialog : MonoBehaviour
{
    public static ModalDialog Instance { get; private set; }

    /// <summary>
    /// Check if a modal is currently active/visible.
    /// Used by MenuManager to prevent ESC key conflicts (Story 3.3).
    /// </summary>
    public bool IsModalActive()
    {
        return isModalActive;
    }

    /// <summary>
    /// Check if modal just handled ESC this frame.
    /// Used by MenuManager to prevent double-handling ESC (Story 3.3).
    /// </summary>
    public bool JustHandledEscThisFrame()
    {
        return justHandledEscThisFrame;
    }

    [Header("UI Components")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private GameObject backgroundOverlay;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Canvas Settings")]
    [SerializeField] private Canvas modalCanvas;

    // Modal queue for sequential display (AC7)
    private Queue<ModalRequest> modalQueue = new Queue<ModalRequest>();
    private bool isModalActive = false;
    private bool justHandledEscThisFrame = false; // Story 3.3: Prevent MenuManager from also handling ESC

    // Current modal state
    private Action<int> currentCallback;
    private List<Button> currentButtons = new List<Button>();

    // Input actions for keyboard shortcuts (AC6)
    private InputAction escapeAction;
    private InputAction enterAction;

    /// <summary>
    /// Modal request data structure for queue system.
    /// </summary>
    private class ModalRequest
    {
        public string title;
        public string message;
        public string[] buttons;
        public Action<int> callback;
    }

    /// <summary>
    /// Awake - Initialize singleton instance.
    /// Pattern 2: Scene-specific singleton (no DontDestroyOnLoad for now).
    /// </summary>
    private void Awake()
    {
        // Singleton pattern (Pattern 2)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Initialize();
    }

    /// <summary>
    /// Initialize modal dialog system.
    /// Sets up input actions for keyboard shortcuts.
    /// </summary>
    private void Initialize()
    {
        // Hide modal on startup
        if (modalPanel != null)
            modalPanel.SetActive(false);
        if (backgroundOverlay != null)
            backgroundOverlay.SetActive(false);

        // Set canvas sort order to render on top (Pattern 7)
        if (modalCanvas != null)
        {
            modalCanvas.sortingOrder = 1000; // High sort order to render on top
        }

        // Setup keyboard shortcuts (AC6)
        SetupInputActions();

        Debug.Log("[ModalDialog] Modal dialog system initialized");
    }

    /// <summary>
    /// Setup input actions for keyboard shortcuts (AC6).
    /// ESC = cancel/rightmost button, Enter = confirm/leftmost button.
    /// </summary>
    private void SetupInputActions()
    {
        // Create input actions for ESC and Enter keys
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
        enterAction = new InputAction(binding: "<Keyboard>/enter");

        // Enable input actions
        escapeAction.Enable();
        enterAction.Enable();

        // Subscribe to performed events
        escapeAction.performed += OnEscapePressed;
        enterAction.performed += OnEnterPressed;
    }

    /// <summary>
    /// OnDestroy - Clean up input actions.
    /// </summary>
    private void OnDestroy()
    {
        if (escapeAction != null)
        {
            escapeAction.performed -= OnEscapePressed;
            escapeAction.Disable();
        }

        if (enterAction != null)
        {
            enterAction.performed -= OnEnterPressed;
            enterAction.Disable();
        }
    }

    /// <summary>
    /// ESC key pressed - trigger cancel/rightmost button (AC6).
    /// </summary>
    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        Debug.Log($"[ModalDialog] ESC pressed. isModalActive={isModalActive}, buttonCount={currentButtons.Count}");

        if (isModalActive && currentButtons.Count > 0)
        {
            // Set flag to prevent MenuManager from also handling ESC this frame
            justHandledEscThisFrame = true;

            // Trigger rightmost button (last button in list)
            int lastButtonIndex = currentButtons.Count - 1;
            Debug.Log($"[ModalDialog] Triggering rightmost button (index {lastButtonIndex}), set ESC handled flag");
            OnButtonClick(lastButtonIndex);
        }
        else
        {
            Debug.LogWarning("[ModalDialog] ESC pressed but modal not active or no buttons available");
        }
    }

    /// <summary>
    /// LateUpdate - Clear ESC handled flag at end of frame.
    /// Story 3.3: Ensures flag is only true for one frame.
    /// </summary>
    private void LateUpdate()
    {
        if (justHandledEscThisFrame)
        {
            justHandledEscThisFrame = false;
            Debug.Log("[ModalDialog] Cleared ESC handled flag in LateUpdate");
        }
    }

    /// <summary>
    /// Enter key pressed - trigger confirm/leftmost button (AC6).
    /// </summary>
    private void OnEnterPressed(InputAction.CallbackContext context)
    {
        if (isModalActive && currentButtons.Count > 0)
        {
            // Trigger leftmost button (first button in list)
            OnButtonClick(0);
        }
    }

    /// <summary>
    /// Show modal dialog with custom buttons (AC1).
    /// Generic modal display with 1-3 buttons and callback.
    /// </summary>
    /// <param name="title">Modal title</param>
    /// <param name="message">Modal message body</param>
    /// <param name="buttons">Array of button labels (1-3 buttons)</param>
    /// <param name="onButtonClick">Callback with button index (0-based)</param>
    public void Show(string title, string message, string[] buttons, Action<int> onButtonClick)
    {
        // Validate parameters
        if (buttons == null || buttons.Length == 0 || buttons.Length > 3)
        {
            Debug.LogError("[ModalDialog] Show() requires 1-3 buttons");
            return;
        }

        // Create modal request
        ModalRequest request = new ModalRequest
        {
            title = title,
            message = message,
            buttons = buttons,
            callback = onButtonClick
        };

        // Add to queue (AC7: Modal queue system)
        modalQueue.Enqueue(request);

        Debug.Log($"[ModalDialog] Modal enqueued: '{title}' (Queue size: {modalQueue.Count})");

        // Process queue if no modal currently active
        if (!isModalActive)
        {
            ProcessNextModal();
        }
    }

    /// <summary>
    /// Process next modal in queue (AC7).
    /// Displays modal from queue if not currently active.
    /// </summary>
    private void ProcessNextModal()
    {
        // Check if queue has pending modals
        if (modalQueue.Count == 0)
        {
            return;
        }

        // Dequeue next modal request
        ModalRequest request = modalQueue.Dequeue();

        // Display modal
        DisplayModal(request.title, request.message, request.buttons, request.callback);

        Debug.Log($"[ModalDialog] Processing modal: '{request.title}' (Remaining in queue: {modalQueue.Count})");
    }

    /// <summary>
    /// Display modal with given parameters.
    /// Internal method called by queue system.
    /// </summary>
    private void DisplayModal(string title, string message, string[] buttons, Action<int> callback)
    {
        // Set modal active
        isModalActive = true;
        currentCallback = callback;

        // Set title and message text
        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;

        // Clear existing buttons
        ClearButtons();

        // Create buttons dynamically (AC1)
        for (int i = 0; i < buttons.Length; i++)
        {
            CreateButton(buttons[i], i);
        }

        // Show modal panel and overlay
        if (modalPanel != null)
            modalPanel.SetActive(true);

        if (backgroundOverlay != null)
            backgroundOverlay.SetActive(true);

        Debug.Log($"[ModalDialog] Showing modal: '{title}' with {buttons.Length} buttons");
    }

    /// <summary>
    /// Create button dynamically with label and index (AC1).
    /// </summary>
    private void CreateButton(string label, int index)
    {
        if (buttonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("[ModalDialog] Button prefab or container is null");
            return;
        }

        // Instantiate button from prefab
        GameObject buttonObject = Instantiate(buttonPrefab, buttonContainer);
        Button button = buttonObject.GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("[ModalDialog] Button prefab missing Button component");
            Destroy(buttonObject);
            return;
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = label;
        }

        // Wire up button click callback
        int buttonIndex = index; // Capture index for lambda
        button.onClick.AddListener(() => OnButtonClick(buttonIndex));

        // Add to current buttons list
        currentButtons.Add(button);
    }

    /// <summary>
    /// Clear all buttons from container.
    /// </summary>
    private void ClearButtons()
    {
        // Remove all button listeners
        foreach (Button button in currentButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        // Clear list
        currentButtons.Clear();

        // Destroy button GameObjects
        if (buttonContainer != null)
        {
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Button click handler (AC1).
    /// Triggers callback with button index and closes modal.
    /// </summary>
    private void OnButtonClick(int buttonIndex)
    {
        Debug.Log($"[ModalDialog] Button clicked: {buttonIndex}");

        // Trigger callback
        if (currentCallback != null)
        {
            currentCallback.Invoke(buttonIndex);
        }

        // Close modal
        CloseModal();

        // Process next modal in queue (AC7)
        ProcessNextModal();
    }

    /// <summary>
    /// Close modal and cleanup.
    /// </summary>
    private void CloseModal()
    {
        // Hide modal panel and overlay
        if (modalPanel != null)
            modalPanel.SetActive(false);

        if (backgroundOverlay != null)
            backgroundOverlay.SetActive(false);

        // Clear buttons
        ClearButtons();

        // Reset state
        isModalActive = false;
        currentCallback = null;

        Debug.Log("[ModalDialog] Modal closed");
    }

    /// <summary>
    /// ShowConfirmation - Convenience wrapper for Yes/No confirmation dialogs (AC2).
    /// </summary>
    /// <param name="message">Confirmation message</param>
    /// <param name="onConfirm">Callback if user confirms</param>
    /// <param name="onCancel">Callback if user cancels</param>
    public void ShowConfirmation(string message, Action onConfirm, Action onCancel)
    {
        // AC2: Default buttons: "Confirm" and "Cancel"
        string[] buttons = { "Confirm", "Cancel" };

        // Show modal with callback that routes to onConfirm or onCancel
        Show("Confirm Action", message, buttons, (buttonIndex) =>
        {
            if (buttonIndex == 0)
            {
                // Confirm button clicked
                Debug.Log("[ModalDialog] Confirmation accepted");
                onConfirm?.Invoke();
            }
            else
            {
                // Cancel button clicked (or ESC pressed)
                Debug.Log("[ModalDialog] Confirmation cancelled");
                onCancel?.Invoke();
            }
        });
    }

    /// <summary>
    /// ShowError - Convenience wrapper for error modals (AC4, Pattern 3).
    /// Single "OK" button closes error modals.
    /// </summary>
    /// <param name="message">Error message</param>
    public void ShowError(string message)
    {
        Show("Error", message, new string[] { "OK" }, (buttonIndex) =>
        {
            Debug.Log("[ModalDialog] Error modal acknowledged");
        });
    }

    /// <summary>
    /// ShowInfo - Convenience wrapper for info modals.
    /// Single "OK" button closes info modals.
    /// </summary>
    /// <param name="title">Info title</param>
    /// <param name="message">Info message</param>
    public void ShowInfo(string title, string message)
    {
        Show(title, message, new string[] { "OK" }, (buttonIndex) =>
        {
            Debug.Log("[ModalDialog] Info modal acknowledged");
        });
    }
}
