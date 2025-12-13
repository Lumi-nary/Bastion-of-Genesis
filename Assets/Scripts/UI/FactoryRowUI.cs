using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI row for a worker type's factories.
/// Shows: [Icon] Factory Name - X/Y (queued/max) [Progress Bar] [Assemble] [Cancel]
/// </summary>
public class FactoryRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image workerIcon;
    [SerializeField] private TextMeshProUGUI factoryNameText;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button assembleButton;
    [SerializeField] private Button cancelButton;

    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;

    private WorkerData workerType;
    private List<WorkerFactoryComponent> factories = new List<WorkerFactoryComponent>();

    public void Initialize(WorkerData workerType)
    {
        this.workerType = workerType;

        // Set icon
        if (workerIcon != null && workerType.icon != null)
        {
            workerIcon.sprite = workerType.icon;
        }

        // Set name
        if (factoryNameText != null)
        {
            factoryNameText.text = $"{workerType.workerName} Factory";
        }

        // Set cost text
        if (costText != null && workerType.cost != null && workerType.cost.Count > 0)
        {
            costText.text = GetCostString(workerType.cost);
        }

        // Setup buttons
        if (assembleButton != null)
        {
            assembleButton.onClick.AddListener(OnAssembleClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // Get factories and subscribe to events
        RefreshFactories();
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from factory events
        foreach (var factory in factories)
        {
            if (factory != null)
            {
                factory.OnQueueChanged -= UpdateDisplay;
                factory.OnProgressChanged -= UpdateProgress;
            }
        }
    }

    private void RefreshFactories()
    {
        // Unsubscribe from old factories
        foreach (var factory in factories)
        {
            if (factory != null)
            {
                factory.OnQueueChanged -= UpdateDisplay;
                factory.OnProgressChanged -= UpdateProgress;
            }
        }

        // Get current factories
        if (BuildingManager.Instance != null)
        {
            factories = BuildingManager.Instance.GetFactoriesForWorkerType(workerType);
        }

        // Subscribe to new factories
        foreach (var factory in factories)
        {
            if (factory != null)
            {
                factory.OnQueueChanged += UpdateDisplay;
                factory.OnProgressChanged += UpdateProgress;
            }
        }
    }

    public void UpdateDisplay()
    {
        if (BuildingManager.Instance == null || workerType == null) return;

        // Refresh factory list in case it changed
        RefreshFactories();

        int factoryCount = factories.Count;
        int totalQueued = BuildingManager.Instance.GetTotalQueuedForType(workerType);
        int totalMax = BuildingManager.Instance.GetTotalMaxQueueForType(workerType);

        // Update queue text: "4x Builder Factory - 2/16"
        if (queueText != null)
        {
            queueText.text = $"{factoryCount}x - {totalQueued}/{totalMax}";
        }

        // Update button states
        if (assembleButton != null)
        {
            assembleButton.interactable = totalQueued < totalMax && CanAfford() && !IsWorkerAtCapacity();
        }
        if (cancelButton != null)
        {
            cancelButton.interactable = totalQueued > 0;
        }

        // Update progress
        UpdateProgressFromFactories();
    }

    private void Update()
    {
        // Continuously update progress display
        UpdateProgressFromFactories();
    }

    private void UpdateProgress(float progress)
    {
        UpdateProgressFromFactories();
    }

    private void UpdateProgressFromFactories()
    {
        // Find a factory that is currently producing
        WorkerFactoryComponent producingFactory = null;
        foreach (var factory in factories)
        {
            if (factory != null && factory.IsProducing)
            {
                producingFactory = factory;
                break;
            }
        }

        if (producingFactory != null)
        {
            float progress = producingFactory.ProductionProgress;
            float maxTime = producingFactory.ProductionTime;
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

    private void OnAssembleClicked()
    {
        if (BuildingManager.Instance != null && workerType != null)
        {
            BuildingManager.Instance.QueueWorker(workerType);
            UpdateDisplay();
        }
    }

    private void OnCancelClicked()
    {
        if (BuildingManager.Instance != null && workerType != null)
        {
            BuildingManager.Instance.CancelWorker(workerType);
            UpdateDisplay();
        }
    }

    private bool CanAfford()
    {
        if (workerType == null || workerType.cost == null) return true;
        if (ResourceManager.Instance == null) return false;

        foreach (var cost in workerType.cost)
        {
            if (ResourceManager.Instance.GetResourceAmount(cost.resourceType) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsWorkerAtCapacity()
    {
        if (workerType == null || WorkerManager.Instance == null || BuildingManager.Instance == null)
            return false;

        int currentWorkers = WorkerManager.Instance.GetAvailableWorkerCount(workerType);
        int totalQueued = BuildingManager.Instance.GetTotalQueuedForType(workerType);
        int capacity = WorkerManager.Instance.GetWorkerCapacity(workerType);

        return currentWorkers + totalQueued >= capacity;
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
