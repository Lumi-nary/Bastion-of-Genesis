using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that shows all buildings that require workers.
/// Has option to combine same building types into one row.
/// </summary>
public class WorkerAssignPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject assignRowPrefab;
    [SerializeField] private Toggle combineToggle;

    [Header("Settings")]
    [SerializeField] private bool combineBuildings = false;

    // Track spawned rows
    private List<WorkerAssignRowUI> rows = new List<WorkerAssignRowUI>();

    private void Start()
    {
        if (combineToggle != null)
        {
            combineToggle.isOn = combineBuildings;
            combineToggle.onValueChanged.AddListener(OnCombineToggleChanged);
        }

        // Subscribe to building changes
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced += OnBuildingChanged;
            BuildingManager.Instance.OnBuildingDestroyedEvent += OnBuildingChanged;
        }

        HidePanel();
    }

    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced -= OnBuildingChanged;
            BuildingManager.Instance.OnBuildingDestroyedEvent -= OnBuildingChanged;
        }
    }

    private void OnBuildingChanged(Building building)
    {
        if (panel.activeSelf)
        {
            RefreshPanel();
        }
    }

    private void OnCombineToggleChanged(bool value)
    {
        combineBuildings = value;
        RefreshPanel();
    }

    public void RefreshPanel()
    {
        if (BuildingManager.Instance == null || rowContainer == null || assignRowPrefab == null)
            return;

        // Clear existing rows
        foreach (var row in rows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }
        rows.Clear();

        // Get all buildings with worker requirements
        List<Building> allBuildings = BuildingManager.Instance.GetAllBuildings();
        List<Building> buildingsWithWorkers = new List<Building>();

        foreach (Building building in allBuildings)
        {
            if (building != null && building.BuildingData != null &&
                building.BuildingData.workerRequirements != null &&
                building.BuildingData.workerRequirements.Count > 0)
            {
                buildingsWithWorkers.Add(building);
            }
        }

        if (combineBuildings)
        {
            CreateCombinedRows(buildingsWithWorkers);
        }
        else
        {
            CreateIndividualRows(buildingsWithWorkers);
        }
    }

    private void CreateIndividualRows(List<Building> buildings)
    {
        foreach (Building building in buildings)
        {
            GameObject rowGO = Instantiate(assignRowPrefab, rowContainer);
            WorkerAssignRowUI row = rowGO.GetComponent<WorkerAssignRowUI>();
            if (row != null)
            {
                row.InitializeIndividual(building);
                rows.Add(row);
            }
        }
    }

    private void CreateCombinedRows(List<Building> buildings)
    {
        // Group buildings by BuildingData
        Dictionary<BuildingData, List<Building>> grouped = new Dictionary<BuildingData, List<Building>>();

        foreach (Building building in buildings)
        {
            if (!grouped.ContainsKey(building.BuildingData))
            {
                grouped[building.BuildingData] = new List<Building>();
            }
            grouped[building.BuildingData].Add(building);
        }

        // Create one row per building type
        foreach (var kvp in grouped)
        {
            GameObject rowGO = Instantiate(assignRowPrefab, rowContainer);
            WorkerAssignRowUI row = rowGO.GetComponent<WorkerAssignRowUI>();
            if (row != null)
            {
                row.InitializeCombined(kvp.Key, kvp.Value);
                rows.Add(row);
            }
        }
    }

    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            RefreshPanel();
        }
    }

    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void TogglePanel()
    {
        if (panel != null)
        {
            if (panel.activeSelf)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }
    }
}
