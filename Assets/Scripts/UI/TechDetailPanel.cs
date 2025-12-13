using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Panel showing selected technology details.
/// Displays name, tier/category, description, cost, time, and research button.
/// Closes when clicking outside the panel.
/// </summary>
public class TechDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI techNameText;
    [SerializeField] private TextMeshProUGUI tierCategoryText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Button researchButton;
    [SerializeField] private TextMeshProUGUI researchButtonText;

    [Header("Click Outside Detection")]
    [SerializeField] private RectTransform panelRect;

    private TechnologyData currentTech;
    private ResearchPanel parentPanel;

    public void Initialize(ResearchPanel panel)
    {
        parentPanel = panel;

        if (researchButton != null)
        {
            researchButton.onClick.AddListener(OnResearchButtonClicked);
        }

        // Hide panel initially
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Check for click outside panel to close (using new Input System)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckClickOutside();
        }
    }

    private void CheckClickOutside()
    {
        if (panelRect == null) return;

        // Check if click is outside this panel
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos))
        {
            Hide();
        }
    }

    public void ShowTechnology(TechnologyData tech)
    {
        if (tech == null)
        {
            gameObject.SetActive(false);
            return;
        }

        currentTech = tech;
        gameObject.SetActive(true);

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (currentTech == null) return;

        // Name
        if (techNameText != null)
        {
            techNameText.text = currentTech.techName;
        }

        // Tier & Category
        if (tierCategoryText != null)
        {
            tierCategoryText.text = $"Tier {currentTech.tier} - {currentTech.category}";
        }

        // Description
        if (descriptionText != null)
        {
            descriptionText.text = currentTech.description;
        }

        // Cost
        UpdateCost();

        // Time
        if (timeText != null)
        {
            timeText.text = $"Time: {currentTech.GetTimeString()}";
        }

        // Button state
        UpdateButtonState();
    }

    private void UpdateCost()
    {
        if (costText == null) return;

        if (currentTech.researchCost == null || currentTech.researchCost.Count == 0)
        {
            costText.text = "Cost: Free";
            return;
        }

        List<string> costParts = new List<string>();

        foreach (ResourceCost cost in currentTech.researchCost)
        {
            if (cost.resourceType == null) continue;

            int currentAmount = 0;
            if (ResourceManager.Instance != null)
            {
                currentAmount = ResourceManager.Instance.GetResourceAmount(cost.resourceType);
            }

            bool hasEnough = currentAmount >= cost.amount;
            string colorTag = hasEnough ? "white" : "red";
            costParts.Add($"<color={colorTag}>{cost.amount} {cost.resourceType.ResourceName}</color>");
        }

        costText.text = $"Cost: {string.Join(", ", costParts)}";
    }

    private void UpdateButtonState()
    {
        if (researchButton == null || researchButtonText == null) return;

        bool isResearched = ResearchManager.Instance != null &&
                           ResearchManager.Instance.IsTechResearched(currentTech);
        bool isAvailable = ResearchManager.Instance != null &&
                          ResearchManager.Instance.IsTechAvailable(currentTech);
        bool isResearching = ResearchManager.Instance != null &&
                            ResearchManager.Instance.CurrentResearch == currentTech;
        bool otherResearching = ResearchManager.Instance != null &&
                               ResearchManager.Instance.IsResearching &&
                               ResearchManager.Instance.CurrentResearch != currentTech;

        if (isResearched)
        {
            researchButtonText.text = "Completed";
            researchButton.interactable = false;
        }
        else if (isResearching)
        {
            researchButtonText.text = "Cancel";
            researchButton.interactable = true;
        }
        else if (!isAvailable)
        {
            researchButtonText.text = "Locked";
            researchButton.interactable = false;
        }
        else if (otherResearching)
        {
            researchButtonText.text = "Busy";
            researchButton.interactable = false;
        }
        else
        {
            bool canAfford = CanAffordResearch();
            researchButtonText.text = "Research";
            researchButton.interactable = canAfford;
        }
    }

    private bool CanAffordResearch()
    {
        if (ResourceManager.Instance == null) return false;

        foreach (ResourceCost cost in currentTech.researchCost)
        {
            if (cost.resourceType == null) continue;

            int currentAmount = ResourceManager.Instance.GetResourceAmount(cost.resourceType);
            if (currentAmount < cost.amount)
            {
                return false;
            }
        }

        return true;
    }

    private void OnResearchButtonClicked()
    {
        if (currentTech == null || ResearchManager.Instance == null) return;

        bool isResearching = ResearchManager.Instance.CurrentResearch == currentTech;

        if (isResearching)
        {
            ResearchManager.Instance.CancelResearch();
        }
        else
        {
            ResearchManager.Instance.StartResearch(currentTech);
        }

        if (parentPanel != null)
        {
            parentPanel.RefreshDisplay();
        }

        UpdateDisplay();
    }

    public void Hide()
    {
        currentTech = null;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (researchButton != null)
        {
            researchButton.onClick.RemoveListener(OnResearchButtonClicked);
        }
    }
}
