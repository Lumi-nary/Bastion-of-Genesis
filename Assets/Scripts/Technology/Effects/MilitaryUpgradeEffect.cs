using UnityEngine;

/// <summary>
/// Improves military/combat effectiveness
/// Example: "Advanced Weapons" increases turret damage by 30%
/// </summary>
[CreateAssetMenu(fileName = "Effect_MilitaryUpgrade", menuName = "Planetfall/Technology Effects/Military Upgrade")]
public class MilitaryUpgradeEffect : TechnologyEffect
{
    [Header("Military Upgrade Configuration")]
    [Tooltip("Type of military upgrade")]
    public MilitaryUpgradeType upgradeType;

    [Tooltip("Percentage increase (e.g., 30 for +30%)")]
    public float upgradeBonus = 30f;

    [Tooltip("Specific building affected (null = all defense buildings)")]
    public BuildingData specificBuilding;

    public override void OnResearched(TechnologyData tech)
    {
        string target = specificBuilding != null ? specificBuilding.buildingName : "all defense buildings";
        Debug.Log($"[MilitaryUpgradeEffect] {upgradeType} increased by {upgradeBonus}% for {target}");
    }

    public override float GetModifier(string modifierType)
    {
        // Format: "TurretDamage_BuildingName", "TurretRange_All", etc.
        string upgradeKey = upgradeType.ToString();

        if (specificBuilding != null)
        {
            if (modifierType == $"{upgradeKey}_{specificBuilding.buildingName}")
            {
                return upgradeBonus / 100f;
            }
        }
        else
        {
            if (modifierType.StartsWith($"{upgradeKey}_"))
            {
                return upgradeBonus / 100f;
            }
        }

        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        string upgradeKey = upgradeType.ToString();

        if (specificBuilding != null)
        {
            return modifierType == $"{upgradeKey}_{specificBuilding.buildingName}";
        }
        else
        {
            return modifierType.StartsWith($"{upgradeKey}_");
        }
    }

    public override string GetEffectDescription()
    {
        string target = specificBuilding != null ? specificBuilding.buildingName : "all defenses";
        return $"+{upgradeBonus}% {upgradeType} for {target}";
    }
}

/// <summary>
/// Types of military upgrades
/// </summary>
public enum MilitaryUpgradeType
{
    TurretDamage,      // Increases turret damage
    TurretRange,       // Increases turret range
    TurretFireRate,    // Increases turret attack speed
    WallHealth,        // Increases wall HP
    BuildingArmor,     // Reduces damage taken by buildings
    ProjectileSpeed    // Increases projectile speed
}
