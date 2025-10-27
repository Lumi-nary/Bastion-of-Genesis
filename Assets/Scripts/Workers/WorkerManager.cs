using System.Collections.Generic;
using UnityEngine;

public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance { get; private set; }

    private Dictionary<WorkerData, int> availableWorkers = new Dictionary<WorkerData, int>();

    public event System.Action<WorkerData, int> OnWorkerCountChanged;

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
}
