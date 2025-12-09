using UnityEngine;

/// <summary>
/// Increases research speed
/// Example: "Advanced Laboratory" increases research speed by 25%
/// </summary>
[CreateAssetMenu(fileName = "Effect_ResearchSpeed", menuName = "Planetfall/Technology Effects/Research Speed")]
public class ResearchSpeedEffect : TechnologyEffect
{
    [Header("Research Speed Configuration")]
    [Tooltip("Percentage increase in research speed (e.g., 25 for +25% faster)")]
    public float researchSpeedBonus = 25f;

    [Tooltip("Specific technology category affected (null = all research)")]
    public TechCategory? affectedCategory = null;

    public override void OnResearched(TechnologyData tech)
    {
        string target = affectedCategory.HasValue ? affectedCategory.Value.ToString() : "all";
        Debug.Log($"[ResearchSpeedEffect] Research speed increased by {researchSpeedBonus}% for {target} technologies");
    }

    public override float GetModifier(string modifierType)
    {
        // Format: "ResearchSpeed_All" or "ResearchSpeed_Economy"
        if (affectedCategory.HasValue)
        {
            if (modifierType == $"ResearchSpeed_{affectedCategory.Value}")
            {
                return researchSpeedBonus / 100f;
            }
        }
        else
        {
            if (modifierType.StartsWith("ResearchSpeed_"))
            {
                return researchSpeedBonus / 100f;
            }
        }

        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        if (affectedCategory.HasValue)
        {
            return modifierType == $"ResearchSpeed_{affectedCategory.Value}";
        }
        else
        {
            return modifierType.StartsWith("ResearchSpeed_");
        }
    }

    public override string GetEffectDescription()
    {
        string target = affectedCategory.HasValue ? $"{affectedCategory.Value}" : "all";
        return $"+{researchSpeedBonus}% research speed for {target} technologies";
    }
}
