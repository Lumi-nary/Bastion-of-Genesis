using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    private InputSystem_Actions inputActions;
    
    // Build Mode
    private BuildingData buildingToPlace;
    private GameObject buildingPreview;

    // Selection Mode
    private Building selectedBuilding;

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

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Click.performed += OnClick;
    }

    private void OnDisable()
    {
        inputActions.UI.Click.performed -= OnClick;
        inputActions.UI.Disable();
    }

    private void Update()
    {
        if (buildingToPlace != null)
        {
            UpdateBuildingPreview();
        }
    }

    public void EnterBuildMode(BuildingData building)
    {
        DeselectBuilding();
        buildingToPlace = building;
        if (buildingPreview != null)
        {
            Destroy(buildingPreview);
        }
        buildingPreview = Instantiate(building.prefab);
        // Add logic to make the preview look like a preview (e.g., transparent material)
    }

    private void ExitBuildMode()
    {
        buildingToPlace = null;
        if (buildingPreview != null)
        {
            Destroy(buildingPreview);
            buildingPreview = null;
        }
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        // Prioritize build mode
        if (buildingToPlace != null)
        {
            PlaceBuilding();
            return;
        }

        // If not in build mode, handle selection
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Building")))
        {
            if (hit.collider.TryGetComponent<Building>(out Building building))
            {
                SelectBuilding(building);
            }
            else
            {
                DeselectBuilding();
            }
        }
        else
        {
            DeselectBuilding();
        }
    }

    private void PlaceBuilding()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Ground")))
        {
            BuildingManager.Instance.PlaceBuilding(hit.point);
            ExitBuildMode();
        }
    }

    private void SelectBuilding(Building building)
    {
        if (selectedBuilding != null)
        {
            // Add visual deselection logic here if needed
        }

        selectedBuilding = building;
        // Add visual selection logic here (e.g., outline)
        Debug.Log("Selected: " + selectedBuilding.BuildingData.buildingName);
        UIManager.Instance.ShowBuildingInfoPanel(selectedBuilding);
    }

    private void DeselectBuilding()
    {
        if (selectedBuilding != null)
        {
            // Add visual deselection logic here
            UIManager.Instance.HideBuildingInfoPanel();
        }
        selectedBuilding = null;
    }

    private void UpdateBuildingPreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Ground")))
        {
            buildingPreview.transform.position = hit.point;
        }
    }
}
