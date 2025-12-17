using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls enemy wave spawning using a threat accumulation system.
/// - Threat builds over time (faster at higher pollution)
/// - When threshold reached, roll attack chance
/// - Success = spawn wave, Fail = partial reset
/// - Direction escalates with pollution tiers
/// </summary>
public class WaveController : MonoBehaviour
{
    public static WaveController Instance { get; private set; }

    [Header("Threat Accumulation")]
    [Tooltip("Base threat points gained per second")]
    [SerializeField] private float baseThreatRate = 1f;

    [Tooltip("Threat threshold to trigger attack chance roll")]
    [SerializeField] private float threatThreshold = 100f;

    [Tooltip("How much threat is retained on failed roll (0.5 = 50%)")]
    [SerializeField] private float failedRollRetention = 0.5f;

    [Header("Attack Chance")]
    [Tooltip("Base chance to attack when threshold reached (0-1)")]
    [SerializeField] private float baseAttackChance = 0.5f;

    [Tooltip("Additional attack chance at max pollution (0-1)")]
    [SerializeField] private float pollutionAttackBonus = 0.3f;

    [Header("Timing")]
    [Tooltip("Maximum seconds before forcing a wave (0 = disabled)")]
    [SerializeField] private float maxWaitTime = 120f;

    [Tooltip("Minimum seconds between waves")]
    [SerializeField] private float minTimeBetweenWaves = 15f;

    [Tooltip("Delay before threat starts accumulating")]
    [SerializeField] private float initialDelay = 10f;

    [Header("Spawn Settings")]
    [Tooltip("Distance outside map edge to spawn")]
    [SerializeField] private float edgeSpawnOffset = 3f;

    [Tooltip("Spread enemies along edge (units)")]
    [SerializeField] private float spawnSpread = 10f;

    // Map bounds (auto-calculated from GridManager)
    private float mapMinX;
    private float mapMaxX;
    private float mapMinY;
    private float mapMaxY;
    private bool boundsInitialized = false;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Runtime state
    private float currentThreat = 0f;
    private float timeSinceLastWave = 0f;
    private float timeSinceStart = 0f;
    private int currentWave = 0;
    private bool isActive = false;
    private bool isPaused = false;

    // Events
    public event System.Action<int> OnWaveStarted;
    public event System.Action<float, float> OnThreatChanged; // current, threshold
    public event System.Action<bool> OnAttackRolled; // success/fail

