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

    public Button GetButton() => button;
}
