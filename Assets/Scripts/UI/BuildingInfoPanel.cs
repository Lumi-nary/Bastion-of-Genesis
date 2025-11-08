using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI resourceGenerationText;
    [SerializeField] private TextMeshProUGUI statusText; // To show operational status
    [SerializeField] private TextMeshProUGUI totalWorkerCountText; // To show total assigned workers / total capacity

    [Header("Worker Slot UI")]
    [SerializeField] private GameObject workerSlotPrefab; // A prefab for displaying one worker type
    [SerializeField] private Transform workerSlotsContainer; // The parent object for the worker slots

    private Building currentBuilding;
    private List<WorkerSlotUI> currentWorkerSlots = new List<WorkerSlotUI>();

    private void Awake()
    {
        panel.SetActive(false);
    }

    private void Update()
    {
        // Continuously update the UI while the panel is active
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
        
        // Clear old worker slots
        foreach (Transform child in workerSlotsContainer)
        {
            Destroy(child.gameObject);
        }
        currentWorkerSlots.Clear();

        // Create new worker slots based on building data
        foreach (var requirement in currentBuilding.BuildingData.workerRequirements)
        {
            GameObject slotGO = Instantiate(workerSlotPrefab, workerSlotsContainer);
            WorkerSlotUI slotUI = slotGO.GetComponent<WorkerSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(currentBuilding, requirement.workerType);
                currentWorkerSlots.Add(slotUI);
            }
        }

        UpdatePanelUI();
    }

    public void HidePanel()
    {
        currentBuilding = null;
        panel.SetActive(false);
    }

    private void UpdatePanelUI()
    {
        if (currentBuilding != null)
        {
            // Health
            healthText.text = $"Health: {currentBuilding.CurrentHealth} / {currentBuilding.BuildingData.maxHealth}";

            // Total Worker Count
            totalWorkerCountText.text = $"Total Workers: {currentBuilding.GetTotalAssignedWorkerCount()} / {currentBuilding.GetTotalWorkerCapacity()}";

            // Status Text
            if (currentBuilding.IsOperational)
            {
                statusText.text = "Status: Operational";
                statusText.color = Color.green;
                resourceGenerationText.gameObject.SetActive(true);

                // Resource Generation Text
                if (currentBuilding.BuildingData.generatedResourceType != null && currentBuilding.BuildingData.generationAmount > 0)
                {
                    float efficiency = (float)currentBuilding.GetTotalAssignedWorkerCount() / currentBuilding.GetTotalWorkerCapacity();
                    float effectiveRate = currentBuilding.BuildingData.generationAmount * efficiency;
                    resourceGenerationText.text = $"Generates: {effectiveRate:F2} {currentBuilding.BuildingData.generatedResourceType.ResourceName}/s";
                }
                else
                {
                    resourceGenerationText.text = "Generates: None";
                }
            }
            else
            {
                statusText.text = "Status: Needs Workers";
                statusText.color = Color.red;
                resourceGenerationText.gameObject.SetActive(false);
            }

            // Update all worker slot UIs
            foreach (var slot in currentWorkerSlots)
            {
                slot.UpdateUI();
            }
        }
    }
}

