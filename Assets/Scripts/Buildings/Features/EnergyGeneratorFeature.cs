using UnityEngine;

/// <summary>
/// Energy generator feature - Produces energy for the base
/// Requires Extractor workers to operate
/// Examples: Steam Engine, Fusion Reactor, Mana Generator
/// </summary>
[CreateAssetMenu(fileName = "Feature_EnergyGenerator", menuName = "Planetfall/Building Features/Energy Generator")]
public class EnergyGeneratorFeature : BuildingFeature
{
    [Header("Energy Production")]
    [Tooltip("Energy produced per second when operational")]
    public int energyOutput = 5;

    [Tooltip("Requires Extractor workers to operate")]
    public bool requiresWorkers = true;

    public override int GetEnergyProduction(Building building)
    {
        // Only produce energy if operational (has required workers)
        if (requiresWorkers && !building.IsOperational)
        {
            return 0;
        }

        return energyOutput;
    }

    public override void OnOperate(Building building)
    {
        // Generators produce energy continuously when operational
        // Actual energy calculation is handled by EnergyManager
    }
}
