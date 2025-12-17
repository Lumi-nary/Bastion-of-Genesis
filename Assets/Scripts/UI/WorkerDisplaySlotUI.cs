using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkerDisplaySlotUI : MonoBehaviour
{
    [SerializeField] private Image workerIcon;
    [SerializeField] private TextMeshProUGUI workerAmountText;

    private WorkerData workerData;
    private TooltipTrigger tooltipTrigger;

    public WorkerData WorkerData => workerData;

    private void Awake()
    {
        // Get or add TooltipTrigger component
        tooltipTrigger = GetComponent<TooltipTrigger>();
        if (tooltipTrigger == null)
        {
            tooltipTrigger = gameObject.AddComponent<TooltipTrigger>();
        }
    }

    public void Setup(WorkerData data, int amount, int capacity)
    {
        workerData = data;

        if (workerData != null && workerData.icon != null)
        {
            workerIcon.sprite = workerData.icon;
        }

        UpdateAmount(amount, capacity);

        // Setup tooltip
        if (tooltipTrigger != null && workerData != null)
        {
            tooltipTrigger.SetTooltipProvider(workerData);
        }
    }

    public void UpdateAmount(int amount, int capacity)
    {
        workerAmountText.text = $"{amount}/{capacity}";
    }
}
