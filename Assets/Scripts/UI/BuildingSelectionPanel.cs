using UnityEngine;
using UnityEngine.UI;

public class BuildingSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        // Find all building buttons that are children of this panel
        BuildingButton[] buildingButtons = GetComponentsInChildren<BuildingButton>();

        foreach (BuildingButton button in buildingButtons)
        {
            BuildingData data = button.BuildingData;
            if (data != null)
            {
                // Set the button's icon from the BuildingData
                Image buttonIcon = button.GetComponent<Image>();
                if (buttonIcon != null && data.icon != null)
                {
                    buttonIcon.sprite = data.icon;
                }

                // Add a listener to the button's onClick event
                button.GetButton().onClick.AddListener(() => OnBuildingSelected(data));
            }
        }

        HidePanel();
    }

    public void ShowPanel()
    {
        panel.SetActive(true);
    }

    public void HidePanel()
    {
        panel.SetActive(false);
    }

    private void OnBuildingSelected(BuildingData buildingData)
    {
        if (buildingData != null)
        {
            PlayerController.Instance.EnterBuildMode(buildingData);
            HidePanel(); // Hide after selection
        }
    }
}
