using UnityEngine;

/// <summary>
/// Unified vegetation system for grass, bushes, shrubs, and trees.
/// All vegetation exists only in Alive/Grass tiles and is destroyed when tile becomes Polluted/Withered.
/// Sort order: Grass (lowest) < Bush < Tree (highest, Y-sorted)
/// </summary>
public class Vegetation : VisualObject
{
    [Header("Vegetation Type")]
    [SerializeField] private VegetationType vegetationType = VegetationType.Grass;

    [Header("Size Configuration")]
    [Tooltip("Size in tiles (most are 1x1, some bushes are 2x1)")]
    [SerializeField] private Vector2Int tileSize = Vector2Int.one;

    [Header("Sprites")]
    [Tooltip("Possible sprites for random variety (picks one on spawn)")]
    [SerializeField] private Sprite[] spriteVariants;

    public enum VegetationType
    {
        Grass,      // Small decoration, lowest sort order
        Bush,       // Medium, can be 2x1
        Shrub,      // Medium decoration
        Tree        // Large, Y-sorted, highest sort order
    }

    // Sort order bases for each type (added to base layer value)
    private const int GRASS_SORT_BASE = -50;    // Behind everything
    private const int BUSH_SORT_BASE = -25;     // Behind trees
    private const int SHRUB_SORT_BASE = -25;    // Same as bush
    private const int TREE_SORT_BASE = 0;       // Default, Y-sorted

    public VegetationType Type => vegetationType;
    public Vector2Int TileSize => tileSize;

    protected override void Awake()
    {
        // Set layer based on vegetation type BEFORE base.Awake() initializes sorting
        SetLayerForType();
        base.Awake();

        // Set sort order based on type
        SetSortOrderForType();

        // Pick random sprite variant if available
        if (spriteVariants != null && spriteVariants.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteVariants[Random.Range(0, spriteVariants.Length)];
        }
    }

    /// <summary>
    /// Initialize vegetation with grid position
    /// </summary>
    public void Initialize(Vector2Int gridPos)
    {
        gridPosition = gridPos;
        SetSortOrderForType();
    }

    /// <summary>
    /// Initialize vegetation with grid position and specific sprite
    /// </summary>
    public void Initialize(Vector2Int gridPos, Sprite sprite)
    {
        gridPosition = gridPos;
        if (sprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
        SetSortOrderForType();
    }

    /// <summary>
    /// Set visual layer based on vegetation type.
    /// Grass/Bush/Shrub use EnvironmentBackground (behind enemies).
    /// Trees use EnvironmentForeground (in front of enemies).
    /// </summary>
    private void SetLayerForType()
    {
        switch (vegetationType)
        {
            case VegetationType.Grass:
            case VegetationType.Bush:
            case VegetationType.Shrub:
                visualLayer = VisualLayer.EnvironmentBackground;
                break;

            case VegetationType.Tree:
                visualLayer = VisualLayer.EnvironmentForeground;
                break;
        }
    }

    /// <summary>
    /// Set sorting order based on vegetation type
    /// </summary>
    private void SetSortOrderForType()
    {
        switch (vegetationType)
        {
            case VegetationType.Grass:
                baseSortingOrder = GRASS_SORT_BASE;
                enableDynamicSorting = false; // Grass doesn't need Y-sorting
                break;

            case VegetationType.Bush:
            case VegetationType.Shrub:
                baseSortingOrder = BUSH_SORT_BASE;
                enableDynamicSorting = false; // Bushes don't need Y-sorting
                break;

            case VegetationType.Tree:
                baseSortingOrder = TREE_SORT_BASE;
                enableDynamicSorting = true; // Trees use Y-sorting
                break;
        }

        // Update sorting immediately
        if (spriteRenderer != null)
        {
            UpdateSortingOrder();
        }
    }

    /// <summary>
    /// Override to handle grass (no Y-sorting) vs trees (Y-sorted)
    /// </summary>
    protected override void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;

        int layerBase = (int)visualLayer;

        if (vegetationType == VegetationType.Tree)
        {
            // Trees: Y-sorted (lower Y = higher order = renders on top)
            int yOffset = -Mathf.RoundToInt(transform.position.y * ySortingPrecision);
            spriteRenderer.sortingOrder = layerBase + baseSortingOrder + yOffset;
        }
        else
        {
            // Grass/Bush: Fixed sort order (behind trees)
            spriteRenderer.sortingOrder = layerBase + baseSortingOrder;
        }
    }

    /// <summary>
    /// Destroy this vegetation (called when tile becomes withered)
    /// </summary>
    public void DestroyVegetation()
    {
        // Notify manager before destroying
        if (EnvironmentManager.Instance != null)
        {
            //EnvironmentManager.Instance.OnVegetationDestroyed(this);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Check if this vegetation occupies a specific grid position
    /// (For multi-tile vegetation like 2x1 bushes)
    /// </summary>
    public bool OccupiesPosition(Vector2Int pos)
    {
        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                if (gridPosition + new Vector2Int(x, y) == pos)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Get all grid positions this vegetation occupies
    /// </summary>
    public Vector2Int[] GetOccupiedPositions()
    {
        Vector2Int[] positions = new Vector2Int[tileSize.x * tileSize.y];
        int index = 0;
        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                positions[index++] = gridPosition + new Vector2Int(x, y);
            }
        }
        return positions;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw vegetation indicator
        switch (vegetationType)
        {
            case VegetationType.Grass:
                Gizmos.color = Color.green;
                break;
            case VegetationType.Bush:
            case VegetationType.Shrub:
                Gizmos.color = Color.yellow;
                break;
            case VegetationType.Tree:
                Gizmos.color = new Color(0.4f, 0.2f, 0f); // Brown
                break;
        }

        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // Draw tile size
        if (tileSize.x > 1 || tileSize.y > 1)
        {
            Vector3 size = new Vector3(tileSize.x, tileSize.y, 0);
            Gizmos.DrawWireCube(transform.position + (Vector3)(Vector2)(tileSize - Vector2Int.one) * 0.5f, size);
        }

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"{vegetationType} at {gridPosition}");
    }
#endif
}
