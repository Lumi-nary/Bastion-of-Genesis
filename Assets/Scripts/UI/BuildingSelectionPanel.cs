using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Building selection panel with category tabs.
/// Shows category buttons at top, building buttons below based on selected category.
/// </summary>
public class BuildingSelectionPanel : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BuildingDatabase buildingDatabase;
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private GameObject categoryButtonPrefab;

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private Transform buildingContainer;

    [Header("Category Settings")]
    [SerializeField] private Color selectedCategoryColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color normalCategoryColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    // Track spawned buttons
    private List<GameObject> categoryButtons = new List<GameObject>();
    private List<GameObject> buildingButtons = new List<GameObject>();

    // Current state
    private BuildingCategory currentCategory = BuildingCategory.Command;
    private Button selectedCategoryButton;

    // Cache buildings by category
    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingData>>();

    private void Awake()
    {
        if (buildingDatabase == null)
        {
            Debug.LogError("[BuildingSelectionPanel] BuildingDatabase not assigned!");
            return;
        }

        CacheBuildingsByCategory();
        CreateCategoryButtons();
    }

    private void Start()
    {
        HidePanel();
    }

    /// <summary>
    /// Cache buildings grouped by category for quick lookup.
    /// </summary>
    private void CacheBuildingsByCategory()
    {
        // Initialize all categories
        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            buildingsByCategory[category] = new List<BuildingData>();
        }

        // Sort buildings into categories
        foreach (BuildingData building in buildingDatabase.availableBuildings)
        {
            if (building != null)
            {
                buildingsByCategory[building.category].Add(building);
            }
        }
    }

    /// <summary>
    /// Create category tab buttons.
    /// </summary>
    private void CreateCategoryButtons()
    {
        if (categoryContainer == null || categoryButtonPrefab == null)
        {
            Debug.LogError("[BuildingSelectionPanel] Category container or prefab not assigned!");
            return;
        }

        // Clear existing
        foreach (Transform child in categoryContainer)
        {
            Destroy(child.gameObject);
        }
        categoryButtons.Clear();

        // Create button for each category
        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            // Skip categories with no buildings
            if (buildingsByCategory[category].Count == 0) continue;

            GameObject buttonGO = Instantiate(categoryButtonPrefab, categoryContainer);
            buttonGO.name = category.ToString() + "_Tab";
            categoryButtons.Add(buttonGO);

            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                BuildingCategory capturedCategory = category;
                button.onClick.AddListener(() => SelectCategory(capturedCategory, button));

                // Set button text
                TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = GetCategoryDisplayName(category);
                }

                // Select first category by default
                if (selectedCategoryButton == null)
                {
                    SelectCategory(category, button);
                }
            }
        }
    }

    /// <summary>
    /// Get display name for category.
    /// </summary>
    private string GetCategoryDisplayName(BuildingCategory category)
    {
        switch (category)
        {
            case BuildingCategory.Command: return "Command";
            case BuildingCategory.Energy: return "Energy";
            case BuildingCategory.Extraction: return "Extract";
            case BuildingCategory.Production: return "Production";
            case BuildingCategory.Defense: return "Defense";
            case BuildingCategory.Research: return "Research";
            default: return category.ToString();
        }
    }

    /// <summary>
    /// Select a category and show its buildings.
    /// </summary>
    private void SelectCategory(BuildingCategory category, Button button)
    {
        currentCategory = category;

        // Update button visuals
        if (selectedCategoryButton != null)
        {
            SetButtonColor(selectedCategoryButton, normalCategoryColor);
        }
        selectedCategoryButton = button;
        SetButtonColor(selectedCategoryButton, selectedCategoryColor);

        // Show buildings for this category
        ShowBuildingsForCategory(category);
    }

    /// <summary>
    /// Set button background color.
    /// </summary>
    private void SetButtonColor(Button button, Color color)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    /// <summary>
    /// Show building buttons for selected category.
    /// </summary>
    private void ShowBuildingsForCategory(BuildingCategory category)
    {
        if (buildingContainer == null || buildingButtonPrefab == null) return;

        // Clear existing building buttons
        foreach (GameObject btn in buildingButtons)
        {
            Destroy(btn);
        }
        buildingButtons.Clear();

        // Create buttons for buildings in this category
        List<BuildingData> buildings = buildingsByCategory[category];
        foreach (BuildingData buildingData in buildings)
        {
            if (buildingData == null) continue;

            // TODO: Check tech requirements when TechnologyManager is implemented
            // if (buildingData.requiredTech != null)
            // {
            //     if (TechnologyManager.Instance == null || !TechnologyManager.Instance.IsTechUnlocked(buildingData.requiredTech))
            //     {
            //         continue; // Skip locked buildings
            //     }
            // }

            GameObject buttonGO = Instantiate(buildingButtonPrefab, buildingContainer);
            buttonGO.name = buildingData.buildingName + "_Button";
            buildingButtons.Add(buttonGO);

            BuildingButton buildingButton = buttonGO.GetComponent<BuildingButton>();
            if (buildingButton != null)
            {
                buildingButton.Configure(buildingData);
                buildingButton.GetButton().onClick.AddListener(() => OnBuildingSelected(buildingData));
            }
        }
    }

    /// <summary>
    /// Called when a building button is clicked.
    /// </summary>
    private void OnBuildingSelected(BuildingData buildingData)
    {
        if (buildingData != null && PlacementSystem.Instance != null)
        {
            PlacementSystem.Instance.EnterBuildMode(buildingData);
            HidePanel();
        }
    }

    /// <summary>
    /// Show the building selection panel.
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            // Refresh buildings in case tech was unlocked
            ShowBuildingsForCategory(currentCategory);
        }
    }

    /// <summary>
    /// Hide the building selection panel.
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
