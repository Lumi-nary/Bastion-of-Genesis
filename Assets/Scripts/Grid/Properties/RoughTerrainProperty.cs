using UnityEngine;

/// <summary>
/// Difficult terrain that slows enemy movement and increases build costs
/// </summary>
[CreateAssetMenu(fileName = "Property_RoughTerrain", menuName = "Planetfall/Grid/Properties/Rough Terrain")]
public class RoughTerrainProperty : TileProperty
{
    [Header("Rough Terrain Configuration")]
    [Tooltip("Movement cost multiplier (1.5 = 50% slower)")]
    public float movementMultiplier = 1.5f;

    [Tooltip("Building cost multiplier (1.2 = 20% more expensive)")]
    public float buildCostMultiplier = 1.2f;

    public override float GetMovementCost()
    {
        return movementMultiplier;
    }

    public override float GetBuildCostMultiplier()
    {
        return buildCostMultiplier;
    }

    public override string GetPropertyDescription()
    {
        int movementPenalty = Mathf.RoundToInt((movementMultiplier - 1f) * 100f);
        int costPenalty = Mathf.RoundToInt((buildCostMultiplier - 1f) * 100f);
        return $"Rough terrain: {movementPenalty}% slower movement, {costPenalty}% higher build cost";
    }
}
