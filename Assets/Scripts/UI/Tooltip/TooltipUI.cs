using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private RectTransform backgroundRect;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10, -10);
    [SerializeField] private float maxWidth = 400f;
    [SerializeField] private float maxHeight = 300f;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        // Get or add CanvasGroup to prevent tooltip from blocking raycasts
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Make tooltip non-blocking for raycasts
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Hide();
    }

    public void Show(string header, string description)
    {
        gameObject.SetActive(true);

        // Set text
        headerText.text = header;
        descriptionText.text = description;

        // Update layout element with max constraints
        if (layoutElement != null)
        {
            layoutElement.enabled = true;
            layoutElement.preferredWidth = maxWidth;
            layoutElement.preferredHeight = -1; // Let height expand

            // Set maximum size constraints
            if (layoutElement.preferredWidth > maxWidth)
            {
                layoutElement.preferredWidth = maxWidth;
            }
        }

        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // Clamp size to maximum after layout calculation
        Vector2 tooltipSize = rectTransform.rect.size;
        if (tooltipSize.x > maxWidth || tooltipSize.y > maxHeight)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(tooltipSize.x, maxWidth));
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(tooltipSize.y, maxHeight));
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdatePosition(Vector2 mousePosition)
    {
        if (parentCanvas == null) return;

        // Convert mouse position to canvas space
        Vector2 position;

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
            parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePosition,
                parentCanvas.worldCamera,
                out position
            );
        }
        else
        {
            // Screen Space - Overlay
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePosition,
                null,
                out position
            );
        }

        // Apply offset
        position += offset;

        // Get canvas and tooltip dimensions
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 tooltipSize = rectTransform.rect.size;

        // Calculate bounds in local space (canvas is centered at origin)
        float halfCanvasWidth = canvasSize.x / 2f;
        float halfCanvasHeight = canvasSize.y / 2f;

        // Check right edge - if tooltip would go off right side, flip to left of cursor
        if (position.x + tooltipSize.x > halfCanvasWidth)
        {
            position.x -= (tooltipSize.x + offset.x * 2); // Flip to left side
        }

        // Check left edge
        if (position.x < -halfCanvasWidth)
        {
            position.x = -halfCanvasWidth;
        }

        // Check bottom edge - if tooltip would go off bottom, flip to top of cursor
        if (position.y - tooltipSize.y < -halfCanvasHeight)
        {
            position.y += (tooltipSize.y - offset.y * 2); // Flip to top side
        }

        // Check top edge
        if (position.y > halfCanvasHeight)
        {
            position.y = halfCanvasHeight;
        }

        rectTransform.localPosition = position;
    }
}
