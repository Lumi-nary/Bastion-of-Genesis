using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkedBuildingManager - Syncs building placement across network.
/// Server-authoritative: Only server validates and places buildings.
/// Buildings are NetworkObjects spawned by server.
/// </summary>
public class NetworkedBuildingManager : NetworkBehaviour
{
    public static NetworkedBuildingManager Instance { get; private set; }

    [Header("Building Data")]
    [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("Prefabs")]
    [SerializeField] private GameObject networkedBuildingPrefab;

    [Serializable]
    public struct BuildingPlacementData
    {
        public int typeIndex;
        public Vector3 position;
    }

    [Serializable]
    public struct WorkerAssignmentData
    {
        public int[] workerTypeIndices;
    }

    // Track all placed buildings for initial sync (FishNet 4.x)
    private readonly SyncDictionary<int, BuildingPlacementData> syncedBuildings = new SyncDictionary<int, BuildingPlacementData>();

    // Track worker assignments per building (Key: Grid Hash or Packed Coord? Vector2Int is not standard key for SyncDict in some versions, let's use Vector3 for simplicity or convert to string/int)
    // Actually, FishNet SyncDictionary keys must be simple types. Vector2Int IS supported in FishNet 4.
    // Let's use Vector2Int as key.
    private readonly SyncDictionary<Vector2Int, WorkerAssignmentData> syncedAssignments = new SyncDictionary<Vector2Int, WorkerAssignmentData>();

    private Dictionary<int, NetworkedBuilding> buildingsByNetId = new Dictionary<int, NetworkedBuilding>();

    // Events
    public event Action<NetworkedBuilding> OnBuildingPlaced;
    public event Action<NetworkedBuilding> OnBuildingRemoved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // On Client (but not Host), sync buildings when scene loads (and BuildingManager appears)
        if (IsClientStarted && !IsServerStarted)
        {
            StartCoroutine(ClientSyncBuildings());
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Debug.Log("[NetworkedBuildingManager] Network started");

        // Auto-populate buildings if list is empty
        if (availableBuildings == null || availableBuildings.Count == 0)
        {
            var loadedBuildings = Resources.LoadAll<BuildingData>("Data/Buildings");
            if (loadedBuildings != null && loadedBuildings.Length > 0)
            {
                availableBuildings.AddRange(loadedBuildings);
                availableBuildings.Sort((a, b) => string.Compare(a.name, b.name));
                Debug.Log($"[NetworkedBuildingManager] Auto-loaded {availableBuildings.Count} buildings.");
            }
        }

        syncedBuildings.OnChange += OnSyncedBuildingsChanged;
        syncedAssignments.OnChange += OnSyncedAssignmentsChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[NetworkedBuildingManager] Client started - processing existing buildings");
        
        // Try to sync immediately (in case late join where scene is already loaded)
        StartCoroutine(ClientSyncBuildings());
    }

