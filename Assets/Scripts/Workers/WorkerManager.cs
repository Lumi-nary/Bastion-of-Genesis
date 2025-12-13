using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // Make this class serializable to show in Inspector
public class WorkerStartConfig
{
    public WorkerData workerData;
    public int initialCount;
    public int maxCapacity;
}

public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance { get; private set; }

    [Header("Worker Configuration")]
    [SerializeField] private List<WorkerStartConfig> startingWorkers = new List<WorkerStartConfig>();

    private Dictionary<WorkerData, int> availableWorkers = new Dictionary<WorkerData, int>();
    private Dictionary<WorkerData, int> workerCapacities = new Dictionary<WorkerData, int>();

    public event System.Action<WorkerData, int> OnWorkerCountChanged;

    // Public property to allow UI to query all workers
    public Dictionary<WorkerData, int> AvailableWorkers => availableWorkers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        InitializeWorkers();
    }

    private void InitializeWorkers()
    {
        foreach (var config in startingWorkers)
        {
            if (config.workerData != null)
            {
                availableWorkers[config.workerData] = config.initialCount;
                workerCapacities[config.workerData] = config.maxCapacity;
                // Notify UI or other systems about the initial count
                OnWorkerCountChanged?.Invoke(config.workerData, config.initialCount);
            }
        }
    }

    public void TrainWorker(WorkerData workerData)
    {
        if (HasEnoughResources(workerData.cost))
        {
            SpendResources(workerData.cost);
            AddWorkerToPool(workerData);
        }
        else
        {
            Debug.Log("Not enough resources to train " + workerData.workerName);
        }
    }

    public bool AssignWorker(WorkerData workerData)
    {
        if (availableWorkers.ContainsKey(workerData) && availableWorkers[workerData] > 0)
        {
            availableWorkers[workerData]--;
            OnWorkerCountChanged?.Invoke(workerData, availableWorkers[workerData]);
            return true;
        }
        return false;
    }

    public void ReturnWorker(WorkerData workerData)
    {
        AddWorkerToPool(workerData);
    }

    public int GetAvailableWorkerCount(WorkerData workerData)
    {
        if (availableWorkers.ContainsKey(workerData))
        {
            return availableWorkers[workerData];
        }
        return 0;
    }

    private void AddWorkerToPool(WorkerData workerData)
    {
        if (!availableWorkers.ContainsKey(workerData))
        {
            availableWorkers[workerData] = 0;
        }
        availableWorkers[workerData]++;
        OnWorkerCountChanged?.Invoke(workerData, availableWorkers[workerData]);
    }

    private bool HasEnoughResources(List<ResourceCost> cost)
    {
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
        foreach (var resourceCost in cost)
        {
            ResourceManager.Instance.RemoveResource(resourceCost.resourceType, resourceCost.amount);
        }
    }

    /// <summary>
    /// Get max capacity for a worker type.
    /// </summary>
    public int GetWorkerCapacity(WorkerData workerData)
    {
        if (workerCapacities.ContainsKey(workerData))
        {
            return workerCapacities[workerData];
        }
        return 0;
    }

    /// <summary>
    /// Check if there's capacity for more workers of this type.
    /// </summary>
    public bool HasCapacityFor(WorkerData workerData, int amount = 1)
    {
        int current = GetAvailableWorkerCount(workerData);
        int capacity = GetWorkerCapacity(workerData);
        return current + amount <= capacity;
    }

    /// <summary>
    /// Get remaining capacity for a worker type.
    /// </summary>
    public int GetRemainingCapacity(WorkerData workerData)
    {
        int current = GetAvailableWorkerCount(workerData);
        int capacity = GetWorkerCapacity(workerData);
        return Mathf.Max(0, capacity - current);
    }

    /// <summary>
    /// Check if worker type is at max capacity.
    /// </summary>
    public bool IsAtCapacity(WorkerData workerData)
    {
        return GetAvailableWorkerCount(workerData) >= GetWorkerCapacity(workerData);
    }
}
