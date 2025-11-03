using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;

    [Header("Preview Materials")]
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    private InputSystem_Actions inputActions;
    private BuildingData buildingToPlace;
    private GameObject buildingPreview;
    private SpriteRenderer previewRenderer;

    public BuildingData BuildingToPlace => buildingToPlace;

    public bool IsBuilding => buildingToPlace != null;

    private Camera mainCamera;

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
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.RightClick.performed += OnRightClick;
    }

    private void OnDisable()
    {
        inputActions.UI.RightClick.performed -= OnRightClick;
        inputActions.Disable();
    }

    private void Update()
    {
        // If we are in build mode, update the preview
        if (buildingToPlace != null)
        {
            UpdateBuildingPreview();
        }

        // Check for left-click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Ignore the click if the pointer is over any UI element
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (buildingToPlace != null)
            {
                // If in build mode, place the building
                PlaceBuilding();
            }
            else
            {
                // If not in build mode, handle selection
                SelectBuilding();
            }
        }
    }

    private void SelectBuilding()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider != null)
        {
            Building clickedBuilding = hit.collider.GetComponent<Building>();
            if (clickedBuilding != null)
            {
                // Clicked on a building, show its info
                UIManager.Instance.ShowBuildingInfoPanel(clickedBuilding);
            }
            else
            {
                // Clicked on something that isn't a building (e.g., the ground, an enemy)
                UIManager.Instance.HideBuildingInfoPanel();
            }
        }
        else
        {
            // Clicked on empty space
            UIManager.Instance.HideBuildingInfoPanel();
        }
    }

    public void EnterBuildMode(BuildingData building)
    {
        buildingToPlace = building;
        if (buildingPreview != null) Destroy(buildingPreview);
        
        buildingPreview = Instantiate(building.prefab);
        previewRenderer = buildingPreview.GetComponentInChildren<SpriteRenderer>();
    }

    private void ExitBuildMode()
    {
        buildingToPlace = null;
        if (buildingPreview != null) Destroy(buildingPreview);
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (buildingToPlace != null)
        {
            ExitBuildMode();
        }
    }

    private void PlaceBuilding()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Vector2Int gridPos = gridManager.WorldToGridPosition(mousePos);

        if (CanPlaceBuilding(gridPos, buildingToPlace.width, buildingToPlace.height))
        {
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);
            BuildingManager.Instance.PlaceBuilding(buildingToPlace, worldPos);
            // After placing, we exit build mode. The click has been consumed by this action,
            // so the SelectBuilding() logic in Update() won't run in the same frame.
            ExitBuildMode();
        }
    }

    private void UpdateBuildingPreview()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Vector2Int gridPos = gridManager.WorldToGridPosition(mousePos);
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);

        buildingPreview.transform.position = worldPos;

        if (CanPlaceBuilding(gridPos, buildingToPlace.width, buildingToPlace.height))
        {
            previewRenderer.material = validPlacementMaterial;
        }
        else
        {
            previewRenderer.material = invalidPlacementMaterial;
        }
    }

    private bool CanPlaceBuilding(Vector2Int startCell, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridManager.IsCellOccupied(new Vector2Int(startCell.x + x, startCell.y + y)))
                {
                    return false; // Area is not clear
                }
            }
        }
        return true;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = mainCamera.nearClipPlane; // Ensure z-depth is correct for ScreenToWorldPoint
        return mainCamera.ScreenToWorldPoint(mousePos);
    }
}