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
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Must use the root canvas for coordinate conversion
        Canvas c = GetComponentInParent<Canvas>();
        rootCanvas = c != null ? c.rootCanvas : c;
        canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;

        // Tooltip pivot must be top-left for positioning to work correctly
        rectTransform.pivot = new Vector2(0, 1);

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

        // Let layout calculate natural size, only constrain max width
        if (layoutElement != null)
        {
            // Measure natural text width to avoid stretching short tooltips
            float headerWidth = headerText.GetPreferredValues(header).x;
            float descWidth = descriptionText.GetPreferredValues(description).x;
            float contentWidth = Mathf.Max(headerWidth, descWidth) + 20f; // padding

            layoutElement.enabled = contentWidth > maxWidth;
            layoutElement.preferredWidth = maxWidth;
            layoutElement.preferredHeight = -1;
        }

        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdatePosition(Vector2 mousePosition)
    {
        if (rootCanvas == null || canvasRect == null) return;

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        // Convert screen mouse position to the tooltip's parent local space
        RectTransform parentRect = rectTransform.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mousePosition, cam, out localPoint);

        Vector2 tooltipSize = rectTransform.rect.size;
        Vector2 parentSize = parentRect.rect.size;

        // Parent rect bounds (assuming pivot at center)
        float minX = parentRect.rect.xMin;
        float maxX = parentRect.rect.xMax;
        float minY = parentRect.rect.yMin;
        float maxY = parentRect.rect.yMax;

        // Default: tooltip to the right and below cursor (pivot is top-left)
        float x = localPoint.x + offset.x;
        float y = localPoint.y + offset.y;

        // Flip left if it would go off the right edge
        if (x + tooltipSize.x > maxX)
            x = localPoint.x - offset.x - tooltipSize.x;

        // Flip up if it would go off the bottom edge
        if (y - tooltipSize.y < minY)
            y = localPoint.y - offset.y + tooltipSize.y;

        // Clamp to parent bounds
        x = Mathf.Clamp(x, minX, maxX - tooltipSize.x);
        y = Mathf.Clamp(y, minY + tooltipSize.y, maxY);

        rectTransform.localPosition = new Vector3(x, y, 0);
    }
}
