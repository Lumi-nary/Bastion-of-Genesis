using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WorkerDisplay : MonoBehaviour
{
    [System.Serializable]
    public class WorkerDisplayItem
    {
        public WorkerData workerData;
        public TextMeshProUGUI displayText;
    }

    [Header("UI References")]
    [SerializeField] private List<WorkerDisplayItem> workerDisplayItems = new List<WorkerDisplayItem>();

    private Dictionary<WorkerData, TextMeshProUGUI> displayMapping = new Dictionary<WorkerData, TextMeshProUGUI>();

    private void Awake()
    {
        // Create a dictionary for fast lookups
        foreach (var item in workerDisplayItems)
        {
            if (item.workerData != null && item.displayText != null)
            {
                displayMapping[item.workerData] = item.displayText;
            }
        }
    }

    private void OnEnable()
    {
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.OnWorkerCountChanged += HandleWorkerCountChanged;
            InitializeDisplay();
        }
    }

    private void OnDisable()
    {
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.OnWorkerCountChanged -= HandleWorkerCountChanged;
        }
    }

    private void InitializeDisplay()
    {
        foreach (var item in displayMapping)
        {
            int currentAmount = WorkerManager.Instance.GetAvailableWorkerCount(item.Key);
            item.Value.text = $"{item.Key.workerName}: {currentAmount}";
        }
    }

    private void HandleWorkerCountChanged(WorkerData workerData, int newAmount)
    {
        if (displayMapping.ContainsKey(workerData))
        {
            displayMapping[workerData].text = $"{workerData.workerName}: {newAmount}";
        }
    }
}
