using UnityEngine;
using TMPro;

public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI workerCountText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI resourceGenerationText;
    // Note: You will need to assign a specific WorkerData for the buttons to use.
    // For a more advanced implementation, you would have different buttons for different worker types.
    [SerializeField] private WorkerData workerTypeToAdd;

    private Building currentBuilding;

    private void Update()
    {
        // Continuously update the worker count while the panel is active
        if (panel.activeSelf && currentBuilding != null)
        {
            UpdatePanelUI();
        }
    }

    public void ShowPanel(Building building)
    {
        currentBuilding = building;
        panel.SetActive(true);
        buildingNameText.text = currentBuilding.BuildingData.buildingName;
        UpdatePanelUI();
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

    private void UpdatePanelUI()
    {
        if (currentBuilding != null)
        {
            // Worker Count
            int assigned = currentBuilding.GetAssignedWorkerCount();
            int capacity = currentBuilding.GetWorkerCapacity();
            workerCountText.text = $"Workers: {assigned} / {capacity}";

            // Health
            healthText.text = $"Health: {currentBuilding.CurrentHealth} / {currentBuilding.BuildingData.maxHealth}";

            // Resource Generation
            if (currentBuilding.BuildingData.generatedResourceType != null && currentBuilding.BuildingData.generationRate > 0)
            {
                resourceGenerationText.text = $"Generates: {currentBuilding.BuildingData.generationRate} {currentBuilding.BuildingData.generatedResourceType.ResourceName}/s";
            }
            else
            {
                resourceGenerationText.text = "Generates: None";
            }
        }
    }
}
