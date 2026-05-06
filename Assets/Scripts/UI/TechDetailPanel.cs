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
    [SerializeField] private TextMeshProUGUI effectsText;
    [SerializeField] private TextMeshProUGUI statusText;
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

        if (effectsText != null)
        {
            effectsText.text = $"Effects:\n{currentTech.GetEffectsDescription()}";
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

        costText.text = $"Cost (over time): {string.Join(", ", costParts)}";
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
            SetStatus("Completed", new Color(0.45f, 1f, 0.75f, 1f));
            researchButton.interactable = false;
        }
        else if (isResearching)
        {
            researchButtonText.text = "Cancel";
            SetStatus("Researching", new Color(1f, 0.84f, 0.38f, 1f));
            researchButton.interactable = true;
        }
        else if (!isAvailable)
        {
            researchButtonText.text = "Locked";
            SetStatus("Locked", new Color(0.85f, 0.9f, 0.95f, 1f));
            researchButton.interactable = false;
        }
        else if (otherResearching)
        {
            researchButtonText.text = "Busy";
            SetStatus("Busy", new Color(1f, 0.84f, 0.38f, 1f));
            researchButton.interactable = false;
        }
        else if (!CanResearchForTutorial())
        {
            researchButtonText.text = "Tutorial Locked";
            SetStatus("Tutorial Locked", new Color(1f, 0.36f, 0.34f, 1f));
            researchButton.interactable = false;
        }
        else if (!ResearchManager.Instance.HasActiveResearchLab())
        {
            researchButtonText.text = "No Lab";
            SetStatus("Requires active research lab", new Color(1f, 0.36f, 0.34f, 1f));
            researchButton.interactable = false;
        }
        else
        {
            researchButtonText.text = "Research";
            SetStatus("Available", new Color(0.62f, 0.9f, 1f, 1f));
            researchButton.interactable = true;
        }

        SetTutorialHighlight(researchButton, TutorialGuideManager.Instance != null &&
            TutorialGuideManager.Instance.IsTargetAction(TutorialTargetAction.ResearchTechnology) &&
            CanResearchForTutorial());
    }

    private void SetStatus(string text, Color color)
    {
        if (statusText == null)
            return;

        statusText.text = text;
        statusText.color = color;
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
            if (!CanResearchForTutorial())
                return;

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

    private bool CanResearchForTutorial()
    {
        return TutorialGuideManager.Instance == null || TutorialGuideManager.Instance.CanResearchTechnology(currentTech);
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
