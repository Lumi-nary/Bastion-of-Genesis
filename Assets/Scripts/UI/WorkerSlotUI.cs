using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkerSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI workerNameText;
    [SerializeField] private TextMeshProUGUI workerCountText;
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;

    private Building currentBuilding;
    private WorkerData workerData;

    public void Setup(Building building, WorkerData worker)
    {
        // Unsubscribe from previous building if any
        if (currentBuilding != null)
        {
            currentBuilding.OnWorkersChanged -= UpdateUI;
        }

        currentBuilding = building;
        workerData = worker;

        // Subscribe to new building
        if (currentBuilding != null)
        {
            currentBuilding.OnWorkersChanged += UpdateUI;
        }

        workerNameText.text = workerData.workerName;
        
        // Remove old listeners to avoid duplicates
        addButton.onClick.RemoveAllListeners();
        removeButton.onClick.RemoveAllListeners();
        
        addButton.onClick.AddListener(OnAddWorker);
        removeButton.onClick.AddListener(OnRemoveWorker);

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (currentBuilding != null)
        {
            currentBuilding.OnWorkersChanged -= UpdateUI;
        }
    }

    private void OnAddWorker()
    {
        if (currentBuilding != null && workerData != null)
        {
            if (NetworkedBuildingManager.Instance != null && NetworkedBuildingManager.Instance.IsClientStarted)
            {
                NetworkedBuildingManager.Instance.RequestAssignWorker(currentBuilding, workerData);
            }
            else
            {
                currentBuilding.AssignWorker(workerData);
            }
        }
    }

    private void OnRemoveWorker()
    {
        if (currentBuilding != null && workerData != null)
        {
            if (NetworkedBuildingManager.Instance != null && NetworkedBuildingManager.Instance.IsClientStarted)
            {
                NetworkedBuildingManager.Instance.RequestRemoveWorker(currentBuilding, workerData);
            }
            else
            {
                currentBuilding.RemoveWorker(workerData);
            }
        }
    }

    public void UpdateUI()
    {
        if (currentBuilding != null && workerData != null)
        {
            int assignedCount = currentBuilding.GetAssignedWorkerCount(workerData);
            int totalAssigned = currentBuilding.GetTotalAssignedWorkerCount();
            int totalCapacity = currentBuilding.GetTotalWorkerCapacity();

            if (currentBuilding.BuildingData.capacityType == WorkerCapacityType.PerType)
            {
                int typeCapacity = currentBuilding.GetCapacityForWorker(workerData);
                workerCountText.text = $"{assignedCount} / {typeCapacity}";
                addButton.interactable = assignedCount < typeCapacity && WorkerManager.Instance.GetAvailableWorkerCount(workerData) > 0;
            }
            else // Shared Capacity
            {
                workerCountText.text = $"{assignedCount}"; // For shared, just show the count for this type
                addButton.interactable = totalAssigned < totalCapacity && WorkerManager.Instance.GetAvailableWorkerCount(workerData) > 0;
            }

            // Disable remove button if there are no workers of this type to remove
            removeButton.interactable = assignedCount > 0;
        }
    }
}
