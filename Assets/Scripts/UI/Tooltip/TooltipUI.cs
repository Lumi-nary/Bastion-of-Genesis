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
    [SerializeField] private int characterWrapLimit = 50;
    [SerializeField] private Vector2 offset = new Vector2(10, -10);

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

        // Update layout
        int headerLength = headerText.text.Length;
        int descriptionLength = descriptionText.text.Length;

        if (headerLength > characterWrapLimit || descriptionLength > characterWrapLimit)
        {
            if (layoutElement != null)
            {
                layoutElement.enabled = true;
            }
        }
        else
        {
            if (layoutElement != null)
            {
                layoutElement.enabled = false;
            }
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
        if (parentCanvas == null) return;

        // Convert mouse position to canvas space
        Vector2 position = mousePosition;

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

        // Keep tooltip within canvas bounds
        Vector2 pivot = rectTransform.pivot;
        Vector3[] canvasCorners = new Vector3[4];
        (parentCanvas.transform as RectTransform).GetWorldCorners(canvasCorners);

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect.rect.size;

        // Get tooltip size
        Vector2 tooltipSize = rectTransform.rect.size;

        // Adjust position to keep within bounds
        if (position.x + tooltipSize.x > canvasSize.x / 2)
        {
            position.x = mousePosition.x - tooltipSize.x - offset.x;
        }

        if (position.y - tooltipSize.y < -canvasSize.y / 2)
        {
            position.y = mousePosition.y + tooltipSize.y - offset.y;
        }

        rectTransform.localPosition = position;
    }
}
