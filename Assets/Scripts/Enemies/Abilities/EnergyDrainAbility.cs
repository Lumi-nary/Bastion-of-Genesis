using UnityEngine;

/// <summary>
/// Elven Healer ability - Drains energy instead of dealing damage
/// Attacks energy-producing or consuming buildings to drain energy
/// </summary>
[CreateAssetMenu(fileName = "Ability_EnergyDrain", menuName = "Planetfall/Abilities/EnergyDrain")]
public class EnergyDrainAbility : SpecialAbility
{
    [Header("Energy Drain Configuration")]
    [Tooltip("Energy drained per second")]
    public float drainRate = 10f;

    [Tooltip("Replace normal damage with energy drain")]
    public bool replaceDamage = true;

    private float lastDrainTime = 0f;

    public override void OnAttack(Enemy attacker, Building target, float damage)
    {
        if (target == null || EnergyManager.Instance == null) return;

        // Drain energy continuously (called every attack tick)
        float energyDrained = drainRate * Time.deltaTime;
        EnergyManager.Instance.DrainEnergy(Mathf.RoundToInt(energyDrained));

        if (Time.time - lastDrainTime > 1f)
        {
            Debug.Log($"[EnergyDrainAbility] Draining {drainRate} energy/s from {target.BuildingData.buildingName}");
            lastDrainTime = Time.time;
        }
    }

    /// <summary>
    /// Elven Healers prioritize production buildings > turrets > generators
    /// </summary>
    public override Building GetPreferredTarget(Enemy owner, Building defaultTarget)
    {
        if (BuildingManager.Instance == null) return defaultTarget;

        // Priority 1: Production buildings (Factories, Extractors)
        var productionBuildings = BuildingManager.Instance.GetBuildingsByCategory(BuildingCategory.Production);
        if (productionBuildings.Count > 0)
        {
            return GetClosestBuilding(owner, productionBuildings);
        }

        // Priority 2: Extraction buildings
        var extractionBuildings = BuildingManager.Instance.GetBuildingsByCategory(BuildingCategory.Extraction);
        if (extractionBuildings.Count > 0)
        {
            return GetClosestBuilding(owner, extractionBuildings);
        }

        // Priority 3: Defense buildings (Turrets)
        var defenseBuildings = BuildingManager.Instance.GetBuildingsByCategory(BuildingCategory.Defense);
        if (defenseBuildings.Count > 0)
        {
            return GetClosestBuilding(owner, defenseBuildings);
        }

        // Priority 4: Energy buildings (Generators)
        var energyBuildings = BuildingManager.Instance.GetBuildingsByCategory(BuildingCategory.Energy);
        if (energyBuildings.Count > 0)
        {
            return GetClosestBuilding(owner, energyBuildings);
        }

        // Fallback to default targeting
        return defaultTarget;
    }

    private Building GetClosestBuilding(Enemy owner, System.Collections.Generic.List<Building> buildings)
    {
        Building closest = null;
        float closestDistance = float.MaxValue;

        foreach (Building building in buildings)
        {
            if (building == null || building.IsDestroyed) continue;

            float distance = Vector3.Distance(owner.transform.position, building.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = building;
            }
        }

        return closest;
    }
}
