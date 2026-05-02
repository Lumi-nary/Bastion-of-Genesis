using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Panel that shows all buildings that require workers.
/// Has option to combine same building types into one row.
/// </summary>
public class WorkerAssignPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject assignRowPrefab;
    [SerializeField] private Toggle combineToggle;
    [SerializeField] private TextMeshProUGUI workerSummaryText;

    [Header("Anchor to trigger button")]
    [Tooltip("Horizontal gap between the trigger button and the left edge of the panel.")]
    [SerializeField] private float anchorGapX = 8f;

    [Header("Slide Animation")]
    [SerializeField] private FlyoutPanelSlider slider;

    [Header("Settings")]
    [SerializeField] private bool combineBuildings = false;

    // Track spawned rows
    private List<WorkerAssignRowUI> rows = new List<WorkerAssignRowUI>();
    private bool isVisible;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (slider == null && panel != null) slider = panel.GetComponent<FlyoutPanelSlider>();
        if (slider == null) slider = GetComponent<FlyoutPanelSlider>();
    }

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

    private void Update()
    {
        if (isVisible)
        {
            UpdateWorkerSummary(GetBuildingsWithWorkerRequirements());
        }

        // Click outside to close
        if (isVisible && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsPointerOverPanel())
            {
                HidePanel();
            }
        }
    }

    private bool IsPointerOverPanel()
    {
        // Check if mouse is over any UI element (like the toggle button)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        if (panelRect == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos);
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

        List<Building> buildingsWithWorkers = GetBuildingsWithWorkerRequirements();

        if (combineBuildings)
        {
            CreateCombinedRows(buildingsWithWorkers);
        }
        else
        {
            CreateIndividualRows(buildingsWithWorkers);
        }

        UpdateWorkerSummary(buildingsWithWorkers);
    }

    private List<Building> GetBuildingsWithWorkerRequirements()
    {
        List<Building> buildingsWithWorkers = new List<Building>();
        if (BuildingManager.Instance == null)
            return buildingsWithWorkers;

        List<Building> allBuildings = BuildingManager.Instance.GetAllBuildings();
        foreach (Building building in allBuildings)
        {
            if (building != null && building.BuildingData != null &&
                building.BuildingData.workerRequirements != null &&
                building.BuildingData.workerRequirements.Count > 0)
            {
                buildingsWithWorkers.Add(building);
            }
        }

        return buildingsWithWorkers;
    }

    private void UpdateWorkerSummary(List<Building> buildingsWithWorkers)
    {
        if (workerSummaryText == null)
            return;

        int assigned = 0;
        int capacity = 0;
        foreach (Building building in buildingsWithWorkers)
        {
            if (building == null)
                continue;

            assigned += building.GetTotalAssignedWorkerCount();
            capacity += building.GetTotalWorkerCapacity();
        }

        int available = 0;
        if (WorkerManager.Instance != null)
        {
            foreach (var kvp in WorkerManager.Instance.AvailableWorkers)
            {
                available += kvp.Value;
            }
        }

        workerSummaryText.text = $"Pool {available}  |  Assigned {assigned}/{capacity}";
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
        ShowPanel(null);
    }

    public void ShowPanel(RectTransform anchorButton)
    {
        if (panel == null) return;
        if (anchorButton != null && panelRect != null)
            PanelPositioner.PositionBeside(panelRect, anchorButton, anchorGapX);
        panel.SetActive(true);
        isVisible = true;
        RefreshPanel();
        if (slider != null) slider.PlayIn();
    }

    public void HidePanel()
    {
        if (panel == null) return;
        if (!isVisible)
        {
            panel.SetActive(false);
            return;
        }
        isVisible = false;
        if (slider != null && panel.activeSelf)
            slider.PlayOut(() => panel.SetActive(false));
        else
            panel.SetActive(false);
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
