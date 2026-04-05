using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top bar worker display. Spawns a slot for each registered worker type.
/// Flat horizontal layout — no accordion. Replaces old WorkerPanel + AccordionPanel.
/// </summary>
public class TopBarWorkerGroup : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject workerSlotPrefab;

    private Dictionary<WorkerData, WorkerDisplaySlotUI> slots = new Dictionary<WorkerData, WorkerDisplaySlotUI>();

    private bool initialized;

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!initialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        if (initialized) return;
        if (WorkerManager.Instance == null) return;

        WorkerManager.Instance.OnWorkerCountChanged += OnWorkerCountChanged;
        InitializeSlots();
        initialized = true;
    }

    private void OnDestroy()
    {
        if (WorkerManager.Instance != null)
            WorkerManager.Instance.OnWorkerCountChanged -= OnWorkerCountChanged;
    }

    private void InitializeSlots()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        slots.Clear();

        foreach (var kvp in WorkerManager.Instance.AvailableWorkers)
            CreateOrUpdateSlot(kvp.Key, kvp.Value);
    }

    private void OnWorkerCountChanged(WorkerData workerData, int amount)
    {
        if (workerData == null) return;
        CreateOrUpdateSlot(workerData, amount);
    }

    private void CreateOrUpdateSlot(WorkerData workerData, int amount)
    {
        int capacity = WorkerManager.Instance.GetWorkerCapacity(workerData);

        if (slots.TryGetValue(workerData, out WorkerDisplaySlotUI existing))
        {
            existing.UpdateAmount(amount, capacity);
        }
        else
        {
            GameObject go = Instantiate(workerSlotPrefab, transform);
            WorkerDisplaySlotUI slot = go.GetComponent<WorkerDisplaySlotUI>();
            if (slot != null)
            {
                slot.Setup(workerData, amount, capacity);
                slots[workerData] = slot;
            }
        }
    }
}
