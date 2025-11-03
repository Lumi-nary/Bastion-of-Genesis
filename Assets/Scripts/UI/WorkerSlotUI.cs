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
        currentBuilding = building;
        workerData = worker;

        workerNameText.text = workerData.workerName;
        addButton.onClick.AddListener(OnAddWorker);
        removeButton.onClick.AddListener(OnRemoveWorker);

        UpdateUI();
    }

    private void OnAddWorker()
    {
        if (currentBuilding != null && workerData != null)
        {
            currentBuilding.AssignWorker(workerData);
            UpdateUI();
        }
    }

    private void OnRemoveWorker()
    {
        if (currentBuilding != null && workerData != null)
        {
            currentBuilding.RemoveWorker(workerData);
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (currentBuilding != null && workerData != null)
        {
            int assignedCount = currentBuilding.GetAssignedWorkerCount(workerData);
            int totalCapacity = currentBuilding.GetWorkerCapacity(); // This is total capacity, might need adjustment if capacity is per-type
            workerCountText.text = $"{assignedCount} / {totalCapacity}";

            // Disable add button if building is full or no workers are available
            addButton.interactable = assignedCount < totalCapacity && WorkerManager.Instance.GetAvailableWorkerCount(workerData) > 0;

            // Disable remove button if there are no workers of this type to remove
            removeButton.interactable = assignedCount > 0;
        }
    }
}
