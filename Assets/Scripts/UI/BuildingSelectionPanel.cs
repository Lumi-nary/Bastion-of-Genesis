using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Building selection with accordion categories in the ActionPanel
/// and a flyout building list panel to the right.
/// </summary>
public class BuildingSelectionPanel : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BuildingDatabase buildingDatabase;
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private GameObject categoryButtonPrefab;

    [Header("UI References")]
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private Transform buildingContainer;
    [SerializeField] private GameObject buildingListPanel;

    [Header("Category Settings")]
    [SerializeField] private Color selectedCategoryColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color normalCategoryColor = new Color(0.25f, 0.25f, 0.3f, 1f);

    // Track spawned buttons
    private List<GameObject> categoryButtons = new List<GameObject>();
    private List<GameObject> buildingButtons = new List<GameObject>();

    // Current state
    private BuildingCategory currentCategory;
    private Button selectedCategoryButton;
    private bool categoriesVisible;

    // Cache
    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingData>>();

    public bool IsVisible => categoriesVisible;

    private void Awake()
    {
        if (buildingDatabase == null)
        {
            Debug.LogError("[BuildingSelectionPanel] BuildingDatabase not assigned!");
            return;
        }

        CacheBuildingsByCategory();
    }

    private void Start()
    {
        HidePanel();
    }

    private void CacheBuildingsByCategory()
    {
        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
            buildingsByCategory[category] = new List<BuildingData>();

        foreach (BuildingData building in buildingDatabase.availableBuildings)
        {
            if (building != null && building.isPlayerBuildable)
                buildingsByCategory[building.category].Add(building);
        }
    }

    /// <summary>
    /// Toggle categories. Called when BUILDING action button is clicked.
    /// </summary>
    public void TogglePanel()
    {
        if (categoriesVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void ShowPanel()
    {
        categoriesVisible = true;
        CreateCategoryButtons();

        if (categoryContainer != null)
            categoryContainer.gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        categoriesVisible = false;

        if (categoryContainer != null)
            categoryContainer.gameObject.SetActive(false);

        HideBuildingList();
        ClearCategoryButtons();
    }

    private void CreateCategoryButtons()
    {
        ClearCategoryButtons();

        if (categoryContainer == null || categoryButtonPrefab == null) return;

        selectedCategoryButton = null;

        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            if (CountUnlockedInCategory(category) == 0) continue;

            GameObject buttonGO = Instantiate(categoryButtonPrefab, categoryContainer);
            buttonGO.name = category.ToString() + "_Tab";
            categoryButtons.Add(buttonGO);

            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                BuildingCategory captured = category;
                button.onClick.AddListener(() => SelectCategory(captured, button));

                TextMeshProUGUI text = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = GetCategoryDisplayName(category);

                SetButtonColor(button, normalCategoryColor);
            }
        }
    }

    private void ClearCategoryButtons()
    {
        foreach (GameObject btn in categoryButtons)
            Destroy(btn);
        categoryButtons.Clear();
    }

    private void SelectCategory(BuildingCategory category, Button button)
    {
        // Deselect previous
        if (selectedCategoryButton != null)
            SetButtonColor(selectedCategoryButton, normalCategoryColor);

        // If clicking the same category, toggle the building list off
        if (selectedCategoryButton == button && buildingListPanel != null && buildingListPanel.activeSelf)
        {
            HideBuildingList();
            selectedCategoryButton = null;
            return;
        }

        currentCategory = category;
        selectedCategoryButton = button;
        SetButtonColor(button, selectedCategoryColor);

        ShowBuildingsForCategory(category);
    }

    private void ShowBuildingsForCategory(BuildingCategory category)
    {
        if (buildingContainer == null || buildingButtonPrefab == null) return;

        // Clear existing
        foreach (GameObject btn in buildingButtons)
            Destroy(btn);
        buildingButtons.Clear();

        // Create building buttons
        List<BuildingData> buildings = buildingsByCategory[category];
        foreach (BuildingData data in buildings)
        {
            if (!IsBuildingUnlocked(data)) continue;

            GameObject buttonGO = Instantiate(buildingButtonPrefab, buildingContainer);
            buttonGO.name = data.buildingName + "_Button";
            buildingButtons.Add(buttonGO);

            BuildingButton buildingButton = buttonGO.GetComponent<BuildingButton>();
            if (buildingButton != null)
            {
                buildingButton.Configure(data);
                Button btn = buildingButton.GetButton();
                if (btn != null)
                    btn.onClick.AddListener(() => OnBuildingSelected(data));
            }
        }

        // Show the flyout panel
        if (buildingListPanel != null)
            buildingListPanel.SetActive(true);
    }

    private void HideBuildingList()
    {
        if (buildingListPanel != null)
            buildingListPanel.SetActive(false);

        foreach (GameObject btn in buildingButtons)
            Destroy(btn);
        buildingButtons.Clear();
    }

    private void OnBuildingSelected(BuildingData data)
    {
        if (data != null && PlacementSystem.Instance != null)
        {
            PlacementSystem.Instance.EnterBuildMode(data);
            HidePanel();
        }
    }

    private bool IsBuildingUnlocked(BuildingData data)
    {
        if (data == null) return false;
        if (data.requiredTech == null) return true;

        bool byResearch = ResearchManager.Instance != null && ResearchManager.Instance.IsTechResearched(data.requiredTech);
        bool byMission = BuildingManager.Instance != null && BuildingManager.Instance.IsBuildingUnlockedByMission(data);
        return byResearch || byMission;
    }

    private int CountUnlockedInCategory(BuildingCategory category)
    {
        int count = 0;
        foreach (BuildingData b in buildingsByCategory[category])
            if (IsBuildingUnlocked(b)) count++;
        return count;
    }

    private void SetButtonColor(Button button, Color color)
    {
        Image img = button.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private string GetCategoryDisplayName(BuildingCategory category)
    {
        switch (category)
        {
            case BuildingCategory.Command: return "Command";
            case BuildingCategory.Energy: return "Energy";
            case BuildingCategory.Extraction: return "Extraction";
            case BuildingCategory.Production: return "Production";
            case BuildingCategory.Defense: return "Defense";
            case BuildingCategory.Research: return "Research";
            default: return category.ToString();
        }
    }
}
