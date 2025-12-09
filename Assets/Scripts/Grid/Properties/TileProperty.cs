using UnityEngine;

/// <summary>
/// Base class for all tile properties
/// Modular system - add new tile behaviors by creating new ScriptableObjects
/// </summary>
public abstract class TileProperty : ScriptableObject
{
    [Header("Property Info")]
    [Tooltip("Display name of this property")]
    public string propertyName;

    [Tooltip("Description of what this property does")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Icon for UI display (optional)")]
    public Sprite icon;

    /// <summary>
    /// Can buildings be placed on this tile?
    /// </summary>
    public virtual bool IsBuildable() { return true; }

    /// <summary>
    /// Can enemies walk on this tile?
    /// </summary>
    public virtual bool IsWalkable() { return true; }

    /// <summary>
    /// Movement cost multiplier for pathfinding (1.0 = normal, higher = slower)
    /// </summary>
    public virtual float GetMovementCost() { return 1.0f; }

    /// <summary>
    /// Cost multiplier for building on this tile (1.0 = normal, higher = more expensive)
    /// </summary>
    public virtual float GetBuildCostMultiplier() { return 1.0f; }

    /// <summary>
    /// Called when a building is placed on this tile
    /// </summary>
    public virtual void OnBuildingPlaced(Building building, Vector2Int tilePosition) { }

    /// <summary>
    /// Called when a building is destroyed on this tile
    /// </summary>
    public virtual void OnBuildingRemoved(Building building, Vector2Int tilePosition) { }

    /// <summary>
    /// Called when an enemy enters this tile
    /// </summary>
    public virtual void OnEnemyEnter(Enemy enemy, Vector2Int tilePosition) { }

    /// <summary>
    /// Called when an enemy exits this tile
    /// </summary>
    public virtual void OnEnemyExit(Enemy enemy, Vector2Int tilePosition) { }

    /// <summary>
    /// Called every frame for tiles with active effects (hazards, trees checking pollution, etc.)
    /// </summary>
    public virtual void OnUpdate(Vector2Int tilePosition) { }

    /// <summary>
    /// Get user-friendly description for UI
    /// </summary>
    public virtual string GetPropertyDescription()
    {
        return description;
    }
}
