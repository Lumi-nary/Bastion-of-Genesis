using UnityEngine;

/// <summary>
/// Reduces pollution generation
/// Example: "Clean Technology" reduces pollution by 20%
/// </summary>
[CreateAssetMenu(fileName = "Effect_PollutionReduction", menuName = "Planetfall/Technology Effects/Pollution Reduction")]
public class PollutionReductionEffect : TechnologyEffect
{
    [Header("Pollution Configuration")]
    [Tooltip("Percentage reduction in pollution (e.g., 20 for -20%)")]
    public float pollutionReduction = 20f;

    [Tooltip("Specific building affected (null = all buildings)")]
    public BuildingData specificBuilding;

    public override void OnResearched(TechnologyData tech)
    {
        string target = specificBuilding != null ? specificBuilding.buildingName : "all buildings";
        Debug.Log($"[PollutionReductionEffect] Pollution reduced by {pollutionReduction}% for {target}");
    }

    public override float GetModifier(string modifierType)
    {
        // Format: "PollutionReduction_BuildingName" or "PollutionReduction_All"
        if (specificBuilding != null)
        {
            if (modifierType == $"PollutionReduction_{specificBuilding.buildingName}")
            {
                return pollutionReduction / 100f;
            }
        }
        else
        {
            if (modifierType.StartsWith("PollutionReduction_"))
            {
                return pollutionReduction / 100f;
            }
        }

        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        if (specificBuilding != null)
        {
            return modifierType == $"PollutionReduction_{specificBuilding.buildingName}";
        }
        else
        {
            return modifierType.StartsWith("PollutionReduction_");
        }
    }

    public override string GetEffectDescription()
    {
        string target = specificBuilding != null ? specificBuilding.buildingName : "all buildings";
        return $"-{pollutionReduction}% pollution for {target}";
    }
}
