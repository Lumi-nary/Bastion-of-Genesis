using UnityEngine;

/// <summary>
/// Increases the placement cap of a specific building type
/// Example: "Expanded Warehouses" allows building +2 more Resource Warehouses
/// Uses modifier key "BuildingCap_{buildingName}" for ResearchManager queries
/// </summary>
[CreateAssetMenu(fileName = "Effect_BuildingCapIncrease", menuName = "Planetfall/Technology Effects/Building Cap Increase")]
public class BuildingCapIncreaseEffect : TechnologyEffect
{
    [Header("Cap Configuration")]
    [Tooltip("Building type whose cap is increased")]
    public BuildingData targetBuilding;

    [Tooltip("Amount to increase the placement cap by")]
    public int capIncrease = 2;

    private string ModifierKey => targetBuilding != null ? $"BuildingCap_{targetBuilding.buildingName}" : "";

    public override void OnResearched(TechnologyData tech)
    {
        if (targetBuilding != null)
        {
            Debug.Log($"[BuildingCapIncreaseEffect] {targetBuilding.buildingName} placement cap increased by {capIncrease}");
        }
    }

    public override void OnRemoved(TechnologyData tech)
    {
        if (targetBuilding != null)
        {
            Debug.Log($"[BuildingCapIncreaseEffect] {targetBuilding.buildingName} placement cap bonus of {capIncrease} removed");
        }
    }

    public override float GetModifier(string modifierType)
    {
        if (modifierType == ModifierKey)
        {
            return capIncrease;
        }
        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        return modifierType == ModifierKey;
    }

    public override string GetEffectDescription()
    {
        if (targetBuilding != null)
        {
            return $"+{capIncrease} {targetBuilding.buildingName} placement cap";
        }
        return $"+{capIncrease} building placement cap";
    }
}
