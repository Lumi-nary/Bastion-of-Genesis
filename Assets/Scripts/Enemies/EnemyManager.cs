using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages all enemy spawning, wave management, and targeting
/// Singleton pattern for global access
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Enemy Configuration")]
    [Tooltip("DEBUG ONLY: Test enemies when ChapterData not available")]
    [SerializeField] private List<EnemyData> debugEnemyTypes = new List<EnemyData>();

    [Tooltip("All enemy types available for current chapter")]
    private List<EnemyData> allEnemyTypes = new List<EnemyData>();

    [Tooltip("Currently active enemies in the scene")]
    private List<Enemy> activeEnemies = new List<Enemy>();

    [Header("Wave System")]
    [Tooltip("Current wave number")]
    private int currentWave = 0;

    [Tooltip("Total enemies killed this mission")]
    private int enemiesKilled = 0;

    [Tooltip("Is a wave currently active?")]
    private bool isWaveActive = false;

    // Events
    public delegate void WaveStartEvent(int waveNumber);
    public event WaveStartEvent OnWaveStart;

    public delegate void WaveCompleteEvent(int waveNumber);
    public event WaveCompleteEvent OnWaveComplete;

    public delegate void EnemySpawnedEvent(Enemy enemy);
    public event EnemySpawnedEvent OnEnemySpawned;

    public delegate void EnemyKilledEvent(Enemy enemy);
    public event EnemyKilledEvent OnEnemyKilledEvent;

    // Public properties
    public int CurrentWave => currentWave;
    public int EnemiesKilled => enemiesKilled;
    public int ActiveEnemyCount => activeEnemies.Count;
    public bool IsWaveActive => isWaveActive;
    public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadAllEnemyTypes();

        // Subscribe to scene changes to reset flow field flag
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset flow field flag so it recalculates for new scene
        flowFieldSetupForThisScene = false;
        Debug.Log($"[EnemyManager] Scene loaded: {scene.name}, flow field will recalculate on next spawn");
    }

    private void Start()
    {
        // Delay flow field setup to ensure all obstacles are registered first
        StartCoroutine(DelayedFlowTargetSetup());
    }

    private System.Collections.IEnumerator DelayedFlowTargetSetup()
    {
        // Wait one frame for all Start() methods to complete
        yield return null;
        SetupFlowTarget();
    }

    /// <summary>
    /// Set up flow field target from Command Center
    /// </summary>
    private void SetupFlowTarget()
    {
        Debug.Log($"[EnemyManager] SetupFlowTarget - BuildingManager: {BuildingManager.Instance != null}, PathfindingManager: {PathfindingManager.Instance != null}");

        if (BuildingManager.Instance == null || PathfindingManager.Instance == null) return;

        Debug.Log($"[EnemyManager] AllBuildings count: {BuildingManager.Instance.AllBuildings.Count}");

        // Find Command Center
        Building commandCenter = BuildingManager.Instance.AllBuildings
            .FirstOrDefault(b => b != null && b.BuildingData != null && b.BuildingData.category == BuildingCategory.Command);

        if (commandCenter != null)
        {
            PathfindingManager.Instance.SetFlowTargetFromWorld(commandCenter.transform.position);
            Debug.Log($"[EnemyManager] Flow target set to Command Center at {commandCenter.transform.position}");
        }
        else
        {
            Debug.LogWarning("[EnemyManager] No Command Center found for flow field target");
        }
    }

    /// <summary>
    /// Refresh flow target (call after Command Center is placed)
    /// </summary>
    public void RefreshFlowTarget()
    {
        SetupFlowTarget();
    }

    /// <summary>
    /// Ensure flow field is set up (called on first enemy spawn)
    /// Always recalculates on first spawn to handle scene reloads
    /// </summary>
    private bool flowFieldSetupForThisScene = false;

    private void EnsureFlowFieldSetup()
    {
        if (flowFieldSetupForThisScene) return;

        Debug.Log($"[EnemyManager] EnsureFlowFieldSetup - forcing recalculation for this scene");
        SetupFlowTarget();
        flowFieldSetupForThisScene = true;
    }

    /// <summary>
    /// Load enemy types from ChapterData or debug list
    /// </summary>
    private void LoadAllEnemyTypes()
    {
        // Try to load from ChapterData (production)
        if (MissionChapterManager.Instance != null &&
            MissionChapterManager.Instance.CurrentChapter != null &&
            MissionChapterManager.Instance.CurrentChapter.chapterEnemies.Count > 0)
        {
            allEnemyTypes = new List<EnemyData>(MissionChapterManager.Instance.CurrentChapter.chapterEnemies);
            Debug.Log($"[EnemyManager] Loaded {allEnemyTypes.Count} enemies from ChapterData: {MissionChapterManager.Instance.CurrentChapter.chapterName}");
        }
        // Fallback to debug list (for scene testing)
        else if (debugEnemyTypes != null && debugEnemyTypes.Count > 0)
        {
            allEnemyTypes = new List<EnemyData>(debugEnemyTypes);
            Debug.Log($"[EnemyManager] Loaded {allEnemyTypes.Count} enemies from DEBUG list");
        }
        else
        {
            Debug.LogWarning("[EnemyManager] No enemies found! Add to ChapterData.chapterEnemies or debug list.");
        }
    }

    /// <summary>
    /// Reload enemy types from current ChapterData.
    /// Called by MissionChapterManager when a new chapter starts.
    /// </summary>
    public void ReloadEnemyTypesFromChapter()
    {
        LoadAllEnemyTypes();
    }

    /// <summary>
    /// Register a networked enemy spawned by FishNet
    /// </summary>
    public void RegisterNetworkedEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
            Debug.Log($"[EnemyManager] Networked enemy registered: {enemy.name}");
        }
    }

    /// <summary>
    /// Unregister a networked enemy
    /// </summary>
    public void UnregisterNetworkedEnemy(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            Debug.Log($"[EnemyManager] Networked enemy unregistered: {enemy.name}");
        }
    }

    /// <summary>
    /// Spawn a single enemy of the specified type
    /// </summary>
    public Enemy SpawnEnemy(EnemyData enemyData, Vector3 spawnPosition)
    {
        if (enemyData == null || enemyData.prefab == null)
        {
            Debug.LogError("[EnemyManager] Cannot spawn enemy: invalid enemy data or missing prefab");
            return null;
        }

        GameObject enemyGO = Instantiate(enemyData.prefab, spawnPosition, Quaternion.identity);
        Enemy enemy = enemyGO.GetComponent<Enemy>();

        if (enemy == null)
        {
            Debug.LogError($"[EnemyManager] Enemy prefab {enemyData.prefab.name} is missing Enemy component!");
            Destroy(enemyGO);
            return null;
        }

        // Calculate difficulty and pollution multipliers
        float diffMult = GetDifficultyMultiplier();
        float pollMult = GetPollutionMultiplier();

        // Initialize enemy with multipliers
        enemy.Initialize(enemyData, diffMult, pollMult);

        // Ensure flow field is set up (in case scene reloaded)
        EnsureFlowFieldSetup();

        // Track enemy
        activeEnemies.Add(enemy);

        // Subscribe to death event
        enemy.OnEnemyDeath += HandleEnemyDeath;

        // Notify listeners
        OnEnemySpawned?.Invoke(enemy);

        Debug.Log($"[EnemyManager] Spawned {enemyData.GetDisplayName()} at {spawnPosition}");

        return enemy;
    }

    /// <summary>
    /// Select an enemy type for the current wave/pollution (Public for NetworkedEnemyManager)
    /// </summary>
    public EnemyData SelectEnemyForWave(int waveNumber, float pollutionNormalized)
    {
        // Enemies are already filtered by ChapterData, just use allEnemyTypes
        if (allEnemyTypes.Count == 0) return null;

        // Filter enemies that can spawn in current chapter
        int currentChapter = 1;
        if (MissionChapterManager.Instance != null)
        {
            currentChapter = MissionChapterManager.Instance.CurrentChapterIndex + 1;
        }

        List<EnemyData> availableEnemies = allEnemyTypes
            .Where(e => e.CanSpawnInChapter(currentChapter))
            .ToList();

        if (availableEnemies.Count == 0) return null;

        return SelectWeightedEnemy(availableEnemies, pollutionNormalized);
    }

    /// <summary>
    /// Spawn a single enemy for a wave using pollution-based weighted selection
    /// </summary>
    public Enemy SpawnEnemyForWave(int waveNumber, Vector3 spawnPosition, float pollutionNormalized)
    {
        currentWave = waveNumber;
        isWaveActive = true;

        // Enemies are already filtered by ChapterData, just use allEnemyTypes
        if (allEnemyTypes.Count == 0)
        {
            Debug.LogWarning("[EnemyManager] No enemies available! Check ChapterData or debug list.");
            return null;
        }

        // Select enemy using weighted random based on pollution
        EnemyData selectedEnemy = SelectWeightedEnemy(allEnemyTypes, pollutionNormalized);

        if (selectedEnemy == null)
        {
            Debug.LogWarning("[EnemyManager] Failed to select enemy!");
            return null;
        }

        return SpawnEnemy(selectedEnemy, spawnPosition);
    }

    /// <summary>
    /// Spawn a wave of enemies based on current wave number and pollution
    /// </summary>
    public void SpawnWave(int waveNumber, Vector3 spawnPosition)
    {
        currentWave = waveNumber;
        isWaveActive = true;

        OnWaveStart?.Invoke(waveNumber);

        // Get current pollution level
        float currentPollution = PollutionManager.Instance != null ? PollutionManager.Instance.CurrentPollution : 0f;

        // Calculate wave difficulty (more enemies, tougher types as waves progress)
        int enemyCount = CalculateEnemyCount(waveNumber);
        List<EnemyData> waveEnemies = SelectEnemiesForWave(waveNumber, currentPollution, enemyCount);

        Debug.Log($"[EnemyManager] Starting Wave {waveNumber} with {waveEnemies.Count} enemies");

        // Spawn all enemies in the wave
        foreach (EnemyData enemyData in waveEnemies)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0f);
            SpawnEnemy(enemyData, spawnPosition + randomOffset);
        }
    }

    /// <summary>
    /// Calculate number of enemies for a wave
    /// Uses pollution-based spawn multiplier: 1.0x at 0%, 3.0x at 100%
    /// </summary>
    private int CalculateEnemyCount(int waveNumber)
    {
        // Base: 5 enemies, +2 per wave
        int baseCount = 5 + (waveNumber * 2);

        // Apply pollution multiplier
        float pollutionMultiplier = 1f;
        if (PollutionManager.Instance != null)
        {
            pollutionMultiplier = PollutionManager.Instance.GetSpawnCountMultiplier();
        }

        return Mathf.RoundToInt(baseCount * pollutionMultiplier);
    }

    /// <summary>
    /// Select which enemies to spawn in a wave using pollution-based weighted random selection
    /// </summary>
    private List<EnemyData> SelectEnemiesForWave(int waveNumber, float pollution, int count)
    {
        List<EnemyData> selectedEnemies = new List<EnemyData>();

        // Get current chapter from MissionChapterManager (chapter numbers are 1-5, index is 0-4)
        int currentChapter = 1;
        if (MissionChapterManager.Instance != null)
        {
            currentChapter = MissionChapterManager.Instance.CurrentChapterIndex + 1;
        }

        // Get normalized pollution (0-1)
        float pollutionNormalized = PollutionManager.Instance != null
            ? PollutionManager.Instance.PollutionNormalized
            : 0f;

        // Filter enemies that can spawn in current chapter
        List<EnemyData> availableEnemies = allEnemyTypes
            .Where(e => e.CanSpawnInChapter(currentChapter))
            .ToList();

        if (availableEnemies.Count == 0)
        {
            Debug.LogWarning("[EnemyManager] No enemies available for current chapter!");
            return selectedEnemies;
        }

        // Select enemies using weighted random selection based on pollution
        for (int i = 0; i < count; i++)
        {
            EnemyData selected = SelectWeightedEnemy(availableEnemies, pollutionNormalized);
            if (selected != null)
            {
                selectedEnemies.Add(selected);
            }
        }

        return selectedEnemies;
    }

    /// <summary>
    /// Select a single enemy using pollution-based weighted random selection
    /// </summary>
    private EnemyData SelectWeightedEnemy(List<EnemyData> availableEnemies, float pollutionNormalized)
    {
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var enemy in availableEnemies)
        {
            totalWeight += enemy.GetSpawnWeight(pollutionNormalized);
        }

        // If no enemies have weight at this pollution level, fall back to random
        if (totalWeight <= 0f)
        {
            return availableEnemies[Random.Range(0, availableEnemies.Count)];
        }

        // Weighted random selection
        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var enemy in availableEnemies)
        {
            cumulative += enemy.GetSpawnWeight(pollutionNormalized);
            if (randomValue <= cumulative)
            {
                return enemy;
            }
        }

        // Fallback (shouldn't reach here)
        return availableEnemies[availableEnemies.Count - 1];
    }

    /// <summary>
    /// Aggro range - enemies will target buildings within this range
    /// </summary>
    [Header("Targeting Settings")]
    [SerializeField] private float aggroRange = 5f;

    /// <summary>
    /// Get the best target for an enemy based on aggro range and priority
    /// 1. Buildings within aggro range (priority: Turrets > Defenses > Generators > Extractors > Walls)
    /// 2. Command Center as ultimate goal if nothing in aggro range
    /// </summary>
    public Building GetTargetForEnemy(Enemy enemy)
    {
        if (BuildingManager.Instance == null) return null;

        List<Building> allBuildings = BuildingManager.Instance.AllBuildings.ToList();
        if (allBuildings.Count == 0) return null;

        Vector3 enemyPos = enemy.transform.position;

        // Get buildings within aggro range (exclude walls - they're just obstacles)
        List<Building> aggroTargets = allBuildings
            .Where(b => b.BuildingData != null &&
                        !b.IsDestroyed &&
                        !b.BuildingData.HasFeature<WallFeature>() &&
                        Vector3.Distance(enemyPos, b.transform.position) <= aggroRange)
            .ToList();

        // If buildings in aggro range, prioritize them
        if (aggroTargets.Count > 0)
        {
            Building target = null;

            // 1. Turrets (highest threat)
            target = aggroTargets.FirstOrDefault(b =>
                b.BuildingData.category == BuildingCategory.Defense &&
                b.BuildingData.HasFeature<TurretFeature>());
            if (target != null) return target;

            // 2. Generators
            target = aggroTargets.FirstOrDefault(b =>
                b.BuildingData.category == BuildingCategory.Energy);
            if (target != null) return target;

            // 3. Extractors
            target = aggroTargets.FirstOrDefault(b =>
                b.BuildingData.category == BuildingCategory.Extraction);
            if (target != null) return target;

            // 4. Command Center (lowest priority when other buildings nearby)
            target = aggroTargets.FirstOrDefault(b =>
                b.BuildingData.category == BuildingCategory.Command);
            if (target != null) return target;

            // 5. Any other nearby building
            target = aggroTargets.FirstOrDefault();
            if (target != null) return target;
        }

        // No buildings in aggro range - target Command Center as ultimate goal
        Building commandCenter = allBuildings.FirstOrDefault(b =>
            b.BuildingData != null && b.BuildingData.category == BuildingCategory.Command);
        if (commandCenter != null) return commandCenter;

        // Fallback
        return allBuildings.FirstOrDefault(b => !b.IsDestroyed);
    }

    /// <summary>
    /// Handle enemy death (called by Enemy component)
    /// </summary>
    private void HandleEnemyDeath(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            enemiesKilled++;

            OnEnemyKilledEvent?.Invoke(enemy);

            Debug.Log($"[EnemyManager] Enemy killed: {enemy.Data.GetDisplayName()} ({activeEnemies.Count} remaining)");

            // Check if wave is complete
            if (isWaveActive && activeEnemies.Count == 0)
            {
                CompleteWave();
            }
        }
    }

    /// <summary>
    /// Called externally when an enemy is killed (alternate entry point)
    /// </summary>
    public void OnEnemyKilled(Enemy enemy)
    {
        HandleEnemyDeath(enemy);
    }

    /// <summary>
    /// Complete the current wave
    /// </summary>
    private void CompleteWave()
    {
        isWaveActive = false;

        OnWaveComplete?.Invoke(currentWave);

        Debug.Log($"[EnemyManager] Wave {currentWave} completed!");
    }

    /// <summary>
    /// Get all enemies of a specific race
    /// </summary>
    public List<Enemy> GetEnemiesByRace(EnemyRace race)
    {
        return activeEnemies.Where(e => e.Data.race == race).ToList();
    }

    /// <summary>
    /// Get all enemies of a specific damage type
    /// </summary>
    public List<Enemy> GetEnemiesByDamageType(DamageType damageType)
    {
        return activeEnemies.Where(e => e.Data.damageType == damageType).ToList();
    }

    /// <summary>
    /// Get all enemies of a specific movement type
    /// </summary>
    public List<Enemy> GetEnemiesByMovementType(MovementType movementType)
    {
        return activeEnemies.Where(e => e.Data.movementType == movementType).ToList();
    }

    /// <summary>
    /// Clear all enemies (for mission end/reset)
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (Enemy enemy in activeEnemies.ToList())
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        activeEnemies.Clear();
        isWaveActive = false;

        Debug.Log("[EnemyManager] All enemies cleared");
    }

    /// <summary>
    /// Get all active enemies (used by healing abilities, etc.)
    /// </summary>
    public List<Enemy> GetAllActiveEnemies()
    {
        return new List<Enemy>(activeEnemies);
    }

    /// <summary>
    /// Reset enemy manager (for new mission)
    /// </summary>
    public void ResetForNewMission()
    {
        ClearAllEnemies();
        currentWave = 0;
        enemiesKilled = 0;

        Debug.Log("[EnemyManager] Reset for new mission");
    }

    /// <summary>
    /// Get difficulty multiplier based on current mission difficulty
    /// Easy: 0.75×, Medium: 1.0×, Hard: 1.2×
    /// </summary>
    private float GetDifficultyMultiplier()
    {
        if (SaveManager.Instance != null)
        {
            switch (SaveManager.Instance.pendingDifficulty)
            {
                case Difficulty.Easy:
                    return 0.75f;
                case Difficulty.Medium:
                    return 1.0f;
                case Difficulty.Hard:
                    return 1.2f;
                default:
                    return 1.0f;
            }
        }
        return 1.0f; // Default to Medium
    }

    /// <summary>
    /// Get pollution multiplier for enemy stats (HP/damage scaling)
    /// Uses linear formula: 1.0 + (pollution * 0.5) for 1.0x to 1.5x scaling
    /// </summary>
    private float GetPollutionMultiplier()
    {
        if (PollutionManager.Instance != null)
        {
            float pollutionNormalized = PollutionManager.Instance.PollutionNormalized;
            // 1.0x at 0% pollution, 1.5x at 100% pollution
            return 1f + (pollutionNormalized * 0.5f);
        }
        return 1.0f; // Default if no pollution manager
    }

    /// <summary>
    /// Get the wave interval multiplier (for wave controllers to use)
    /// </summary>
    public float GetWaveIntervalMultiplier()
    {
        if (PollutionManager.Instance != null)
        {
            return PollutionManager.Instance.GetWaveIntervalMultiplier();
        }
        return 1.0f;
    }
}
