using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkedWorkerManager - Syncs worker state across network.
/// Server-authoritative: Only server can train/assign workers.
/// Bridge pattern: Syncs local WorkerManager (Host) to network.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedWorkerManager : NetworkBehaviour
{
    public static NetworkedWorkerManager Instance { get; private set; }

    [Header("Worker Types")]
    [SerializeField] private List<WorkerData> workerTypes = new List<WorkerData>();

    // Synced worker counts (FishNet 4.x - no attribute needed)
    private readonly SyncDictionary<int, int> syncedAvailable = new SyncDictionary<int, int>();

    // Synced worker capacities (FishNet 4.x - no attribute needed)
    private readonly SyncDictionary<int, int> syncedCapacities = new SyncDictionary<int, int>();

    // Events
    public event Action<WorkerData, int> OnWorkerCountChanged;
    public event Action<WorkerData, int> OnCapacityChanged;

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
        // On Client (but not Host, since Host uses local events), request sync when scene loads
        if (IsClientStarted && !IsServerStarted)
        {
            StartCoroutine(ClientWaitForWorkerManager());
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        syncedAvailable.OnChange += OnSyncedAvailableChanged;
        syncedCapacities.OnChange += OnSyncedCapacitiesChanged;

        // Start debug sync log
        StartCoroutine(SyncDebugLoop());

        Debug.Log("[NetworkedWorkerManager] Network started");
    }

    private System.Collections.IEnumerator SyncDebugLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (IsServerStarted || IsClientStarted)
            {
                string state = IsServerStarted ? "SERVER" : "CLIENT";
                string details = string.Join(", ", syncedAvailable.Select(kvp => {
                    var type = GetWorkerTypeByIndex(kvp.Key);
                    return $"{(type != null ? type.workerName : kvp.Key.ToString())}: {kvp.Value}";
                }));
                Debug.Log($"[NetworkedWorkerManager] {state} SYNC STATUS: {details}");
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        EnsureWorkerTypesLoaded();
        
        // Start coroutine to wait for WorkerManager (handles race condition)
        StartCoroutine(SubscribeToWorkerManager());

        // Also re-sync whenever a new chapter starts
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnChapterStarted += (chapter) => {
                Debug.Log($"[NetworkedWorkerManager] Chapter {chapter.chapterName} started. Clearing and performing full worker sync.");
                
                // Clear existing state to force all subsequent updates to be seen as "changes"
                syncedAvailable.Clear();
                syncedCapacities.Clear();
                
                InitializeServerStateFromLocal();
            };
        }

        Debug.Log("[NetworkedWorkerManager] Server started. Waiting for WorkerManager...");
    }

    private System.Collections.IEnumerator SubscribeToWorkerManager()
    {
        while (WorkerManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log("[NetworkedWorkerManager] WorkerManager found. Subscribing and Initializing...");

        // Unsubscribe first to avoid duplicates if this runs multiple times (safety)
        WorkerManager.Instance.OnWorkerCountChanged -= OnLocalWorkerChanged;
        WorkerManager.Instance.OnWorkerCountChanged += OnLocalWorkerChanged;

        // Initialize from local WorkerManager (Host state)
        InitializeServerStateFromLocal();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.OnWorkerCountChanged -= OnLocalWorkerChanged;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[NetworkedWorkerManager] Client started network");
        EnsureWorkerTypesLoaded();
        
        // Wait for WorkerManager then sync
        StartCoroutine(ClientWaitForWorkerManager());
    }

    private System.Collections.IEnumerator ClientWaitForWorkerManager()
    {
        // Wait for a valid WorkerManager (handles scene transitions and race conditions)
        while (WorkerManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log("[NetworkedWorkerManager] Client found WorkerManager. Requesting Full Sync...");
        RequestFullSyncServerRpc();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        syncedAvailable.OnChange -= OnSyncedAvailableChanged;
        syncedCapacities.OnChange -= OnSyncedCapacitiesChanged;
    }

    private void EnsureWorkerTypesLoaded()
    {
        // Fallback: Load from Resources if list is empty
        if (workerTypes == null || workerTypes.Count == 0)
        {
            var loaded = Resources.LoadAll<WorkerData>("Data/Workers");
            if (loaded != null && loaded.Length > 0)
            {
                workerTypes.AddRange(loaded);
            }
        }

        // Sort by name for deterministic indexing
        workerTypes.Sort((a, b) => string.Compare(a.workerName, b.workerName, StringComparison.Ordinal));
        
        string names = string.Join(", ", workerTypes.ConvertAll(w => w.workerName));
        Debug.Log($"[NetworkedWorkerManager] Loaded and Sorted {workerTypes.Count} worker types: {names}");
    }

    private void InitializeServerStateFromLocal()
    {
        if (WorkerManager.Instance == null) return;

        Debug.Log($"[NetworkedWorkerManager] Syncing {WorkerManager.Instance.AvailableWorkers.Count} local worker types to network.");

        foreach (var entry in WorkerManager.Instance.AvailableWorkers)
        {
            WorkerData type = entry.Key;
            int index = GetWorkerIndex(type);
            
            if (index >= 0)
            {
                int amount = entry.Value;
                int cap = WorkerManager.Instance.GetWorkerCapacity(type);

                if (!syncedAvailable.ContainsKey(index)) syncedAvailable.Add(index, amount);
                else syncedAvailable[index] = amount;

                if (!syncedCapacities.ContainsKey(index)) syncedCapacities.Add(index, cap);
                else syncedCapacities[index] = cap;
                
                Debug.Log($"[NetworkedWorkerManager] Initial Sync: {type.workerName} = {amount}/{cap}");
            }
            else
            {
                 Debug.LogWarning($"[NetworkedWorkerManager] Failed to find index for worker: {type.workerName}");
            }
        }
    }

    private void OnLocalWorkerChanged(WorkerData type, int amount)
    {
        if (type == null) return;

        int index = GetWorkerIndex(type);
        if (index >= 0)
        {
            if (!syncedAvailable.ContainsKey(index) || syncedAvailable[index] != amount)
            {
                syncedAvailable[index] = amount;
                Debug.Log($"[NetworkedWorkerManager] Local Change -> Network: {type.workerName} = {amount}");
            }

            // Also sync capacity
            if (WorkerManager.Instance != null)
            {
                int cap = WorkerManager.Instance.GetWorkerCapacity(type);
                if (!syncedCapacities.ContainsKey(index) || syncedCapacities[index] != cap)
                {
                    syncedCapacities[index] = cap;
                }
            }
        }
        else
        {
             Debug.LogWarning($"[NetworkedWorkerManager] OnLocalWorkerChanged: Unknown worker type '{type.workerName}'. Available types: {workerTypes.Count}");
        }
    }

    // ============================================================================
    // SYNC CALLBACKS
    // ============================================================================

    private void OnSyncedAvailableChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        if (asServer) return;
        if (IsServerStarted) return; // Host is source of truth, ignore echo

        if (key >= 0 && key < workerTypes.Count)
        {
            OnWorkerCountChanged?.Invoke(workerTypes[key], value);

            // Update local WorkerManager on Client
            if (WorkerManager.Instance != null)
            {
                WorkerManager.Instance.SetWorkerCount(workerTypes[key], value);
            }
        }
    }

    private void OnSyncedCapacitiesChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        if (asServer) return;
        if (IsServerStarted) return; // Host is source of truth, ignore echo

        if (key >= 0 && key < workerTypes.Count)
        {
            OnCapacityChanged?.Invoke(workerTypes[key], value);
        }
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    public int GetAvailableWorkers(WorkerData type)
    {
        int index = GetWorkerIndex(type);
        if (index >= 0 && syncedAvailable.ContainsKey(index))
            return syncedAvailable[index];
        return 0;
    }

    public int GetWorkerCapacity(WorkerData type)
    {
        int index = GetWorkerIndex(type);
        if (index >= 0 && syncedCapacities.ContainsKey(index))
            return syncedCapacities[index];
        return 0;
    }

    public bool HasEnoughWorkers(WorkerData type, int count)
    {
        return GetAvailableWorkers(type) >= count;
    }

    // ============================================================================
    // CLIENT-TO-SERVER SYNC REQUEST
    // ============================================================================

    /// <summary>
    /// Called by client to request the server to re-broadcast current worker state.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestFullSyncServerRpc()
    {
        Debug.Log("[NetworkedWorkerManager] Client requested full sync - re-broadcasting state");
        BroadcastFullStateToClients();
    }

    /// <summary>
    /// Server broadcasts full worker state to all clients via RPC.
    /// </summary>
    [Server]
    private void BroadcastFullStateToClients()
    {
        List<int> indices = new List<int>();
        List<int> amounts = new List<int>();
        List<int> capacities = new List<int>();

        foreach (var kvp in syncedAvailable)
        {
            indices.Add(kvp.Key);
            amounts.Add(kvp.Value);
            capacities.Add(syncedCapacities.ContainsKey(kvp.Key) ? syncedCapacities[kvp.Key] : 0);
        }

        ReceiveFullStateClientRpc(indices.ToArray(), amounts.ToArray(), capacities.ToArray());
    }

    /// <summary>
    /// Client receives full worker state from server.
    /// </summary>
    [ObserversRpc]
    private void ReceiveFullStateClientRpc(int[] indices, int[] amounts, int[] capacities)
    {
        // Server already has correct state
        if (IsServerStarted) return;

        Debug.Log($"[NetworkedWorkerManager] CLIENT received full state RPC with {indices.Length} worker types");

        for (int i = 0; i < indices.Length; i++)
        {
            int index = indices[i];
            if (index >= 0 && index < workerTypes.Count)
            {
                WorkerData type = workerTypes[index];

                // Register and update in local manager
                if (WorkerManager.Instance != null)
                {
                    // Register with 0 if not exists (to set base capacity logic if needed)
                    // We use RegisterWorkerType to ensure it exists.
                    WorkerManager.Instance.RegisterWorkerType(type, amounts[i]);
                    
                    int currentCap = WorkerManager.Instance.GetWorkerCapacity(type);
                    if (capacities[i] > currentCap)
                    {
                        WorkerManager.Instance.AddCapacity(type, capacities[i] - currentCap);
                    }
                }

                OnCapacityChanged?.Invoke(type, capacities[i]);
                OnWorkerCountChanged?.Invoke(type, amounts[i]);

                Debug.Log($"[NetworkedWorkerManager] CLIENT synced {type.workerName}: {amounts[i]}/{capacities[i]}");
            }
        }
    }

    // ============================================================================
    // SERVER METHODS
    // ============================================================================

    [Server]
    public void ServerTrainWorker(int workerTypeIndex, NetworkPlayer requestingPlayer)
    {
        if (workerTypeIndex < 0 || workerTypeIndex >= workerTypes.Count)
        {
            SendErrorToPlayer(requestingPlayer, "Invalid worker type");
            return;
        }

        WorkerData workerData = workerTypes[workerTypeIndex];

        // Check capacity
        int current = syncedAvailable.ContainsKey(workerTypeIndex) ? syncedAvailable[workerTypeIndex] : 0;
        int capacity = syncedCapacities.ContainsKey(workerTypeIndex) ? syncedCapacities[workerTypeIndex] : 0;

        if (current >= capacity)
        {
            SendErrorToPlayer(requestingPlayer, "Worker capacity reached");
            return;
        }

        // Check resources via NetworkedResourceManager
        if (NetworkedResourceManager.Instance != null)
        {
            if (!NetworkedResourceManager.Instance.CanAfford(workerData.cost))
            {
                SendErrorToPlayer(requestingPlayer, "Not enough resources to train worker");
                return;
            }
            NetworkedResourceManager.Instance.ServerSpendResources(workerData.cost);
        }

        // Add worker
        int newAmount = current + 1;
        syncedAvailable[workerTypeIndex] = newAmount;
        
        // Update local manager on Host
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.SetWorkerCount(workerData, newAmount);
        }

        Debug.Log($"[NetworkedWorkerManager] Trained {workerData.workerName}, now: {syncedAvailable[workerTypeIndex]}");
    }

    [Server]
    public bool ServerConsumeWorkers(WorkerData type, int count)
    {
        int index = GetWorkerIndex(type);
        if (index < 0) return false;

        int current = syncedAvailable.ContainsKey(index) ? syncedAvailable[index] : 0;
        if (current < count) return false;

        int newAmount = current - count;
        syncedAvailable[index] = newAmount;

        // Update local manager on Host
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.SetWorkerCount(type, newAmount);
        }
        return true;
    }

    [Server]
    public void ServerReturnWorkers(WorkerData type, int count)
    {
        int index = GetWorkerIndex(type);
        if (index < 0) return;

        int current = syncedAvailable.ContainsKey(index) ? syncedAvailable[index] : 0;
        int capacity = syncedCapacities.ContainsKey(index) ? syncedCapacities[index] : 0;
        int newAmount = Mathf.Min(current + count, capacity);
        syncedAvailable[index] = newAmount;

        // Update local manager on Host
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.SetWorkerCount(type, newAmount);
        }
    }

    /// <summary>
    /// Assign workers to a building
    /// </summary>
    [Server]
    public void ServerAssignWorkers(int buildingNetId, int workerTypeIndex, int count, NetworkPlayer requestingPlayer)
    {
        // TODO: Implement worker assignment to specific buildings
        Debug.Log($"[NetworkedWorkerManager] Assign workers request - Building: {buildingNetId}, Type: {workerTypeIndex}, Count: {count}");
    }

    [Server]
    public void ServerAddCapacity(WorkerData type, int amount)
    {
        int index = GetWorkerIndex(type);
        if (index < 0) return;

        int current = syncedCapacities.ContainsKey(index) ? syncedCapacities[index] : 0;
        int newCapacity = current + amount;
        syncedCapacities[index] = newCapacity;

        // Update local manager on Host
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.AddCapacity(type, amount);
        }
    }

    [Server]
    public void ServerSetWorkers(WorkerData type, int count)
    {
        int index = GetWorkerIndex(type);
        if (index < 0) return;

        int capacity = syncedCapacities.ContainsKey(index) ? syncedCapacities[index] : 0;
        int newAmount = Mathf.Clamp(count, 0, capacity);
        syncedAvailable[index] = newAmount;

        // Update local manager on Host
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.SetWorkerCount(type, newAmount);
        }
    }

    [Server]
    public void ServerResetWorkers()
    {
        syncedAvailable.Clear();
        // Reset capacities to base
        for (int i = 0; i < workerTypes.Count; i++)
        {
            syncedCapacities[i] = workerTypes[i].baseCapacity;
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    public int GetWorkerIndex(WorkerData type)
    {
        if (type == null) return -1;
        for (int i = 0; i < workerTypes.Count; i++)
        {
            if (workerTypes[i].workerName == type.workerName) return i;
        }
        return -1;
    }

    public WorkerData GetWorkerTypeByIndex(int index)
    {
        if (index >= 0 && index < workerTypes.Count) return workerTypes[index];
        return null;
    }

    private void SendErrorToPlayer(NetworkPlayer player, string message)
    {
        if (player != null && player.Owner != null)
        {
            player.TargetSendError(player.Owner, message);
        }
    }

    public int GetWorkerTypeCount() => workerTypes.Count;
}
