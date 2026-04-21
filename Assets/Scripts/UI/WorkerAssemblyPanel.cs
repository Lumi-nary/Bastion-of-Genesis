using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Panel that displays all worker factories and resource converters.
/// Shows queue status, progress, and allows assembling/cancelling.
/// </summary>
public class WorkerAssemblyPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject factoryRowPrefab;
    [SerializeField] private GameObject converterRowPrefab;

    [Header("Anchor to trigger button")]
    [Tooltip("Horizontal gap between the trigger button and the left edge of the panel.")]
    [SerializeField] private float anchorGapX = 8f;

    [Header("Slide Animation")]
    [SerializeField] private FlyoutPanelSlider slider;

    // Track spawned rows
    private Dictionary<WorkerData, FactoryRowUI> factoryRows = new Dictionary<WorkerData, FactoryRowUI>();
    private Dictionary<ResourceType, ConverterRowUI> converterRows = new Dictionary<ResourceType, ConverterRowUI>();
    private bool isVisible;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (slider == null && panel != null) slider = panel.GetComponent<FlyoutPanelSlider>();
        if (slider == null) slider = GetComponent<FlyoutPanelSlider>();
    }

    private void Start()
    {
        // Subscribe to changes
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnFactoriesChanged += RefreshPanel;
            BuildingManager.Instance.OnConvertersChanged += RefreshPanel;
        }

        HidePanel();
    }

    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnFactoriesChanged -= RefreshPanel;
            BuildingManager.Instance.OnConvertersChanged -= RefreshPanel;
        }
    }

    private void Update()
    {
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

    /// <summary>
    /// Refresh the panel to show current factories and converters.
    /// </summary>
    public void RefreshPanel()
    {
        if (BuildingManager.Instance == null || rowContainer == null) return;

        RefreshFactoryRows();
        RefreshConverterRows();
    }

    private void RefreshFactoryRows()
    {
        if (factoryRowPrefab == null) return;

        // Get all worker types with factories
        List<WorkerData> workerTypes = BuildingManager.Instance.GetAvailableWorkerTypes();

        // Remove rows for worker types that no longer have factories
        List<WorkerData> toRemove = new List<WorkerData>();
        foreach (var kvp in factoryRows)
        {
            if (!workerTypes.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }
        foreach (var key in toRemove)
        {
            factoryRows.Remove(key);
        }

        // Add or update rows for each worker type
        foreach (WorkerData workerType in workerTypes)
        {
            if (!factoryRows.ContainsKey(workerType))
            {
                // Create new row
                GameObject rowGO = Instantiate(factoryRowPrefab, rowContainer);
                FactoryRowUI row = rowGO.GetComponent<FactoryRowUI>();
                if (row != null)
                {
                    row.Initialize(workerType);
                    factoryRows[workerType] = row;
                }
            }
            else
            {
                // Update existing row
                factoryRows[workerType].UpdateDisplay();
            }
        }
    }

    private void RefreshConverterRows()
    {
        if (converterRowPrefab == null) return;

        // Get all resource types with converters
        List<ResourceType> resourceTypes = BuildingManager.Instance.GetAvailableConversionTypes();

        // Remove rows for resource types that no longer have converters
        List<ResourceType> toRemove = new List<ResourceType>();
        foreach (var kvp in converterRows)
        {
            if (!resourceTypes.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }
        foreach (var key in toRemove)
        {
            converterRows.Remove(key);
        }

        // Add or update rows for each resource type
        foreach (ResourceType resourceType in resourceTypes)
        {
            if (!converterRows.ContainsKey(resourceType))
            {
                // Create new row
                GameObject rowGO = Instantiate(converterRowPrefab, rowContainer);
                ConverterRowUI row = rowGO.GetComponent<ConverterRowUI>();
                if (row != null)
                {
                    row.Initialize(resourceType);
                    converterRows[resourceType] = row;
                }
            }
            else
            {
                // Update existing row
                converterRows[resourceType].UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// Show the assembly panel.
    /// </summary>
    public void ShowPanel()
    {
        ShowPanel(null);
    }

    /// <summary>
    /// Show the assembly panel anchored next to the button that triggered it.
    /// </summary>
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

    /// <summary>
    /// Hide the assembly panel.
    /// </summary>
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

    /// <summary>
    /// Toggle panel visibility.
    /// </summary>
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
