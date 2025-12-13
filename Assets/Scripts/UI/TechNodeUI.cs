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
        if (nodeButton != null) nodeButton.interactable = true; // Still clickable to show info
    }

    private void SetAvailableState()
    {
        if (backgroundImage != null) backgroundImage.color = availableColor;
        if (lockIcon != null) lockIcon.SetActive(false);
        if (checkIcon != null) checkIcon.SetActive(false);
        if (statusOverlay != null) statusOverlay.color = Color.clear;
        if (nodeButton != null) nodeButton.interactable = true;
    }

    private void SetResearchingState()
    {
        if (backgroundImage != null) backgroundImage.color = researchingColor;
        if (lockIcon != null) lockIcon.SetActive(false);
        if (checkIcon != null) checkIcon.SetActive(false);
        if (statusOverlay != null) statusOverlay.color = Color.clear;
        if (nodeButton != null) nodeButton.interactable = true;
    }

    private void SetCompletedState()
    {
        if (backgroundImage != null) backgroundImage.color = completedColor;
        if (lockIcon != null) lockIcon.SetActive(false);
        if (checkIcon != null) checkIcon.SetActive(true);
        if (statusOverlay != null) statusOverlay.color = Color.clear;
        if (nodeButton != null) nodeButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (nodeButton != null)
        {
            nodeButton.onClick.RemoveListener(OnNodeClicked);
        }
    }
}
