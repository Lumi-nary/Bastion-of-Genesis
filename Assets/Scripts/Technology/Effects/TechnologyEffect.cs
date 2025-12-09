using UnityEngine;

/// <summary>
/// Base class for all technology effects
/// Modular system - add new effects by creating new ScriptableObjects
/// Each effect defines what happens when the technology is researched
/// </summary>
public abstract class TechnologyEffect : ScriptableObject
{
    [Header("Effect Info")]
    [Tooltip("Display name of this effect")]
    public string effectName;

    [Tooltip("Description of what this effect does")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Icon for UI display (optional)")]
    public Sprite icon;

    /// <summary>
    /// Called when the technology is researched
    /// Apply permanent effects here
    /// </summary>
    public virtual void OnResearched(TechnologyData tech)
    {
        Debug.Log($"[TechnologyEffect] {effectName} researched");
    }

    /// <summary>
    /// Called when the effect should be removed (tech tree reset, debugging)
    /// </summary>
    public virtual void OnRemoved(TechnologyData tech)
    {
        Debug.Log($"[TechnologyEffect] {effectName} removed");
    }

    /// <summary>
    /// Get a specific modifier value (for querying effects)
    /// Examples: "ResourceProduction_Iron", "WorkerEfficiency_Builder"
    /// </summary>
    public virtual float GetModifier(string modifierType)
    {
        return 0f;
    }

    /// <summary>
    /// Check if this effect provides a specific modifier
    /// </summary>
    public virtual bool ProvidesModifier(string modifierType)
    {
        return false;
    }

    /// <summary>
    /// Get user-friendly description for UI
    /// </summary>
    public virtual string GetEffectDescription()
    {
        return description;
    }
}
