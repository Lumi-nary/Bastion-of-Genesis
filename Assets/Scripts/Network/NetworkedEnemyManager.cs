using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NetworkedEnemyManager - Syncs enemy spawning and state across network.
/// Server-authoritative: Only server spawns and controls enemies.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedEnemyManager : NetworkBehaviour
{
    public static NetworkedEnemyManager Instance { get; private set; }

    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    [SerializeField] private float baseSpawnInterval = 60f;
    [SerializeField] private int baseWaveSize = 5;

    // Synced state (FishNet 4.x SyncVar<T>)
    private readonly SyncVar<int> _currentWave = new SyncVar<int>();
    private readonly SyncVar<int> _totalEnemiesKilled = new SyncVar<int>();
    private readonly SyncVar<int> _activeEnemyCount = new SyncVar<int>();
    private readonly SyncVar<bool> _spawningEnabled = new SyncVar<bool>();

    // Server-only tracking
    private List<Enemy> activeEnemies = new List<Enemy>();
    private float spawnTimer;

    // Events
    public event Action<int> OnWaveStarted;
    public event Action<int> OnEnemyKilled;
    public event Action OnAllEnemiesDefeated;

    // Properties
    public int CurrentWave => _currentWave.Value;
    public int TotalKilled => _totalEnemiesKilled.Value;
    public int ActiveEnemies => _activeEnemyCount.Value;
    public bool IsSpawningEnabled => _spawningEnabled.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _currentWave.OnChange += OnWaveCountChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _currentWave.OnChange -= OnWaveCountChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _currentWave.Value = 0;
        _totalEnemiesKilled.Value = 0;
        _activeEnemyCount.Value = 0;
        _spawningEnabled.Value = false;

        Debug.Log("[NetworkedEnemyManager] Server initialized");
    }

    private void Update()
    {
        if (!IsServerStarted) return;
        if (!_spawningEnabled.Value) return;

        float spawnInterval = GetAdjustedSpawnInterval();
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            ServerSpawnWave();
        }
    }

    private void OnWaveCountChanged(int prev, int next, bool asServer)
    {
        OnWaveStarted?.Invoke(next);
    }

    [Server]
    public void ServerSetSpawningEnabled(bool enabled)
    {
        _spawningEnabled.Value = enabled;
        if (enabled) spawnTimer = 0f;
        Debug.Log($"[NetworkedEnemyManager] Spawning enabled: {enabled}");
    }

    [Server]
    public void ServerSpawnWave()
    {
        if (EnemyManager.Instance == null)
        {
            Debug.LogWarning("[NetworkedEnemyManager] EnemyManager not found, cannot spawn wave");
            return;
        }

        _currentWave.Value++;
        int waveSize = GetAdjustedWaveSize();

        Debug.Log($"[NetworkedEnemyManager] Spawning wave {_currentWave.Value} with {waveSize} enemies");
        
        // Use EnemyManager logic to select enemies
        // We need to calculate pollution here or use EnemyManager's helpers
        
        float pollutionNormalized = NetworkedPollutionManager.Instance != null 
            ? NetworkedPollutionManager.Instance.PollutionPercent 
            : 0f;

        for (int i = 0; i < waveSize; i++)
        {
             EnemyData selected = EnemyManager.Instance.SelectEnemyForWave(_currentWave.Value, pollutionNormalized);
             if (selected != null)
             {
                 SpawnNetworkedEnemy(selected, GetSpawnPosition());
             }
        }
    }

    // Helper to replace SpawnEnemy
    [Server]
    public void SpawnNetworkedEnemy(EnemyData data, Vector3 position)
    {
        if (data == null || data.prefab == null) return;
        
        GameObject enemyObj = Instantiate(data.prefab, position, Quaternion.identity);
        
        // Ensure NetworkObject exists (user should have added it, but safety check)
        // If it's a prefab, it should have it. If we add it at runtime, it might not work if not in addressables/network manager list.
        // We assume the user configures the prefab correctly.
        
        InstanceFinder.ServerManager.Spawn(enemyObj);
        
        Enemy enemyComp = enemyObj.GetComponent<Enemy>();
        if (enemyComp != null)
        {
             float diff = 1f; // TODO: Get from SaveManager/Network GameSettings
             float poll = NetworkedPollutionManager.Instance?.GetEnemyStatMultiplier() ?? 1f;
             enemyComp.Initialize(data, diff, poll);

             activeEnemies.Add(enemyComp);
             enemyComp.OnEnemyDeath += ServerOnEnemyDeath;
        }
        
        _activeEnemyCount.Value = activeEnemies.Count;
    }

    [Server]
    private void ServerOnEnemyDeath(Enemy enemy)
    {
        activeEnemies.Remove(enemy);
        _activeEnemyCount.Value = activeEnemies.Count;
        _totalEnemiesKilled.Value++;

        OnEnemyKilled?.Invoke(_totalEnemiesKilled.Value);

        if (_activeEnemyCount.Value == 0 && !_spawningEnabled.Value)
        {
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    [Server]
    public void ServerKillAllEnemies()
    {
        foreach (var enemy in new List<Enemy>(activeEnemies))
        {
            if (enemy != null)
                enemy.TakeDamage(99999);
        }
        activeEnemies.Clear();
        _activeEnemyCount.Value = 0;
    }

    [Server]
    public void ServerReset()
    {
        ServerKillAllEnemies();
        _currentWave.Value = 0;
        _totalEnemiesKilled.Value = 0;
        _spawningEnabled.Value = false;
        spawnTimer = 0f;
    }

    private Vector3 GetSpawnPosition()
    {
        float mapSize = 50f;
        int edge = UnityEngine.Random.Range(0, 4);

        return edge switch
        {
            0 => new Vector3(UnityEngine.Random.Range(-mapSize, mapSize), mapSize, 0),
            1 => new Vector3(UnityEngine.Random.Range(-mapSize, mapSize), -mapSize, 0),
            2 => new Vector3(-mapSize, UnityEngine.Random.Range(-mapSize, mapSize), 0),
            3 => new Vector3(mapSize, UnityEngine.Random.Range(-mapSize, mapSize), 0),
            _ => Vector3.zero
        };
    }

    private float GetAdjustedSpawnInterval()
    {
        float multiplier = NetworkedPollutionManager.Instance?.GetSpawnRateMultiplier() ?? 1f;
        return baseSpawnInterval / multiplier;
    }

    private int GetAdjustedWaveSize()
    {
        float multiplier = NetworkedPollutionManager.Instance?.GetWaveSizeMultiplier() ?? 1f;
        return Mathf.RoundToInt(baseWaveSize * multiplier);
    }
}
