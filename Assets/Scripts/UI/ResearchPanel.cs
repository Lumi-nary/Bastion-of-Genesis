using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main research panel UI controller.
/// Displays category tabs, tech nodes grid, current research progress, and tech details.
/// </summary>
public class ResearchPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    [Header("Category Tabs")]
    [SerializeField] private Button economyTabButton;
    [SerializeField] private Button militaryTabButton;
    [SerializeField] private Button expansionTabButton;
    [SerializeField] private Button automationTabButton;
    [SerializeField] private Button researchTabButton;

    [Header("Tab Visual Settings")]
    [SerializeField] private Color activeTabColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Tech Nodes Grid")]
    [SerializeField] private Transform techNodesContainer;
    [SerializeField] private GameObject techNodePrefab;

    [Header("Current Research Progress")]
    [SerializeField] private GameObject currentResearchSection;
    [SerializeField] private TextMeshProUGUI currentResearchNameText;
    [SerializeField] private Image currentResearchProgressFill;
    [SerializeField] private TextMeshProUGUI currentResearchProgressText;
    [SerializeField] private TextMeshProUGUI currentResearchTimeText;
    [SerializeField] private Button cancelResearchButton;

    [Header("Tech Detail Panel")]
    [SerializeField] private TechDetailPanel techDetailPanel;

    // State
    private TechCategory currentCategory = TechCategory.Economy;
    private List<TechNodeUI> spawnedNodes = new List<TechNodeUI>();
    private TechnologyData selectedTech = null;

    // Tab button references for easy iteration
    private Dictionary<TechCategory, Button> categoryButtons;

    private void Start()
    {
        // Setup category button dictionary
        categoryButtons = new Dictionary<TechCategory, Button>
        {
            { TechCategory.Economy, economyTabButton },
            { TechCategory.Military, militaryTabButton },
            { TechCategory.Expansion, expansionTabButton },
            { TechCategory.Automation, automationTabButton },
            { TechCategory.Research, researchTabButton }
        };

        // Setup tab button listeners
        if (economyTabButton != null)
            economyTabButton.onClick.AddListener(() => SelectCategory(TechCategory.Economy));
        if (militaryTabButton != null)
            militaryTabButton.onClick.AddListener(() => SelectCategory(TechCategory.Military));
        if (expansionTabButton != null)
            expansionTabButton.onClick.AddListener(() => SelectCategory(TechCategory.Expansion));
        if (automationTabButton != null)
            automationTabButton.onClick.AddListener(() => SelectCategory(TechCategory.Automation));
        if (researchTabButton != null)
            researchTabButton.onClick.AddListener(() => SelectCategory(TechCategory.Research));

        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        // Setup cancel research button
        if (cancelResearchButton != null)
            cancelResearchButton.onClick.AddListener(OnCancelResearchClicked);

        // Initialize tech detail panel
        if (techDetailPanel != null)
            techDetailPanel.Initialize(this);

        // Subscribe to ResearchManager events
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnTechResearched += OnTechResearched;
            ResearchManager.Instance.OnResearchProgress += OnResearchProgress;
            ResearchManager.Instance.OnTechAvailable += OnTechAvailable;
        }

        // Hide panel initially
        HidePanel();
    }

    private void Update()
    {
        // Update current research display if panel is active
        if (panel != null && panel.activeSelf)
        {
            UpdateCurrentResearchDisplay();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnTechResearched -= OnTechResearched;
            ResearchManager.Instance.OnResearchProgress -= OnResearchProgress;
            ResearchManager.Instance.OnTechAvailable -= OnTechAvailable;
        }

        // Remove button listeners
        if (economyTabButton != null) economyTabButton.onClick.RemoveAllListeners();
        if (militaryTabButton != null) militaryTabButton.onClick.RemoveAllListeners();
        if (expansionTabButton != null) expansionTabButton.onClick.RemoveAllListeners();
        if (automationTabButton != null) automationTabButton.onClick.RemoveAllListeners();
        if (researchTabButton != null) researchTabButton.onClick.RemoveAllListeners();
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        if (cancelResearchButton != null) cancelResearchButton.onClick.RemoveAllListeners();
    }

    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            RefreshDisplay();
        }
    }

    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (techDetailPanel != null)
        {
            techDetailPanel.Hide();
        }

        selectedTech = null;
    }

    public void TogglePanel()
    {
        if (panel != null)
        {
            if (panel.activeSelf)
                HidePanel();
            else
                ShowPanel();
        }
    }

    private void SelectCategory(TechCategory category)
    {
        currentCategory = category;
        UpdateTabVisuals();
        PopulateTechNodes();

        // Clear selection when changing category
        if (techDetailPanel != null)
        {
            techDetailPanel.Hide();
        }
        selectedTech = null;
    }

    private void UpdateTabVisuals()
    {
        foreach (var kvp in categoryButtons)
        {
            if (kvp.Value == null) continue;

            Image buttonImage = kvp.Value.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = (kvp.Key == currentCategory) ? activeTabColor : inactiveTabColor;
            }
        }
    }

    private void PopulateTechNodes()
    {
        // Clear existing nodes
        foreach (var node in spawnedNodes)
        {
            if (node != null)
            {
                Destroy(node.gameObject);
            }
        }
        spawnedNodes.Clear();

        if (ResearchManager.Instance == null || techNodesContainer == null || techNodePrefab == null)
            return;

        // Get technologies for current category
        List<TechnologyData> techs = ResearchManager.Instance.GetTechnologiesByCategory(currentCategory);

        // Sort by tier
        techs.Sort((a, b) => a.tier.CompareTo(b.tier));

        // Create nodes
        foreach (TechnologyData tech in techs)
        {
            GameObject nodeGO = Instantiate(techNodePrefab, techNodesContainer);
            TechNodeUI node = nodeGO.GetComponent<TechNodeUI>();

            if (node != null)
            {
                node.Initialize(tech, this);
                spawnedNodes.Add(node);
            }
        }
    }

    public void SelectTechnology(TechnologyData tech)
    {
        selectedTech = tech;

        if (techDetailPanel != null)
        {
            techDetailPanel.ShowTechnology(tech);
        }
    }

    public void RefreshDisplay()
    {
        UpdateTabVisuals();
        PopulateTechNodes();
        UpdateCurrentResearchDisplay();

        // Refresh tech detail panel if showing
        if (techDetailPanel != null && selectedTech != null)
        {
            techDetailPanel.UpdateDisplay();
        }
    }

    private void UpdateCurrentResearchDisplay()
    {
        if (currentResearchSection == null) return;

        if (ResearchManager.Instance == null || !ResearchManager.Instance.IsResearching)
        {
            currentResearchSection.SetActive(false);
            return;
        }

        currentResearchSection.SetActive(true);

        TechnologyData currentResearch = ResearchManager.Instance.CurrentResearch;
        float progress = ResearchManager.Instance.CurrentResearchProgress;

        if (currentResearchNameText != null)
        {
            currentResearchNameText.text = $"Researching: {currentResearch.techName}";
        }

        if (currentResearchProgressFill != null)
        {
            currentResearchProgressFill.fillAmount = progress;
        }

        if (currentResearchProgressText != null)
        {
            currentResearchProgressText.text = $"{(progress * 100f):F0}%";
        }

        if (currentResearchTimeText != null)
        {
            float remainingTime = currentResearch.researchTime * (1f - progress);
            currentResearchTimeText.text = FormatTime(remainingTime);
        }
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 60f)
        {
            return $"{seconds:F0}s";
        }
        else
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return secs > 0 ? $"{minutes}m {secs}s" : $"{minutes}m";
        }
    }

    private void OnCancelResearchClicked()
    {
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.CancelResearch();
            RefreshDisplay();
        }
    }

    // Event handlers
    private void OnTechResearched(TechnologyData tech)
    {
        RefreshDisplay();
    }

    private void OnResearchProgress(TechnologyData tech, float progress)
    {
        // Update node progress if visible
        foreach (var node in spawnedNodes)
        {
            if (node != null && node.TechData == tech)
            {
                node.UpdateStatus();
                break;
            }
        }
    }

    private void OnTechAvailable(TechnologyData tech)
    {
        // Refresh if in same category
        if (tech.category == currentCategory)
        {
            RefreshDisplay();
        }
    }
}
