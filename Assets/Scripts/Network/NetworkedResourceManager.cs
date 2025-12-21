using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkedResourceManager - Syncs resource state across network.
/// Server-authoritative: Only server can modify resources, clients receive synced state.
/// Works alongside existing ResourceManager for backwards compatibility.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedResourceManager : NetworkBehaviour
{
    public static NetworkedResourceManager Instance { get; private set; }

    [Header("Resource Types")]
    [SerializeField] private List<ResourceType> resourceTypes = new List<ResourceType>();

    // Synced resource amounts (FishNet 4.x - no attribute needed)
    private readonly SyncDictionary<int, int> syncedAmounts = new SyncDictionary<int, int>();

    // Synced resource capacities (FishNet 4.x - no attribute needed)
    private readonly SyncDictionary<int, int> syncedCapacities = new SyncDictionary<int, int>();

    // Events for UI updates
    public event Action<ResourceType, int> OnResourceChanged;
    public event Action<ResourceType, int> OnCapacityChanged;

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
        // On Client (but not Host), request sync when scene loads
        if (IsClientStarted && !IsServerStarted)
        {
            StartCoroutine(ClientWaitForResourceManager());
        }

        // On Server (including Host), re-subscribe to the new local ResourceManager
        if (IsServerStarted)
        {
            StartCoroutine(ServerWaitForResourceManager());
        }
    }

    private System.Collections.IEnumerator ClientWaitForResourceManager()
    {
        while (ResourceManager.Instance == null)
        {
            yield return null;
        }
        Debug.Log("[NetworkedResourceManager] Client found ResourceManager. Requesting Full Sync...");
        RequestFullSyncServerRpc();
    }

    private System.Collections.IEnumerator ServerWaitForResourceManager()
    {
        // Wait for the new scene's ResourceManager to be ready
        while (ResourceManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log("[NetworkedResourceManager] Server found new ResourceManager. Re-subscribing...");

        // Unsubscribe from old instance (if any logic remains, though usually handled by OnStopServer or previous cleanup)
        ResourceManager.Instance.OnResourceChanged -= OnLocalResourceChanged;
        
        // Subscribe to new instance
        ResourceManager.Instance.OnResourceChanged += OnLocalResourceChanged;

        // Capture initial state from the new scene
        InitializeServerStateFromLocal();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        syncedAmounts.OnChange += OnSyncedAmountsChanged;
        syncedCapacities.OnChange += OnSyncedCapacitiesChanged;
        
        // Start debug sync log
        StartCoroutine(SyncDebugLoop());
    }

    private System.Collections.IEnumerator SyncDebugLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (IsServerStarted || IsClientStarted)
            {
                string state = IsServerStarted ? "SERVER" : "CLIENT";
                string details = string.Join(", ", syncedAmounts.Select(kvp => {
                    var type = GetResourceTypeByIndex(kvp.Key);
                    return $"{(type != null ? type.ResourceName : kvp.Key.ToString())}: {kvp.Value}";
                }));
                Debug.Log($"[NetworkedResourceManager] {state} SYNC STATUS: {details}");
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[NetworkedResourceManager] Started on Server.");
        
        // Ensure types are loaded
        EnsureResourceTypesLoaded();

        // Start coroutine to wait for ResourceManager (handles race condition)
        StartCoroutine(ServerWaitForResourceManager());
        
        // Also re-sync whenever a new chapter starts to ensure initial values are captured
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnChapterStarted += (chapter) => {
                Debug.Log($"[NetworkedResourceManager] Chapter {chapter.chapterName} started. Clearing and performing full resource sync.");
                
                // Clear existing state to force all subsequent updates to be seen as "changes" by FishNet
                syncedAmounts.Clear();
                syncedCapacities.Clear();
                
                // InitializeServerStateFromLocal will be called by OnSceneLoaded or explicit logic in MissionChapterManager
                // But we call it here just in case scene doesn't change
                if (ResourceManager.Instance != null)
                {
                    InitializeServerStateFromLocal();
                }
            };
        }

        Debug.Log("[NetworkedResourceManager] Server initialized resources from local state");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= OnLocalResourceChanged;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[NetworkedResourceManager] Started on Client.");
        EnsureResourceTypesLoaded();

        // Force initial sync of any data already received
        StartCoroutine(DelayedInitialSync());
        
        // Wait for ResourceManager then sync
        StartCoroutine(ClientWaitForResourceManager());
    }

    private System.Collections.IEnumerator DelayedInitialSync()
    {
        // Wait a frame for FishNet to complete initial SyncDictionary synchronization
        yield return null;
        yield return null; // Two frames to be safe

        Debug.Log($"[NetworkedResourceManager] Delayed initial sync - syncedAmounts has {syncedAmounts.Count} entries");
        ForceSyncToLocal();
    }

    /// <summary>
    /// Forces all currently synced networked values into the local ResourceManager.
    /// Useful after scene transitions to ensure local UI/Managers are up to date.
    /// </summary>
    public void ForceSyncToLocal()
    {
        if (ResourceManager.Instance == null) return;

        Debug.Log($"[NetworkedResourceManager] Force syncing {syncedAmounts.Count} resources to local manager.");
        foreach (var kvp in syncedAmounts)
        {
            int index = kvp.Key;
            int amount = kvp.Value;
            
            if (index >= 0 && index < resourceTypes.Count)
            {
                ResourceType type = resourceTypes[index];
                
                // Sync capacity first
                if (syncedCapacities.ContainsKey(index))
                {
                    ResourceManager.Instance.SetCapacity(type, syncedCapacities[index]);
                }
                
                // Then amount
                ResourceManager.Instance.SetResourceAmount(type, amount);
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("[NetworkedResourceManager] Stopped on Client.");
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        syncedAmounts.OnChange -= OnSyncedAmountsChanged;
        syncedCapacities.OnChange -= OnSyncedCapacitiesChanged;
    }

    private void EnsureResourceTypesLoaded()
    {
        // Fallback: Load from Resources if inspector list is empty
        if (resourceTypes == null || resourceTypes.Count == 0)
        {
            Debug.Log("[NetworkedResourceManager] resourceTypes list is empty! Attempting to auto-load from Resources/Data/Resources...");
            var loadedResources = Resources.LoadAll<ResourceType>("Data/Resources");
            if (loadedResources != null && loadedResources.Length > 0)
            {
                resourceTypes.AddRange(loadedResources);
            }
            else
            {
                Debug.LogError("[NetworkedResourceManager] Failed to auto-load resource types! Sync will not work.");
                return;
            }
        }

        // CRITICAL: Sort by name to ensure consistent indices across Host and Client
        resourceTypes.Sort((a, b) => string.Compare(a.ResourceName, b.ResourceName, StringComparison.Ordinal));
        
        string names = string.Join(", ", resourceTypes.ConvertAll(r => r.ResourceName));
        Debug.Log($"[NetworkedResourceManager] Loaded and Sorted {resourceTypes.Count} resource types: {names}");
    }

    private void InitializeServerStateFromLocal()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("[NetworkedResourceManager] No local ResourceManager found to initialize server state.");
            return;
        }

        Debug.Log($"[NetworkedResourceManager] Initializing server state from {ResourceManager.Instance.ResourceAmounts.Count} local resources.");

        // Only sync resources that are currently registered in the local manager
        foreach (var entry in ResourceManager.Instance.ResourceAmounts)
        {
            ResourceType type = entry.Key;
            int index = GetResourceIndex(type);
            
            if (index >= 0)
            {
                int localAmount = entry.Value;
                int localCap = ResourceManager.Instance.GetResourceCapacity(type);

                if (!syncedAmounts.ContainsKey(index)) syncedAmounts.Add(index, localAmount);
                else syncedAmounts[index] = localAmount;

                if (!syncedCapacities.ContainsKey(index)) syncedCapacities.Add(index, localCap);
                else syncedCapacities[index] = localCap;
                
                Debug.Log($"[NetworkedResourceManager] Initial Sync: {type.ResourceName} = {localAmount}/{localCap}");
            }
        }
    }

    private void OnLocalResourceChanged(ResourceType type, int amount)
    {
        if (type == null) return;

        // When local resource changes on server, update network
        int index = GetResourceIndex(type);
        if (index >= 0)
        {
            // Only update if value actually changed or if it's not in the dictionary yet
            if (!syncedAmounts.ContainsKey(index) || syncedAmounts[index] != amount)
            {
                syncedAmounts[index] = amount;
                Debug.Log($"[NetworkedResourceManager] Local Change -> Network: {type.ResourceName} = {amount}");
            }

            // Also sync capacity if it's missing or changed
            if (ResourceManager.Instance != null)
            {
                int cap = ResourceManager.Instance.GetResourceCapacity(type);
                if (!syncedCapacities.ContainsKey(index) || syncedCapacities[index] != cap)
                {
                    syncedCapacities[index] = cap;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkedResourceManager] Received local change for unknown resource type: {type.ResourceName}");
        }
    }
    

    // ============================================================================
    // SYNC CALLBACKS (Called on clients when server updates)
    // ============================================================================

    private void OnSyncedAmountsChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        // Host (Server) already has the data (Source of Truth), so ignore network echo
        if (asServer) return;
        if (IsServerStarted) return; // Host is source of truth, ignore echo

        // Handle initial full sync from server (when client first joins)
        if (op == SyncDictionaryOperation.Complete)
        {
            Debug.Log($"[NetworkedResourceManager] Received Complete sync operation - forcing full sync to local");
            ForceSyncToLocal();
            return;
        }

        if (key >= 0 && key < resourceTypes.Count)
        {
            OnResourceChanged?.Invoke(resourceTypes[key], value);

            // Update local ResourceManager on Client to reflect Server state
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetResourceAmount(resourceTypes[key], value);
            }
        }
    }

    private void OnSyncedCapacitiesChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        if (asServer) return;
        if (IsServerStarted) return; // Host is source of truth, ignore echo

        // Handle initial full sync from server (when client first joins)
        if (op == SyncDictionaryOperation.Complete)
        {
            Debug.Log($"[NetworkedResourceManager] Received Complete capacity sync - forcing full sync to local");
            ForceSyncToLocal();
            return;
        }

        if (key >= 0 && key < resourceTypes.Count)
        {
            OnCapacityChanged?.Invoke(resourceTypes[key], value);

            // Update local ResourceManager on Client
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetCapacity(resourceTypes[key], value);
            }
        }
    }

    // ============================================================================
    // PUBLIC API (Use these instead of ResourceManager when networked)
    // ============================================================================

    public int GetResourceAmount(ResourceType type)
    {
        int index = GetResourceIndex(type);
        if (index >= 0 && syncedAmounts.ContainsKey(index))
            return syncedAmounts[index];
        return 0;
    }

    public int GetResourceCapacity(ResourceType type)
    {
        int index = GetResourceIndex(type);
        if (index >= 0 && syncedCapacities.ContainsKey(index))
            return syncedCapacities[index];
        return 0;
    }

    public bool HasEnoughResources(ResourceType type, int amount)
    {
        return GetResourceAmount(type) >= amount;
    }

    public bool CanAfford(List<ResourceCost> costs)
    {
        foreach (var cost in costs)
        {
            if (!HasEnoughResources(cost.resourceType, cost.amount))
                return false;
        }
        return true;
    }

    // ============================================================================
    // SERVER-ONLY METHODS
    // ============================================================================

    [Server]
    public void ServerAddResource(ResourceType type, int amount)
    {
        int index = GetResourceIndex(type);
        if (index < 0) return;

        int current = syncedAmounts.ContainsKey(index) ? syncedAmounts[index] : 0;
        int capacity = syncedCapacities.ContainsKey(index) ? syncedCapacities[index] : 0;
        int newAmount = Mathf.Min(current + amount, capacity);

        syncedAmounts[index] = newAmount;

        // Update local manager on Host so UI updates
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetResourceAmount(type, newAmount);
        }
    }

    [Server]
    public bool ServerRemoveResource(ResourceType type, int amount)
    {
        int index = GetResourceIndex(type);
        if (index < 0) return false;

        int current = syncedAmounts.ContainsKey(index) ? syncedAmounts[index] : 0;
        if (current < amount) return false;

        int newAmount = current - amount;
        syncedAmounts[index] = newAmount;

        // Update local manager on Host so UI updates
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetResourceAmount(type, newAmount);
        }
        return true;
    }

    [Server]
    public void ServerSetResource(ResourceType type, int amount)
    {
        int index = GetResourceIndex(type);
        if (index < 0) return;

        int capacity = syncedCapacities.ContainsKey(index) ? syncedCapacities[index] : 0;
        int newAmount = Mathf.Clamp(amount, 0, capacity);
        syncedAmounts[index] = newAmount;

        // Update local manager on Host so UI updates
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetResourceAmount(type, newAmount);
        }
    }

    [Server]
    public void ServerAddCapacity(ResourceType type, int amount)
    {
        int index = GetResourceIndex(type);
        if (index < 0) return;

        int current = syncedCapacities.ContainsKey(index) ? syncedCapacities[index] : 0;
        int newCapacity = current + amount;
        syncedCapacities[index] = newCapacity;

        // Update local manager on Host so UI updates
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetCapacity(type, newCapacity);
        }
    }

    [Server]
    public bool ServerSpendResources(List<ResourceCost> costs)
    {
        if (!CanAfford(costs)) return false;
        foreach (var cost in costs) ServerRemoveResource(cost.resourceType, cost.amount);
        return true;
    }

    [Server]
    public void ServerResetResources()
    {
        for (int i = 0; i < resourceTypes.Count; i++)
        {
            if (syncedAmounts.ContainsKey(i)) syncedAmounts[i] = 0;
            if (syncedCapacities.ContainsKey(i)) syncedCapacities[i] = resourceTypes[i].BaseCapacity;
        }
    }

    // ============================================================================
    // CLIENT-TO-SERVER SYNC REQUEST
    // ============================================================================

    /// <summary>
    /// Called by client to request the server to re-broadcast current resource state.
    /// Useful for late-joining clients or after scene transitions.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestFullSyncServerRpc()
    {
        Debug.Log("[NetworkedResourceManager] Client requested full sync - re-broadcasting state");

        // Force re-broadcast by temporarily storing and re-setting all values
        // This ensures the SyncDictionary sends updates to all clients
        Dictionary<int, int> tempAmounts = new Dictionary<int, int>(syncedAmounts);
        Dictionary<int, int> tempCapacities = new Dictionary<int, int>(syncedCapacities);

        foreach (var kvp in tempAmounts)
        {
            // Setting to same value won't trigger sync, so we need to toggle
            syncedAmounts[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in tempCapacities)
        {
            syncedCapacities[kvp.Key] = kvp.Value;
        }

        // Also respond with an RPC to ensure client gets current state
        BroadcastFullStateToClients();
    }

    /// <summary>
    /// Server broadcasts full resource state to all clients via RPC.
    /// </summary>
    [Server]
    private void BroadcastFullStateToClients()
    {
        List<int> indices = new List<int>();
        List<int> amounts = new List<int>();
        List<int> capacities = new List<int>();

        foreach (var kvp in syncedAmounts)
        {
            indices.Add(kvp.Key);
            amounts.Add(kvp.Value);
            capacities.Add(syncedCapacities.ContainsKey(kvp.Key) ? syncedCapacities[kvp.Key] : 0);
        }

        ReceiveFullStateClientRpc(indices.ToArray(), amounts.ToArray(), capacities.ToArray());
    }

    /// <summary>
    /// Client receives full resource state from server.
    /// </summary>
    [ObserversRpc]
    private void ReceiveFullStateClientRpc(int[] indices, int[] amounts, int[] capacities)
    {
        // Server already has correct state
        if (IsServerStarted) return;

        Debug.Log($"[NetworkedResourceManager] CLIENT received full state RPC with {indices.Length} resources");

        for (int i = 0; i < indices.Length; i++)
        {
            int index = indices[i];
            if (index >= 0 && index < resourceTypes.Count)
            {
                ResourceType type = resourceTypes[index];

                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.SetCapacity(type, capacities[i]);
                    ResourceManager.Instance.SetResourceAmount(type, amounts[i]);
                }

                OnCapacityChanged?.Invoke(type, capacities[i]);
                OnResourceChanged?.Invoke(type, amounts[i]);

                Debug.Log($"[NetworkedResourceManager] CLIENT synced {type.ResourceName}: {amounts[i]}/{capacities[i]}");
            }
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private int GetResourceIndex(ResourceType type)
    {
        if (type == null) return -1;
        for (int i = 0; i < resourceTypes.Count; i++)
        {
            if (resourceTypes[i].ResourceName == type.ResourceName) return i;
        }
        return -1;
    }

    public ResourceType GetResourceTypeByIndex(int index)
    {
        if (index >= 0 && index < resourceTypes.Count) return resourceTypes[index];
        return null;
    }

    public int GetResourceTypeCount() => resourceTypes.Count;
}
