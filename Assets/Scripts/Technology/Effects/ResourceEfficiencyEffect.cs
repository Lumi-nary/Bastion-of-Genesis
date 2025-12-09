using UnityEngine;

/// <summary>
/// Increases resource production efficiency
/// Example: "Improved Mining" increases Iron production by 25%
/// </summary>
[CreateAssetMenu(fileName = "Effect_ResourceEfficiency", menuName = "Planetfall/Technology Effects/Resource Efficiency")]
public class ResourceEfficiencyEffect : TechnologyEffect
{
    [Header("Efficiency Configuration")]
    [Tooltip("Resource type affected (null = all resources)")]
    public ResourceType affectedResource;

    [Tooltip("Percentage increase in production (e.g., 25 for +25%)")]
    public float efficiencyBonus = 25f;

    public override void OnResearched(TechnologyData tech)
    {
        if (affectedResource != null)
        {
            Debug.Log($"[ResourceEfficiencyEffect] {affectedResource.ResourceName} production increased by {efficiencyBonus}%");
        }
        else
        {
            Debug.Log($"[ResourceEfficiencyEffect] All resource production increased by {efficiencyBonus}%");
        }
    }

    public override float GetModifier(string modifierType)
    {
        // Format: "ResourceProduction_ResourceName" or "ResourceProduction_All"
        if (affectedResource != null)
        {
            if (modifierType == $"ResourceProduction_{affectedResource.ResourceName}")
            {
                return efficiencyBonus / 100f;
            }
        }
        else
        {
            if (modifierType.StartsWith("ResourceProduction_"))
            {
                return efficiencyBonus / 100f;
            }
        }

        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        if (affectedResource != null)
        {
            return modifierType == $"ResourceProduction_{affectedResource.ResourceName}";
        }
        else
        {
            return modifierType.StartsWith("ResourceProduction_");
        }
    }

    public override string GetEffectDescription()
    {
        if (affectedResource != null)
        {
            return $"+{efficiencyBonus}% {affectedResource.ResourceName} production";
        }
        else
        {
            return $"+{efficiencyBonus}% all resource production";
        }
    }
}
