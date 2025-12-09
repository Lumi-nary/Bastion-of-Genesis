using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unlocks new building types when researched
/// Example: "Advanced Mining" unlocks Advanced Iron Extractor
/// </summary>
[CreateAssetMenu(fileName = "Effect_UnlockBuilding", menuName = "Planetfall/Technology Effects/Unlock Building")]
public class UnlockBuildingEffect : TechnologyEffect
{
    [Header("Unlock Configuration")]
    [Tooltip("Buildings unlocked by this effect")]
    public List<BuildingData> unlockedBuildings = new List<BuildingData>();

    public override void OnResearched(TechnologyData tech)
    {
        // Buildings are unlocked by BuildingManager checking researched techs
        // No active code needed here - the unlock is data-driven

        foreach (var building in unlockedBuildings)
        {
            if (building != null)
            {
                Debug.Log($"[UnlockBuildingEffect] Unlocked building: {building.buildingName}");
            }
        }
    }

    public override string GetEffectDescription()
    {
        if (unlockedBuildings.Count == 0) return "No buildings unlocked";

        List<string> buildingNames = new List<string>();
        foreach (var building in unlockedBuildings)
        {
            if (building != null)
            {
                buildingNames.Add(building.buildingName);
            }
        }

        return $"Unlocks: {string.Join(", ", buildingNames)}";
    }
}
