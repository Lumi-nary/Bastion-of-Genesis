using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elven Healer ability - Heals nearby allied enemies
/// Does NOT heal self (per ENEMY_ROSTER.md design)
/// Can self-heal only if no buildings in path (optional)
/// </summary>
[CreateAssetMenu(fileName = "Ability_Healing", menuName = "Planetfall/Abilities/Healing")]
public class HealingAbility : SpecialAbility
{
    [Header("Healing Configuration")]
    [Tooltip("HP healed per second to allies")]
    public float healingRate = 15f;

    [Tooltip("Range to heal allies (tiles)")]
    public float healingRange = 6f;

    [Tooltip("Allow self-healing if no buildings to attack")]
    public bool canSelfHeal = false;

    [Tooltip("Self-healing rate (HP/second)")]
    public float selfHealingRate = 10f;

    private float lastHealTime = 0f;

    public override void OnUpdate(Enemy owner)
    {
        // Heal allies every 0.5 seconds (tick rate)
        if (Time.time - lastHealTime < 0.5f) return;

        HealNearbyAllies(owner);

        // Self-heal if no target and allowed
        if (canSelfHeal && owner.CurrentTarget == null && owner.CurrentHealth < owner.MaxHealth)
        {
            owner.Heal(selfHealingRate * 0.5f); // 0.5s tick
        }

        lastHealTime = Time.time;
    }

    private void HealNearbyAllies(Enemy owner)
    {
        if (EnemyManager.Instance == null) return;

        List<Enemy> allEnemies = EnemyManager.Instance.GetAllActiveEnemies();
        int healed = 0;

        foreach (Enemy ally in allEnemies)
        {
            if (ally == owner || ally.IsDead) continue; // Don't heal self

            float distance = Vector3.Distance(owner.transform.position, ally.transform.position);
            if (distance <= healingRange && ally.CurrentHealth < ally.MaxHealth)
            {
                float healAmount = healingRate * 0.5f; // 0.5s tick rate
                ally.Heal(healAmount);
                healed++;
            }
        }

        if (healed > 0)
        {
            Debug.Log($"[HealingAbility] Healed {healed} allies for {healingRate * 0.5f} HP each");
        }
    }
}
