using UnityEngine;

/// <summary>
/// Dwarf Tunneler ability - Bypasses Iron Walls only
/// Cannot bypass Steel or Null-Magic Walls (too strong)
/// </summary>
[CreateAssetMenu(fileName = "Ability_Tunneling", menuName = "Planetfall/Abilities/Tunneling")]
public class TunnelingAbility : SpecialAbility
{
    [Header("Tunneling Configuration")]
    [Tooltip("Tunneling speed multiplier underground")]
    public float undergroundSpeedMultiplier = 1.2f;

    private bool isUnderground = false;

    public override void OnUpdate(Enemy owner)
    {
        // Check if there's an Iron Wall between enemy and target
        // If yes, go underground and bypass it
        // This is a simplified version - actual implementation would use raycasting

        if (owner.CurrentTarget == null) return;

        // TODO: Implement wall detection and underground state
        // For now, this is a placeholder that marks the enemy as having tunneling ability
        // Actual wall bypassing logic would be in a pathfinding/collision system
    }

    public override Vector3 GetMovementDirection(Enemy owner, Vector3 defaultDirection)
    {
        // When underground, move faster
        if (isUnderground)
        {
            return defaultDirection * undergroundSpeedMultiplier;
        }

        return defaultDirection;
    }
}
