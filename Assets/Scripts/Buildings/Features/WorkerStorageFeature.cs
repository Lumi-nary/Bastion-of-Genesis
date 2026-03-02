using UnityEngine;

/// <summary>
/// Worker storage feature - Increases robot resource capacity and all worker pool capacities
/// Used by Robot Bay to expand both resource and worker limits
/// </summary>
[CreateAssetMenu(fileName = "Feature_WorkerStorage", menuName = "Planetfall/Building Features/Worker Storage")]
public class WorkerStorageFeature : BuildingFeature
{
    [Header("Robot Capacity")]
    [Tooltip("Resource type for robots (RoboticSwarm)")]
    public ResourceType robotResource;

    [Tooltip("Amount to increase robot resource capacity")]
    public int robotCapacityIncrease = 5;

    [Header("Worker Capacity")]
    [Tooltip("Amount to increase each worker type's capacity")]
    public int workerCapacityIncrease = 5;

    public override void OnBuilt(Building building)
    {
        // Increase robot resource capacity
        if (ResourceManager.Instance != null && robotResource != null)
        {
            ResourceManager.Instance.AddCapacity(robotResource, robotCapacityIncrease);
            Debug.Log($"[WorkerStorage] Added {robotCapacityIncrease} robot capacity");
        }

        // Increase all worker type capacities
        if (WorkerManager.Instance != null)
        {
            foreach (var kvp in WorkerManager.Instance.AvailableWorkers)
            {
                WorkerManager.Instance.AddCapacity(kvp.Key, workerCapacityIncrease);
            }
            Debug.Log($"[WorkerStorage] Added {workerCapacityIncrease} capacity to all worker types");
        }
    }

    public override void OnDestroyed(Building building)
    {
        // Decrease robot resource capacity
        if (ResourceManager.Instance != null && robotResource != null)
        {
            ResourceManager.Instance.RemoveCapacity(robotResource, robotCapacityIncrease);
            Debug.Log($"[WorkerStorage] Removed {robotCapacityIncrease} robot capacity");
        }

        // Decrease all worker type capacities
        if (WorkerManager.Instance != null)
        {
            foreach (var kvp in WorkerManager.Instance.AvailableWorkers)
            {
                WorkerManager.Instance.RemoveCapacity(kvp.Key, workerCapacityIncrease);
            }
            Debug.Log($"[WorkerStorage] Removed {workerCapacityIncrease} capacity from all worker types");
        }
    }
}
