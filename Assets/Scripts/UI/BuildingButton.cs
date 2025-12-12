using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Building button that displays building icon and name.
/// Used in BuildingSelectionPanel.
/// </summary>
[RequireComponent(typeof(Button))]
public class BuildingButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Runtime")]
    [SerializeField] private BuildingData buildingData;

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

    public Button GetButton() => button;
}
