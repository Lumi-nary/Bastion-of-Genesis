using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// NetworkBehaviour for all co-op multiplayer state sync (RPCs).
/// Spawned at runtime by CoopBootstrap when the server starts.
/// Use CoopBootstrap.Instance for server control and connection state.
/// Use CoopManager.Instance for RPCs (only available during active multiplayer).
/// </summary>
public class CoopManager : NetworkBehaviour
{
    public static CoopManager Instance { get; private set; }

    // Convenience accessors (delegate to CoopBootstrap)
    public bool IsOnline => CoopBootstrap.Instance != null && CoopBootstrap.Instance.IsOnline;
    public new bool IsHost => CoopBootstrap.Instance != null && CoopBootstrap.Instance.IsHost;
    public new bool IsClientOnly => CoopBootstrap.Instance != null && CoopBootstrap.Instance.IsClientOnly;
    public new bool IsServer => CoopBootstrap.Instance != null && CoopBootstrap.Instance.IsServer;

    // ========================================================================
    // ENEMY TRACKING
    // ========================================================================

    private int nextEnemyId = 1;
    private readonly Dictionary<int, Enemy> networkEnemies = new();
    private readonly Dictionary<Enemy, int> enemyToId = new();

    // Research progress throttle
    private float lastResearchProgressSync;
    private const float RESEARCH_SYNC_INTERVAL = 0.5f; // ~2 Hz

    // ========================================================================
    // LIFECYCLE
    // ========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        UnsubscribeFromManagers();
        if (Instance == this) Instance = null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        InstanceFinder.ServerManager.OnRemoteConnectionState += OnRemoteConnectionStateChanged;

        SubscribeToManagers();
        StartCoroutine(EnemyPositionSyncLoop());

        // Register current scene as global so clients auto-load into it
        if (CoopSceneLoader.Instance != null)
            CoopSceneLoader.Instance.RegisterCurrentScene();

        Debug.Log("[CoopManager] Server started - syncing enabled");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;

        UnsubscribeFromManagers();
        StopAllCoroutines();
        networkEnemies.Clear();
        enemyToId.Clear();

