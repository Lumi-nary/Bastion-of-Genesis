using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Building feature for factories that produce workers.
/// Players manually queue workers for production at resource cost.
/// </summary>
[CreateAssetMenu(fileName = "Feature_WorkerFactory", menuName = "Planetfall/Building Features/Worker Factory")]
public class WorkerFactoryFeature : BuildingFeature
{
    [Header("Factory Configuration")]
    [Tooltip("Type of worker this factory produces")]
    public WorkerData workerType;

    [Tooltip("Time in seconds to produce one worker")]
    public float productionTime = 10f;

    [Tooltip("Maximum workers that can be queued")]
    public int maxQueueSize = 4;

    /// <summary>
    /// Get the worker type this factory produces.
    /// </summary>
    public WorkerData GetWorkerType() => workerType;

    /// <summary>
    /// Get production time per worker.
    /// </summary>
    public float GetProductionTime() => productionTime;

    /// <summary>
    /// Get max queue size.
    /// </summary>
    public int GetMaxQueueSize() => maxQueueSize;
}

/// <summary>
/// Runtime component attached to buildings with WorkerFactoryFeature.
/// Handles production queue and timing.
/// </summary>
public class WorkerFactoryComponent : MonoBehaviour
{
    private Building building;
    private WorkerFactoryFeature factoryFeature;

    // Production state
    private int queueCount = 0;
    private float productionProgress = 0f;
    private bool isProducing = false;

    // Events
    public event System.Action OnQueueChanged;
    public event System.Action<float> OnProgressChanged;

    public int QueueCount => queueCount;
    public int MaxQueueSize => factoryFeature?.GetMaxQueueSize() ?? 0;
    public float ProductionProgress => productionProgress;
    public float ProductionTime => factoryFeature?.GetProductionTime() ?? 1f;
    public bool IsProducing => isProducing;
    public WorkerData WorkerType => factoryFeature?.GetWorkerType();
    public Building Building => building;

    public void Initialize(Building building, WorkerFactoryFeature feature)
    {
        this.building = building;
        this.factoryFeature = feature;

        // Register with BuildingManager
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterFactory(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from BuildingManager
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.UnregisterFactory(this);
        }
    }

    private void Update()
    {
        if (!isProducing || factoryFeature == null) return;

        productionProgress += Time.deltaTime;
        OnProgressChanged?.Invoke(productionProgress);

        if (productionProgress >= factoryFeature.GetProductionTime())
        {
            CompleteProduction();
        }
    }

    /// <summary>
    /// Queue a worker for production.
    /// </summary>
    public bool QueueWorker()
    {
        if (factoryFeature == null) return false;
        if (queueCount >= factoryFeature.GetMaxQueueSize()) return false;

        // Check resources
        WorkerData workerType = factoryFeature.GetWorkerType();
        if (workerType == null) return false;

        if (!HasEnoughResources(workerType.cost)) return false;

        // Spend resources
        SpendResources(workerType.cost);

        queueCount++;
        OnQueueChanged?.Invoke();

        // Start production if not already producing
        if (!isProducing)
        {
            StartProduction();
        }

        return true;
    }

    /// <summary>
    /// Cancel one worker from queue.
    /// </summary>
    public bool CancelWorker()
    {
        if (queueCount <= 0) return false;

        // Refund resources for the cancelled worker
        WorkerData workerType = factoryFeature.GetWorkerType();
        if (workerType != null)
        {
            RefundResources(workerType.cost);
        }

        queueCount--;
        OnQueueChanged?.Invoke();

        // If we cancelled the one being produced, reset progress
        if (queueCount == 0)
        {
            StopProduction();
        }

        return true;
    }

    private void StartProduction()
    {
        isProducing = true;
        productionProgress = 0f;
        OnProgressChanged?.Invoke(productionProgress);
    }

    private void StopProduction()
    {
        isProducing = false;
        productionProgress = 0f;
        OnProgressChanged?.Invoke(productionProgress);
    }

    private void CompleteProduction()
    {
        // Add worker to pool
        WorkerData workerType = factoryFeature.GetWorkerType();
        if (workerType != null && WorkerManager.Instance != null)
        {
            WorkerManager.Instance.ReturnWorker(workerType);
            Debug.Log($"[WorkerFactory] Produced {workerType.workerName}");
        }

        queueCount--;
        OnQueueChanged?.Invoke();

        // Continue with next in queue or stop
        if (queueCount > 0)
        {
            StartProduction();
        }
        else
        {
            StopProduction();
        }
    }

    private bool HasEnoughResources(List<ResourceCost> cost)
    {
        if (ResourceManager.Instance == null) return false;

        foreach (var resourceCost in cost)
        {
            if (ResourceManager.Instance.GetResourceAmount(resourceCost.resourceType) < resourceCost.amount)
            {
                return false;
            }
        }
        return true;
    }

    private void SpendResources(List<ResourceCost> cost)
    {
        if (ResourceManager.Instance == null) return;

        foreach (var resourceCost in cost)
        {
            ResourceManager.Instance.RemoveResource(resourceCost.resourceType, resourceCost.amount);
        }
    }

    private void RefundResources(List<ResourceCost> cost)
    {
        if (ResourceManager.Instance == null) return;

        foreach (var resourceCost in cost)
        {
            ResourceManager.Instance.AddResource(resourceCost.resourceType, resourceCost.amount);
        }
    }
}
