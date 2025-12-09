using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Turret component for defensive buildings
/// Handles targeting, attacking enemies, manned vs automated modes
/// </summary>
[RequireComponent(typeof(Building))]
public class Turret : MonoBehaviour
{
    [Header("Turret Configuration")]
    private Building building;
    private TurretFeature turretFeature;

    [Header("Combat State")]
    private Enemy currentTarget;
    private float lastAttackTime;
    private bool isActive = false;

    [Header("Targeting")]
    [Tooltip("How often to search for new targets (seconds)")]
    [SerializeField] private float targetSearchInterval = 0.5f;
    private float targetSearchTimer = 0f;

    // Public properties
    public bool IsActive => isActive;
    public bool IsManned => building != null && building.GetTotalAssignedWorkerCount() > 0;
    public Enemy CurrentTarget => currentTarget;

    private void Awake()
    {
        building = GetComponent<Building>();
        if (building == null)
        {
            Debug.LogError("[Turret] No Building component found!");
            enabled = false;
            return;
        }

        // Get TurretFeature
        turretFeature = building.BuildingData.GetFeature<TurretFeature>();

        if (turretFeature == null)
        {
            Debug.LogWarning($"[Turret] {building.BuildingData.buildingName} does not have TurretFeature!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Check if turret can operate
        if (!CanOperate())
        {
            isActive = false;
            currentTarget = null;
            return;
        }

        isActive = true;

        // Update target search timer
        targetSearchTimer += Time.deltaTime;
        if (targetSearchTimer >= targetSearchInterval)
        {
            targetSearchTimer = 0f;
            FindTarget();
        }

        // Attack current target if in range
        if (currentTarget != null)
        {
            if (IsTargetInRange())
            {
                TryAttack();
            }
            else
            {
                // Target out of range, find new one
                currentTarget = null;
            }
        }
    }

    /// <summary>
    /// Check if turret can operate (energy, manning requirements)
    /// </summary>
    private bool CanOperate()
    {
        if (building == null || turretFeature == null) return false;

        // Check if turret requires manning
        if (turretFeature.requiresManning)
        {
            // Must have workers assigned to function
            if (!IsManned)
            {
                return false;
            }
        }

        // Check energy requirements (unless turret can function without energy)
        if (!turretFeature.CanFunctionWithoutEnergy())
        {
            // Check if we have power
            if (EnergyManager.Instance != null && !EnergyManager.Instance.HasEnergy)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Find the nearest enemy within range
    /// </summary>
    private void FindTarget()
    {
        if (EnemyManager.Instance == null) return;

        Enemy nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        // Search all active enemies
        foreach (Enemy enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // Check if enemy is in range
            if (distance <= turretFeature.attackRange && distance < nearestDistance)
            {
                nearestEnemy = enemy;
                nearestDistance = distance;
            }
        }

        currentTarget = nearestEnemy;
    }

    /// <summary>
    /// Check if current target is in range
    /// </summary>
    private bool IsTargetInRange()
    {
        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        return distance <= turretFeature.attackRange;
    }

    /// <summary>
    /// Try to attack current target (respecting attack speed cooldown)
    /// </summary>
    private void TryAttack()
    {
        if (currentTarget == null) return;

        // Check attack cooldown
        if (Time.time - lastAttackTime < turretFeature.attackSpeed) return;

        // Perform attack
        Attack(currentTarget);

        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Attack an enemy
    /// </summary>
    private void Attack(Enemy enemy)
    {
        if (enemy == null || enemy.IsDead) return;

        float damage = turretFeature.damage;

        // Apply damage modifiers based on mode
        if (IsManned)
        {
            // Manned turrets could have damage bonus (future enhancement)
            damage = turretFeature.damage;
        }
        else
        {
            // Automated turrets
            damage = turretFeature.damage;
        }

        // Deal damage to primary target
        enemy.TakeDamage(damage);

        Debug.Log($"[Turret] {building.BuildingData.buildingName} dealt {damage} damage to {enemy.Data.GetDisplayName()}");

        // Check for splash damage (Explosive Ammunition tech)
        if (HasExplosiveAmmo())
        {
            ApplySplashDamage(enemy.transform.position, damage);
        }
    }

    /// <summary>
    /// Check if Explosive Ammunition tech is researched
    /// </summary>
    private bool HasExplosiveAmmo()
    {
        if (ResearchManager.Instance == null) return false;

        // Look for Explosive Ammunition tech in researched techs
        foreach (TechnologyData tech in ResearchManager.Instance.ResearchedTechs)
        {
            if (tech.techName == "Explosive Ammunition" ||
                tech.techName.Contains("Explosive") && tech.techName.Contains("Ammunition"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Apply splash damage to enemies in radius (2 tiles)
    /// </summary>
    private void ApplySplashDamage(Vector3 impactPosition, float baseDamage)
    {
        if (EnemyManager.Instance == null) return;

        float splashRadius = 2f; // 2 tile radius as per design
        float splashDamage = baseDamage * 0.5f; // 50% damage to splash targets

        foreach (Enemy enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (enemy == currentTarget) continue; // Skip primary target (already damaged)

            float distance = Vector3.Distance(impactPosition, enemy.transform.position);

            if (distance <= splashRadius)
            {
                enemy.TakeDamage(splashDamage);
                Debug.Log($"[Turret] Splash damage: {splashDamage} to {enemy.Data.GetDisplayName()}");
            }
        }
    }

    /// <summary>
    /// Get turret operation mode string
    /// </summary>
    public string GetModeString()
    {
        if (!isActive) return "OFFLINE";

        if (turretFeature.requiresManning)
        {
            return IsManned ? "MANNED" : "UNMANNED (OFFLINE)";
        }
        else if (turretFeature.canBeManned)
        {
            return IsManned ? "MANNED" : "AUTOMATED";
        }
        else
        {
            return "AUTOMATED";
        }
    }

    /// <summary>
    /// Get energy consumption for this turret
    /// </summary>
    public int GetEnergyConsumption()
    {
        if (IsManned)
        {
            return turretFeature.mannedEnergyCost;
        }
        else
        {
            return turretFeature.automatedEnergyCost;
        }
    }

    /// <summary>
    /// Apply slow effect from Water Mage enemies
    /// Reduces attack speed temporarily
    /// </summary>
    public void ApplySlow(float slowPercent, float duration)
    {
        // TODO: Implement slow debuff system
        // For now, just log the effect
        Debug.Log($"[Turret] {building.BuildingData.buildingName} slowed by {slowPercent * 100}% for {duration}s");
    }

    /// <summary>
    /// Apply stun effect from Elven Stunner enemies
    /// Disables turret temporarily
    /// </summary>
    public void ApplyStun(float duration)
    {
        // TODO: Implement stun debuff system
        // For now, just log the effect
        Debug.Log($"[Turret] {building.BuildingData.buildingName} stunned for {duration}s");
    }

    /// <summary>
    /// Visualize turret range in editor/debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (turretFeature != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, turretFeature.attackRange);

            if (HasExplosiveAmmo())
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(transform.position, turretFeature.attackRange + 2f);
            }
        }
    }
}
