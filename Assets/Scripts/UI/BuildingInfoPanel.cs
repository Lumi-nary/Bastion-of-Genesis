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

    [Header("Worker Slot UI")]
    [SerializeField] private GameObject workerSlotPrefab; // A prefab for displaying one worker type
    [SerializeField] private Transform workerSlotsContainer; // The parent object for the worker slots

    private Building currentBuilding;
    private List<WorkerSlotUI> currentWorkerSlots = new List<WorkerSlotUI>();

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
        foreach (var workerType in currentBuilding.BuildingData.allowedWorkerTypes)
        {
            GameObject slotGO = Instantiate(workerSlotPrefab, workerSlotsContainer);
            WorkerSlotUI slotUI = slotGO.GetComponent<WorkerSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(currentBuilding, workerType);
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

            // Resource Generation
            if (currentBuilding.BuildingData.generatedResourceType != null && currentBuilding.BuildingData.generationAmount > 0 && currentBuilding.BuildingData.generationInterval > 0)
            {
                resourceGenerationText.text = $"Generates: {currentBuilding.BuildingData.generationAmount} {currentBuilding.BuildingData.generatedResourceType.ResourceName} every {currentBuilding.BuildingData.generationInterval}s";
            }
            else
            {
                resourceGenerationText.text = "Generates: None";
            }

            // Update all worker slot UIs
            foreach (var slot in currentWorkerSlots)
            {
                slot.UpdateUI();
            }
        }
    }
}

