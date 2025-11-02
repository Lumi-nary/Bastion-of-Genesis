using UnityEngine;
using TMPro;

public class BuildingSelectionPanel : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BuildingDatabase buildingDatabase;
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private Transform buttonContainer;

    [Header("UI References")]
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        if (buildingDatabase == null || buildingButtonPrefab == null || buttonContainer == null)
        {
            Debug.LogError("BuildingSelectionPanel is not configured correctly!");
            return;
        }

        // Clear any existing buttons (in case of editor testing)
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Instantiate and configure a button for each available building
        foreach (BuildingData buildingData in buildingDatabase.availableBuildings)
        {
            GameObject buttonGO = Instantiate(buildingButtonPrefab, buttonContainer);
            buttonGO.name = buildingData.buildingName + " Button";
            BuildingButton buildingButton = buttonGO.GetComponent<BuildingButton>();

            if (buildingButton != null)
            {
                buildingButton.Configure(buildingData);
                buildingButton.GetButton().onClick.AddListener(() => OnBuildingSelected(buildingData));

                // Set the text of the button
                TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = buildingData.buildingName;
                }
            }
        }
    }

    private void Start()
    {
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
            PlacementSystem.Instance.EnterBuildMode(buildingData);
            HidePanel(); // Hide after selection
        }
    }
}
