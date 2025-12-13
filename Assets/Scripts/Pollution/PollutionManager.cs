using UnityEngine;
using System;

/// <summary>
/// Manages pollution levels and difficulty scaling.
/// Combines two systems:
/// 1. Menu Difficulty (Easy/Medium/Hard) - static per chapter
/// 2. Pollution Tiers (1-4) - dynamic based on player actions
/// Final difficulty = Pollution Tier × Menu Difficulty (multiplicative)
/// </summary>
public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Pollution Settings")]
    [SerializeField] private float maxPollution = 1000f;
    [SerializeField] private float pollutionDecayRate = 0.5f;
    [SerializeField] private bool enableNaturalDecay = true;

    [Header("Difficulty Configuration")]
    [Tooltip("ScriptableObject containing all difficulty settings")]
    [SerializeField] private DifficultySettings difficultySettings;

    [Header("Integration Radius (Buildable Zone)")]
    [Tooltip("Starting integration radius - zone will never shrink below this")]
    [SerializeField] private float starterIntegrationRadius = 10f;
    [Tooltip("Additional radius gained at max pollution")]
    [SerializeField] private float maxAdditionalRadius = 40f;

    // Runtime state
    private float currentPollution = 0f;
    private float lastIntegrationRadius = -1f;
    private float peakIntegrationRadius = 0f; // Never shrinks - only grows
    private DifficultyTier currentTier = DifficultyTier.Tier1;
    private Difficulty menuDifficulty = Difficulty.Medium;

    // Events
    public event Action<float, float> OnPollutionChanged; // current, max
    public event Action<DifficultyTier> OnDifficultyTierChanged;

    // Properties
    public float CurrentPollution => currentPollution;
    public float MaxPollution => maxPollution;
    public float PollutionPercentage => (currentPollution / maxPollution) * 100f;
    public DifficultyTier CurrentTier => currentTier;
    public Difficulty MenuDifficulty => menuDifficulty;
    public float IntegrationRadius => GetIntegrationRadius();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Load menu difficulty from SaveManager if available
        if (SaveManager.Instance != null)
        {
            menuDifficulty = SaveManager.Instance.pendingDifficulty;
            Debug.Log($"[PollutionManager] Loaded menu difficulty: {menuDifficulty}");
        }
    }

    private void Update()
    {
        if (enableNaturalDecay && currentPollution > 0)
        {
            RemovePollution(pollutionDecayRate * Time.deltaTime);
        }
    }

    /// <summary>
    /// Set the menu difficulty (Easy/Medium/Hard) - called when loading a save
    /// </summary>
    public void SetMenuDifficulty(Difficulty difficulty)
    {
        menuDifficulty = difficulty;
        Debug.Log($"[PollutionManager] Menu difficulty set to: {menuDifficulty}");
    }

    public void AddPollution(float amount)
    {
        if (amount <= 0) return;

        currentPollution = Mathf.Clamp(currentPollution + amount, 0, maxPollution);
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);

        CheckDifficultyTier();
        UpdateTileStates();
    }

    public void RemovePollution(float amount)
    {
        if (amount <= 0) return;

        currentPollution = Mathf.Max(0, currentPollution - amount);
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);

        CheckDifficultyTier();
        UpdateTileStates();
    }

    public void SetPollution(float amount)
    {
        currentPollution = Mathf.Clamp(amount, 0, maxPollution);
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);

        CheckDifficultyTier();
        UpdateTileStates();
    }

    /// <summary>
    /// Check and update the current difficulty tier based on pollution level
    /// </summary>
    private void CheckDifficultyTier()
    {
        DifficultyTier newTier;

        if (difficultySettings != null)
        {
            newTier = difficultySettings.GetTierFromPollution(currentPollution);
        }
        else
        {
            // Fallback to default thresholds if no settings assigned
            if (currentPollution >= 750f) newTier = DifficultyTier.Tier4;
            else if (currentPollution >= 500f) newTier = DifficultyTier.Tier3;
            else if (currentPollution >= 250f) newTier = DifficultyTier.Tier2;
            else newTier = DifficultyTier.Tier1;
        }

        if (newTier != currentTier)
        {
            DifficultyTier oldTier = currentTier;
            currentTier = newTier;
            OnDifficultyTierChanged?.Invoke(currentTier);
            Debug.Log($"[PollutionManager] Difficulty tier changed: {oldTier} -> {currentTier} (Pollution: {currentPollution:F0})");
        }
    }

    // ==========================================================================
    // COMBINED DIFFICULTY API (Pollution Tier × Menu Difficulty)
    // ==========================================================================

    /// <summary>
    /// Get final spawn interval (pollution tier × menu difficulty)
    /// </summary>
    public float GetSpawnInterval()
    {
        float baseInterval = GetBaseTierSettings().spawnInterval;
        float menuModifier = GetMenuModifiers().spawnRateModifier;
        return baseInterval * menuModifier;
    }

    /// <summary>
    /// Get final enemy count for this wave (pollution tier × menu difficulty)
    /// </summary>
    public int GetEnemyCount()
    {
        var tierSettings = GetBaseTierSettings();
        float menuModifier = GetMenuModifiers().waveSizeModifier;

        int baseMin = Mathf.RoundToInt(tierSettings.minEnemies * menuModifier);
        int baseMax = Mathf.RoundToInt(tierSettings.maxEnemies * menuModifier);

        return UnityEngine.Random.Range(baseMin, baseMax + 1);
    }

    /// <summary>
    /// Get enemy count range (pollution tier × menu difficulty)
    /// </summary>
    public (int min, int max) GetEnemyCountRange()
    {
        var tierSettings = GetBaseTierSettings();
        float menuModifier = GetMenuModifiers().waveSizeModifier;

        int min = Mathf.RoundToInt(tierSettings.minEnemies * menuModifier);
        int max = Mathf.RoundToInt(tierSettings.maxEnemies * menuModifier);

        return (min, max);
    }

    /// <summary>
    /// Get final enemy HP modifier (pollution tier × menu difficulty)
    /// </summary>
    public float GetEnemyHPModifier()
    {
        float tierModifier = GetBaseTierSettings().statModifier;
        float menuModifier = GetMenuModifiers().enemyHPModifier;
        return tierModifier * menuModifier;
    }

    /// <summary>
    /// Get final enemy damage modifier (pollution tier × menu difficulty)
    /// </summary>
    public float GetEnemyDamageModifier()
    {
        float tierModifier = GetBaseTierSettings().statModifier;
        float menuModifier = GetMenuModifiers().enemyDamageModifier;
        return tierModifier * menuModifier;
    }

    // ==========================================================================
    // HELPER METHODS
    // ==========================================================================

    /// <summary>
    /// Get base tier settings from ScriptableObject (or defaults)
    /// </summary>
    private TierSettings GetBaseTierSettings()
    {
        if (difficultySettings != null)
        {
            return difficultySettings.GetTierSettings(currentTier);
        }

        // Fallback defaults if no ScriptableObject assigned
        return currentTier switch
        {
            DifficultyTier.Tier1 => new TierSettings { spawnInterval = 90f, minEnemies = 5, maxEnemies = 10, statModifier = 1.0f },
            DifficultyTier.Tier2 => new TierSettings { spawnInterval = 60f, minEnemies = 10, maxEnemies = 20, statModifier = 1.0f },
            DifficultyTier.Tier3 => new TierSettings { spawnInterval = 45f, minEnemies = 20, maxEnemies = 30, statModifier = 1.15f },
            DifficultyTier.Tier4 => new TierSettings { spawnInterval = 30f, minEnemies = 30, maxEnemies = 50, statModifier = 1.30f },
            _ => new TierSettings { spawnInterval = 90f, minEnemies = 5, maxEnemies = 10, statModifier = 1.0f }
        };
    }

    /// <summary>
    /// Get menu difficulty modifiers from ScriptableObject (or defaults)
    /// </summary>
    private MenuDifficultyModifiers GetMenuModifiers()
    {
        if (difficultySettings != null)
        {
            return difficultySettings.GetMenuModifiers(menuDifficulty);
        }

        // Fallback defaults if no ScriptableObject assigned
        return menuDifficulty switch
        {
            Difficulty.Easy => new MenuDifficultyModifiers { enemyHPModifier = 0.75f, enemyDamageModifier = 0.75f, spawnRateModifier = 1.3f, waveSizeModifier = 0.75f },
            Difficulty.Medium => new MenuDifficultyModifiers { enemyHPModifier = 1.0f, enemyDamageModifier = 1.0f, spawnRateModifier = 1.0f, waveSizeModifier = 1.0f },
            Difficulty.Hard => new MenuDifficultyModifiers { enemyHPModifier = 1.25f, enemyDamageModifier = 1.25f, spawnRateModifier = 0.7f, waveSizeModifier = 1.25f },
            _ => new MenuDifficultyModifiers { enemyHPModifier = 1.0f, enemyDamageModifier = 1.0f, spawnRateModifier = 1.0f, waveSizeModifier = 1.0f }
        };
    }

    /// <summary>
    /// Get the threshold for a specific tier
    /// </summary>
    public float GetTierThreshold(DifficultyTier tier)
    {
        if (difficultySettings != null)
        {
            return difficultySettings.GetTierThreshold(tier);
        }

        return tier switch
        {
            DifficultyTier.Tier1 => 0f,
            DifficultyTier.Tier2 => 250f,
            DifficultyTier.Tier3 => 500f,
            DifficultyTier.Tier4 => 750f,
            _ => 0f
        };
    }

    /// <summary>
    /// Calculate integration radius based on current pollution level
    /// </summary>
    private float CalculateIntegrationRadius()
    {
        float pollutionRatio = currentPollution / maxPollution;
        float additionalRadius = maxAdditionalRadius * pollutionRatio;
        return starterIntegrationRadius + additionalRadius;
    }

    /// <summary>
    /// Get integration radius - uses peak value so it never shrinks
    /// </summary>
    private float GetIntegrationRadius()
    {
        // Update peak if current calculation is higher
        float currentRadius = CalculateIntegrationRadius();
        if (currentRadius > peakIntegrationRadius)
        {
            peakIntegrationRadius = currentRadius;
        }
        return peakIntegrationRadius;
    }

    /// <summary>
    /// Update tile states (only when radius changes by 1+ unit)
    /// </summary>
    private void UpdateTileStates()
    {
        float currentRadius = GetIntegrationRadius();

        // Only update when radius INCREASES (never shrink)
        if (currentRadius > lastIntegrationRadius && Mathf.Abs(currentRadius - lastIntegrationRadius) >= 1f)
        {
            lastIntegrationRadius = currentRadius;

            if (TileStateManager.Instance != null)
            {
                TileStateManager.Instance.SetIntegrationRadius(currentRadius);
            }
        }
    }

    /// <summary>
    /// Reset pollution to 0 (called when starting new chapter)
    /// </summary>
    public void ResetPollution()
    {
        currentPollution = 0f;
        currentTier = DifficultyTier.Tier1;
        lastIntegrationRadius = -1f;
        peakIntegrationRadius = 0f; // Reset peak for new chapter
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);
        OnDifficultyTierChanged?.Invoke(currentTier);
    }

    /// <summary>
    /// Debug: Log current difficulty state
    /// </summary>
    [ContextMenu("Log Difficulty State")]
    public void LogDifficultyState()
    {
        Debug.Log($"=== DIFFICULTY STATE ===");
        Debug.Log($"Menu Difficulty: {menuDifficulty}");
        Debug.Log($"Pollution: {currentPollution:F0}/{maxPollution:F0} ({PollutionPercentage:F1}%)");
        Debug.Log($"Pollution Tier: {currentTier}");
        Debug.Log($"--- COMBINED VALUES ---");
        Debug.Log($"Spawn Interval: {GetSpawnInterval():F1}s");
        var (min, max) = GetEnemyCountRange();
        Debug.Log($"Enemy Count: {min}-{max}");
        Debug.Log($"Enemy HP Modifier: {GetEnemyHPModifier():F2}x");
        Debug.Log($"Enemy Damage Modifier: {GetEnemyDamageModifier():F2}x");
        Debug.Log($"========================");
    }
}

/// <summary>
/// Difficulty tiers based on pollution level (dynamic, reversible)
/// </summary>
public enum DifficultyTier
{
    Tier1,  // Low pollution: peaceful expansion
    Tier2,  // Medium pollution: moderate threat
    Tier3,  // High pollution: high pressure
    Tier4   // Extreme pollution: maximum challenge
}

/// <summary>
/// Enemy race types - determined by chapter, NOT pollution
/// </summary>
public enum RaceType
{
    Human,
    Elf,
    Dwarf,
    Demon
}
