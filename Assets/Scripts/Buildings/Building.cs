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

    private float accumulatedResources = 0f;
    public float CurrentHealth { get; private set; }

    private void Start()
    {
        CurrentHealth = buildingData.maxHealth;
    }

    private void Update()
    {
        if (buildingData.generatedResourceType != null && buildingData.generationAmount > 0 && buildingData.generationInterval > 0)
        {
            GenerateResources();
        }
    }

    private void GenerateResources()
    {
        accumulatedResources += (buildingData.generationAmount / buildingData.generationInterval) * assignedWorkers.Count * Time.deltaTime;

        if (accumulatedResources >= 1)
        {
            int wholeResources = Mathf.FloorToInt(accumulatedResources);
            ResourceManager.Instance.AddResource(buildingData.generatedResourceType, wholeResources);
            accumulatedResources -= wholeResources;
        }
    }

    public int GetAssignedWorkerCount()
    {
        return assignedWorkers.Count;
    }

    public int GetAssignedWorkerCount(WorkerData workerData)
    {
        return assignedWorkers.Count(w => w == workerData);
    }

    public int GetWorkerCapacity()
    {
        return buildingData.workerCapacity;
    }

    public bool AssignWorker(WorkerData workerData)
    {
        // Check if this worker type is allowed and if there is capacity
        if (buildingData.allowedWorkerTypes.Contains(workerData) && assignedWorkers.Count < GetWorkerCapacity())
        {
            if (WorkerManager.Instance.AssignWorker(workerData))
            {
                assignedWorkers.Add(workerData);
                return true;
            }
        }
        return false;
    }

    public void RemoveWorker(WorkerData workerData)
    {
        if (assignedWorkers.Contains(workerData))
        {
            assignedWorkers.Remove(workerData);
            WorkerManager.Instance.ReturnWorker(workerData);
        }
    }
}