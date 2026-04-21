using System;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Always-available singleton on the NetworkManager prefab.
/// Handles server start/stop lifecycle and spawns CoopManager (NetworkBehaviour) at runtime.
/// Other scripts use CoopBootstrap.Instance for server control and connection state,
/// and CoopManager.Instance for RPCs (only available during active multiplayer).
/// </summary>
public class CoopBootstrap : MonoBehaviour
{
    public static CoopBootstrap Instance { get; private set; }

    [SerializeField] private GameObject coopManagerPrefab;
    [Header("Configuration")]
    [SerializeField] private int port = 7770;
    [SerializeField] private int maxPlayers = 2;

    private GameObject spawnedInstance;

    // ========================================================================
    // PUBLIC STATE
    // ========================================================================

    public bool IsOnline => InstanceFinder.NetworkManager != null &&
                            (InstanceFinder.IsServerStarted || InstanceFinder.IsClientStarted);
    public bool IsHost => InstanceFinder.IsHostStarted;
    public bool IsClientOnly => InstanceFinder.IsClientOnlyStarted;
    public bool IsServer => InstanceFinder.IsServerStarted;
    public int Port => port;
    public int MaxPlayers => maxPlayers;
    // PlayerCount is authoritative on the server (from ServerManager.Clients.Count).
    // Clients get their value synced via CoopManager.RpcSyncPlayerCount.
    private int cachedPlayerCount = 1;
    public int PlayerCount
    {
        get
        {
            if (InstanceFinder.NetworkManager != null && InstanceFinder.IsServerStarted)
                return InstanceFinder.ServerManager.Clients.Count;
            return cachedPlayerCount;
        }
    }

    /// <summary>
    /// Called by CoopManager on clients to update the cached player count.
    /// </summary>
    public void SetClientPlayerCount(int count)
    {
        cachedPlayerCount = count;
    }
    public string LocalIP => LANBroadcaster.GetLocalIP();

    // Events
    public event Action<NetworkConnection> OnPlayerJoined;
    public event Action<NetworkConnection> OnPlayerLeft;

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
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (InstanceFinder.NetworkManager != null)
        {
            InstanceFinder.NetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionStateChanged;
        }
    }

    private void OnDisable()
    {
        if (InstanceFinder.NetworkManager != null)
        {
            InstanceFinder.NetworkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
            InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
        }
    }

    // ========================================================================
    // SERVER START / STOP (called by UI)
    // ========================================================================

    public void StartServer()
    {
        if (InstanceFinder.NetworkManager == null)
        {
            Debug.LogError("[CoopBootstrap] NetworkManager not found");
            return;
        }

        // Configure transport port
        var transport = InstanceFinder.NetworkManager.TransportManager.Transport;
        if (transport is FishNet.Transporting.Tugboat.Tugboat tugboat)
        {
            tugboat.SetPort((ushort)port);
        }

        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection(); // Host = server + client

        Debug.Log($"[CoopBootstrap] Starting host on port {port}");
    }

    public void StopServer()
    {
        if (InstanceFinder.NetworkManager == null) return;

        if (InstanceFinder.IsServerStarted)
            InstanceFinder.ServerManager.StopConnection(true);

        Debug.Log("[CoopBootstrap] Server stopped");
    }

    public void Disconnect()
    {
        if (InstanceFinder.NetworkManager == null) return;

        if (InstanceFinder.IsClientStarted)
            InstanceFinder.ClientManager.StopConnection();
        if (InstanceFinder.IsServerStarted)
            InstanceFinder.ServerManager.StopConnection(true);
    }

    // ========================================================================
    // CONNECTION CALLBACKS
    // ========================================================================

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Spawn CoopManager as a networked object
            if (coopManagerPrefab == null)
            {
                Debug.LogError("[CoopBootstrap] CoopManager prefab not assigned!");
                return;
            }

            spawnedInstance = Instantiate(coopManagerPrefab);
            InstanceFinder.ServerManager.Spawn(spawnedInstance);
            Debug.Log("[CoopBootstrap] Server started, CoopManager spawned");
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            if (spawnedInstance != null)
            {
                if (InstanceFinder.IsServerStarted)
                    InstanceFinder.ServerManager.Despawn(spawnedInstance);
                else
                    Destroy(spawnedInstance);

                spawnedInstance = null;
            }

            Debug.Log("[CoopBootstrap] Server stopped, CoopManager cleaned up");
        }
    }

    private void OnRemoteConnectionStateChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            OnPlayerJoined?.Invoke(conn);
            Debug.Log($"[CoopBootstrap] Player joined. Count: {PlayerCount}");
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            OnPlayerLeft?.Invoke(conn);
            Debug.Log($"[CoopBootstrap] Player left. Count: {PlayerCount}");
        }
    }
}
