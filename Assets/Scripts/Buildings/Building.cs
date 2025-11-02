using System;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Building Data")]
    [SerializeField] private BuildingData buildingData;
    public BuildingData BuildingData => buildingData;

    [Header("Worker Settings")]
    [SerializeField] private int workerCapacity;
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
        if (buildingData.generatedResourceType != null && buildingData.generationRate > 0)
        {
            GenerateResources();
        }
    }

    private void GenerateResources()
    {
        accumulatedResources += buildingData.generationRate * assignedWorkers.Count * Time.deltaTime;

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

    public int GetWorkerCapacity()
    {
        return workerCapacity;
    }


    public bool AssignWorker(WorkerData workerData)
    {
        if (assignedWorkers.Count < workerCapacity)
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