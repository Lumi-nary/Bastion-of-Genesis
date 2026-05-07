using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Building button that displays building icon and name.
/// Used in BuildingSelectionPanel.
/// </summary>
[RequireComponent(typeof(Button))]
public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Runtime")]
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private float requirementPopupDuration = 4f;

    public BuildingData BuildingData => buildingData;

    private Button button;
    private Image backgroundImage;

    private void Awake()
    {
        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();

        // Auto-find references if not assigned
        if (iconImage == null)
        {
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
        }
        if (nameText == null)
        {
            nameText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Configure button with building data.
    /// </summary>
    public void Configure(BuildingData data)
    {
        buildingData = data;
        ResetVisualTint();

        if (data == null) return;

        // Set icon
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        // Set name
        if (nameText != null)
        {
            nameText.text = data.buildingName;
        }
    }

    public Button GetButton()
    {
        if (button == null)
            button = GetComponent<Button>();
        return button;
    }

    private void ResetVisualTint()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            backgroundImage.color = Color.white;

        if (iconImage != null)
            iconImage.color = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UIManager.Instance != null && buildingData != null)
        {
            UIManager.Instance.ShowBuildingRequirementPopup(buildingData, requirementPopupDuration, this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBuildingHoverPopup(this);
        }
    }
}
