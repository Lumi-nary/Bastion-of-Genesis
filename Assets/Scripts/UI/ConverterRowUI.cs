using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI row for a resource converter.
/// Shows: [Icon] Resource Name - X/Y (queued/max) [Progress Bar] [Convert] [Cancel]
/// </summary>
public class ConverterRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceNameText;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button convertButton;
    [SerializeField] private Button cancelButton;

    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;

    private ResourceType resourceType;
    private List<ResourceConverterComponent> converters = new List<ResourceConverterComponent>();

    public void Initialize(ResourceType resourceType)
    {
        this.resourceType = resourceType;

        // Set icon
        if (resourceIcon != null && resourceType.Icon != null)
        {
            resourceIcon.sprite = resourceType.Icon;
        }

        // Set name
        if (resourceNameText != null)
        {
            resourceNameText.text = resourceType.ResourceName;
        }

        // Setup buttons
        if (convertButton != null)
        {
            convertButton.onClick.AddListener(OnConvertClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // Get converters and subscribe to events
        RefreshConverters();
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from converter events
        foreach (var converter in converters)
        {
            if (converter != null)
            {
                converter.OnQueueChanged -= UpdateDisplay;
                converter.OnProgressChanged -= UpdateProgress;
            }
        }
    }

    private void RefreshConverters()
    {
        // Unsubscribe from old converters
        foreach (var converter in converters)
        {
            if (converter != null)
            {
                converter.OnQueueChanged -= UpdateDisplay;
                converter.OnProgressChanged -= UpdateProgress;
            }
        }

        // Get current converters
        if (BuildingManager.Instance != null)
        {
            converters = BuildingManager.Instance.GetConvertersForResourceType(resourceType);
        }

        // Subscribe to new converters
        foreach (var converter in converters)
        {
            if (converter != null)
            {
                converter.OnQueueChanged += UpdateDisplay;
                converter.OnProgressChanged += UpdateProgress;
            }
        }

        // Set cost text from first converter
        if (costText != null && converters.Count > 0 && converters[0].InputCost != null)
        {
            costText.text = GetCostString(converters[0].InputCost);
        }
    }

    public void UpdateDisplay()
    {
        if (BuildingManager.Instance == null || resourceType == null) return;

        // Refresh converter list in case it changed
        RefreshConverters();

        int converterCount = converters.Count;
        int totalQueued = BuildingManager.Instance.GetTotalQueuedConversions(resourceType);
        int totalMax = BuildingManager.Instance.GetTotalMaxConversionQueue(resourceType);

        // Update queue text
        if (queueText != null)
        {
            queueText.text = $"{converterCount}x - {totalQueued}/{totalMax}";
        }

        // Update button states
        if (convertButton != null)
        {
            convertButton.interactable = totalQueued < totalMax && CanAfford() && !IsResourceAtCapacity();
        }
        if (cancelButton != null)
        {
            cancelButton.interactable = totalQueued > 0;
        }

        // Update progress
        UpdateProgressFromConverters();
    }

    private void UpdateProgress(float progress)
    {
        UpdateProgressFromConverters();
    }

    private void Update()
    {
        // Continuously update progress display
        UpdateProgressFromConverters();
    }

    private void UpdateProgressFromConverters()
    {
        // Find a converter that is currently converting
        ResourceConverterComponent activeConverter = null;
        foreach (var converter in converters)
        {
            if (converter != null && converter.IsConverting)
            {
                activeConverter = converter;
                break;
            }
        }

        if (activeConverter != null)
        {
            float progress = activeConverter.ConversionProgress;
            float maxTime = activeConverter.ConversionTime;
            float percentage = (progress / maxTime) * 100f;

            if (progressBar != null)
            {
                progressBar.value = progress / maxTime;
            }

            if (progressText != null)
            {
                progressText.text = $"{percentage:F0}%";
            }
        }
        else
        {
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }
            if (progressText != null)
            {
                progressText.text = "0%";
            }
        }
    }

    private void OnConvertClicked()
    {
        if (BuildingManager.Instance != null && resourceType != null)
        {
            BuildingManager.Instance.QueueConversion(resourceType);
            UpdateDisplay();
        }
    }

    private void OnCancelClicked()
    {
        if (BuildingManager.Instance != null && resourceType != null)
        {
            BuildingManager.Instance.CancelConversion(resourceType);
            UpdateDisplay();
        }
    }

    private bool CanAfford()
    {
        if (converters.Count == 0) return false;

        var inputCost = converters[0].InputCost;
        if (inputCost == null) return true;
        if (ResourceManager.Instance == null) return false;

        foreach (var cost in inputCost)
        {
            if (ResourceManager.Instance.GetResourceAmount(cost.resourceType) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsResourceAtCapacity()
    {
        if (resourceType == null || ResourceManager.Instance == null || BuildingManager.Instance == null)
            return false;

        if (converters.Count == 0) return false;

        int outputAmount = converters[0].OutputAmount;
        int currentAmount = ResourceManager.Instance.GetResourceAmount(resourceType);
        int totalQueued = BuildingManager.Instance.GetTotalQueuedConversions(resourceType) * outputAmount;
        int capacity = ResourceManager.Instance.GetResourceCapacity(resourceType);

        return currentAmount + totalQueued + outputAmount > capacity;
    }

    private string GetCostString(List<ResourceCost> costs)
    {
        if (costs == null || costs.Count == 0) return "Free";

        List<string> parts = new List<string>();
        foreach (var cost in costs)
        {
            parts.Add($"{cost.amount} {cost.resourceType.ResourceName}");
        }
        return string.Join(", ", parts);
    }
}
