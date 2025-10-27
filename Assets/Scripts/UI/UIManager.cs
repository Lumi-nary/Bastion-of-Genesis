using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private BuildingInfoPanel buildingInfoPanel;
    [SerializeField] private BuildingSelectionPanel buildingSelectionPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void ToggleBuildingSelectionPanel()
    {
        if (buildingSelectionPanel != null)
        {
            // This is a simple toggle. A more robust implementation might want to manage panel states.
            if (buildingSelectionPanel.gameObject.activeSelf)
            {
                buildingSelectionPanel.HidePanel();
            }
            else
            {
                buildingSelectionPanel.ShowPanel();
            }
        }
    }

    public void ShowBuildingInfoPanel(Building building)
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.ShowPanel(building);
        }
    }

    public void HideBuildingInfoPanel()
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.HidePanel();
        }
    }
}
