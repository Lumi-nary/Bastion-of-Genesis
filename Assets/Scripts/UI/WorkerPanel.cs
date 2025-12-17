using System.Collections.Generic;
using UnityEngine;

public class WorkerPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject workerSlotPrefab;
    [SerializeField] private Transform container;

    private Dictionary<WorkerData, WorkerDisplaySlotUI> workerSlots = new Dictionary<WorkerData, WorkerDisplaySlotUI>();

    private void Start()
    {
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.OnWorkerCountChanged += UpdateWorkerDisplay;
            InitializePanel();
        }
    }

    private void OnDestroy()
    {
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.OnWorkerCountChanged -= UpdateWorkerDisplay;
        }
    }

    private void InitializePanel()
    {
        // Clear any existing slots in case of re-initialization
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        workerSlots.Clear();

        // Create slots for all workers that already exist in the manager
        if (WorkerManager.Instance != null)
        {
            foreach (var worker in WorkerManager.Instance.AvailableWorkers)
            {
                UpdateWorkerDisplay(worker.Key, worker.Value);
            }
        }
    }

    private void UpdateWorkerDisplay(WorkerData workerData, int amount)
    {
        int capacity = WorkerManager.Instance.GetWorkerCapacity(workerData);

        if (workerSlots.ContainsKey(workerData))
        {
            // Slot exists
            if (amount > 0)
            {
                // Update existing slot
                workerSlots[workerData].UpdateAmount(amount, capacity);
            }
            else
            {
                // Amount is zero or negative, destroy the slot
                Destroy(workerSlots[workerData].gameObject);
                workerSlots.Remove(workerData);
            }
        }
        else
        {
            // Slot does not exist
            if (amount > 0)
            {
                // Create a new slot
                GameObject slotGO = Instantiate(workerSlotPrefab, container);
                WorkerDisplaySlotUI slotUI = slotGO.GetComponent<WorkerDisplaySlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(workerData, amount, capacity);
                    workerSlots.Add(workerData, slotUI);
                }
            }
        }
    }
}
