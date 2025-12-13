using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines how a technology is unlocked
/// </summary>
public enum TechUnlockMethod
{
    Researchable,    // Can be researched by player (costs resources + time)
    MissionReward    // Unlocked as mission reward (still needs research to activate)
}

/// <summary>
/// Category for organizing technologies in UI
/// </summary>
public enum TechCategory
{
    Economy,      // Resource production, worker efficiency
    Military,     // Combat upgrades, defenses
    Expansion,    // Pollution, territory, exploration
    Automation,   // Worker automation, efficiency
    Research      // Research speed, tech unlocks
}

/// <summary>
/// ScriptableObject representing a single technology in the tech tree
/// Data-driven design - add technologies by creating assets, no code changes needed
/// </summary>
[CreateAssetMenu(fileName = "Tech_", menuName = "Planetfall/Technology")]
public class TechnologyData : ScriptableObject
{
    [Header("Technology Info")]
    [Tooltip("Display name of the technology")]
    public string techName;

    [Tooltip("Description of what this technology does")]
    [TextArea(3, 6)]
    public string description;

    [Tooltip("Icon for UI display")]
    public Sprite icon;

    [Tooltip("Technology tier (1-5, higher = more advanced)")]
    [Range(1, 5)]
    public int tier = 1;

    [Header("Unlock Method")]
    [Tooltip("How this technology becomes available")]
    public TechUnlockMethod unlockMethod = TechUnlockMethod.Researchable;

    [Header("Research Requirements")]
    [Tooltip("Resources consumed to research this technology")]
    public List<ResourceCost> researchCost = new List<ResourceCost>();

    [Tooltip("Time (in seconds) required to complete research")]
    public float researchTime = 60f;

    [Tooltip("Technologies that must be researched before this one")]
    public List<TechnologyData> requiredTechs = new List<TechnologyData>();

    [Header("Technology Effects (Modular System)")]
    [Tooltip("List of effects this technology provides")]
    public List<TechnologyEffect> effects = new List<TechnologyEffect>();

    [Tooltip("Category for UI organization")]
    public TechCategory category;

    [Header("Debug Info")]
    [Tooltip("For tracking research status at runtime (read-only)")]
    [SerializeField] private bool isResearched = false;

    [Tooltip("For tracking if tech is available to research (read-only)")]
    [SerializeField] private bool isAvailable = false;

    // Public properties for runtime access
    public bool IsResearched
    {
        get => isResearched;
        set => isResearched = value;
    }

    public bool IsAvailable
    {
        get => isAvailable;
        set => isAvailable = value;
    }

    /// <summary>
    /// Check if all prerequisite technologies have been researched
    /// </summary>
    public bool ArePrerequisitesMet(HashSet<TechnologyData> researchedTechs)
    {
        foreach (TechnologyData requiredTech in requiredTechs)
        {
            if (requiredTech == null) continue;

            if (!researchedTechs.Contains(requiredTech))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Get display string for research cost
    /// </summary>
    public string GetCostString()
    {
        if (researchCost.Count == 0) return "Free";

        List<string> costs = new List<string>();
        foreach (ResourceCost cost in researchCost)
        {
            if (cost.resourceType != null)
            {
                costs.Add($"{cost.amount} {cost.resourceType.ResourceName}");
            }
        }
        return string.Join(", ", costs);
    }

    /// <summary>
    /// Get formatted research time string
    /// </summary>
    public string GetTimeString()
    {
        if (researchTime < 60f)
        {
            return $"{researchTime:F0}s";
        }
        else
        {
            int minutes = Mathf.FloorToInt(researchTime / 60f);
            int seconds = Mathf.FloorToInt(researchTime % 60f);
            return seconds > 0 ? $"{minutes}m {seconds}s" : $"{minutes}m";
        }
    }

    /// <summary>
    /// Reset research status (for new game or chapter reset)
    /// </summary>
    public void ResetResearchStatus()
    {
        isResearched = false;
        isAvailable = (unlockMethod == TechUnlockMethod.Researchable && requiredTechs.Count == 0);
    }

    /// <summary>
    /// Check if this technology has a specific effect type
    /// </summary>
    public bool HasEffect<T>() where T : TechnologyEffect
    {
        foreach (var effect in effects)
        {
            if (effect is T) return true;
        }
        return false;
    }

    /// <summary>
    /// Get effect of specific type (returns null if not found)
    /// </summary>
    public T GetEffect<T>() where T : TechnologyEffect
    {
        foreach (var effect in effects)
        {
            if (effect is T) return effect as T;
        }
        return null;
    }

    /// <summary>
    /// Get all effects of specific type
    /// </summary>
    public List<T> GetEffects<T>() where T : TechnologyEffect
    {
        List<T> result = new List<T>();
        foreach (var effect in effects)
        {
            if (effect is T) result.Add(effect as T);
        }
        return result;
    }

    /// <summary>
    /// Get combined description of all effects
    /// </summary>
    public string GetEffectsDescription()
    {
        if (effects.Count == 0) return "No effects";

        List<string> effectDescriptions = new List<string>();
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effectDescriptions.Add(effect.GetEffectDescription());
            }
        }

        return string.Join("\n", effectDescriptions);
    }
}
