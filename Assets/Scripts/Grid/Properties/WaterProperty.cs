using UnityEngine;

/// <summary>
/// Impassable water tiles that block building and movement
/// </summary>
[CreateAssetMenu(fileName = "Property_Water", menuName = "Planetfall/Grid/Properties/Water")]
public class WaterProperty : TileProperty
{
    [Header("Water Configuration")]
    [Tooltip("If true, completely impassable; if false, flying units can cross")]
    public bool isDeepWater = true;

    public override bool IsBuildable()
    {
        return false;
    }

    public override bool IsWalkable()
    {
        return false;
    }

    public override float GetMovementCost()
    {
        return float.MaxValue; // Impassable
    }

    public override string GetPropertyDescription()
    {
        return isDeepWater ? "Impassable deep water" : "Shallow water - flying units can cross";
    }
}
