using UnityEngine;

/// <summary>
/// Marker property for tree placement. Placed on tiles where trees should spawn.
/// Trees are non-buildable and always walkable by enemies.
/// Visual handling and destruction managed by EnvironmentManager.
/// </summary>
[CreateAssetMenu(fileName = "Property_Tree", menuName = "Planetfall/Grid/Properties/Tree")]
public class TreeProperty : TileProperty
{
    [Header("Tree Configuration")]
    [Tooltip("Optional: specific tree sprite variant index (-1 for random)")]
    public int spriteVariantIndex = -1;

    public override bool IsBuildable()
    {
        // Trees block building placement
        return false;
    }

    public override bool IsWalkable()
    {
        // Enemies can walk through trees (visual only)
        return true;
    }

    public override string GetPropertyDescription()
    {
        return "Tree - blocks building, walkable by enemies";
    }
}
