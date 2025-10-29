using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    private InputSystem_Actions inputActions;
    
    // Build Mode
    private BuildingData buildingToPlace;
    private GameObject buildingPreview;

    [Header("Building Placement Settings")]
    [SerializeField] private float gridSize = 1f; // Assuming 32 pixels per unit = 1 unit grid

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
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hitCollider = Physics2D.OverlapPoint(worldPoint, LayerMask.GetMask("Building"));

        if (hitCollider != null)
        {
            if (hitCollider.TryGetComponent<Building>(out Building building))
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
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 snappedPosition = SnapToGrid(worldPoint);
        BuildingManager.Instance.PlaceBuilding(snappedPosition);
        ExitBuildMode();
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
        if (buildingPreview == null) return;

        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 snappedPosition = SnapToGrid(worldPoint);
        buildingPreview.transform.position = snappedPosition;
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
        float snappedY = Mathf.Round(position.y / gridSize) * gridSize;
        return new Vector3(snappedX, snappedY, position.z);
    }}
