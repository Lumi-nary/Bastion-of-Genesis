using UnityEngine;

/// <summary>
/// Wall tier types (from BUILDINGS.md)
/// </summary>
public enum WallTier
{
    Iron,       // Tier 1 - Basic wall, bypassed by Tunneling enemies
    Steel,      // Tier 2 - Blocks Tunneling enemies
    NullMagic   // Tier 3 - Blocks Tunneling, 80% magic damage resistance
}

/// <summary>
/// Feature for wall buildings
/// Defines wall properties for pathfinding and damage resistance
/// </summary>
[CreateAssetMenu(fileName = "Feature_Wall", menuName = "Planetfall/Building Features/Wall")]
public class WallFeature : BuildingFeature
{
    [Header("Wall Configuration")]
    [Tooltip("Wall tier determining strength and tunneling resistance")]
    public WallTier wallTier = WallTier.Iron;

    [Tooltip("Magic damage resistance (0-1, where 0.8 = 80% resistance)")]
    [Range(0f, 1f)]
    public float magicResistance = 0f;

    /// <summary>
    /// Check if this wall blocks tunneling enemies
    /// Iron walls are bypassed, Steel and Null-Magic block tunneling
    /// </summary>
    public bool BlocksTunneling()
    {
        return wallTier != WallTier.Iron;
    }

    /// <summary>
    /// Get magic damage resistance multiplier
    /// Returns the multiplier for incoming magic damage (1 = full damage, 0.2 = 80% resistance)
    /// </summary>
    public float GetMagicDamageMultiplier()
    {
        return 1f - magicResistance;
    }

    public override void OnDamaged(Building building, float damage)
    {
        // Magic resistance is handled in Building.TakeDamage by checking damage type
        // This method can be extended for wall-specific damage effects
    }

    public override void OnDestroyed(Building building)
    {
        // Notify PathfindingManager that a wall was destroyed
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }

    public override void OnBuilt(Building building)
    {
        // Notify PathfindingManager that a new wall was placed
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }
}
