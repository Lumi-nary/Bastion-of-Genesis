using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy race/faction (from ENEMY_ROSTER.md)
/// </summary>
public enum EnemyRace
{
    Human,   // 7 types - varied units with physical and elemental magic
    Dwarf,   // 2 types - high HP, tunneling ability
    Demon,   // 2 types - AOE dark magic, curse ability
    Elf      // 2 types - energy drain, healing, stunning
}

/// <summary>
/// Damage type classification (from ENEMY_ROSTER.md)
/// </summary>
public enum DamageType
{
    Physical,    // Blocked by walls, countered by high HP
    Fire,        // Burst + burning DoT
    Water,       // Slows turret attack speed
    Earth,       // Low DPS but high armor
    Air,         // Fast movement, can dodge
    Dark,        // AOE damage, curse ability
    Light        // Stuns defenses, energy drain
}

/// <summary>
/// Movement type classification (from ENEMY_ROSTER.md)
/// </summary>
public enum MovementType
{
    Ground,      // Blocked by all walls
    Flying,      // Bypasses all walls completely
    Tunneling    // Bypasses Iron Walls only, blocked by Steel/Null-Magic
}

/// <summary>
/// ScriptableObject representing a single enemy type
/// Based on complete specifications from ENEMY_ROSTER.md
/// REFACTORED: Now uses modular SpecialAbility system for extensibility
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "Planetfall/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Identity")]
    [Tooltip("Display name of the enemy")]
    public string enemyName;

    [Tooltip("Description of enemy type and behavior")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Icon for UI display")]
    public Sprite icon;

    [Tooltip("Enemy prefab (must have Enemy component)")]
    public GameObject prefab;

    [Header("Race & Classification")]
    [Tooltip("Enemy race (Human, Dwarf, Demon, Elf)")]
    public EnemyRace race;

    [Tooltip("Damage type this enemy deals")]
    public DamageType damageType;

    [Tooltip("Movement type (Ground, Flying, Tunneling)")]
    public MovementType movementType;

    [Header("Base Stats (Scaled by Difficulty + Pollution)")]
    [Tooltip("Base health points")]
    public int baseHP = 100;

    [Tooltip("Base damage per attack")]
    public int baseDamage = 10;

    [Tooltip("Movement speed in tiles/second")]
    public float moveSpeed = 2f;

    [Tooltip("Attack range in tiles (0.5 = melee)")]
    public float attackRange = 0.5f;

    [Tooltip("Attacks per second (or drain rate for Elven Healer)")]
    public float attackSpeed = 1f;

    [Tooltip("Flat damage reduction from armor")]
    public int armor = 0;

    [Header("Special Abilities (Modular System)")]
    [Tooltip("List of special abilities this enemy has")]
    public List<SpecialAbility> specialAbilities = new List<SpecialAbility>();

    [Header("Spawn Conditions")]
    [Tooltip("Chapters where this enemy can spawn (e.g., 1, 2, 5)")]
    public List<int> allowedChapters = new List<int>();

    /// <summary>
    /// Get effective HP (base HP scaled by difficulty and pollution)
    /// </summary>
    public int GetEffectiveHP(float difficultyMultiplier, float pollutionMultiplier)
    {
        return Mathf.RoundToInt(baseHP * difficultyMultiplier * pollutionMultiplier);
    }

    /// <summary>
    /// Get effective damage (base damage scaled by difficulty and pollution)
    /// </summary>
    public int GetEffectiveDamage(float difficultyMultiplier, float pollutionMultiplier)
    {
        return Mathf.RoundToInt(baseDamage * difficultyMultiplier * pollutionMultiplier);
    }

    /// <summary>
    /// Check if this enemy can spawn in a specific chapter
    /// </summary>
    public bool CanSpawnInChapter(int chapterNumber)
    {
        return allowedChapters.Contains(chapterNumber);
    }

    /// <summary>
    /// Get display name (for UI)
    /// </summary>
    public string GetDisplayName()
    {
        return enemyName;
    }

    /// <summary>
    /// Get race name as string
    /// </summary>
    public string GetRaceName()
    {
        return race.ToString();
    }

    /// <summary>
    /// Check if this enemy has a specific ability type
    /// </summary>
    public bool HasAbility<T>() where T : SpecialAbility
    {
        foreach (var ability in specialAbilities)
        {
            if (ability is T) return true;
        }
        return false;
    }

    /// <summary>
    /// Get ability of specific type (returns null if not found)
    /// </summary>
    public T GetAbility<T>() where T : SpecialAbility
    {
        foreach (var ability in specialAbilities)
        {
            if (ability is T) return ability as T;
        }
        return null;
    }
}
