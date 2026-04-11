using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using FishNet.Object; // Added for NetworkObject
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkGameManager - Central manager for FishNet multiplayer.
/// Handles server/client connections, LAN discovery, and network state.
/// Host-authoritative model: Host runs server + client, others are clients only.
/// </summary>
public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private ushort port = 7777;
    [SerializeField] private int maxPlayers = 2;

    [Header("Scene Settings")]
    [SerializeField] private string cutsceneSceneName = "CutsceneScene";
    [SerializeField] private string gameSceneName = "Chapter1Map";
    [SerializeField] private string menuSceneName = "MenuScene";

    // Network state
    private NetworkManager networkManager;
    private bool isHost;
    private bool isClient;
    private string hostIP;
    private int hostLocalClientId = -1; // Track the host's own ClientId to filter it out

    // Connected players (host tracks this) - only REMOTE players, not including host
    private List<NetworkConnection> connectedPlayers = new List<NetworkConnection>();

    // Events
    public event Action OnServerStarted;
    public event Action OnServerStopped;
    public event Action OnClientConnected;
    public event Action OnClientDisconnected;
    public event Action<NetworkConnection> OnPlayerJoined;
    public event Action<NetworkConnection> OnPlayerLeft;
    public event Action<string> OnConnectionFailed;

    // Properties
    public bool IsHost => isHost;
    public bool IsClient => isClient;
    public bool IsOnline => isHost || isClient;
    public bool IsServer => networkManager != null && networkManager.IsServerStarted;
    public int PlayerCount => connectedPlayers.Count + (IsServer ? 1 : 0); // Include host as player (count host when server starts)
    public int RemotePlayerCount => connectedPlayers.Count; // Only remote clients (not including host)
    public int MaxPlayers => maxPlayers;
    public string LocalIP => GetLocalIPAddress();
    public ushort Port => port;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[NetworkGameManager] Duplicate detected on {gameObject.name}, destroying new instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Ensure this object persists across scenes
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("[NetworkGameManager] Attached to a child object. DontDestroyOnLoad might not work as expected.");
        }

        // Find NetworkManager immediately in Awake
        InitializeNetworkManager();
    }

    private void Start()
    {
        // Retry initialization if it failed in Awake or components weren't ready
        InitializeNetworkManager();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[NetworkGameManager] Scene loaded: {scene.name}. Re-verifying NetworkManager connection...");
        InitializeNetworkManager();

        // If we are the server, ensure global managers exist in the new scene
        if (IsServer)
        {
            SpawnGlobalManagers();
        }
    }

    private void InitializeNetworkManager()
    {
        // Check if existing reference is still valid (not destroyed)
        bool needFind = (networkManager == null || networkManager.Equals(null));
        NetworkManager foundManager = null;
        
        if (needFind)
        {
            Debug.Log("[NetworkGameManager] NetworkManager reference missing or destroyed. Searching for new instance...");
            foundManager = InstanceFinder.NetworkManager;
            if (foundManager == null)
            {
                foundManager = FindFirstObjectByType<NetworkManager>();
            }
        }
        else
        {
            foundManager = InstanceFinder.NetworkManager;
            if (foundManager != null && foundManager != networkManager)
            {
                Debug.Log("[NetworkGameManager] Found different active NetworkManager. Switching...");
            }
            else
            {
                foundManager = networkManager;
            }
        }

        if (foundManager == null)
        {
            // Not an error in singleplayer — FishNet is only needed when Open to LAN is enabled
            return;
        }

        // If manager changed, or we are re-initializing, ensure we are subscribed to the NEW one
        if (foundManager != networkManager)
        {
            // Unsubscribe from old if exists
            if (networkManager != null)
            {
                networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionStateChanged;
                networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionStateChanged;
                networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
            }

            networkManager = foundManager;

            // Guard: FishNet may have failed to initialize (prefab registry errors)
            if (networkManager.ServerManager == null || networkManager.ClientManager == null)
            {
                Debug.LogWarning($"[NetworkGameManager] NetworkManager found but not fully initialized. ServerManager: {networkManager.ServerManager != null}, ClientManager: {networkManager.ClientManager != null}. Will retry later.");
                // Don't null out - keep the reference so we can retry
                return;
            }

            // Subscribe to new
            networkManager.ServerManager.OnServerConnectionState += OnServerConnectionStateChanged;
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionStateChanged;
            networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionStateChanged;
            
            Debug.Log($"[NetworkGameManager] Subscribed to NetworkManager events. IsServerStarted: {networkManager.IsServerStarted}");
        }

        // FORCE ACTIVATE (Fix for Client 'Inactive' issue)
        if (!networkManager.gameObject.activeSelf)
        {
            Debug.Log("[NetworkGameManager] Activating inactive NetworkManager GameObject");
            networkManager.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionStateChanged;
            networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionStateChanged;
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    // ============================================================================
    // SERVER (HOST) METHODS
    // ============================================================================

    /// <summary>
    /// Start as host (server + local client)
    /// </summary>
    public void StartHost()
    {
        if (networkManager == null)
        {
            Debug.LogWarning("[NetworkGameManager] NetworkManager null at StartHost, retrying initialization...");
            InitializeNetworkManager();
        }

        if (networkManager == null)
        {
            Debug.LogError("[NetworkGameManager] Cannot start host - NetworkManager not found even after retry");
            return;
        }

        if (IsOnline)
        {
            Debug.LogWarning("[NetworkGameManager] Already online, disconnect first");
            return;
        }

        Debug.Log($"[NetworkGameManager] Starting host on port {port}...");

        // Set flag BEFORE starting connection to ensure events are handled correctly
        isHost = true;
        hostIP = GetLocalIPAddress();

        // Start server
        bool serverStarted = networkManager.ServerManager.StartConnection(port);
        Debug.Log($"[NetworkGameManager] ServerManager.StartConnection result: {serverStarted}");

        // Start local client (connects to localhost)
        bool clientStarted = networkManager.ClientManager.StartConnection("localhost", port);
        Debug.Log($"[NetworkGameManager] ClientManager.StartConnection result: {clientStarted}");
    }

    /// <summary>
    /// Start as local host for singleplayer (server + client on localhost, no LAN broadcast).
    /// All NetworkBehaviour code works transparently without any COOP networking.
    /// </summary>
    public void StartLocalHost()
    {
        if (networkManager == null)
        {
            Debug.LogError("[NetworkGameManager] Cannot start local host - NetworkManager not found");
            return;
        }

        if (IsOnline)
        {
            Debug.LogWarning("[NetworkGameManager] Already online, disconnect first");
            return;
        }

        Debug.Log("[NetworkGameManager] Starting local host for singleplayer...");

        isHost = true;
        hostIP = "127.0.0.1";

        // Start server + local client (no LAN broadcast)
        networkManager.ServerManager.StartConnection(port);
        networkManager.ClientManager.StartConnection("localhost", port);
    }

    /// <summary>
    /// Stop hosting (disconnects all clients and stops server)
    /// </summary>
    public void StopHost()
    {
        if (!isHost)
        {
            Debug.LogWarning("[NetworkGameManager] Not hosting");
            return;
        }

        Debug.Log("[NetworkGameManager] Stopping host...");

        // Stop client first
        if (networkManager.ClientManager.Started)
            networkManager.ClientManager.StopConnection();

        // Stop server
        if (networkManager.ServerManager.Started)
            networkManager.ServerManager.StopConnection(true);

        isHost = false;
        isClient = false;
        connectedPlayers.Clear();
    }

    // ============================================================================
    // CLIENT METHODS
    // ============================================================================

    /// <summary>
    /// Connect to a host as client
    /// </summary>
    public void JoinGame(string ipAddress)
    {
        // Retry finding NetworkManager if reference was lost
        if (networkManager == null)
        {
            Debug.LogWarning("[NetworkGameManager] NetworkManager null at JoinGame, retrying initialization...");
            InitializeNetworkManager();
        }

        if (networkManager == null)
        {
            Debug.LogError("[NetworkGameManager] Cannot join - NetworkManager not found even after retry");
            return;
        }

        if (IsOnline)
        {
            Debug.LogWarning("[NetworkGameManager] Already online, disconnect first");
            return;
        }

        Debug.Log($"[NetworkGameManager] Joining game at {ipAddress}:{port}...");

        hostIP = ipAddress;
        networkManager.ClientManager.StartConnection(ipAddress, port);
    }

    /// <summary>
    /// Disconnect from server (client only)
    /// </summary>
    public void LeaveGame()
    {
        if (!isClient || isHost)
        {
            Debug.LogWarning("[NetworkGameManager] Not a client or is host");
            return;
        }

        Debug.Log("[NetworkGameManager] Leaving game...");

        if (networkManager.ClientManager.Started)
            networkManager.ClientManager.StopConnection();

        isClient = false;
    }

    /// <summary>
    /// Disconnect from network (works for both host and client)
    /// </summary>
    public void Disconnect()
    {
        if (isHost)
            StopHost();
        else if (isClient)
            LeaveGame();
    }

    // ============================================================================
    // CONNECTION EVENT HANDLERS
    // ============================================================================

    private void OnServerConnectionStateChanged(ServerConnectionStateArgs args)
    {
        Debug.Log($"[NetworkGameManager] Server state: {args.ConnectionState}");

        switch (args.ConnectionState)
        {
            case LocalConnectionState.Started:
                Debug.Log($"[NetworkGameManager] Server started on {LocalIP}:{port}");

                // Spawn Global Managers (Resources, Workers, etc.)
                SpawnGlobalManagers();

                OnServerStarted?.Invoke();
                break;

            case LocalConnectionState.Stopped:
                Debug.Log("[NetworkGameManager] Server stopped");
                isHost = false;
                connectedPlayers.Clear();
                OnServerStopped?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Spawns the GlobalManagers prefab which contains NetworkedResourceManager, etc.
    /// This ensures they persist across scenes and are available to all clients immediately.
    /// </summary>
    private void SpawnGlobalManagers()
    {
        if (networkManager == null || !networkManager.IsServerStarted) return;

        // Check if it's already spawned (to avoid duplicates on restart)
        if (FindFirstObjectByType<NetworkedResourceManager>() != null)
        {
            Debug.Log("[NetworkGameManager] NetworkedResourceManager already exists, skipping spawn.");
            return;
        }

        // Load prefab from Resources as GameObject first
        GameObject prefabObj = Resources.Load<GameObject>("Prefabs/GlobalManagers");
        if (prefabObj == null)
        {
            Debug.LogError("[NetworkGameManager] Failed to load 'Prefabs/GlobalManagers'. Please create this prefab in Assets/Resources/Prefabs!");
            return;
        }

        NetworkObject globalManagersPrefab = prefabObj.GetComponent<NetworkObject>();
        if (globalManagersPrefab == null)
        {
            Debug.LogError("[NetworkGameManager] CRITICAL: 'Prefabs/GlobalManagers' does not have a NetworkObject component!");
            return;
        }

        // Instantiate and spawn as a global object (null owner = server owned)
        NetworkObject instance = Instantiate(globalManagersPrefab);
        DontDestroyOnLoad(instance.gameObject); // Ensure it persists across scenes
        networkManager.ServerManager.Spawn(instance);
        Debug.Log("[NetworkGameManager] Spawned GlobalManagers prefab.");
    }

    private void OnClientConnectionStateChanged(ClientConnectionStateArgs args)
    {
        Debug.Log($"[NetworkGameManager] Client state: {args.ConnectionState}");

        switch (args.ConnectionState)
        {
            case LocalConnectionState.Started:
                Debug.Log("[NetworkGameManager] Connected to server");
                isClient = true;

                // If we're the host, verify our local ID and cleanup any accidental self-adds
                if (isHost)
                {
                    StartCoroutine(WaitForLocalClientIdAndCleanup());
                    
                    // Spawn player object for Host
                    if (networkManager != null && networkManager.ClientManager.Connection != null)
                    {
                        SpawnPlayerForConnection(networkManager.ClientManager.Connection);
                    }
                }

                OnClientConnected?.Invoke();
                break;

            case LocalConnectionState.Stopped:
                Debug.Log("[NetworkGameManager] Disconnected from server");
                isClient = false;
                hostLocalClientId = -1;
                OnClientDisconnected?.Invoke();

                // Automatically return to menu if disconnected while in-game
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != menuSceneName)
                {
                    Debug.Log("[NetworkGameManager] Disconnected while in-game. Returning to Menu.");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
                }
                break;
        }
    }

    /// <summary>
    /// Spawns a NetworkPlayer object for the given connection.
    /// </summary>
    private void SpawnPlayerForConnection(NetworkConnection conn)
    {
        Debug.Log($"[NetworkGameManager] Attempting to spawn NetworkPlayer for ConnectionId: {conn.ClientId}");

        if (networkManager == null || !networkManager.IsServerStarted)
        {
            Debug.LogError("[NetworkGameManager] Cannot spawn player: NetworkManager null or Server not started.");
            return;
        }

        // Load prefab as GameObject first for safety
        GameObject prefabObj = Resources.Load<GameObject>("Prefabs/NetworkPlayer");
        if (prefabObj == null)
        {
            Debug.LogError("[NetworkGameManager] CRITICAL: Failed to load 'Prefabs/NetworkPlayer' from Resources. Check path!");
            return;
        }

        NetworkObject playerPrefab = prefabObj.GetComponent<NetworkObject>();
        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkGameManager] CRITICAL: 'Prefabs/NetworkPlayer' does not have a NetworkObject component!");
            return;
        }

        // Instantiate and spawn
        NetworkObject playerInstance = Instantiate(playerPrefab);
        DontDestroyOnLoad(playerInstance.gameObject); // Ensure it persists across scenes
        networkManager.ServerManager.Spawn(playerInstance, conn);
        Debug.Log($"[NetworkGameManager] SUCCESS: Spawned NetworkPlayer for ConnectionId {conn.ClientId}. ObjectId: {playerInstance.ObjectId}");
    }

    /// <summary>
    /// Waits for the local client to be assigned a valid ID, then filters it out from the connected list.
    /// </summary>
    private System.Collections.IEnumerator WaitForLocalClientIdAndCleanup()
    {
        // Wait until ID is assigned (>= 0)
        while (networkManager.ClientManager.Connection == null || networkManager.ClientManager.Connection.ClientId < 0)
        {
            yield return null;
        }

        hostLocalClientId = networkManager.ClientManager.Connection.ClientId;
        Debug.Log($"[NetworkGameManager] Host's local ClientId confirmed: {hostLocalClientId}");

        // Cleanup: If this ID is in the connectedPlayers list, remove it
        // (This happens if OnRemoteConnectionStateChanged fired before we knew our ID)
        for (int i = connectedPlayers.Count - 1; i >= 0; i--)
        {
            if (connectedPlayers[i].ClientId == hostLocalClientId)
            {
                Debug.Log($"[NetworkGameManager] Removing host ({hostLocalClientId}) from remote player list (self-correction)");
                NetworkConnection conn = connectedPlayers[i];
                connectedPlayers.RemoveAt(i);
                OnPlayerLeft?.Invoke(conn); // Notify UI to remove "Player 2"
            }
        }
    }

    private void OnRemoteConnectionStateChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        Debug.Log($"[NetworkGameManager] OnRemoteConnectionStateChanged: ConnectionId {conn.ClientId}, State {args.ConnectionState}");

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Determine if this is the host's local client
            bool isLocalClient = false;

            // Method 1: Check against ClientManager (if ID is assigned)
            if (isHost && networkManager.ClientManager.Connection != null && networkManager.ClientManager.Connection.ClientId >= 0)
            {
                if (conn.ClientId == networkManager.ClientManager.Connection.ClientId)
                {
                    isLocalClient = true;
                }
            }

            if (isLocalClient)
            {
                hostLocalClientId = conn.ClientId;
                Debug.Log($"[NetworkGameManager] Host's local client connected (ClientId: {conn.ClientId}) - not counting as remote player");
                return;
            }

            // If we couldn't confirm it's local (e.g. race condition), add it for now.
            // We will filter it out later when the local ID becomes available.
            connectedPlayers.Add(conn);
            Debug.Log($"[SYNC] Remote player joined: ClientId {conn.ClientId}. PlayerCount: {PlayerCount}/{MaxPlayers}");
            OnPlayerJoined?.Invoke(conn);

            // Update LAN broadcast with new player count
            if (LANDiscovery.Instance != null && LANDiscovery.Instance.IsBroadcasting)
            {
                LANDiscovery.Instance.UpdatePlayerCount(PlayerCount, MaxPlayers);
            }

            // Spawn player object for Remote Client
            SpawnPlayerForConnection(conn);

            // Load the host's current game scene on the connecting client.
            // This uses LoadConnectionScenes so only the CLIENT loads the scene — host is unaffected.
            LoadSceneForClient(conn);

            // Push full game state to the new client so they sync immediately
            StartCoroutine(BroadcastFullStateToNewClient());

            // Check if over max players
            // Dynamic Limit Logic:
            // - If host is identified and filtered out (hostLocalClientId >= 0), valid remote players = MaxPlayers - 1 (Host).
            // - If host is NOT identified yet (hostLocalClientId < 0), host is likely IN this list. Valid list size = MaxPlayers.
            int listLimit = (hostLocalClientId >= 0) ? (maxPlayers - 1) : maxPlayers;

            if (connectedPlayers.Count > listLimit)
            {
                Debug.LogWarning($"[NetworkGameManager] Server full (Count: {connectedPlayers.Count}, Limit: {listLimit}), kicking {conn.ClientId}");
                conn.Kick(FishNet.Managing.Server.KickReason.Unset);
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Don't process disconnect for host's local client
            if (isHost && conn.ClientId == hostLocalClientId)
            {
                Debug.Log($"[NetworkGameManager] Host's local client disconnected (ClientId: {conn.ClientId})");
                return;
            }

            Debug.Log($"[NetworkGameManager] Remote player left: {conn.ClientId}");
            connectedPlayers.Remove(conn);
            OnPlayerLeft?.Invoke(conn);

            // Update LAN broadcast with new player count
            if (LANDiscovery.Instance != null && LANDiscovery.Instance.IsBroadcasting)
            {
                LANDiscovery.Instance.UpdatePlayerCount(PlayerCount, MaxPlayers);
                Debug.Log($"[NetworkGameManager] Updated LAN broadcast: {PlayerCount}/{MaxPlayers}");
            }
        }
    }

    /// <summary>
    /// Load the host's current game scene on a specific connecting client.
    /// Uses LoadConnectionScenes so ONLY the client loads the scene — host is unaffected.
    /// ReplaceOption.All ensures the client's MenuScene is replaced.
    /// </summary>
    private void LoadSceneForClient(NetworkConnection conn)
    {
        if (networkManager == null || networkManager.SceneManager == null) return;

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentSceneName == menuSceneName) return;

        Debug.Log($"[NetworkGameManager] Loading '{currentSceneName}' on client {conn.ClientId}");
        SceneLoadData sld = new SceneLoadData(currentSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        networkManager.SceneManager.LoadConnectionScenes(conn, sld);
    }

    /// <summary>
    /// Broadcast full game state to all clients after a short delay.
    /// Called when a new remote player joins so they receive current state.
    /// The delay allows the client's NetworkBehaviours to initialize first.
    /// </summary>
    private System.Collections.IEnumerator BroadcastFullStateToNewClient()
    {
        // Wait for the client to load through CutsceneScene and into the game scene.
        // The client also requests sync via RequestFullSyncServerRpc() on their own,
        // but this server-side push ensures they get data even if their request is missed.
        yield return new WaitForSecondsRealtime(5f);

        Debug.Log("[SYNC] Broadcasting full game state to new client (server push)...");

        if (NetworkedResourceManager.Instance != null && NetworkedResourceManager.Instance.IsSpawned)
        {
            NetworkedResourceManager.Instance.BroadcastFullStateToClients();
            Debug.Log("[SYNC] Resource state broadcast sent.");
        }

        if (NetworkedWorkerManager.Instance != null && NetworkedWorkerManager.Instance.IsSpawned)
        {
            NetworkedWorkerManager.Instance.BroadcastFullStateToClients();
            Debug.Log("[SYNC] Worker state broadcast sent.");
        }
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    /// <summary>
    /// Get local IP address for LAN hosting
    /// </summary>
    public string GetLocalIPAddress()
    {
        try
        {
            // Prefer real LAN/Wi-Fi adapters over VPN adapters
            string fallback = null;
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;

                // Skip virtual/VPN adapters by description
                string desc = ni.Description.ToLower();
                bool isVirtual = desc.Contains("virtual") || desc.Contains("vpn") || desc.Contains("radmin")
                    || desc.Contains("zerotier") || desc.Contains("hamachi") || desc.Contains("vethernet")
                    || ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback;

                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        if (ip.StartsWith("127.")) continue;

                        if (!isVirtual)
                            return ip; // Preferred: real adapter

                        if (fallback == null)
                            fallback = ip; // Fallback: VPN adapter
                    }
                }
            }

            if (fallback != null)
                return fallback;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkGameManager] Failed to get local IP: {ex.Message}");
        }
        return "127.0.0.1";
    }

    /// <summary>
    /// Check if an IP address is valid
    /// </summary>
    public bool IsValidIPAddress(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }

    /// <summary>
    /// Load cutscene scene for all connected players (host only).
    /// Called from lobby when host clicks Start Game.
    /// </summary>
    public void LoadGameScene()
    {
        LoadNetworkedScene(cutsceneSceneName);
    }

    /// <summary>
    /// Load a scene for all connected players (host only).
    /// Uses FishNet's scene manager to sync scene loading.
    /// </summary>
    public void LoadNetworkedScene(string sceneName)
    {
        if (!isHost)
        {
            Debug.LogWarning("[NetworkGameManager] Only host can load scenes");
            return;
        }

        if (networkManager == null || networkManager.SceneManager == null)
        {
            Debug.LogError("[NetworkGameManager] NetworkManager or SceneManager not available");
            return;
        }

        Debug.Log($"[NetworkGameManager] Loading networked scene: {sceneName}");

        // Use FishNet's scene manager for networked scene loading
        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        networkManager.SceneManager.LoadGlobalScenes(sld);
    }

    /// <summary>
    /// Load chapter map scene (called after cutscene/worldmap).
    /// </summary>
    public void LoadChapterScene()
    {
        LoadNetworkedScene(gameSceneName);
    }

    /// <summary>
    /// Return to menu (disconnects network)
    /// </summary>
    public void ReturnToMenu()
    {
        Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>
    /// Get connection info string for display
    /// </summary>
    public string GetConnectionInfo()
    {
        if (isHost)
            return $"Hosting: {LocalIP}:{port} ({PlayerCount}/{MaxPlayers} players)";
        else if (isClient)
            return $"Connected to: {hostIP}:{port}";
        else
            return "Offline";
    }
}
