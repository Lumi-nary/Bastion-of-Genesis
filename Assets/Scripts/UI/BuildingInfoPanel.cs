using UnityEngine;
using TMPro;

public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI workerCountText;
    // Note: You will need to assign a specific WorkerData for the buttons to use.
    // For a more advanced implementation, you would have different buttons for different worker types.
    [SerializeField] private WorkerData workerTypeToAdd;

    private Building currentBuilding;

    private void Update()
    {
        // Continuously update the worker count while the panel is active
        if (panel.activeSelf && currentBuilding != null)
        {
            UpdateWorkerCount();
        }
    }

    public void ShowPanel(Building building)
    {
        currentBuilding = building;
        panel.SetActive(true);
        buildingNameText.text = currentBuilding.BuildingData.buildingName;
        UpdateWorkerCount();
    }

    public void HidePanel()
    {
        currentBuilding = null;
        panel.SetActive(false);
    }

    public void OnAddWorkerClicked()
    {
        if (currentBuilding != null && workerTypeToAdd != null)
        {
            currentBuilding.AssignWorker(workerTypeToAdd);
        }
    }

    public void OnRemoveWorkerClicked()
    {
        if (currentBuilding != null && workerTypeToAdd != null)
        {
            currentBuilding.RemoveWorker(workerTypeToAdd);
        }
    }

    private void UpdateWorkerCount()
    {
        if (currentBuilding != null)
        {
            // This is a simplified display. A real implementation would need to know which worker type to count.
            // For now, we'll just count all assigned workers.
            int assigned = currentBuilding.GetAssignedWorkerCount();
            int capacity = currentBuilding.GetWorkerCapacity();
            workerCountText.text = $"Workers: {assigned} / {capacity}";
        }
    }
}
