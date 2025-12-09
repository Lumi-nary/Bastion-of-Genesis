using UnityEngine;

/// <summary>
/// Demon ability - Reduces building max HP temporarily
/// Cursed buildings have their maximum health reduced
/// </summary>
[CreateAssetMenu(fileName = "Ability_Curse", menuName = "Planetfall/Abilities/Curse")]
public class CurseAbility : SpecialAbility
{
    [Header("Curse Configuration")]
    [Tooltip("Max HP reduction percentage (0.2 = -20% max HP)")]
    [Range(0f, 1f)]
    public float maxHPReduction = 0.2f;

    [Tooltip("Duration of curse (seconds)")]
    public float duration = 10f;

    public override void OnAttack(Enemy attacker, Building target, float damage)
    {
        if (target == null || target.IsDestroyed) return;

        target.ApplyCurse(maxHPReduction, duration);
        Debug.Log($"[CurseAbility] {target.BuildingData.buildingName} cursed! Max HP reduced by {maxHPReduction * 100}% for {duration}s");
    }
}
