using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration for starting workers in a chapter.
/// Used by ChapterData to define initial worker counts.
/// Base capacity comes from WorkerData.baseCapacity.
/// </summary>
[System.Serializable]
public class WorkerStartConfig
{
    public WorkerData workerData;
    public int initialCount;
}

public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance { get; private set; }

    private Dictionary<WorkerData, int> availableWorkers = new Dictionary<WorkerData, int>();
    private Dictionary<WorkerData, int> workerCapacities = new Dictionary<WorkerData, int>();

    // Track registered worker types for reset
    private HashSet<WorkerData> registeredTypes = new HashSet<WorkerData>();

    public event System.Action<WorkerData, int> OnWorkerCountChanged;

    // Public property to allow UI to query all workers
    public Dictionary<WorkerData, int> AvailableWorkers => availableWorkers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // WorkerManager persists across scenes but is RESET by MissionChapterManager
        // when starting each chapter. ChapterData handles starting counts.
        DontDestroyOnLoad(gameObject);

        // No auto-initialization - ChapterData handles starting counts
        // MissionChapterManager.InitializeChapterWorkers() will register types and set counts
    }

    /// <summary>
    /// Register a worker type with its base capacity. Called by MissionChapterManager.
    /// </summary>
    public void RegisterWorkerType(WorkerData workerData, int startingCount = 0)
    {
        if (workerData == null) return;

        registeredTypes.Add(workerData);
        workerCapacities[workerData] = workerData.baseCapacity;
        availableWorkers[workerData] = startingCount;
        OnWorkerCountChanged?.Invoke(workerData, startingCount);

        Debug.Log($"[WorkerManager] Registered {workerData.workerName}: {startingCount}/{workerData.baseCapacity}");
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

    /// <summary>
    /// Set worker count directly. Used for network sync.
    /// </summary>
    public void SetWorkerCount(WorkerData workerData, int count)
    {
        if (workerData == null || !availableWorkers.ContainsKey(workerData)) return;

        int capacity = GetWorkerCapacity(workerData);
        availableWorkers[workerData] = Mathf.Clamp(count, 0, capacity);
        OnWorkerCountChanged?.Invoke(workerData, availableWorkers[workerData]);
    }

    /// <summary>
    /// Reset all workers to zero and capacities to base values.
    /// Called by MissionChapterManager when starting a new chapter.
    /// </summary>
    public void ResetAllWorkers()
    {
        // Clear everything for fresh chapter start
        availableWorkers.Clear();
        workerCapacities.Clear();
        registeredTypes.Clear();

        Debug.Log("[WorkerManager] All workers reset (counts, capacities, registrations cleared)");
    }

    /// <summary>
    /// Add to the capacity of a worker type. Used by buildings and research.
    /// </summary>
    public void AddCapacity(WorkerData workerData, int amount)
    {
        if (workerData == null || !workerCapacities.ContainsKey(workerData)) return;

        workerCapacities[workerData] += amount;
        Debug.Log($"[WorkerManager] {workerData.workerName} capacity increased by {amount} to {workerCapacities[workerData]}");
    }

    /// <summary>
    /// Remove from the capacity of a worker type. Used when buildings are destroyed.
    /// </summary>
    public void RemoveCapacity(WorkerData workerData, int amount)
    {
        if (workerData == null || !workerCapacities.ContainsKey(workerData)) return;

        workerCapacities[workerData] = Mathf.Max(0, workerCapacities[workerData] - amount);

        // Note: We don't remove workers if over capacity, they just can't train more
        Debug.Log($"[WorkerManager] {workerData.workerName} capacity decreased by {amount} to {workerCapacities[workerData]}");
    }
}