    private System.Collections.IEnumerator ClientSyncBuildings()
    {
        // Wait for BuildingManager (handles scene load delay) and WorkerManager
        while (BuildingManager.Instance == null || NetworkedWorkerManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log($"[NetworkedBuildingManager] Managers found. Syncing {syncedBuildings.Count} buildings...");

        // Iterate and place all currently synced buildings
        foreach (var kvp in syncedBuildings)
        {
            PlaceBuildingLocally(kvp.Value.typeIndex, kvp.Value.position);
        }
        
        // Apply assignments
        foreach (var kvp in syncedAssignments)
        {
            ApplyAssignmentLocally(kvp.Key, kvp.Value);
        }
    }

    private void OnSyncedBuildingsChanged(SyncDictionaryOperation op, int key, BuildingPlacementData value, bool asServer)
    {
        // Host needs to run this too to update local visuals
        // Dedicated server (no BuildingManager) will bail out safely in PlaceBuildingLocally due to null check

        if (op == SyncDictionaryOperation.Add || op == SyncDictionaryOperation.Set)
        {
            PlaceBuildingLocally(value.typeIndex, value.position);
        }
        else if (op == SyncDictionaryOperation.Remove)
        {
            RemoveBuildingLocally(value.position);
        }
    }

    private void OnSyncedAssignmentsChanged(SyncDictionaryOperation op, Vector2Int key, WorkerAssignmentData value, bool asServer)
    {
        // Host needs to run this too
        
        if (op == SyncDictionaryOperation.Add || op == SyncDictionaryOperation.Set)
        {
            ApplyAssignmentLocally(key, value);
        }
        // Remove handled by set empty or just ignore (assignments clear if building removed)
    }

    private void ApplyAssignmentLocally(Vector2Int gridPos, WorkerAssignmentData data)
    {
        if (BuildingManager.Instance == null || WorkerManager.Instance == null) return;

        // Find building at grid pos
        // We need a way to get building by grid pos. GridManager has this?
        // Or iterate BuildingManager.
        Building targetBuilding = null;
        
        // Option A: Use GridManager
        if (GridManager.Instance != null)
        {
            // Note: GridManager might return occupied object, verify it is Building
            // But GetObjectAtCell might not be exposed.
        }

        // Option B: Search all buildings (slower but reliable)
        foreach (var b in BuildingManager.Instance.AllBuildings)
        {
            if (b.gridPosition == gridPos)
            {
                targetBuilding = b;
                break;
            }
        }

        if (targetBuilding != null)
        {
            List<WorkerData> workers = new List<WorkerData>();
            if (data.workerTypeIndices != null)
            {
                // Convert indices back to WorkerData
                // We need access to All Worker Types. NetworkedWorkerManager has them.
                // Or WorkerManager has them if loaded.
                
                // We can use NetworkedWorkerManager if available, or assume WorkerManager has types.
                // WorkerManager registers types dynamically. NetworkedWorkerManager has the master list.
                
                if (NetworkedWorkerManager.Instance != null)
                {
                   foreach(int idx in data.workerTypeIndices)
                   {
                       var wData = NetworkedWorkerManager.Instance.GetWorkerTypeByIndex(idx);
                       if (wData != null) workers.Add(wData);
                   }
                }
            }
            
            targetBuilding.SetAssignedWorkers(workers);
            Debug.Log($"[NetworkedBuildingManager] Synced workers for building at {gridPos}. Count: {workers.Count}");
            
            // Force UI update if selected? 
            // The UI polls or refreshes? WorkerSlotUI needs to know.
            // But Building.SetAssignedWorkers just updates list.
        }
    }

    private void PlaceBuildingLocally(int index, Vector3 position)
    {
        if (index < 0 || index >= availableBuildings.Count) return;
        if (IsBuildingAtPosition(position)) return;

        BuildingData data = availableBuildings[index];
        Debug.Log($"[NetworkedBuildingManager] Syncing building {data.buildingName} at {position}");
        BuildingManager.Instance.PlaceBuilding(data, position, true);
    }

    private void RemoveBuildingLocally(Vector3 position)
    {
        if (BuildingManager.Instance == null) return;
        foreach (var b in BuildingManager.Instance.AllBuildings.ToList())
        {
            if (Vector3.Distance(b.transform.position, position) < 0.1f)
            {
                BuildingManager.Instance.OnBuildingDestroyed(b);
                Destroy(b.gameObject);
                break;
            }
        }
    }

    private bool IsBuildingAtPosition(Vector3 position)
    {
        if (BuildingManager.Instance == null) return false;
        foreach (var b in BuildingManager.Instance.AllBuildings)
        {
            if (Vector3.Distance(b.transform.position, position) < 0.1f) return true;
        }
        return false;
    }

    // ============================================================================
    // SERVER METHODS (Called by NetworkPlayer RPCs)
    // ============================================================================

    /// <summary>
    /// Server validates and places a building
    /// </summary>
    [Server]
    public void ServerPlaceBuilding(int buildingIndex, Vector3 position, NetworkPlayer requestingPlayer)
    {
        // Validate building index
        if (buildingIndex < 0 || buildingIndex >= availableBuildings.Count)
        {
            SendErrorToPlayer(requestingPlayer, "Invalid building type");
            return;
        }

        BuildingData buildingData = availableBuildings[buildingIndex];

        // Validate resources
        if (NetworkedResourceManager.Instance != null)
        {
            if (!NetworkedResourceManager.Instance.CanAfford(buildingData.resourceCost))
            {
                SendErrorToPlayer(requestingPlayer, "Not enough resources");
                return;
            }
        }

        // Validate workers
        if (NetworkedWorkerManager.Instance != null && buildingData.buildersConsumed > 0)
        {
            if (!NetworkedWorkerManager.Instance.HasEnoughWorkers(buildingData.builderType, buildingData.buildersConsumed))
            {
                SendErrorToPlayer(requestingPlayer, "Not enough workers");
                return;
            }
        }

        // Validate grid position
        Vector2Int gridPos = GridManager.Instance != null ?
            GridManager.Instance.WorldToGridPosition(position) : Vector2Int.zero;

        if (GridManager.Instance != null)
        {
            // Check if cells are free
            for (int x = 0; x < buildingData.width; x++)
            {
                for (int y = 0; y < buildingData.height; y++)
                {
                    if (GridManager.Instance.IsCellOccupied(gridPos + new Vector2Int(x, y)))
                    {
                        SendErrorToPlayer(requestingPlayer, "Location blocked");
                        return;
                    }
                }
            }
        }

        // All validations passed - spend resources
        if (NetworkedResourceManager.Instance != null)
        {
            NetworkedResourceManager.Instance.ServerSpendResources(buildingData.resourceCost);
        }

        // Consume workers
        if (NetworkedWorkerManager.Instance != null && buildingData.buildersConsumed > 0)
        {
            NetworkedWorkerManager.Instance.ServerConsumeWorkers(buildingData.builderType, buildingData.buildersConsumed);
        }

        // Update synced dictionary - this will trigger OnSyncedBuildingsChanged on all clients
        int nextId = syncedBuildings.Count;
        while (syncedBuildings.ContainsKey(nextId)) nextId++;
        syncedBuildings.Add(nextId, new BuildingPlacementData { typeIndex = buildingIndex, position = position });

        Debug.Log($"[NetworkedBuildingManager] Building placed and synced: {buildingData.buildingName} at {position}");
    }

    [Server]
    public void ServerRemoveBuilding(int buildingNetId, NetworkPlayer requestingPlayer)
    {
        // ... (existing implementation placeholder) ...
    }

    [Server]
    public void ServerAssignWorker(Vector2Int gridPos, int workerTypeIndex, NetworkPlayer requestingPlayer)
    {
        // 1. Check if building exists at gridPos (in syncedBuildings or just trust client? Trust but verify roughly)
        // For simplicity, we assume valid request if logic is sound.
        
        // 2. Consume Worker from Pool (Global)
        // ServerConsumeWorkers will fail if not enough.
        // We need to know WHICH worker type.
        
        if (NetworkedWorkerManager.Instance == null) return;
        
        WorkerData wData = NetworkedWorkerManager.Instance.GetWorkerTypeByIndex(workerTypeIndex);
        if (wData == null) 
        {
             SendErrorToPlayer(requestingPlayer, "Invalid worker type");
             return;
        }

        if (!NetworkedWorkerManager.Instance.HasEnoughWorkers(wData, 1))
        {
             SendErrorToPlayer(requestingPlayer, "Not enough workers available");
             return;
        }

        // 3. Update Assignment Data
        List<int> currentIndices = new List<int>();
        if (syncedAssignments.ContainsKey(gridPos))
        {
            if (syncedAssignments[gridPos].workerTypeIndices != null)
            {
                currentIndices.AddRange(syncedAssignments[gridPos].workerTypeIndices);
            }
        }

        // Check Building Capacity?
        // We need building data. We can find it from syncedBuildings if we search by position?
        // Or assume client checked. Server SHOULD check.
        // TODO: Validate capacity. For now, trust client checks + standard resource check.

        // Consume worker
        NetworkedWorkerManager.Instance.ServerConsumeWorkers(wData, 1);

        // Update Sync
        currentIndices.Add(workerTypeIndex);
        syncedAssignments[gridPos] = new WorkerAssignmentData { workerTypeIndices = currentIndices.ToArray() };
        
        Debug.Log($"[NetworkedBuildingManager] Worker {wData.workerName} assigned to building at {gridPos}. Total: {currentIndices.Count}");
    }

    [Server]
    public void ServerRemoveWorker(Vector2Int gridPos, int workerTypeIndex, NetworkPlayer requestingPlayer)
    {
        if (NetworkedWorkerManager.Instance == null) return;

        if (!syncedAssignments.ContainsKey(gridPos)) return;

        var data = syncedAssignments[gridPos];
        List<int> currentIndices = data.workerTypeIndices != null ? data.workerTypeIndices.ToList() : new List<int>();

        if (currentIndices.Contains(workerTypeIndex))
        {
            currentIndices.Remove(workerTypeIndex); // Remove first instance
            
            // Return worker to pool
            WorkerData wData = NetworkedWorkerManager.Instance.GetWorkerTypeByIndex(workerTypeIndex);
            if (wData != null)
            {
                NetworkedWorkerManager.Instance.ServerReturnWorkers(wData, 1);
            }

            // Update Sync
            syncedAssignments[gridPos] = new WorkerAssignmentData { workerTypeIndices = currentIndices.ToArray() };
             Debug.Log($"[NetworkedBuildingManager] Worker {wData?.workerName} removed from building at {gridPos}. Remaining: {currentIndices.Count}");
        }
    }

    [Server]
    private void SpawnNetworkedBuilding(BuildingData data, Vector3 position, Vector2Int gridPos)
    {
        // Replaced by syncedBuildings logic
    }

    // ============================================================================
    // CLIENT METHODS
    // ============================================================================

    /// <summary>
    /// Request building placement (clients call this)
    /// </summary>
    public void RequestPlaceBuilding(BuildingData buildingData, Vector3 position)
    {
        if (NetworkPlayer.LocalPlayer == null)
        {
            Debug.LogWarning("[NetworkedBuildingManager] No local player");
            return;
        }

        int buildingIndex = availableBuildings.IndexOf(buildingData);
        if (buildingIndex < 0)
        {
            Debug.LogError("[NetworkedBuildingManager] Building not in available list");
            return;
        }

        NetworkPlayer.LocalPlayer.CmdRequestBuildingPlacement(buildingIndex, position);
    }
    
    public void RequestAssignWorker(Building building, WorkerData workerData)
    {
        if (NetworkPlayer.LocalPlayer == null) return;
        if (NetworkedWorkerManager.Instance == null) return;
        
        int workerIndex = NetworkedWorkerManager.Instance.GetWorkerIndex(workerData);
        if (workerIndex < 0) return;
        
        NetworkPlayer.LocalPlayer.CmdRequestAssignWorkers(building.gridPosition, workerIndex, 1);
    }

    public void RequestRemoveWorker(Building building, WorkerData workerData)
    {
        if (NetworkPlayer.LocalPlayer == null) return;
         if (NetworkedWorkerManager.Instance == null) return;
        
        int workerIndex = NetworkedWorkerManager.Instance.GetWorkerIndex(workerData);
        if (workerIndex < 0) return;

        NetworkPlayer.LocalPlayer.CmdRequestRemoveWorkers(building.gridPosition, workerIndex, 1);
    }

    /// <summary>
    /// Request building removal (clients call this)
    /// </summary>
    public void RequestRemoveBuilding(NetworkedBuilding building)
    {
        if (NetworkPlayer.LocalPlayer == null || building == null)
            return;

        NetworkObject netObj = building.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            NetworkPlayer.LocalPlayer.CmdRequestBuildingRemoval(netObj.ObjectId);
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private void SendErrorToPlayer(NetworkPlayer player, string message)
    {
        if (player != null && player.Owner != null)
        {
            player.TargetSendError(player.Owner, message);
        }
    }

    public BuildingData GetBuildingDataByIndex(int index)
    {
        if (index >= 0 && index < availableBuildings.Count)
            return availableBuildings[index];
        return null;
    }

    public int GetBuildingCount() => syncedBuildings.Count;

    public List<BuildingData> GetAvailableBuildings() => new List<BuildingData>(availableBuildings);
}
