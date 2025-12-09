using UnityEngine;

/// <summary>
/// Base class for all building features
/// Features are modular ScriptableObjects that can be assigned to any building
/// This allows mixing and matching features without code changes
/// Examples: Energy generation, combat, resource extraction, upgrades, pollution
/// </summary>
public abstract class BuildingFeature : ScriptableObject
{
    [Header("Feature Identity")]
    [Tooltip("Display name of this feature")]
    public string featureName;

    [Tooltip("Description of what this feature does")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Icon for UI display")]
    public Sprite icon;

    /// <summary>
    /// Called when building is constructed/placed
    /// </summary>
    public virtual void OnBuilt(Building building) { }

    /// <summary>
    /// Called every frame while building exists
    /// Used for resource generation, pollution, passive effects, etc.
    /// </summary>
    public virtual void OnUpdate(Building building) { }

    /// <summary>
    /// Called when building is operational (has required workers)
    /// Used for production, energy generation, etc.
    /// </summary>
    public virtual void OnOperate(Building building) { }

    /// <summary>
    /// Called when building takes damage
    /// Can modify damage or trigger effects
    /// </summary>
    public virtual void OnDamaged(Building building, float damage) { }

    /// <summary>
    /// Called when building is destroyed
    /// </summary>
    public virtual void OnDestroyed(Building building) { }

    /// <summary>
    /// Called when building is upgraded
    /// </summary>
    public virtual void OnUpgraded(Building building, BuildingData newData) { }

    /// <summary>
    /// Get energy production from this feature
    /// </summary>
    public virtual int GetEnergyProduction(Building building)
    {
        return 0;
    }

    /// <summary>
    /// Get energy consumption from this feature
    /// </summary>
    public virtual int GetEnergyConsumption(Building building)
    {
        return 0;
    }

    /// <summary>
    /// Check if this feature can function without energy
    /// </summary>
    public virtual bool CanFunctionWithoutEnergy()
    {
        return false;
    }
}
