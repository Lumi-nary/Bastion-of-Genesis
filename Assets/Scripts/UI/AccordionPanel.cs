using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccordionPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject contentArea;
    [SerializeField] private RectTransform arrowIcon;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Settings")]
    [SerializeField] private string panelTitle = "Workers";
    [SerializeField] private bool startExpanded = true;
    [SerializeField] private float arrowRotationSpeed = 10f;

    private bool isExpanded;
    private float targetArrowRotation;

    private void Start()
    {
        // Set initial state
        isExpanded = startExpanded;
        UpdateVisuals(immediate: true);

        // Set title
        if (titleText != null)
        {
            titleText.text = panelTitle;
        }

        // Setup button listener
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
    }

    private void Update()
    {
        // Smoothly rotate arrow icon
        if (arrowIcon != null)
        {
            float currentRotation = arrowIcon.localEulerAngles.z;
            // Normalize angles to 0-360
            if (currentRotation > 180f) currentRotation -= 360f;

            float newRotation = Mathf.LerpAngle(currentRotation, targetArrowRotation, Time.deltaTime * arrowRotationSpeed);
            arrowIcon.localEulerAngles = new Vector3(0, 0, newRotation);
        }
    }

    public void TogglePanel()
    {
        isExpanded = !isExpanded;
        UpdateVisuals(immediate: false);
    }

    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;
        UpdateVisuals(immediate: false);
    }

    private void UpdateVisuals(bool immediate)
    {
        // Show/hide content
        if (contentArea != null)
        {
            contentArea.SetActive(isExpanded);
        }

        // Update arrow rotation
        targetArrowRotation = isExpanded ? 0f : -90f;

        // Apply immediately if requested
        if (immediate && arrowIcon != null)
        {
            arrowIcon.localEulerAngles = new Vector3(0, 0, targetArrowRotation);
        }
    }
}
