using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingButton : MonoBehaviour
{
    [Header("Building Data")]
    [SerializeField] private BuildingData buildingData;

    public BuildingData BuildingData => buildingData;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Configure(BuildingData data)
    {
        buildingData = data;
        Image buttonIcon = GetComponent<Image>();
        if (buttonIcon != null && buildingData.icon != null)
        {
            buttonIcon.sprite = buildingData.icon;
        }
    }

    public Button GetButton() => button;
}
