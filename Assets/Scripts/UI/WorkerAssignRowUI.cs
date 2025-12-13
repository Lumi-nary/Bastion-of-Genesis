using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI row for worker assignment.
/// Can display individual building or combined buildings of same type.
/// </summary>
public class WorkerAssignRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image buildingIcon;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI workerCountText;
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TextMeshProUGUI statusText;

    // Mode
    private bool isCombined = false;

    // Individual mode
    private Building singleBuilding;

    // Combined mode
    private BuildingData buildingData;
    private List<Building> buildings = new List<Building>();

    public void InitializeIndividual(Building building)
    {
        isCombined = false;
        singleBuilding = building;
        buildingData = building.BuildingData;

        SetupUI();
        SetupButtons();
        UpdateDisplay();
    }

    public void InitializeCombined(BuildingData data, List<Building> buildingList)
    {
        isCombined = true;
        singleBuilding = null;
        buildingData = data;
        buildings = new List<Building>(buildingList);

        SetupUI();
        SetupButtons();
        UpdateDisplay();
    }

    private void SetupUI()
    {
        // Set icon
        if (buildingIcon != null && buildingData.icon != null)
        {
            buildingIcon.sprite = buildingData.icon;
        }

        // Set name
        if (buildingNameText != null)
        {
            buildingNameText.text = buildingData.buildingName;
        }

        // Hide status text in combined mode
        if (statusText != null)
        {
            statusText.gameObject.SetActive(!isCombined);
        }
    }

    private void SetupButtons()
    {
        if (addButton != null)
        {
            addButton.onClick.AddListener(OnAddClicked);
        }
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnRemoveClicked);
        }
    }

    private void Update()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (isCombined)
        {
            UpdateCombinedDisplay();
        }
        else
        {
            UpdateIndividualDisplay();
        }
    }

    private void UpdateIndividualDisplay()
    {
        if (singleBuilding == null) return;

        int assigned = singleBuilding.GetTotalAssignedWorkerCount();
        int capacity = singleBuilding.GetTotalWorkerCapacity();

        // Worker count
        if (workerCountText != null)
        {
            workerCountText.text = $"{assigned}/{capacity}";
        }

        // Operational status
        if (statusText != null)
        {
            if (singleBuilding.IsOperational)
            {
                statusText.text = "Operational";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Needs Workers";
                statusText.color = Color.red;
            }
        }

        // Button states
        UpdateButtonStates(assigned, capacity);
    }

    private void UpdateCombinedDisplay()
    {
        int totalAssigned = 0;
        int totalCapacity = 0;

        foreach (Building building in buildings)
        {
            if (building != null)
            {
                totalAssigned += building.GetTotalAssignedWorkerCount();
                totalCapacity += building.GetTotalWorkerCapacity();
            }
        }

        // Worker count
        if (workerCountText != null)
        {
            workerCountText.text = $"{totalAssigned}/{totalCapacity}";
        }

        // Button states
        UpdateButtonStates(totalAssigned, totalCapacity);
    }

    private void UpdateButtonStates(int assigned, int capacity)
    {
        // Add button - enabled if capacity available and workers exist
        if (addButton != null)
        {
            addButton.interactable = assigned < capacity && HasAvailableWorkers();
        }

        // Remove button - enabled if any workers assigned
        if (removeButton != null)
        {
            removeButton.interactable = assigned > 0;
        }
    }

    private bool HasAvailableWorkers()
    {
        if (buildingData == null || buildingData.workerRequirements == null)
            return false;

        if (WorkerManager.Instance == null)
            return false;

        // Check if any required worker type has available workers
        foreach (var req in buildingData.workerRequirements)
        {
            if (WorkerManager.Instance.GetAvailableWorkerCount(req.workerType) > 0)
            {
                return true;
            }
        }
        return false;
    }

    private void OnAddClicked()
    {
        if (isCombined)
        {
            AddWorkerToCombined();
        }
        else
        {
            AddWorkerToBuilding(singleBuilding);
        }
        UpdateDisplay();
    }

    private void OnRemoveClicked()
    {
        if (isCombined)
        {
            RemoveWorkerFromCombined();
        }
        else
        {
            RemoveWorkerFromBuilding(singleBuilding);
        }
        UpdateDisplay();
    }

    private void AddWorkerToBuilding(Building building)
    {
        if (building == null) return;

        // Find first worker type that has capacity and available workers
        foreach (var req in building.BuildingData.workerRequirements)
        {
            int currentCount = building.GetAssignedWorkerCount(req.workerType);
            int typeCapacity = building.GetCapacityForWorker(req.workerType);

            if (currentCount < typeCapacity &&
                WorkerManager.Instance.GetAvailableWorkerCount(req.workerType) > 0)
            {
                building.AssignWorker(req.workerType);
                return;
            }
        }
    }

    private void RemoveWorkerFromBuilding(Building building)
    {
        if (building == null) return;

        // Find first worker type that has workers assigned
        foreach (var req in building.BuildingData.workerRequirements)
        {
            int currentCount = building.GetAssignedWorkerCount(req.workerType);

            if (currentCount > 0)
            {
                building.RemoveWorker(req.workerType);
                return;
            }
        }
    }

    private void AddWorkerToCombined()
    {
        // Find first building with capacity and add worker
        foreach (Building building in buildings)
        {
            if (building == null) continue;

            int assigned = building.GetTotalAssignedWorkerCount();
            int capacity = building.GetTotalWorkerCapacity();

            if (assigned < capacity)
            {
                AddWorkerToBuilding(building);
                return;
            }
        }
    }

    private void RemoveWorkerFromCombined()
    {
        // Find last building with workers and remove one (LIFO)
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            Building building = buildings[i];
            if (building == null) continue;

            int assigned = building.GetTotalAssignedWorkerCount();

            if (assigned > 0)
            {
                RemoveWorkerFromBuilding(building);
                return;
            }
        }
    }
}
