using UnityEngine;

/// <summary>
/// Reduces energy consumption or increases energy production
/// Example: "Power Optimization" reduces energy consumption by 15%
/// </summary>
[CreateAssetMenu(fileName = "Effect_EnergyEfficiency", menuName = "Planetfall/Technology Effects/Energy Efficiency")]
public class EnergyEfficiencyEffect : TechnologyEffect
{
    [Header("Energy Configuration")]
    [Tooltip("Type of energy improvement")]
    public EnergyEfficiencyType efficiencyType;

    [Tooltip("Percentage change (e.g., 15 for +/-15%)")]
    public float efficiencyBonus = 15f;

    [Tooltip("Specific building affected (null = all buildings)")]
    public BuildingData specificBuilding;

    public override void OnResearched(TechnologyData tech)
    {
        string target = specificBuilding != null ? specificBuilding.buildingName : "all buildings";
        string changeType = efficiencyType == EnergyEfficiencyType.ReducedConsumption ? "reduced" : "increased";
        Debug.Log($"[EnergyEfficiencyEffect] Energy {changeType} by {efficiencyBonus}% for {target}");
    }

    public override float GetModifier(string modifierType)
    {
        // Format: "EnergyConsumption_BuildingName", "EnergyProduction_BuildingName"
        string modifierKey = efficiencyType == EnergyEfficiencyType.ReducedConsumption
            ? "EnergyConsumption"
            : "EnergyProduction";

        if (specificBuilding != null)
        {
            if (modifierType == $"{modifierKey}_{specificBuilding.buildingName}")
            {
                return efficiencyBonus / 100f;
            }
        }
        else
        {
            if (modifierType.StartsWith($"{modifierKey}_"))
            {
                return efficiencyBonus / 100f;
            }
        }

        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        string modifierKey = efficiencyType == EnergyEfficiencyType.ReducedConsumption
            ? "EnergyConsumption"
            : "EnergyProduction";

        if (specificBuilding != null)
        {
            return modifierType == $"{modifierKey}_{specificBuilding.buildingName}";
        }
        else
        {
            return modifierType.StartsWith($"{modifierKey}_");
        }
    }

    public override string GetEffectDescription()
    {
        string target = specificBuilding != null ? specificBuilding.buildingName : "all buildings";

        if (efficiencyType == EnergyEfficiencyType.ReducedConsumption)
        {
            return $"-{efficiencyBonus}% energy consumption for {target}";
        }
        else
        {
            return $"+{efficiencyBonus}% energy production for {target}";
        }
    }
}

/// <summary>
/// Types of energy efficiency improvements
/// </summary>
public enum EnergyEfficiencyType
{
    ReducedConsumption,   // Reduces energy consumption
    IncreasedProduction   // Increases energy production
}
