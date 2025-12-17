using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Content")]
    [SerializeField] private string tooltipHeader;
    [TextArea(3, 10)]
    [SerializeField] private string tooltipDescription;

    [Header("Dynamic Content")]
    [SerializeField] private bool useProvider = false;
    [Tooltip("If set, this component will be used to get tooltip content dynamically")]
    [SerializeField] private Component providerComponent;

    private ITooltipProvider tooltipProvider;

    private void Awake()
    {
        if (useProvider && providerComponent != null)
        {
            tooltipProvider = providerComponent as ITooltipProvider;
            if (tooltipProvider == null)
            {
                Debug.LogWarning($"Component {providerComponent.GetType().Name} does not implement ITooltipProvider on {gameObject.name}");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIManager.Instance == null) return;

        if (useProvider && tooltipProvider != null)
        {
            UIManager.Instance.ShowTooltipFromProvider(tooltipProvider);
        }
        else
        {
            UIManager.Instance.ShowTooltip(tooltipHeader, tooltipDescription);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideTooltip();
        }
    }

    /// <summary>
    /// Manually set tooltip content at runtime
    /// </summary>
    public void SetTooltipContent(string header, string description)
    {
        tooltipHeader = header;
        tooltipDescription = description;
        useProvider = false;
    }

    /// <summary>
    /// Manually set a tooltip provider at runtime
    /// </summary>
    public void SetTooltipProvider(ITooltipProvider provider)
    {
        tooltipProvider = provider;
        useProvider = true;
    }
}
