using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ground Demon ability - Deals damage to all buildings in radius
/// Attack hits multiple buildings simultaneously
/// </summary>
[CreateAssetMenu(fileName = "Ability_AOE", menuName = "Planetfall/Abilities/AOE")]
public class AOEAbility : SpecialAbility
{
    [Header("AOE Configuration")]
    [Tooltip("Radius of AOE attack (tiles)")]
    public float radius = 3f;

    [Tooltip("Damage multiplier for AOE (1.0 = full damage to all targets)")]
    [Range(0f, 2f)]
    public float damageMultiplier = 1.0f;

    public override void OnAttack(Enemy attacker, Building target, float damage)
    {
        if (target == null || BuildingManager.Instance == null) return;

        Vector3 center = target.transform.position;
        float aoeDamage = damage * damageMultiplier;

        // Get all buildings within radius
        List<Building> allBuildings = BuildingManager.Instance.GetAllBuildings();
        int hitCount = 0;

        foreach (Building building in allBuildings)
        {
            if (building == null || building.IsDestroyed) continue;

            float distance = Vector3.Distance(center, building.transform.position);
            if (distance <= radius)
            {
                building.TakeDamage(aoeDamage);
                hitCount++;
            }
        }

        Debug.Log($"[AOEAbility] AOE attack hit {hitCount} buildings in {radius} tile radius for {aoeDamage} damage each");
    }
}
