using UnityEngine;
using UnityEngine.InputSystem;

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
        inputActions.UI.Enable();
        inputActions.UI.Click.performed += OnClick;
        inputActions.UI.RightClick.performed += OnRightClick;
    }

    private void OnDisable()
    {
        inputActions.UI.Click.performed -= OnClick;
        inputActions.UI.RightClick.performed -= OnRightClick;
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

    private void OnClick(InputAction.CallbackContext context)
    {
        if (buildingToPlace != null)
        {
            PlaceBuilding();
        }
        else
        {
            // Not in build mode, check if a building was clicked
            Vector3 mousePos = GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                Building clickedBuilding = hit.collider.GetComponent<Building>();
                if (clickedBuilding != null)
                {
                    UIManager.Instance.ShowBuildingInfoPanel(clickedBuilding);
                }
            }
        }
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