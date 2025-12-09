using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Upgradeable feature - Allows building to be upgraded to better version
/// Example: Iron Extractor → Advanced Extractor
/// </summary>
[CreateAssetMenu(fileName = "Feature_Upgradeable", menuName = "Planetfall/Building Features/Upgradeable")]
public class UpgradeableFeature : BuildingFeature
{
    [Header("Upgrade Configuration")]
    [Tooltip("Building this upgrades to")]
    public BuildingData upgradesTo;

    [Tooltip("Technology required to unlock upgrade")]
    public TechnologyData requiredTech;

    [Tooltip("Resources required for upgrade")]
    public List<ResourceCost> upgradeCost = new List<ResourceCost>();

    /// <summary>
    /// Check if upgrade is available
    /// </summary>
    public bool CanUpgrade(Building building)
    {
        // Check if tech is researched
        if (requiredTech != null && ResearchManager.Instance != null)
        {
            if (!ResearchManager.Instance.IsTechResearched(requiredTech))
            {
                return false;
            }
        }

        // Check if we have resources
        if (ResourceManager.Instance != null)
        {
            foreach (var cost in upgradeCost)
            {
                if (ResourceManager.Instance.GetResourceAmount(cost.resourceType) < cost.amount)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Perform upgrade
    /// </summary>
    public bool PerformUpgrade(Building building)
    {
        if (!CanUpgrade(building)) return false;

        // Consume resources
        if (ResourceManager.Instance != null)
        {
            foreach (var cost in upgradeCost)
            {
                ResourceManager.Instance.RemoveResource(cost.resourceType, cost.amount);
            }
        }

        // TODO: Replace building with upgraded version
        // This would involve spawning new building at same location
        // and transferring workers, health percentage, etc.

        Debug.Log($"[Upgradeable] {building.BuildingData.buildingName} upgraded to {upgradesTo.buildingName}");

        return true;
    }
}
