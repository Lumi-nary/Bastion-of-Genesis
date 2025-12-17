using UnityEngine;

/// <summary>
/// Ore mound type (matches resource types)
/// </summary>
public enum OreMoundType
{
    Iron,
    Copper,
    Mana
}

/// <summary>
/// Component representing an ore mound (resource node)
/// Ore mounds are fixed locations that can have one extractor placed on them
/// Extractors on mounds produce infinite resources
/// Extends VisualObject for proper Y-sorting
/// </summary>
public class OreMound : VisualObject
{
    [Header("Ore Mound Configuration")]
    [Tooltip("Type of ore this mound provides")]
    public OreMoundType moundType;

    [Tooltip("Resource type this mound provides")]
    public ResourceType resourceType;

    [Tooltip("Is this mound visible from mission start?")]
    public bool starterMound = false;

    [Tooltip("Pollution level required to discover this mound (0 = starter)")]
    public float pollutionRequiredToDiscover = 0f;

    [Header("Mound State")]
    [Tooltip("Has this mound been discovered?")]
    private bool isDiscovered = false;

    [Tooltip("Does this mound have an extractor placed on it?")]
    private bool hasExtractor = false;

    [Tooltip("Reference to the extractor building on this mound")]
    private Building assignedExtractor;

    // Events
    public delegate void MoundDiscoveredEvent(OreMound mound);
    public static event MoundDiscoveredEvent OnMoundDiscovered;

    public delegate void ExtractorPlacedEvent(OreMound mound, Building extractor);
    public static event ExtractorPlacedEvent OnExtractorPlaced;

    public delegate void ExtractorRemovedEvent(OreMound mound);
    public static event ExtractorRemovedEvent OnExtractorRemoved;

    // Public properties
    public bool IsDiscovered => isDiscovered;
    public bool HasExtractor => hasExtractor;
    public Building AssignedExtractor => assignedExtractor;
    public Vector3 Position => transform.position;

    protected override void Awake()
    {
        // Set visual layer for ore mounds (renders behind trees/buildings)
        visualLayer = VisualLayer.Resources;

        base.Awake();
    }

    private void Start()
    {
        // Add BoxCollider2D for collision detection (blocks enemies, prevents non-extractor buildings)
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        // Ore mounds are typically 3x3
        collider.size = new Vector2(3f, 3f);
        collider.offset = Vector2.zero;

        // Register with GridManager to block pathfinding
        RegisterWithGrid();

        // Starter mounds are discovered immediately
        if (starterMound)
        {
            Discover();
        }
        else
        {
            // Hide until discovered
            SetVisibility(false);
        }

        // Register with GridManager (ore mound system)
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterOreMound(this);
        }
    }

    /// <summary>
    /// Register ore mound cells with GridManager to block pathfinding
    /// </summary>
    private void RegisterWithGrid()
    {
        if (GridManager.Instance == null) return;

        Vector2Int centerPos = GridManager.Instance.WorldToGridPosition(transform.position);

        // Register 3x3 area as occupied (ore mounds block movement)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int cellPos = new Vector2Int(centerPos.x + x, centerPos.y + y);
                GridManager.Instance.RegisterObstacle(cellPos);
            }
        }

        Debug.Log($"[OreMound] Registered obstacle at grid {centerPos} (world {transform.position})");

        // Request flow field recalculation now that obstacle is registered
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }

    private void OnDestroy()
    {
        // Unregister from GridManager (ore mound system)
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterOreMound(this);
        }
    }

    /// <summary>
    /// Discover this ore mound (make visible)
    /// </summary>
    public void Discover()
    {
        if (isDiscovered) return;

        isDiscovered = true;
        SetVisibility(true);

        OnMoundDiscovered?.Invoke(this);

        Debug.Log($"[OreMound] Discovered {moundType} mound at {Position}");
    }

    /// <summary>
    /// Check if mound can be discovered at current pollution level
    /// </summary>
    public bool CanDiscoverAtPollution(float currentPollution)
    {
        return currentPollution >= pollutionRequiredToDiscover;
    }

    /// <summary>
    /// Set visibility of mound (for discovery system)
    /// </summary>
    private void SetVisibility(bool visible)
    {
        // Toggle renderers
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.enabled = visible;
        }

        // Toggle colliders (prevent interaction when hidden)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = visible;
        }
    }

    /// <summary>
    /// Check if an extractor can be placed on this mound
    /// </summary>
    public bool CanPlaceExtractor()
    {
        // Must be discovered and not already have an extractor
        return isDiscovered && !hasExtractor;
    }

    /// <summary>
    /// Place an extractor on this mound
    /// </summary>
    public bool PlaceExtractor(Building extractor)
    {
        if (!CanPlaceExtractor())
        {
            Debug.LogWarning($"[OreMound] Cannot place extractor on {moundType} mound");
            return false;
        }

        // Verify extractor is correct type for this mound
        if (!IsCorrectExtractorType(extractor))
        {
            Debug.LogWarning($"[OreMound] Wrong extractor type for {moundType} mound");
            return false;
        }

        hasExtractor = true;
        assignedExtractor = extractor;

        // Hide ore mound visual - building replaces it
        SetVisibility(false);

        OnExtractorPlaced?.Invoke(this, extractor);

        Debug.Log($"[OreMound] Placed {extractor.BuildingData.buildingName} on {moundType} mound - hiding mound visual");

        return true;
    }

    /// <summary>
    /// Remove extractor from this mound (when destroyed)
    /// </summary>
    public void RemoveExtractor()
    {
        if (!hasExtractor) return;

        hasExtractor = false;
        assignedExtractor = null;

        // Show ore mound again - it's available for new extractor
        SetVisibility(true);

        OnExtractorRemoved?.Invoke(this);

        Debug.Log($"[OreMound] Removed extractor from {moundType} mound - showing mound visual");
    }

    /// <summary>
    /// Check if building is correct extractor type for this mound
    /// </summary>
    private bool IsCorrectExtractorType(Building extractor)
    {
        if (extractor == null || extractor.BuildingData.category != BuildingCategory.Extraction)
        {
            return false;
        }

        // Check if extractor has ResourceExtractorFeature
        if (!extractor.BuildingData.HasFeature<ResourceExtractorFeature>())
        {
            return false;
        }

        // Check if feature's resource type matches this mound's resource type
        ResourceExtractorFeature extractorFeature = extractor.BuildingData.GetFeature<ResourceExtractorFeature>();
        if (extractorFeature != null && extractorFeature.resourceType == resourceType)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get mound type name
    /// </summary>
    public string GetMoundTypeName()
    {
        switch (moundType)
        {
            case OreMoundType.Iron:
                return "Iron Ore Mound";
            case OreMoundType.Copper:
                return "Copper Ore Mound";
            case OreMoundType.Mana:
                return "Mana Crystal Vein";
            default:
                return "Unknown Mound";
        }
    }

    /// <summary>
    /// Visualize mound in editor
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = isDiscovered ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (hasExtractor)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