        Debug.Log("[CoopManager] Server stopped - syncing disabled");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsServerStarted)
        {
            Debug.Log("[CoopManager] Client connected - awaiting full state sync");
        }
    }

    // ========================================================================
    // CONNECTION HANDLING (client join → full state sync)
    // ========================================================================

    /// <summary>
    /// Fires on the server when this object is being spawned to a specific client.
    /// At this point the client IS an observer, so TargetRpc is safe to send.
    /// </summary>
    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);

        // Broadcast updated player count to all clients
        BroadcastPlayerCount();

        // Skip host's own client (it's the server too)
        if (connection == InstanceFinder.ClientManager.Connection) return;

        // Send full state after a short delay so client scene fully loads
        StartCoroutine(SendFullStateDelayed(connection));
    }

    private void OnRemoteConnectionStateChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!IsServerStarted) return;
        // Broadcast player count whenever a connection state changes
        BroadcastPlayerCount();
    }

    private void BroadcastPlayerCount()
    {
        if (!IsServerStarted) return;
        int count = InstanceFinder.ServerManager.Clients.Count;
        RpcSyncPlayerCount(count);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcSyncPlayerCount(int count)
    {
        if (CoopBootstrap.Instance != null)
            CoopBootstrap.Instance.SetClientPlayerCount(count);
    }

    // ========================================================================
    // FULL STATE SYNC (client join)
    // ========================================================================

    private IEnumerator SendFullStateDelayed(NetworkConnection conn)
    {
        // Wait for client scene to load
        yield return new WaitForSeconds(1f);

        if (!conn.IsActive) yield break;

        SaveData snapshot = CollectCurrentState();
        string json = JsonUtility.ToJson(snapshot);

        RpcReceiveFullState(conn, json);
        Debug.Log($"[CoopManager] Full state sent to client ({json.Length} chars)");
    }

    private SaveData CollectCurrentState()
    {
        SaveData data = new SaveData();

        if (ResourceManager.Instance != null)
            data.resources = ResourceManager.Instance.ExportState();
        if (WorkerManager.Instance != null)
            data.workers = WorkerManager.Instance.ExportState();
        if (BuildingManager.Instance != null)
            data.buildings = BuildingManager.Instance.ExportState();
        if (ResearchManager.Instance != null)
            data.research = ResearchManager.Instance.ExportState();
        if (PollutionManager.Instance != null)
            data.pollution = PollutionManager.Instance.ExportState();
        if (MissionChapterManager.Instance != null)
            data.mission = MissionChapterManager.Instance.ExportState();
        if (GridManager.Instance != null)
            data.oreMounds = GridManager.Instance.ExportOreMoundState();
        if (EnemyManager.Instance != null)
            data.enemies = EnemyManager.Instance.ExportState();

        return data;
    }

    [TargetRpc]
    private void RpcReceiveFullState(NetworkConnection conn, string json)
    {
        Debug.Log($"[CoopManager] Received full state ({json.Length} chars)");

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogError("[CoopManager] Failed to deserialize full state");
            return;
        }

        // Import in dependency order (same as SaveManager.RestoreStateFromSave)
        if (ResourceManager.Instance != null && data.resources != null)
            ResourceManager.Instance.ImportState(data.resources);
        if (WorkerManager.Instance != null && data.workers != null)
            WorkerManager.Instance.ImportState(data.workers);
        if (ResearchManager.Instance != null && data.research != null)
            ResearchManager.Instance.ImportState(data.research);
        if (BuildingManager.Instance != null && data.buildings != null)
            BuildingManager.Instance.ImportState(data.buildings);
        if (PollutionManager.Instance != null && data.pollution != null)
            PollutionManager.Instance.ImportState(data.pollution);
        if (MissionChapterManager.Instance != null && data.mission != null)
            MissionChapterManager.Instance.ImportState(data.mission);
        if (GridManager.Instance != null && data.oreMounds != null)
            GridManager.Instance.ImportOreMoundState(data.oreMounds);
        if (EnemyManager.Instance != null && data.enemies != null)
            EnemyManager.Instance.ImportState(data.enemies);
        if (EnergyManager.Instance != null)
            EnergyManager.Instance.ForceUpdate();

        // Force UI refresh — ImportState doesn't fire events that UI listens to
        if (MissionChapterManager.Instance != null)
            MissionChapterManager.Instance.NotifyUIRefresh();

        // Re-resolve Command Center reference since BuildingManager.ImportState
        // destroys and recreates all buildings (including the scene-placed one).
        if (TileStateManager.Instance != null)
            TileStateManager.Instance.RefreshPollutionCenter();

        // Refresh tile visuals with synced pollution state
        if (PollutionManager.Instance != null)
            PollutionManager.Instance.SetPollution(PollutionManager.Instance.CurrentPollution);

        Debug.Log("[CoopManager] Full state imported successfully");
    }

    // ========================================================================
    // MANAGER EVENT SUBSCRIPTIONS (server-side)
    // ========================================================================

    private void SubscribeToManagers()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
        if (WorkerManager.Instance != null)
            WorkerManager.Instance.OnWorkerCountChanged += OnWorkerCountChanged;
        if (PollutionManager.Instance != null)
            PollutionManager.Instance.OnPollutionChanged += OnPollutionChanged;
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced += OnBuildingPlaced;
            BuildingManager.Instance.OnBuildingDestroyedEvent += OnBuildingDestroyed;
        }
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemySpawned += OnEnemySpawned;
            EnemyManager.Instance.OnEnemyKilledEvent += OnEnemyKilled;
        }
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnTechResearched += OnTechResearched;
            ResearchManager.Instance.OnResearchProgress += OnResearchProgress;
        }
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted += OnMissionStarted;
            MissionChapterManager.Instance.OnMissionCompleted += OnMissionCompleted;
            MissionChapterManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
            MissionChapterManager.Instance.OnChapterStarted += OnChapterStarted;
        }
    }

    private void UnsubscribeFromManagers()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
        if (WorkerManager.Instance != null)
            WorkerManager.Instance.OnWorkerCountChanged -= OnWorkerCountChanged;
        if (PollutionManager.Instance != null)
            PollutionManager.Instance.OnPollutionChanged -= OnPollutionChanged;
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced -= OnBuildingPlaced;
            BuildingManager.Instance.OnBuildingDestroyedEvent -= OnBuildingDestroyed;
        }
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemySpawned -= OnEnemySpawned;
            EnemyManager.Instance.OnEnemyKilledEvent -= OnEnemyKilled;
        }
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnTechResearched -= OnTechResearched;
            ResearchManager.Instance.OnResearchProgress -= OnResearchProgress;
        }
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted -= OnMissionStarted;
            MissionChapterManager.Instance.OnMissionCompleted -= OnMissionCompleted;
            MissionChapterManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
            MissionChapterManager.Instance.OnChapterStarted -= OnChapterStarted;
        }
    }

    // ========================================================================
    // INCREMENTAL SYNC — Resources
    // ========================================================================

    private void OnResourceChanged(ResourceType type, int amount)
    {
        if (!IsServerStarted) return;
        int capacity = ResourceManager.Instance.GetResourceCapacity(type);
        RpcSyncResource(type.ResourceName, amount, capacity);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncResource(string resourceName, int amount, int capacity)
    {
        var rt = ScriptableObjectResolver.ResolveResource(resourceName);
        if (rt == null) return;
        ResourceManager.Instance?.SetResourceAmount(rt, amount);
        ResourceManager.Instance?.SetCapacity(rt, capacity);
    }

    // ========================================================================
    // INCREMENTAL SYNC — Workers
    // ========================================================================

    private void OnWorkerCountChanged(WorkerData worker, int count)
    {
        if (!IsServerStarted) return;
        RpcSyncWorker(worker.workerName, count);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncWorker(string workerName, int count)
    {
        var wd = ScriptableObjectResolver.ResolveWorker(workerName);
        if (wd == null) return;
        WorkerManager.Instance?.SetWorkerCount(wd, count);
    }

    // ========================================================================
    // INCREMENTAL SYNC — Pollution
    // ========================================================================

    private void OnPollutionChanged(float current, float max)
    {
        if (!IsServerStarted) return;
        RpcSyncPollution(current, max);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncPollution(float current, float max)
    {
        PollutionManager.Instance?.SetPollution(current);
    }

    // ========================================================================
    // INCREMENTAL SYNC — Buildings
    // ========================================================================

    private void OnBuildingPlaced(Building building)
    {
        if (!IsServerStarted) return;
        RpcSyncBuildingPlaced(building.BuildingData.buildingName, building.transform.position);
    }

    private void OnBuildingDestroyed(Building building)
    {
        if (!IsServerStarted) return;
        RpcSyncBuildingDestroyed(new Vector3(building.gridPosition.x, building.gridPosition.y, 0));
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncBuildingPlaced(string buildingName, Vector3 worldPos)
    {
        var bd = ScriptableObjectResolver.ResolveBuilding(buildingName);
        if (bd == null || BuildingManager.Instance == null) return;
        BuildingManager.Instance.PlaceBuilding(bd, worldPos, true); // ignoreCost = true on clients
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncBuildingDestroyed(Vector3 gridPos)
    {
        var building = FindBuildingAtGrid(new Vector2Int((int)gridPos.x, (int)gridPos.y));
        if (building != null)
        {
            building.TakeDamage(float.MaxValue); // Force destroy
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncBuildingHealth(Vector3 gridPos, float health)
    {
        var building = FindBuildingAtGrid(new Vector2Int((int)gridPos.x, (int)gridPos.y));
        if (building != null)
        {
            building.SetHealth(health);
        }
    }

    // ========================================================================
    // INCREMENTAL SYNC — Enemies
    // ========================================================================

    private void OnEnemySpawned(Enemy enemy)
    {
        if (!IsServerStarted) return;

        int id = nextEnemyId++;
        networkEnemies[id] = enemy;
        enemyToId[enemy] = id;
        enemy.NetworkId = id;

        RpcSyncEnemySpawn(id, enemy.Data.enemyName, enemy.transform.position);
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        if (!IsServerStarted) return;

        if (enemyToId.TryGetValue(enemy, out int id))
        {
            RpcSyncEnemyDeath(id);
            networkEnemies.Remove(id);
            enemyToId.Remove(enemy);
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncEnemySpawn(int enemyId, string enemyName, Vector3 pos)
    {
        var ed = ScriptableObjectResolver.ResolveEnemy(enemyName);
        if (ed == null || EnemyManager.Instance == null) return;

        Enemy enemy = EnemyManager.Instance.SpawnEnemy(ed, pos);
        if (enemy != null)
        {
            enemy.NetworkId = enemyId;
            networkEnemies[enemyId] = enemy;
            enemyToId[enemy] = enemyId;
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncEnemyDeath(int enemyId)
    {
        if (networkEnemies.TryGetValue(enemyId, out Enemy enemy))
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.ForceSetHealth(0);
            }
            networkEnemies.Remove(enemyId);
            enemyToId.Remove(enemy);
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncEnemyPositions(int[] ids, Vector3[] positions)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (networkEnemies.TryGetValue(ids[i], out Enemy enemy) && enemy != null)
            {
                enemy.ForceSetPosition(positions[i]);
            }
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncEnemyHealth(int enemyId, float health)
    {
        if (networkEnemies.TryGetValue(enemyId, out Enemy enemy) && enemy != null)
        {
            enemy.ForceSetHealth(health);
        }
    }

    private IEnumerator EnemyPositionSyncLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f); // ~5 Hz
        while (true)
        {
            yield return wait;
            if (!IsServerStarted) yield break;

            // Clean up dead/null references
            var deadKeys = networkEnemies.Where(kvp => kvp.Value == null || kvp.Value.IsDead)
                                         .Select(kvp => kvp.Key).ToList();
            foreach (int key in deadKeys)
            {
                if (networkEnemies.TryGetValue(key, out Enemy e) && e != null)
                    enemyToId.Remove(e);
                networkEnemies.Remove(key);
            }

            if (networkEnemies.Count == 0) continue;

            int[] ids = new int[networkEnemies.Count];
            Vector3[] positions = new Vector3[networkEnemies.Count];
            int idx = 0;
            foreach (var kvp in networkEnemies)
            {
                ids[idx] = kvp.Key;
                positions[idx] = kvp.Value.transform.position;
                idx++;
            }

            RpcSyncEnemyPositions(ids, positions);
        }
    }

    // ========================================================================
    // INCREMENTAL SYNC — Research
    // ========================================================================

    private void OnTechResearched(TechnologyData tech)
    {
        if (!IsServerStarted) return;
        RpcSyncResearchCompleted(tech.techName);
    }

    private void OnResearchProgress(TechnologyData tech, float progress)
    {
        if (!IsServerStarted) return;

        // Throttle progress updates
        if (Time.time - lastResearchProgressSync < RESEARCH_SYNC_INTERVAL) return;
        lastResearchProgressSync = Time.time;

        RpcSyncResearchProgress(tech.techName, progress);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncResearchCompleted(string techName)
    {
        // Full state re-sync for research is simplest — avoids partial application issues
        if (ResearchManager.Instance == null) return;

        var tech = ScriptableObjectResolver.ResolveTechnology(techName);
        if (tech != null && !tech.IsResearched)
        {
            // Apply the completed tech directly
            ResearchManager.Instance.ForceCompleteTech(tech);
        }
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncResearchStarted(string techName)
    {
        // Informational for client UI — the actual research runs on server
        Debug.Log($"[CoopManager] Host started researching: {techName}");
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncResearchProgress(string techName, float progress)
    {
        // Update client-side progress display
        // ResearchManager on client can track this for UI without running the actual research
        Debug.Log($"[CoopManager] Research progress: {techName} = {progress:P0}");
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncResearchCanceled()
    {
        Debug.Log("[CoopManager] Host canceled research");
    }

    // ========================================================================
    // INCREMENTAL SYNC — Missions
    // ========================================================================

    private void OnMissionStarted(MissionData mission)
    {
        if (!IsServerStarted || MissionChapterManager.Instance == null) return;
        int index = MissionChapterManager.Instance.CurrentMissionIndex;
        RpcSyncMissionStarted(index);
    }

    private void OnMissionCompleted(MissionData mission)
    {
        if (!IsServerStarted || MissionChapterManager.Instance == null) return;
        int index = MissionChapterManager.Instance.CurrentMissionIndex;
        RpcSyncMissionCompleted(index);
    }

    private void OnObjectiveCompleted(MissionObjective objective)
    {
        if (!IsServerStarted) return;
        // Re-sync full mission state for simplicity
        if (MissionChapterManager.Instance != null)
        {
            var missionData = MissionChapterManager.Instance.ExportState();
            string json = JsonUtility.ToJson(missionData);
            RpcSyncMissionState(json);
        }
    }

    private void OnChapterStarted(ChapterData chapter)
    {
        if (!IsServerStarted) return;
        int index = MissionChapterManager.Instance?.CurrentChapterIndex ?? 0;
        RpcSyncChapterStarted(index);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncMissionStarted(int missionIndex)
    {
        Debug.Log($"[CoopManager] Mission started: index {missionIndex}");
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncMissionCompleted(int missionIndex)
    {
        Debug.Log($"[CoopManager] Mission completed: index {missionIndex}");
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncMissionState(string missionJson)
    {
        if (MissionChapterManager.Instance == null) return;
        var data = JsonUtility.FromJson<MissionSaveData>(missionJson);
        if (data != null)
            MissionChapterManager.Instance.ImportState(data);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void RpcSyncChapterStarted(int chapterIndex)
    {
        Debug.Log($"[CoopManager] Chapter started: index {chapterIndex}");
        // Scene loading is handled by FishNet's scene management
    }

    // ========================================================================
    // CLIENT → SERVER COMMANDS
    // ========================================================================

    [ServerRpc(RequireOwnership = false)]
    public void CmdPlaceBuilding(string buildingName, Vector3 worldPos)
    {
        var bd = ScriptableObjectResolver.ResolveBuilding(buildingName);
        if (bd == null || BuildingManager.Instance == null) return;

        BuildingManager.Instance.PlaceBuilding(bd, worldPos);
        // OnBuildingPlaced event will broadcast to all clients
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdAssignWorker(Vector3 buildingGridPos, string workerName)
    {
        var building = FindBuildingAtGrid(new Vector2Int((int)buildingGridPos.x, (int)buildingGridPos.y));
        var wd = ScriptableObjectResolver.ResolveWorker(workerName);
        if (building == null || wd == null) return;

        building.AssignWorker(wd);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdRemoveWorker(Vector3 buildingGridPos, string workerName)
    {
        var building = FindBuildingAtGrid(new Vector2Int((int)buildingGridPos.x, (int)buildingGridPos.y));
        var wd = ScriptableObjectResolver.ResolveWorker(workerName);
        if (building == null || wd == null) return;

        building.RemoveWorker(wd);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdStartResearch(string techName)
    {
        if (ResearchManager.Instance == null) return;

        var tech = ScriptableObjectResolver.ResolveTechnology(techName);
        if (tech != null)
        {
            ResearchManager.Instance.StartResearch(tech);
            RpcSyncResearchStarted(techName);
        }
    }

    // ========================================================================
    // UTILITY
    // ========================================================================

    private Building FindBuildingAtGrid(Vector2Int gridPos)
    {
        if (BuildingManager.Instance == null) return null;

        foreach (var b in BuildingManager.Instance.AllBuildings)
        {
            if (b.gridPosition == gridPos) return b;
        }
        return null;
    }
}
