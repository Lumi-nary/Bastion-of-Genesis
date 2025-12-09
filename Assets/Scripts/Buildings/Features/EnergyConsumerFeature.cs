using UnityEngine;

/// <summary>
/// Energy consumer feature - Consumes energy to operate
/// Most buildings consume energy except generators and manned turrets
/// </summary>
[CreateAssetMenu(fileName = "Feature_EnergyConsumer", menuName = "Planetfall/Building Features/Energy Consumer")]
public class EnergyConsumerFeature : BuildingFeature
{
    [Header("Energy Consumption")]
    [Tooltip("Energy consumed per second")]
    public int energyConsumption = 2;

    [Tooltip("Can this building function without energy?")]
    public bool functionsWithoutEnergy = false;

    public override int GetEnergyConsumption(Building building)
    {
        return energyConsumption;
    }

    public override bool CanFunctionWithoutEnergy()
    {
        return functionsWithoutEnergy;
    }
}
