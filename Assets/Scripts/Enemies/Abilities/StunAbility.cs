using UnityEngine;

/// <summary>
/// Elven Stunner ability - Stuns turrets temporarily
/// Stunned turrets cannot attack for the duration
/// </summary>
[CreateAssetMenu(fileName = "Ability_Stun", menuName = "Planetfall/Abilities/Stun")]
public class StunAbility : SpecialAbility
{
    [Header("Stun Configuration")]
    [Tooltip("Duration of stun effect (seconds)")]
    public float stunDuration = 3f;

    public override void OnAttack(Enemy attacker, Building target, float damage)
    {
        if (target == null || target.IsDestroyed) return;

        // Only affects turrets (buildings with TurretFeature)
        if (!target.BuildingData.HasFeature<TurretFeature>()) return;

        Turret turretComponent = target.GetComponent<Turret>();
        if (turretComponent != null)
        {
            turretComponent.ApplyStun(stunDuration);
            Debug.Log($"[StunAbility] {target.BuildingData.buildingName} stunned for {stunDuration}s");
        }
    }
}
