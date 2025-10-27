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

    private float accumulatedResources = 0f;

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