    // Properties
    public int CurrentWave => currentWave;
    public float CurrentThreat => currentThreat;
    public float ThreatThreshold => threatThreshold;
    public float ThreatPercentage => (currentThreat / threatThreshold) * 100f;
    public bool IsActive => isActive;
    public bool IsPaused => isPaused;

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
        InitializeMapBounds();
        StartWaveSystem();
    }

    /// <summary>
    /// Initialize map bounds from GridManager tilemap
    /// </summary>
    private void InitializeMapBounds()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogWarning("[WaveController] GridManager not found, using default bounds");
            mapMinX = -50f;
            mapMaxX = 50f;
            mapMinY = -50f;
            mapMaxY = 50f;
            boundsInitialized = true;
            return;
        }

        BoundsInt tileBounds = GridManager.Instance.GetTilemapBounds();
        float cellSize = GridManager.Instance.GetCellSize();

        // Calculate actual world edges (not cell centers)
        // tileBounds.xMin/yMin = first cell, tileBounds.xMax/yMax = one past last cell
        mapMinX = tileBounds.xMin * cellSize;
        mapMaxX = tileBounds.xMax * cellSize;
        mapMinY = tileBounds.yMin * cellSize;
        mapMaxY = tileBounds.yMax * cellSize;

        boundsInitialized = true;

        Debug.Log($"[WaveController] Map bounds from tilemap: grid({tileBounds.xMin} to {tileBounds.xMax}, {tileBounds.yMin} to {tileBounds.yMax}), world X({mapMinX:F1} to {mapMaxX:F1}), Y({mapMinY:F1} to {mapMaxY:F1})");
    }

    private void Update()
    {
        if (!isActive || isPaused) return;

        timeSinceStart += Time.deltaTime;

        // Wait for initial delay
        if (timeSinceStart < initialDelay) return;

        timeSinceLastWave += Time.deltaTime;

        // Enforce minimum time between waves
        if (timeSinceLastWave < minTimeBetweenWaves) return;

        // Accumulate threat
        AccumulateThreat();

        // Check for forced wave (max wait time)
        if (maxWaitTime > 0 && timeSinceLastWave >= maxWaitTime)
        {
            if (debugMode) Debug.Log("[WaveController] Max wait time reached, forcing wave!");
            TriggerWave();
            return;
        }

        // Check if threshold reached
        if (currentThreat >= threatThreshold)
        {
            RollForAttack();
        }
    }

    /// <summary>
    /// Start the wave system
    /// </summary>
    public void StartWaveSystem()
    {
        isActive = true;
        isPaused = false;
        currentThreat = 0f;
        timeSinceLastWave = 0f;
        timeSinceStart = 0f;
        currentWave = 0;

        Debug.Log($"[WaveController] Wave system started. First threat check in {initialDelay}s");
    }

    /// <summary>
    /// Stop the wave system
    /// </summary>
    public void StopWaveSystem()
    {
        isActive = false;
        Debug.Log("[WaveController] Wave system stopped");
    }

    /// <summary>
    /// Pause/Resume
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
        Debug.Log($"[WaveController] Waves {(paused ? "paused" : "resumed")}");
    }

    /// <summary>
    /// Accumulate threat based on time and pollution
    /// </summary>
    private void AccumulateThreat()
    {
        float pollutionMultiplier = GetPollutionThreatMultiplier();
        float threatGain = baseThreatRate * pollutionMultiplier * Time.deltaTime;

        currentThreat += threatGain;
        OnThreatChanged?.Invoke(currentThreat, threatThreshold);

        if (debugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[WaveController] Threat: {currentThreat:F1}/{threatThreshold} (×{pollutionMultiplier:F2})");
        }
    }

    /// <summary>
    /// Get threat accumulation multiplier based on pollution (1.0x to 2.5x)
    /// </summary>
    private float GetPollutionThreatMultiplier()
    {
        if (PollutionManager.Instance == null) return 1f;

        float pollution = PollutionManager.Instance.PollutionNormalized;
        // 1.0x at 0%, 2.5x at 100%
        return 1f + (pollution * 1.5f);
    }

    /// <summary>
    /// Roll for attack when threshold reached
    /// </summary>
    private void RollForAttack()
    {
        float attackChance = GetAttackChance();
        float roll = Random.value;
        bool success = roll <= attackChance;

        if (debugMode)
        {
            Debug.Log($"[WaveController] Attack roll: {roll:F2} vs {attackChance:F2} = {(success ? "SUCCESS" : "FAIL")}");
        }

        OnAttackRolled?.Invoke(success);

        if (success)
        {
            TriggerWave();
        }
        else
        {
            // Partial reset on fail
            currentThreat *= failedRollRetention;
            OnThreatChanged?.Invoke(currentThreat, threatThreshold);

            if (debugMode)
            {
                Debug.Log($"[WaveController] Attack failed, threat reduced to {currentThreat:F1}");
            }
        }
    }

    /// <summary>
    /// Get attack chance (50% base + pollution bonus up to 30%)
    /// </summary>
    private float GetAttackChance()
    {
        float pollution = PollutionManager.Instance != null
            ? PollutionManager.Instance.PollutionNormalized
            : 0f;

        return Mathf.Clamp01(baseAttackChance + (pollution * pollutionAttackBonus));
    }

    /// <summary>
    /// Trigger a wave spawn
    /// </summary>
    private void TriggerWave()
    {
        currentWave++;
        currentThreat = 0f;
        timeSinceLastWave = 0f;

        // Get spawn edges based on pollution tier
        List<MapEdge> edges = GetSpawnEdges();

        // Get enemy count from EnemyManager
        int enemyCount = 0;
        if (EnemyManager.Instance != null)
        {
            // Use the pollution-scaled count
            enemyCount = GetEnemyCountForWave();
        }

        Debug.Log($"[WaveController] === WAVE {currentWave} === Spawning {enemyCount} enemies from {edges.Count} edge(s)");

        // Spawn enemies across selected edges
        SpawnEnemiesOnEdges(edges, enemyCount);

        OnWaveStarted?.Invoke(currentWave);
        OnThreatChanged?.Invoke(0f, threatThreshold);
    }

    /// <summary>
    /// Get number of edges based on pollution tier
    /// </summary>
    private List<MapEdge> GetSpawnEdges()
    {
        int edgeCount = GetEdgeCountFromPollution();
        List<MapEdge> allEdges = new List<MapEdge> { MapEdge.North, MapEdge.South, MapEdge.East, MapEdge.West };
        List<MapEdge> selectedEdges = new List<MapEdge>();

        // Shuffle and pick
        for (int i = allEdges.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allEdges[i], allEdges[j]) = (allEdges[j], allEdges[i]);
        }

        for (int i = 0; i < Mathf.Min(edgeCount, allEdges.Count); i++)
        {
            selectedEdges.Add(allEdges[i]);
        }

        return selectedEdges;
    }

    /// <summary>
    /// Get edge count based on pollution tier (5 tiers)
    /// Tier 1: 1 edge, Tier 2: 2 edges, Tier 3-4: 3 edges, Tier 5: 4 edges
    /// </summary>
    private int GetEdgeCountFromPollution()
    {
        float pollution = PollutionManager.Instance != null
            ? PollutionManager.Instance.PollutionNormalized
            : 0f;

        if (pollution <= 0.2f) return 1;      // Tier 1: 0-20%
        if (pollution <= 0.4f) return 2;      // Tier 2: 21-40%
        if (pollution <= 0.8f) return 3;      // Tier 3-4: 41-80%
        return 4;                              // Tier 5: 81-100%
    }

    /// <summary>
    /// Calculate enemy count for wave (base + wave scaling × pollution multiplier)
    /// </summary>
    private int GetEnemyCountForWave()
    {
        // Base: 5 enemies, +2 per wave
        int baseCount = 5 + (currentWave * 2);

        // Apply pollution multiplier (1.0x to 3.0x)
        float pollutionMultiplier = 1f;
        if (PollutionManager.Instance != null)
        {
            pollutionMultiplier = PollutionManager.Instance.GetSpawnCountMultiplier();
        }

        return Mathf.RoundToInt(baseCount * pollutionMultiplier);
    }

    /// <summary>
    /// Spawn enemies distributed across selected edges
    /// </summary>
    private void SpawnEnemiesOnEdges(List<MapEdge> edges, int totalCount)
    {
        if (EnemyManager.Instance == null || edges.Count == 0) return;

        // Get normalized pollution for enemy selection
        float pollutionNormalized = PollutionManager.Instance != null
            ? PollutionManager.Instance.PollutionNormalized
            : 0f;

        // Distribute enemies across edges
        int enemiesPerEdge = totalCount / edges.Count;
        int remainder = totalCount % edges.Count;

        for (int i = 0; i < edges.Count; i++)
        {
            int countForThisEdge = enemiesPerEdge + (i < remainder ? 1 : 0);
            Vector3 baseSpawnPos = GetSpawnPositionOnEdge(edges[i]);

            for (int j = 0; j < countForThisEdge; j++)
            {
                // Add random offset for each enemy
                Vector3 offset = new Vector3(
                    Random.Range(-spawnSpread, spawnSpread),
                    Random.Range(-spawnSpread, spawnSpread),
                    0f
                );
                Vector3 spawnPos = baseSpawnPos + offset;

                // Select and spawn individual enemy using EnemyManager
                EnemyManager.Instance.SpawnEnemyForWave(currentWave, spawnPos, pollutionNormalized);
            }
        }
    }

    /// <summary>
    /// Get a random spawn position along an edge (spawns INSIDE map bounds)
    /// Validates position is within flow field and on walkable tile
    /// </summary>
    private Vector3 GetSpawnPositionOnEdge(MapEdge edge)
    {
        const int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float randomOffset = Random.Range(-spawnSpread, spawnSpread);
            Vector3 position;

            switch (edge)
            {
                case MapEdge.North:
                    position = new Vector3(
                        Mathf.Clamp((mapMinX + mapMaxX) / 2f + randomOffset, mapMinX + edgeSpawnOffset, mapMaxX - edgeSpawnOffset),
                        mapMaxY - edgeSpawnOffset,
                        0f
                    );
                    break;
                case MapEdge.South:
                    position = new Vector3(
                        Mathf.Clamp((mapMinX + mapMaxX) / 2f + randomOffset, mapMinX + edgeSpawnOffset, mapMaxX - edgeSpawnOffset),
                        mapMinY + edgeSpawnOffset,
                        0f
                    );
                    break;
                case MapEdge.East:
                    position = new Vector3(
                        mapMaxX - edgeSpawnOffset,
                        Mathf.Clamp((mapMinY + mapMaxY) / 2f + randomOffset, mapMinY + edgeSpawnOffset, mapMaxY - edgeSpawnOffset),
                        0f
                    );
                    break;
                case MapEdge.West:
                    position = new Vector3(
                        mapMinX + edgeSpawnOffset,
                        Mathf.Clamp((mapMinY + mapMaxY) / 2f + randomOffset, mapMinY + edgeSpawnOffset, mapMaxY - edgeSpawnOffset),
                        0f
                    );
                    break;
                default:
                    position = Vector3.zero;
                    break;
            }

            // Validate spawn position
            if (IsValidSpawnPosition(position))
            {
                return position;
            }
        }

        // Fallback: return center of edge without validation
        if (debugMode)
        {
            Debug.LogWarning($"[WaveController] Could not find valid spawn on {edge} edge after {maxAttempts} attempts");
        }

        return GetEdgeCenter(edge);
    }

    /// <summary>
    /// Check if position is valid for spawning (within grid, walkable, has flow)
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 worldPosition)
    {
        if (GridManager.Instance == null) return true; // No validation if no grid

        Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPosition);

        // Check if within tilemap bounds first
        BoundsInt tileBounds = GridManager.Instance.GetTilemapBounds();
        if (gridPos.x < tileBounds.xMin || gridPos.x >= tileBounds.xMax ||
            gridPos.y < tileBounds.yMin || gridPos.y >= tileBounds.yMax)
        {
            if (debugMode)
            {
                Debug.Log($"[WaveController] Position {gridPos} outside tilemap bounds ({tileBounds.xMin}-{tileBounds.xMax}, {tileBounds.yMin}-{tileBounds.yMax})");
            }
            return false;
        }

        // Check if tile exists
        if (!GridManager.Instance.HasTileAt(gridPos))
        {
            return false;
        }

        // Check if walkable (not water, buildings, etc.)
        if (!GridManager.Instance.IsWalkable(gridPos))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get center position of an edge (fallback)
    /// </summary>
    private Vector3 GetEdgeCenter(MapEdge edge)
    {
        switch (edge)
        {
            case MapEdge.North:
                return new Vector3((mapMinX + mapMaxX) / 2f, mapMaxY - edgeSpawnOffset, 0f);
            case MapEdge.South:
                return new Vector3((mapMinX + mapMaxX) / 2f, mapMinY + edgeSpawnOffset, 0f);
            case MapEdge.East:
                return new Vector3(mapMaxX - edgeSpawnOffset, (mapMinY + mapMaxY) / 2f, 0f);
            case MapEdge.West:
                return new Vector3(mapMinX + edgeSpawnOffset, (mapMinY + mapMaxY) / 2f, 0f);
            default:
                return new Vector3((mapMinX + mapMaxX) / 2f, (mapMinY + mapMaxY) / 2f, 0f);
        }
    }

    /// <summary>
    /// Override map bounds manually (optional, normally auto-detected from GridManager)
    /// </summary>
    public void SetMapBoundsOverride(float minX, float maxX, float minY, float maxY)
    {
        mapMinX = minX;
        mapMaxX = maxX;
        mapMinY = minY;
        mapMaxY = maxY;
        boundsInitialized = true;
        Debug.Log($"[WaveController] Map bounds overridden: X({minX} to {maxX}), Y({minY} to {maxY})");
    }

    /// <summary>
    /// Force trigger next wave (for testing/debug)
    /// </summary>
    public void ForceWave()
    {
        TriggerWave();
    }

    /// <summary>
    /// Reset for new mission
    /// </summary>
    public void ResetForNewMission()
    {
        StopWaveSystem();
        currentWave = 0;
        currentThreat = 0f;
        timeSinceLastWave = 0f;
        timeSinceStart = 0f;
        Debug.Log("[WaveController] Reset for new mission");
    }

    private enum MapEdge { North, South, East, West }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float minX = mapMinX, maxX = mapMaxX, minY = mapMinY, maxY = mapMaxY;

        // In edit mode, find tilemap directly since Instance won't be set
        if (!boundsInitialized || !Application.isPlaying)
        {
            // Try to find GridManager in scene
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                // Use reflection or direct tilemap access
                var tilemapField = typeof(GridManager).GetField("terrainTilemap",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (tilemapField != null)
                {
                    var tilemap = tilemapField.GetValue(gridManager) as UnityEngine.Tilemaps.Tilemap;
                    if (tilemap != null)
                    {
                        tilemap.CompressBounds();
                        BoundsInt tileBounds = tilemap.cellBounds;
                        float cellSize = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize.x : 1f;

                        minX = tileBounds.xMin * cellSize;
                        maxX = tileBounds.xMax * cellSize;
                        minY = tileBounds.yMin * cellSize;
                        maxY = tileBounds.yMax * cellSize;
                    }
                }
            }
        }

        // Draw map bounds (red)
        Gizmos.color = Color.red;
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw spawn zone (yellow) - INSIDE the map bounds
        Gizmos.color = Color.yellow;
        float offset = edgeSpawnOffset;
        Vector3 spawnBL = new Vector3(minX + offset, minY + offset, 0);
        Vector3 spawnBR = new Vector3(maxX - offset, minY + offset, 0);
        Vector3 spawnTL = new Vector3(minX + offset, maxY - offset, 0);
        Vector3 spawnTR = new Vector3(maxX - offset, maxY - offset, 0);

        Gizmos.DrawLine(spawnBL, spawnBR);
        Gizmos.DrawLine(spawnBR, spawnTR);
        Gizmos.DrawLine(spawnTR, spawnTL);
        Gizmos.DrawLine(spawnTL, spawnBL);
    }
#endif
}
