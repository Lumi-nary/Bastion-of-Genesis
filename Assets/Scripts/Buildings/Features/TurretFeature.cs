using UnityEngine;

/// <summary>
/// Turret/defense feature - Enables combat capabilities
/// Turrets can be manned (with workers) or automated
/// Manned vs automated modes have different energy costs
/// </summary>
[CreateAssetMenu(fileName = "Feature_Turret", menuName = "Planetfall/Building Features/Turret")]
public class TurretFeature : BuildingFeature
{
    [Header("Combat Configuration")]
    [Tooltip("Damage dealt per attack")]
    public float damage = 25f;

    [Tooltip("Attack range in tiles")]
    public float attackRange = 8f;

    [Tooltip("Attack speed (time between attacks in seconds)")]
    public float attackSpeed = 1f;

    [Header("Turret Mode")]
    [Tooltip("Can be manned by workers")]
    public bool canBeManned = true;

    [Tooltip("Requires workers to function (cannot auto-attack)")]
    public bool requiresManning = false;

    [Tooltip("Energy cost when manned (with workers)")]
    public int mannedEnergyCost = 1;

    [Tooltip("Energy cost when automated (no workers)")]
    public int automatedEnergyCost = 3;

    public override int GetEnergyConsumption(Building building)
    {
        // Manned turrets use less energy than automated
        bool isManned = building.GetTotalAssignedWorkerCount() > 0;

        if (requiresManning && !isManned)
        {
            return 0; // Not functioning, no energy cost
        }

        return isManned ? mannedEnergyCost : automatedEnergyCost;
    }

    public override bool CanFunctionWithoutEnergy()
    {
        // Manned turrets can function without energy (manual operation)
        return canBeManned;
    }

    public override void OnBuilt(Building building)
    {
        // Attach Turret component if not already present
        Turret turretComponent = building.GetComponent<Turret>();
        if (turretComponent == null)
        {
            turretComponent = building.gameObject.AddComponent<Turret>();
        }

        // Turret component handles actual combat logic
    }
}
