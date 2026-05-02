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

    private void Awake()
    {
        button = GetComponent<Button>();

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

        if (data == null) return;

        // Set icon
        if (iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
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
