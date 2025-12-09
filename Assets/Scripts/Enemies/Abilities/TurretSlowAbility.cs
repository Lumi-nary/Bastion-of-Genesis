using UnityEngine;

/// <summary>
/// Water Mage ability - Slows turret attack speed
/// Turrets hit by Water Mage attacks fire slower
/// </summary>
[CreateAssetMenu(fileName = "Ability_TurretSlow", menuName = "Planetfall/Abilities/TurretSlow")]
public class TurretSlowAbility : SpecialAbility
{
    [Header("Slow Configuration")]
    [Tooltip("Attack speed reduction percentage (0.3 = 30% slower)")]
    [Range(0f, 1f)]
    public float slowPercent = 0.3f;

    [Tooltip("Duration of slow effect (seconds)")]
    public float duration = 5f;

    public override void OnAttack(Enemy attacker, Building target, float damage)
    {
        if (target == null || target.IsDestroyed) return;

        // Only affects turrets (buildings with TurretFeature)
        if (!target.BuildingData.HasFeature<TurretFeature>()) return;

        Turret turretComponent = target.GetComponent<Turret>();
        if (turretComponent != null)
        {
            turretComponent.ApplySlow(slowPercent, duration);
            Debug.Log($"[TurretSlowAbility] {target.BuildingData.buildingName} slowed by {slowPercent * 100}% for {duration}s");
        }
    }
}
