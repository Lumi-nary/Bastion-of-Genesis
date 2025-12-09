using UnityEngine;
using System;

/// <summary>
/// ScriptableObject containing all difficulty-related settings.
/// Allows designers to configure difficulty without code changes.
/// Create via Assets > Create > Planetfall > Difficulty Settings
/// </summary>
[CreateAssetMenu(fileName = "DifficultySettings", menuName = "Planetfall/Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Pollution Tier Thresholds")]
    [Tooltip("Pollution levels that trigger each difficulty tier")]
    public float tier2Threshold = 250f;
    public float tier3Threshold = 500f;
    public float tier4Threshold = 750f;

    [Header("Tier 1 Settings (0 - Tier2Threshold)")]
    public TierSettings tier1 = new TierSettings
    {
        spawnInterval = 90f,
        minEnemies = 5,
        maxEnemies = 10,
        statModifier = 1.0f
    };

    [Header("Tier 2 Settings (Tier2 - Tier3 Threshold)")]
    public TierSettings tier2 = new TierSettings
    {
        spawnInterval = 60f,
        minEnemies = 10,
        maxEnemies = 20,
        statModifier = 1.0f
    };

    [Header("Tier 3 Settings (Tier3 - Tier4 Threshold)")]
    public TierSettings tier3 = new TierSettings
    {
        spawnInterval = 45f,
        minEnemies = 20,
        maxEnemies = 30,
        statModifier = 1.15f  // +15% HP/damage
    };

    [Header("Tier 4 Settings (Tier4+)")]
    public TierSettings tier4 = new TierSettings
    {
        spawnInterval = 30f,
        minEnemies = 30,
        maxEnemies = 50,
        statModifier = 1.30f  // +30% HP/damage
    };

    [Header("Menu Difficulty Modifiers (Easy/Medium/Hard)")]
    [Tooltip("Multipliers applied on top of pollution tier")]
    public MenuDifficultyModifiers easyModifiers = new MenuDifficultyModifiers
    {
        enemyHPModifier = 0.75f,       // -25% HP
        enemyDamageModifier = 0.75f,   // -25% damage
        spawnRateModifier = 1.3f,      // 30% slower spawns
        waveSizeModifier = 0.75f       // 25% fewer enemies
    };

    public MenuDifficultyModifiers mediumModifiers = new MenuDifficultyModifiers
    {
        enemyHPModifier = 1.0f,
        enemyDamageModifier = 1.0f,
        spawnRateModifier = 1.0f,
        waveSizeModifier = 1.0f
    };

    public MenuDifficultyModifiers hardModifiers = new MenuDifficultyModifiers
    {
        enemyHPModifier = 1.25f,       // +25% HP
        enemyDamageModifier = 1.25f,   // +25% damage
        spawnRateModifier = 0.7f,      // 30% faster spawns
        waveSizeModifier = 1.25f       // 25% more enemies
    };

    /// <summary>
    /// Get tier settings for a specific pollution tier
    /// </summary>
    public TierSettings GetTierSettings(DifficultyTier tier)
    {
        return tier switch
        {
            DifficultyTier.Tier1 => tier1,
            DifficultyTier.Tier2 => tier2,
            DifficultyTier.Tier3 => tier3,
            DifficultyTier.Tier4 => tier4,
            _ => tier1
        };
    }

    /// <summary>
    /// Get menu difficulty modifiers for Easy/Medium/Hard setting
    /// </summary>
    public MenuDifficultyModifiers GetMenuModifiers(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => easyModifiers,
            Difficulty.Medium => mediumModifiers,
            Difficulty.Hard => hardModifiers,
            _ => mediumModifiers
        };
    }

    /// <summary>
    /// Get pollution tier based on current pollution level
    /// </summary>
    public DifficultyTier GetTierFromPollution(float pollution)
    {
        if (pollution >= tier4Threshold) return DifficultyTier.Tier4;
        if (pollution >= tier3Threshold) return DifficultyTier.Tier3;
        if (pollution >= tier2Threshold) return DifficultyTier.Tier2;
        return DifficultyTier.Tier1;
    }

    /// <summary>
    /// Get threshold for a specific tier
    /// </summary>
    public float GetTierThreshold(DifficultyTier tier)
    {
        return tier switch
        {
            DifficultyTier.Tier1 => 0f,
            DifficultyTier.Tier2 => tier2Threshold,
            DifficultyTier.Tier3 => tier3Threshold,
            DifficultyTier.Tier4 => tier4Threshold,
            _ => 0f
        };
    }
}

/// <summary>
/// Settings for a single pollution tier
/// </summary>
[Serializable]
public class TierSettings
{
    [Tooltip("Seconds between enemy waves")]
    public float spawnInterval = 60f;

    [Tooltip("Minimum enemies per wave")]
    public int minEnemies = 10;

    [Tooltip("Maximum enemies per wave")]
    public int maxEnemies = 20;

    [Tooltip("HP/Damage multiplier for enemies (1.0 = standard)")]
    public float statModifier = 1.0f;
}

/// <summary>
/// Modifiers applied based on menu difficulty selection (Easy/Medium/Hard)
/// These are multiplicative with pollution tier settings
/// </summary>
[Serializable]
public class MenuDifficultyModifiers
{
    [Tooltip("Enemy HP multiplier (0.75 = -25%, 1.25 = +25%)")]
    public float enemyHPModifier = 1.0f;

    [Tooltip("Enemy damage multiplier")]
    public float enemyDamageModifier = 1.0f;

    [Tooltip("Spawn rate multiplier (1.3 = 30% slower, 0.7 = 30% faster)")]
    public float spawnRateModifier = 1.0f;

    [Tooltip("Wave size multiplier")]
    public float waveSizeModifier = 1.0f;
}
