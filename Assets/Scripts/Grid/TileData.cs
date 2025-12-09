using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ground states for tiles - determines buildability and visual state
/// </summary>
public enum GroundState
{
    Alive,      // Living nature (trees, grass) - non-buildable
    Polluted,   // Withered/dead nature - non-buildable
    Integrated  // Cleared/processed land - buildable
}

/// <summary>
/// Terrain types for fixed terrain tiles - determines visual transitions
/// Grass → Sand → Water (from buildable zone outward)
/// </summary>
public enum TerrainType
{
    None,       // No terrain type (uses GroundState transitions)
    Grass,      // Grass terrain (outermost buildable)
    Sand,       // Sand/beach terrain (non-buildable)
    Water       // Water terrain (non-buildable, non-walkable)
}

/// <summary>
/// ScriptableObject defining tile types with modular properties
/// </summary>
[CreateAssetMenu(fileName = "Tile_", menuName = "Planetfall/Grid/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Tile Info")]
    [Tooltip("Display name of this tile type")]
    public string tileName;

    [Tooltip("Description of this tile type")]
    [TextArea(2, 4)]
    public string description;

    [Header("Ground State")]
    [Tooltip("Current state of the ground - determines buildability")]
    public GroundState groundState = GroundState.Integrated;

    [Header("Terrain Type")]
    [Tooltip("Fixed terrain type for visual transitions (Grass/Sand/Water)")]
    public TerrainType terrainType = TerrainType.None;

    [Header("Visual")]
    [Tooltip("Sprite for this tile (used in Tilemap)")]
    public Sprite tileSprite;

    [Tooltip("Color tint for placement preview")]
    public Color previewColor = Color.white;

    [Header("Tile Properties (Modular System)")]
    [Tooltip("Additional properties this tile has (water, rough terrain, etc.)")]
    public List<TileProperty> properties = new List<TileProperty>();

    // Computed properties - groundState takes priority, then check properties
    public bool IsBuildable => CheckBuildable();
    public bool IsWalkable => CheckWalkable();
    public float MovementCost => CalculateMovementCost();
    public float BuildCostMultiplier => CalculateBuildCost();

    /// <summary>
    /// Check if tile is buildable - only Integrated ground state allows building
    /// </summary>
    private bool CheckBuildable()
    {
        // Ground state is primary check - only Integrated allows building
        if (groundState != GroundState.Integrated)
        {
            return false;
        }

        // Also check any properties that might block building (water, etc.)
        foreach (var property in properties)
        {
            if (property != null && !property.IsBuildable())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check if tile is walkable (all properties must allow)
    /// </summary>
    private bool CheckWalkable()
    {
        foreach (var property in properties)
        {
            if (property != null && !property.IsWalkable())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Calculate total movement cost (sum of all property costs)
    /// </summary>
    private float CalculateMovementCost()
    {
        float cost = 1.0f;
        foreach (var property in properties)
        {
            if (property != null)
            {
                float propertyCost = property.GetMovementCost();
                if (propertyCost == float.MaxValue)
                {
                    return float.MaxValue; // Impassable
                }
                cost += (propertyCost - 1.0f); // Accumulate penalties
            }
        }
        return cost;
    }

    /// <summary>
    /// Calculate total build cost multiplier (product of all modifiers)
    /// </summary>
    private float CalculateBuildCost()
    {
        float multiplier = 1.0f;
        foreach (var property in properties)
        {
            if (property != null)
            {
                multiplier *= property.GetBuildCostMultiplier();
            }
        }
        return multiplier;
    }

    /// <summary>
    /// Check if tile has specific property type
    /// </summary>
    public bool HasProperty<T>() where T : TileProperty
    {
        foreach (var property in properties)
        {
            if (property is T) return true;
        }
        return false;
    }

    /// <summary>
    /// Get property of specific type (returns null if not found)
    /// </summary>
    public T GetProperty<T>() where T : TileProperty
    {
        foreach (var property in properties)
        {
            if (property is T) return property as T;
        }
        return null;
    }

    /// <summary>
    /// Get all properties of specific type
    /// </summary>
    public List<T> GetProperties<T>() where T : TileProperty
    {
        List<T> result = new List<T>();
        foreach (var property in properties)
        {
            if (property is T) result.Add(property as T);
        }
        return result;
    }

    /// <summary>
    /// Notify all properties that building was placed
    /// </summary>
    public void NotifyBuildingPlaced(Building building, Vector2Int tilePosition)
    {
        foreach (var property in properties)
        {
            if (property != null)
            {
                property.OnBuildingPlaced(building, tilePosition);
            }
        }
    }

    /// <summary>
    /// Notify all properties that building was removed
    /// </summary>
    public void NotifyBuildingRemoved(Building building, Vector2Int tilePosition)
    {
        foreach (var property in properties)
        {
            if (property != null)
            {
                property.OnBuildingRemoved(building, tilePosition);
            }
        }
    }

    /// <summary>
    /// Notify all properties that enemy entered
    /// </summary>
    public void NotifyEnemyEnter(Enemy enemy, Vector2Int tilePosition)
    {
        foreach (var property in properties)
        {
            if (property != null)
            {
                property.OnEnemyEnter(enemy, tilePosition);
            }
        }
    }

    /// <summary>
    /// Notify all properties that enemy exited
    /// </summary>
    public void NotifyEnemyExit(Enemy enemy, Vector2Int tilePosition)
    {
        foreach (var property in properties)
        {
            if (property != null)
            {
                property.OnEnemyExit(enemy, tilePosition);
            }
        }
    }

    /// <summary>
    /// Update all properties (called by GridManager for active tiles)
    /// </summary>
    public void UpdateProperties(Vector2Int tilePosition)
    {
        foreach (var property in properties)
        {
            if (property != null)
            {
                property.OnUpdate(tilePosition);
            }
        }
    }
}
