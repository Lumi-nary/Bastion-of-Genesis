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
        // Convert mouse position to world position for 2D
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 mouseWorldPos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        // Use OverlapPoint for reliable 2D detection (checks all colliders at point)
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos2D);

        Building clickedBuilding = null;

        // Find the first Building component among all hit colliders
        foreach (Collider2D hit in hits)
        {
            Building building = hit.GetComponent<Building>();
            if (building != null)
            {
                clickedBuilding = building;
                break;
            }
        }

        if (clickedBuilding != null)
        {
            // Clicked on a building, show its info
            UIManager.Instance.ShowBuildingInfoPanel(clickedBuilding);
        }
        else
        {
            // Clicked on empty space or non-building
            UIManager.Instance.HideBuildingInfoPanel();
        }
    }

    public void EnterBuildMode(BuildingData building)
    {
        buildingToPlace = building;
        if (buildingPreview != null) Destroy(buildingPreview);

        buildingPreview = Instantiate(building.prefab);
        previewRenderer = buildingPreview.GetComponentInChildren<SpriteRenderer>();

        // Disable colliders on preview so it doesn't block physics checks
        foreach (Collider2D col in buildingPreview.GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // Disable Building component so it doesn't register with managers
        Building previewBuilding = buildingPreview.GetComponent<Building>();
        if (previewBuilding != null)
        {
            previewBuilding.enabled = false;
        }
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

        // Calculate center offset based on building size
        int centerOffsetX = buildingToPlace.width / 2;
        int centerOffsetY = buildingToPlace.height / 2;

        // Treat mouse position as CENTER for all buildings, calculate bottom-left corner
        Vector2Int startCell = new Vector2Int(gridPos.x - centerOffsetX, gridPos.y - centerOffsetY);

        if (CanPlaceBuilding(startCell, buildingToPlace.width, buildingToPlace.height))
        {
            // Calculate placement position at center (for center-pivoted sprites)
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

        // Calculate center offset based on building size
        int centerOffsetX = buildingToPlace.width / 2;
        int centerOffsetY = buildingToPlace.height / 2;

        // Cursor is at center, calculate bottom-left for validation
        Vector2Int startCell = new Vector2Int(gridPos.x - centerOffsetX, gridPos.y - centerOffsetY);

        // Show preview at center (where cursor is)
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);

        buildingPreview.transform.position = worldPos;

        if (CanPlaceBuilding(startCell, buildingToPlace.width, buildingToPlace.height))
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
        // Check if building requires ore mound (extractors)
        bool requiresOreMound = false;
        OreMound mound = null;

        if (buildingToPlace != null && buildingToPlace.HasFeature<ResourceExtractorFeature>())
        {
            ResourceExtractorFeature extractor = buildingToPlace.GetFeature<ResourceExtractorFeature>();
            requiresOreMound = extractor.requiresOreMound;

            // If requires ore mound, validate manually-placed ore mound
            if (requiresOreMound)
            {
                // Check if there's a manually-placed ore mound at this position
                if (GridManager.Instance == null)
                {
                    Debug.Log("[Placement] BLOCKED: GridManager.Instance is null");
                    return false;
                }

                // STRICT CENTER VALIDATION: building center MUST align with ore mound
                // Calculate center grid position based on building size
                int centerOffsetX = width / 2;
                int centerOffsetY = height / 2;
                Vector2Int centerGridPos = new Vector2Int(startCell.x + centerOffsetX, startCell.y + centerOffsetY);
                Vector3 centerWorldPos = gridManager.GridToWorldPosition(centerGridPos);

                // Search for ore mound at exact center with tight tolerance (0.5 units)
                // This ensures the player must manually align the building center with the ore mound
                float centerTolerance = 0.5f;

                mound = GridManager.Instance.GetMoundAtPosition(centerWorldPos, centerTolerance);

                if (mound == null)
                {
                    Debug.Log($"[Placement] BLOCKED: No ore mound at center {centerWorldPos}");
                    return false; // Building not centered on ore mound
                }

                // Check if ore mound is discovered
                if (!mound.IsDiscovered)
                {
                    Debug.Log("[Placement] BLOCKED: Ore mound not discovered");
                    return false;
                }

                // Check if ore mound is within integrated zone
                if (TileStateManager.Instance != null)
                {
                    Vector2Int moundGridPos = gridManager.WorldToGridPosition(mound.Position);
                    if (TileStateManager.Instance.GetGroundState(moundGridPos) != GroundState.Integrated)
                    {
                        Debug.Log("[Placement] BLOCKED: Ore mound is outside integrated zone");
                        return false;
                    }
                }

                // Check if ore mound already has extractor
                if (mound.HasExtractor)
                {
                    Debug.Log("[Placement] BLOCKED: Ore mound already has extractor");
                    return false;
                }

                // Check if extractor matches ore mound type
                OreMoundType expectedMoundType = GetOreMoundTypeFromResource(extractor.resourceType);
                if (mound.moundType != expectedMoundType)
                {
                    Debug.Log($"[Placement] BLOCKED: Mound type mismatch (expected {expectedMoundType}, got {mound.moundType})");
                    return false;
                }
            }
        }
        else
        {
            // This building does NOT require an ore mound
            // Check if there's an ore mound at this location - if so, prevent placement
            if (GridManager.Instance != null)
            {
                // Check center position for ore mound (assuming most buildings would overlap center)
                Vector2Int centerGridPos = new Vector2Int(startCell.x + (width / 2), startCell.y + (height / 2));
                Vector3 centerWorldPos = gridManager.GridToWorldPosition(centerGridPos);

                OreMound moundAtLocation = GridManager.Instance.GetMoundAtPosition(centerWorldPos, 1.5f);

                if (moundAtLocation != null)
                {
                    Debug.Log($"[Placement] BLOCKED: Ore mound at {centerWorldPos} but building doesn't extract");
                    return false; // Ore mound exists here, but this building doesn't extract from it
                }
            }
        }

        // Use Physics2D to check for collisions with existing buildings and ore mounds
        // Shrink box slightly (0.9x) to allow adjacent placement (edges can touch)
        Vector2 centerPosition = gridManager.GridToWorldPosition(new Vector2Int(startCell.x + (width / 2), startCell.y + (height / 2)));
        Vector2 boxSize = new Vector2(width * 0.9f, height * 0.9f);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(centerPosition, boxSize, 0f);

        foreach (Collider2D col in colliders)
        {
            // Check if there's a building blocking this position
            Building existingBuilding = col.GetComponent<Building>();
            if (existingBuilding != null)
            {
                Debug.Log($"[Placement] BLOCKED: Collider hit building '{existingBuilding.BuildingData.buildingName}'");
                return false; // Another building is in the way
            }

            // Check if there's an ore mound blocking this position (for non-extractors)
            OreMound oreMound = col.GetComponent<OreMound>();
            if (oreMound != null && !requiresOreMound)
            {
                Debug.Log($"[Placement] BLOCKED: Collider hit ore mound at {col.transform.position}");
                return false; // Ore mound exists here, but this building doesn't extract from it
            }
        }

        // Standard validation for all tiles in the building footprint
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cellPos = new Vector2Int(startCell.x + x, startCell.y + y);

                // Check if already occupied by another building (grid-based check)
                if (gridManager.IsCellOccupied(cellPos))
                {
                    Debug.Log($"[Placement] BLOCKED: Cell {cellPos} is occupied (grid check)");
                    return false; // Area is not clear
                }

                // Skip terrain checks for ore mound positions (already validated above)
                if (requiresOreMound && mound != null)
                {
                    continue; // Ore mound positions are valid by definition
                }

                // Check if tile is buildable (trees, etc. - water handled by physics above)
                if (!gridManager.IsBuildable(cellPos))
                {
                    Debug.Log($"[Placement] BLOCKED: Cell {cellPos} is not buildable (terrain check)");
                    return false; // Terrain doesn't allow building
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Convert ResourceType to OreMoundType for validation
    /// </summary>
    private OreMoundType GetOreMoundTypeFromResource(ResourceType resourceType)
    {
        if (resourceType == null) return OreMoundType.Iron;

        // Match by resource name
        string resourceName = resourceType.ResourceName.ToLower();

        if (resourceName.Contains("iron"))
            return OreMoundType.Iron;
        else if (resourceName.Contains("copper"))
            return OreMoundType.Copper;
        else if (resourceName.Contains("mana"))
            return OreMoundType.Mana;

        // Default to Iron
        Debug.LogWarning($"[PlacementSystem] Unknown resource type: {resourceType.ResourceName}, defaulting to Iron");
        return OreMoundType.Iron;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = mainCamera.nearClipPlane; // Ensure z-depth is correct for ScreenToWorldPoint
        return mainCamera.ScreenToWorldPoint(mousePos);
    }
}