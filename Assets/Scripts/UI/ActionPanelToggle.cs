using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Slides the ActionPanel on/off screen with an arrow toggle button.
/// Auto-hides after a period of inactivity. Mouse hover resets the timer.
/// </summary>
public class ActionPanelToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI arrowText;

    [Header("Settings")]
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float autoHideDelay = 5f;

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

    private void Start()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        shownPosition = panelRect.anchoredPosition;

        float panelWidth = panelRect.rect.width;
        hiddenPosition = new Vector2(shownPosition.x - panelWidth, shownPosition.y);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        idleTimer = 0f;
        UpdateArrow();
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
        if (!isHidden && !isHovering && !isAnimating && !isPaused)
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

    private void UpdateArrow()
    {
        if (arrowText != null)
            arrowText.text = isHidden ? ">" : "<";
    }
}
