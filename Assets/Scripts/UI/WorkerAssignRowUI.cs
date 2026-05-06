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
    [SerializeField] private TextMeshProUGUI workerTypeText;
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Status Colors")]
    [SerializeField] private Color operationalColor = new Color(0.45f, 1f, 0.75f, 1f);
    [SerializeField] private Color needsWorkersColor = new Color(1f, 0.36f, 0.34f, 1f);
    [SerializeField] private Color combinedStatusColor = new Color(0.62f, 0.9f, 1f, 1f);

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
            statusText.gameObject.SetActive(true);
        }

        if (workerTypeText != null)
        {
            workerTypeText.text = GetWorkerRequirementSummary();
        }
    }

    private string GetWorkerRequirementSummary()
    {
        if (buildingData == null || buildingData.workerRequirements == null || buildingData.workerRequirements.Count == 0)
            return "No crew required";

        List<string> parts = new List<string>();
        foreach (var req in buildingData.workerRequirements)
        {
            if (req == null || req.workerType == null)
                continue;

            int target = req.requiredCount > 0 ? req.requiredCount : req.capacity;
            parts.Add($"{target} {req.workerType.workerName}");
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : "Crew required";
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
                statusText.color = operationalColor;
            }
            else
            {
                statusText.text = "Needs Workers";
                statusText.color = needsWorkersColor;
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

        if (statusText != null)
        {
            int operational = 0;
            int total = 0;
            foreach (Building building in buildings)
            {
                if (building == null)
                    continue;

                total++;
                if (building.IsOperational)
                    operational++;
            }

            statusText.text = $"Active {operational}/{total}";
            statusText.color = total > 0 && operational == total ? operationalColor : combinedStatusColor;
        }

        // Button states
        UpdateButtonStates(totalAssigned, totalCapacity);
    }

    private void UpdateButtonStates(int assigned, int capacity)
    {
        // Add button - enabled if capacity available and workers exist
        if (addButton != null)
        {
            bool tutorialAllowed = HasTutorialAllowedAssignment();
            addButton.interactable = assigned < capacity && HasAvailableWorkers() && tutorialAllowed;
            SetTutorialHighlight(addButton, TutorialGuideManager.Instance != null &&
                TutorialGuideManager.Instance.IsTargetAction(TutorialTargetAction.AssignWorker) &&
                tutorialAllowed);
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

    private bool AddWorkerToBuilding(Building building)
    {
        if (building == null) return false;

        // Find first worker type that has capacity and available workers
        foreach (var req in building.BuildingData.workerRequirements)
        {
            int currentCount = building.GetAssignedWorkerCount(req.workerType);
            int typeCapacity = building.GetCapacityForWorker(req.workerType);

            if (currentCount < typeCapacity &&
                WorkerManager.Instance.GetAvailableWorkerCount(req.workerType) > 0 &&
                CanAssignForTutorial(building, req.workerType))
            {
                return building.AssignWorker(req.workerType);
            }
        }

        return false;
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
                if (AddWorkerToBuilding(building))
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

    private bool HasTutorialAllowedAssignment()
    {
        if (TutorialGuideManager.Instance == null || !TutorialGuideManager.Instance.HasActiveHardGate)
            return true;

        if (isCombined)
        {
            foreach (Building building in buildings)
            {
                if (HasAssignableWorkerForBuilding(building))
                    return true;
            }

            return false;
        }

        return HasAssignableWorkerForBuilding(singleBuilding);
    }

    private bool HasAssignableWorkerForBuilding(Building building)
    {
        if (building == null || building.BuildingData == null || building.BuildingData.workerRequirements == null)
            return false;

        foreach (var req in building.BuildingData.workerRequirements)
        {
            if (WorkerManager.Instance != null &&
                WorkerManager.Instance.GetAvailableWorkerCount(req.workerType) > 0 &&
                CanAssignForTutorial(building, req.workerType))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanAssignForTutorial(Building building, WorkerData workerData)
    {
        return TutorialGuideManager.Instance == null || TutorialGuideManager.Instance.CanAssignWorker(building, workerData);
    }

    private void SetTutorialHighlight(Button button, bool highlighted)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        TutorialPulseHighlight pulse = image.GetComponent<TutorialPulseHighlight>() ?? image.gameObject.AddComponent<TutorialPulseHighlight>();
        pulse.SetHighlighted(highlighted);
    }
}
