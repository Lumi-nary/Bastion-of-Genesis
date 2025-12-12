using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel that displays all worker factories grouped by worker type.
/// Shows queue status, progress, and allows assembling/cancelling workers.
/// </summary>
public class WorkerAssemblyPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject factoryRowPrefab;

    // Track spawned rows
    private Dictionary<WorkerData, FactoryRowUI> factoryRows = new Dictionary<WorkerData, FactoryRowUI>();

    private void Start()
    {
        // Subscribe to factory changes
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnFactoriesChanged += RefreshPanel;
        }

        HidePanel();
    }

    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnFactoriesChanged -= RefreshPanel;
        }
    }

    /// <summary>
    /// Refresh the panel to show current factories.
    /// </summary>
    public void RefreshPanel()
    {
        if (BuildingManager.Instance == null || rowContainer == null) return;

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

    /// <summary>
    /// Show the assembly panel.
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            RefreshPanel();
        }
    }

    /// <summary>
    /// Hide the assembly panel.
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
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
