using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fire Mage ability - Applies burning DoT to buildings
/// Buildings take additional fire damage over time after being hit
/// </summary>
[CreateAssetMenu(fileName = "Ability_Burning", menuName = "Planetfall/Abilities/Burning")]
public class BurningAbility : SpecialAbility
{
    [Header("Burning Configuration")]
    [Tooltip("Damage dealt per second")]
    public int damagePerSecond = 5;

    [Tooltip("Duration of burning effect (seconds)")]
    public float duration = 5f;

    // Track burning buildings (static so all Fire Mages share the same tracking)
    private static Dictionary<Building, BurningEffect> burningBuildings = new Dictionary<Building, BurningEffect>();

    private class BurningEffect
    {
        public float damagePerSecond;
        public float remainingDuration;
    }

    public override void OnAttack(Enemy attacker, Building target, float damage)
    {
        if (target == null || target.IsDestroyed) return;

        // Apply or refresh burning effect
        if (!burningBuildings.ContainsKey(target))
        {
            burningBuildings[target] = new BurningEffect
            {
                damagePerSecond = damagePerSecond,
                remainingDuration = duration
            };
        }
        else
        {
            // Refresh duration if already burning
            burningBuildings[target].remainingDuration = duration;
        }

        Debug.Log($"[BurningAbility] {target.BuildingData.buildingName} is now burning! ({damagePerSecond} dmg/s for {duration}s)");
    }

    public override void OnUpdate(Enemy owner)
    {
        // Update all burning effects
        if (burningBuildings.Count == 0) return;

        List<Building> toRemove = new List<Building>();

        foreach (var kvp in burningBuildings)
        {
            Building building = kvp.Key;
            BurningEffect effect = kvp.Value;

            if (building == null || building.IsDestroyed)
            {
                toRemove.Add(building);
                continue;
            }

            // Apply burning damage
            building.TakeDamage(effect.damagePerSecond * Time.deltaTime);

            // Update duration
            effect.remainingDuration -= Time.deltaTime;
            if (effect.remainingDuration <= 0)
            {
                toRemove.Add(building);
            }
        }

        // Remove expired effects
        foreach (Building building in toRemove)
        {
            burningBuildings.Remove(building);
        }
    }
}
