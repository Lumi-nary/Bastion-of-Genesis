using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI element representing a single technology node in the research panel.
/// Shows icon, name, tier, and current status (locked/available/researching/completed).
/// </summary>
public class TechNodeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private Image statusOverlay;
    [SerializeField] private Image progressFill;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject checkIcon;
    [SerializeField] private Button nodeButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Status Colors")]
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color availableColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    [SerializeField] private Color researchingColor = new Color(0.8f, 0.6f, 0.2f, 1f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.7f, 0.3f, 1f);

    private TechnologyData techData;
    private ResearchPanel parentPanel;

    public TechnologyData TechData => techData;

    public void Initialize(TechnologyData tech, ResearchPanel panel)
    {
        techData = tech;
        parentPanel = panel;

        // Set basic info
        if (iconImage != null && tech.icon != null)
        {
            iconImage.sprite = tech.icon;
        }

        if (nameText != null)
        {
            nameText.text = tech.techName;
        }

        if (tierText != null)
        {
            tierText.text = $"T{tech.tier}";
        }

        // Setup button
        if (nodeButton != null)
        {
            nodeButton.onClick.AddListener(OnNodeClicked);
        }

        UpdateStatus();
    }

    private void OnNodeClicked()
    {
        if (parentPanel != null)
        {
            parentPanel.SelectTechnology(techData);
        }
    }

    public void UpdateStatus()
    {
        if (techData == null) return;

        bool isResearched = ResearchManager.Instance != null &&
                           ResearchManager.Instance.IsTechResearched(techData);
        bool isAvailable = ResearchManager.Instance != null &&
                          ResearchManager.Instance.IsTechAvailable(techData);
        bool isResearching = ResearchManager.Instance != null &&
                            ResearchManager.Instance.CurrentResearch == techData;

        // Update visuals based on status
        if (isResearched)
        {
            SetCompletedState();
        }
        else if (isResearching)
        {
            SetResearchingState();
        }
        else if (isAvailable)
        {
            SetAvailableState();
        }
        else
        {
            SetLockedState();
        }

        // Update progress bar if researching
        if (progressFill != null)
        {
            if (isResearching && ResearchManager.Instance != null)
            {
                progressFill.gameObject.SetActive(true);
                progressFill.fillAmount = ResearchManager.Instance.CurrentResearchProgress;
            }
            else
            {
                progressFill.gameObject.SetActive(false);
            }
        }
    }

    private void SetLockedState()
    {
        if (backgroundImage != null) backgroundImage.color = lockedColor;
        if (lockIcon != null) lockIcon.SetActive(true);
        if (checkIcon != null) checkIcon.SetActive(false);
        if (statusOverlay != null) statusOverlay.color = new Color(0, 0, 0, 0.5f);
        if (statusText != null)
        {
            statusText.text = "LOCKED";
            statusText.color = new Color(0.85f, 0.9f, 0.95f, 1f);
        }
        ApplyTutorialGate(true); // Still clickable to show info unless another tutorial tech is required.
    }

    private void SetAvailableState()
    {
        if (backgroundImage != null) backgroundImage.color = availableColor;
        if (lockIcon != null) lockIcon.SetActive(false);
        if (checkIcon != null) checkIcon.SetActive(false);
        if (statusOverlay != null) statusOverlay.color = Color.clear;
        if (statusText != null)
        {
            statusText.text = "AVAILABLE";
            statusText.color = new Color(0.62f, 0.9f, 1f, 1f);
        }
        ApplyTutorialGate(true);
    }

    private void SetResearchingState()
    {
        if (backgroundImage != null) backgroundImage.color = researchingColor;
        if (lockIcon != null) lockIcon.SetActive(false);
        if (checkIcon != null) checkIcon.SetActive(false);
        if (statusOverlay != null) statusOverlay.color = Color.clear;
        if (statusText != null)
        {
            statusText.text = "RESEARCHING";
            statusText.color = new Color(1f, 0.84f, 0.38f, 1f);
        }
        ApplyTutorialGate(true);
    }

    private void SetCompletedState()
    {
        if (backgroundImage != null) backgroundImage.color = completedColor;
        if (lockIcon != null) lockIcon.SetActive(false);
        if (checkIcon != null) checkIcon.SetActive(true);
        if (statusOverlay != null) statusOverlay.color = Color.clear;
        if (statusText != null)
        {
            statusText.text = "COMPLETE";
            statusText.color = new Color(0.45f, 1f, 0.75f, 1f);
        }
        ApplyTutorialGate(false);
    }

    private void OnDestroy()
    {
        if (nodeButton != null)
        {
            nodeButton.onClick.RemoveListener(OnNodeClicked);
        }
    }

    private void ApplyTutorialGate(bool baseInteractable)
    {
        bool tutorialAllowed = TutorialGuideManager.Instance == null ||
            TutorialGuideManager.Instance.CanResearchTechnology(techData);

        if (nodeButton != null)
            nodeButton.interactable = baseInteractable && tutorialAllowed;

        if (backgroundImage == null)
            return;

        TutorialPulseHighlight pulse = backgroundImage.GetComponent<TutorialPulseHighlight>() ?? backgroundImage.gameObject.AddComponent<TutorialPulseHighlight>();
        pulse.SetHighlighted(TutorialGuideManager.Instance != null &&
            TutorialGuideManager.Instance.IsTargetAction(TutorialTargetAction.ResearchTechnology) &&
            tutorialAllowed);
    }
}
