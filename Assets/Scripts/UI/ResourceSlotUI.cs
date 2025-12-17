using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceSlotUI : MonoBehaviour
{
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceAmountText;

    private ResourceType resourceType;
    private TooltipTrigger tooltipTrigger;

    public ResourceType ResourceType => resourceType;

    private void Awake()
    {
        // Get or add TooltipTrigger component
        tooltipTrigger = GetComponent<TooltipTrigger>();
        if (tooltipTrigger == null)
        {
            tooltipTrigger = gameObject.AddComponent<TooltipTrigger>();
        }
    }

    public void Setup(ResourceType type, int amount, int capacity)
    {
        resourceType = type;
        resourceIcon.sprite = resourceType.Icon;
        UpdateAmount(amount, capacity);

        // Setup tooltip
        if (tooltipTrigger != null && resourceType != null)
        {
            tooltipTrigger.SetTooltipProvider(resourceType);
        }
    }

    public void UpdateAmount(int amount, int capacity)
    {
        resourceAmountText.text = $"{amount}/{capacity}";
    }
}
