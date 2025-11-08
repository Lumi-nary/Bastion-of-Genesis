using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Building Data")]
    [SerializeField] private BuildingData buildingData;
    public BuildingData BuildingData => buildingData;

    private List<WorkerData> assignedWorkers = new List<WorkerData>();

    [Header("Grid Information")]
    public Vector2Int gridPosition;
    public int width;
    public int height;

    private float generationTimer = 0f;
    public float CurrentHealth { get; private set; }

    // Property to check if the building has the minimum required workers to function
    public bool IsOperational
    {
        get
        {
            if (buildingData.workerRequirements.Count == 0) return true; // No workers required, so always operational

            foreach (var req in buildingData.workerRequirements)
            {
                if (GetAssignedWorkerCount(req.workerType) < req.requiredCount)
                {
                    return false; // Missing required workers
                }
            }
            return true;
        }
    }

    private void Start()
    {
        CurrentHealth = buildingData.maxHealth;
    }

    private void Update()
    {
        if (IsOperational && buildingData.generatedResourceType != null && buildingData.generationAmount > 0)
        {
            GenerateResources();
        }
    }

    private void GenerateResources()
    {
        // Timer-based generation instead of accumulating per frame
        generationTimer += Time.deltaTime;

        if (generationTimer >= buildingData.generationInterval)
        {
            generationTimer -= buildingData.generationInterval;

            // Simple efficiency model: generation is proportional to the number of assigned workers vs. total capacity.
            // This can be made more complex later.
            float efficiency = (float)GetTotalAssignedWorkerCount() / GetTotalWorkerCapacity();
            int resourcesToGenerate = Mathf.FloorToInt(buildingData.generationAmount * efficiency);

            if (resourcesToGenerate > 0)
            {
                ResourceManager.Instance.AddResource(buildingData.generatedResourceType, resourcesToGenerate);
            }
        }
    }

    public int GetTotalAssignedWorkerCount()
    {
        return assignedWorkers.Count;
    }

    public int GetAssignedWorkerCount(WorkerData workerData)
    {
        return assignedWorkers.Count(w => w == workerData);
    }

    public int GetTotalWorkerCapacity()
    {
        if (buildingData.capacityType == WorkerCapacityType.Shared)
        {
            return buildingData.totalWorkerCapacity;
        }
        else // PerType
        {
            // Sum of all individual capacities
            return buildingData.workerRequirements.Sum(req => req.capacity);
        }
    }

    public int GetCapacityForWorker(WorkerData workerData)
    {
        if (buildingData.capacityType == WorkerCapacityType.Shared)
        {
            return buildingData.totalWorkerCapacity;
        }
        else // PerType
        {
            var requirement = buildingData.workerRequirements.FirstOrDefault(r => r.workerType == workerData);
            return requirement != null ? requirement.capacity : 0;
        }
    }

    public bool AssignWorker(WorkerData workerData)
    {
        var requirement = buildingData.workerRequirements.FirstOrDefault(r => r.workerType == workerData);
        if (requirement == null) 
        {
            Debug.LogWarning($"Worker type {workerData.name} is not allowed in {buildingData.name}.");
            return false; // This worker type is not allowed
        }

        // Check capacity
        if (buildingData.capacityType == WorkerCapacityType.Shared)
        {
            if (GetTotalAssignedWorkerCount() >= GetTotalWorkerCapacity()) return false; // Shared capacity is full
        }
        else // PerType
        {
            if (GetAssignedWorkerCount(workerData) >= requirement.capacity) return false; // This type's capacity is full
        }

        // Try to assign the worker from the manager
        if (WorkerManager.Instance.AssignWorker(workerData))
        {
            assignedWorkers.Add(workerData);
            return true;
        }

        return false;
    }

    public void RemoveWorker(WorkerData workerData)
    {
        // Find the specific worker to remove. Using reference equality.
        WorkerData workerToRemove = assignedWorkers.FirstOrDefault(w => w == workerData);
        if (workerToRemove != null)
        {
            assignedWorkers.Remove(workerToRemove);
            WorkerManager.Instance.ReturnWorker(workerData);
        }
    }
}
