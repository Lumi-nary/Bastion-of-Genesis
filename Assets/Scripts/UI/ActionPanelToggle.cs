using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// Slides the ActionPanel on/off screen with an arrow toggle button.
/// Auto-hides after a period of inactivity. Mouse hover resets the timer.
/// </summary>
public class ActionPanelToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlideDirection { Left, Right }

    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI arrowText;

    [Header("Settings")]
    [SerializeField] private SlideDirection slideDirection = SlideDirection.Left;
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float autoHideDelay = 5f;
    [SerializeField] private bool autoHide = true;

    private bool isHidden;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;

    // Animation state
    private bool isAnimating;
    private Vector2 animStart;
    private Vector2 animEnd;
    private float animTimer;

    // Auto-hide state
    private float idleTimer;
    private bool isHovering;
    private bool manuallyHidden; // user clicked arrow to hide

    private bool initialized;

    /// <summary>
    /// When true, the auto-hide countdown is paused (e.g., while the building list flyout is open).
    /// </summary>
    public bool SuppressAutoHide { get; set; }

    public event Action OnShown;
    public event Action OnHidden;

    private void Awake()
    {
        InitializePositions();
    }

    private void Start()
    {
        // Safety: ensure positions are cached even if Awake was skipped (prefabbed instantiation).
        if (!initialized) InitializePositions();

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        idleTimer = 0f;
        UpdateArrow();
    }

    private void InitializePositions()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();
        if (panelRect == null) return;

        shownPosition = panelRect.anchoredPosition;

        float panelWidth = panelRect.rect.width;
        float offset = slideDirection == SlideDirection.Left ? -panelWidth : panelWidth;
        hiddenPosition = new Vector2(shownPosition.x + offset, shownPosition.y);

        initialized = true;
    }

    /// <summary>
    /// Snap to the hidden position instantly (no slide animation). Use for initial state setup.
    /// </summary>
    public void HideInstant()
    {
        if (!initialized) InitializePositions();
        isHidden = true;
        isAnimating = false;
        if (panelRect != null) panelRect.anchoredPosition = hiddenPosition;
        UpdateArrow();
        OnHidden?.Invoke();
    }

    private void Update()
    {
        // Slide animation
        if (isAnimating)
        {
            animTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(animTimer / slideDuration);
            float eased = 1f - (1f - t) * (1f - t);

            panelRect.anchoredPosition = Vector2.Lerp(animStart, animEnd, eased);

            if (t >= 1f)
            {
                panelRect.anchoredPosition = animEnd;
                isAnimating = false;
            }
        }

        // Auto-hide countdown (only when shown, not hovering, not manually hidden, not paused)
        bool isPaused = UIManager.Instance != null && UIManager.Instance.IsPaused;
        if (IsAutoHideSuppressed())
        {
            idleTimer = 0f;
        }
        else if (autoHide && !isHidden && !isHovering && !isAnimating && !isPaused)
        {
            idleTimer += Time.unscaledDeltaTime;
            if (idleTimer >= autoHideDelay)
            {
                Hide();
            }
        }
    }

    public void Toggle()
    {
        if (isHidden)
        {
            Show();
            manuallyHidden = false;
        }
        else
        {
            Hide();
            manuallyHidden = true;
        }
    }

    public void Show()
    {
        if (!isHidden && !isAnimating) return;

        isHidden = false;
        idleTimer = 0f;

        animStart = panelRect.anchoredPosition;
        animEnd = shownPosition;
        animTimer = 0f;
        isAnimating = true;

        UpdateArrow();
        OnShown?.Invoke();
    }

    public void Hide()
    {
        if (isHidden && !isAnimating) return;

        isHidden = true;

        animStart = panelRect.anchoredPosition;
        animEnd = hiddenPosition;
        animTimer = 0f;
        isAnimating = true;

        UpdateArrow();
        OnHidden?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPaused) return;

        isHovering = true;
        idleTimer = 0f;

        if (isHidden && !manuallyHidden)
            Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPaused) return;

        isHovering = false;
        idleTimer = 0f;
    }

    private bool IsAutoHideSuppressed()
    {
        return SuppressAutoHide
            || (UIManager.Instance != null && UIManager.Instance.HasVisibleNavPanel())
            || (PlacementSystem.Instance != null && PlacementSystem.Instance.IsBuilding);
    }

    private void UpdateArrow()
    {
        if (arrowText == null) return;
        // Arrow always points toward the direction the panel will move when clicked.
        // Left-slide panel: hidden → points right ">", shown → points left "<".
        // Right-slide panel: hidden → points left "<", shown → points right ">".
        if (slideDirection == SlideDirection.Left)
            arrowText.text = isHidden ? ">" : "<";
        else
            arrowText.text = isHidden ? "<" : ">";
    }
}
