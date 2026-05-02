using UnityEngine;

/// <summary>
/// Impassable cliff tiles that block building and ground movement.
/// </summary>
[CreateAssetMenu(fileName = "Property_Cliff", menuName = "Planetfall/Grid/Properties/Cliff")]
public class CliffProperty : TileProperty
{
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
        return float.MaxValue;
    }

    public override string GetPropertyDescription()
    {
        return string.IsNullOrWhiteSpace(description)
            ? "Impassable cliff"
            : description;
    }
}
