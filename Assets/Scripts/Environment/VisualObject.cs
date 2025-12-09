using UnityEngine;

/// <summary>
/// Base class for all visual GameObjects with Y-sorting
/// Handles proper layering for trees, ore mounds, rocks, buildings, walls, etc.
/// Lower Y position = rendered on top (closer to camera)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class VisualObject : MonoBehaviour
{
    [Header("Sorting Configuration")]
    [Tooltip("Sorting layer - defines what type of object this is")]
    [SerializeField] protected VisualLayer visualLayer = VisualLayer.Environment;

    [Tooltip("Base sorting order within layer")]
    [SerializeField] protected int baseSortingOrder = 0;

    [Tooltip("Y-sorting precision (higher = more granular sorting)")]
    [SerializeField] protected float ySortingPrecision = 100f;

    [Tooltip("Enable dynamic Y-sorting every frame")]
    [SerializeField] protected bool enableDynamicSorting = true;

    protected SpriteRenderer spriteRenderer;
    protected Vector2Int gridPosition;

    /// <summary>
    /// Visual layer hierarchy for proper rendering order
    /// </summary>
    public enum VisualLayer
    {
        Ground = 0,        // Terrain effects, shadows (renders first/behind)
        Resources = 100,   // Ore mounds, geysers, resource nodes
        Environment = 200, // Trees, rocks, vegetation
        Structures = 300,  // Walls, facilities, defenses
        Buildings = 400,   // Player buildings, enemy structures
        UI = 500          // World-space UI elements
    }

    public Vector2Int GridPosition => gridPosition;
    public VisualLayer Layer => visualLayer;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        InitializeSorting();
    }

    protected virtual void LateUpdate()
    {
        if (enableDynamicSorting)
        {
            UpdateSortingOrder();
        }
    }

    /// <summary>
    /// Initialize sorting layer and order
    /// </summary>
    protected virtual void InitializeSorting()
    {
        if (spriteRenderer == null) return;

        // Set sorting layer name based on enum
        spriteRenderer.sortingLayerName = GetSortingLayerName(visualLayer);

        // Calculate initial sorting order
        UpdateSortingOrder();
    }

    /// <summary>
    /// Update sorting order based on Y position
    /// Formula: layerBase + baseOrder - (Y * precision)
    /// Lower Y = higher sort order = renders on top
    /// </summary>
    protected virtual void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;

        int layerBase = (int)visualLayer;
        int yOffset = -Mathf.RoundToInt(transform.position.y * ySortingPrecision);
        int finalOrder = layerBase + baseSortingOrder + yOffset;

        spriteRenderer.sortingOrder = finalOrder;
    }

    /// <summary>
    /// Get Unity sorting layer name from enum
    /// </summary>
    protected string GetSortingLayerName(VisualLayer layer)
    {
        switch (layer)
        {
            case VisualLayer.Ground:
                return "Ground";
            case VisualLayer.Resources:
                return "Resources";
            case VisualLayer.Environment:
                return "Environment";
            case VisualLayer.Structures:
                return "Structures";
            case VisualLayer.Buildings:
                return "Buildings";
            case VisualLayer.UI:
                return "UI";
            default:
                return "Default";
        }
    }

    /// <summary>
    /// Set grid position for this object
    /// </summary>
    public virtual void SetGridPosition(Vector2Int pos)
    {
        gridPosition = pos;
    }

    /// <summary>
    /// Change visual layer at runtime
    /// </summary>
    public void SetVisualLayer(VisualLayer layer)
    {
        visualLayer = layer;
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = GetSortingLayerName(layer);
            UpdateSortingOrder();
        }
    }

    /// <summary>
    /// Change sprite at runtime
    /// </summary>
    public virtual void SetSprite(Sprite newSprite)
    {
        if (spriteRenderer != null && newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }

    /// <summary>
    /// Destroy this visual object
    /// </summary>
    public virtual void DestroyObject()
    {
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw grid position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);

        // Show sorting info
        if (spriteRenderer != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"Layer: {visualLayer}\nY: {transform.position.y:F2}\nOrder: {spriteRenderer.sortingOrder}"
            );
        }
    }
#endif
}
