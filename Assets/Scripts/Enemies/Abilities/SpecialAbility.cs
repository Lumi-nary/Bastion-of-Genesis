using UnityEngine;

/// <summary>
/// Base class for all enemy special abilities
/// Abilities are modular ScriptableObjects that can be assigned to any enemy
/// This allows mixing and matching abilities without code changes
/// </summary>
public abstract class SpecialAbility : ScriptableObject
{
    [Header("Ability Identity")]
    [Tooltip("Display name of this ability")]
    public string abilityName;

    [Tooltip("Description of what this ability does")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Icon for UI display")]
    public Sprite icon;

    /// <summary>
    /// Called when enemy is initialized
    /// </summary>
    public virtual void OnInitialize(Enemy owner) { }

    /// <summary>
    /// Called every frame while enemy is alive
    /// Used for passive effects, healing, etc.
    /// </summary>
    public virtual void OnUpdate(Enemy owner) { }

    /// <summary>
    /// Called when enemy attacks a building
    /// Used for special attack effects (burning, curse, stun, etc.)
    /// </summary>
    public virtual void OnAttack(Enemy attacker, Building target, float damage) { }

    /// <summary>
    /// Called when enemy takes damage from turrets
    /// Return true to block/dodge the damage
    /// </summary>
    public virtual bool OnTakeDamage(Enemy owner, ref float damage)
    {
        return false; // false = damage not blocked
    }

    /// <summary>
    /// Called when enemy is searching for a target
    /// Override to customize targeting behavior (e.g., Elven Healer prioritizes production buildings)
    /// Return null to use default targeting
    /// </summary>
    public virtual Building GetPreferredTarget(Enemy owner, Building defaultTarget)
    {
        return defaultTarget; // Use default targeting
    }

    /// <summary>
    /// Called when enemy movement is calculated
    /// Override to customize movement (e.g., flying, tunneling)
    /// </summary>
    public virtual Vector3 GetMovementDirection(Enemy owner, Vector3 defaultDirection)
    {
        return defaultDirection; // Use default movement
    }

    /// <summary>
    /// Called when enemy dies
    /// </summary>
    public virtual void OnDeath(Enemy owner) { }
}
