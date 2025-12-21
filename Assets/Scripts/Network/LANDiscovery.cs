using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// LANDiscovery - Broadcasts server presence and discovers LAN servers.
/// Uses UDP broadcast for local network discovery.
/// Server broadcasts its presence, clients listen and collect server list.
/// </summary>
public class LANDiscovery : MonoBehaviour
{
    public static LANDiscovery Instance { get; private set; }

    [Header("Discovery Settings")]
    [SerializeField] private int broadcastPort = 47777;
    [SerializeField] private float broadcastInterval = 1f;
    [SerializeField] private float serverTimeout = 5f;
    [SerializeField] private string gameIdentifier = "PLANETFALL";

    // Server info structure
    [Serializable]
    public class ServerInfo
    {
        public string serverName;
        public string ipAddress;
        public int port;
        public int currentPlayers;
        public int maxPlayers;
        public float lastSeenTime;

        public bool IsExpired(float timeout, float currentTime) => currentTime - lastSeenTime > timeout;
    }

    // State
    private UdpClient broadcastClient;
    private UdpClient listenClient;
    private UdpClient hostListenClient; // Host also listens for discovery requests
    private Thread listenThread;
    private Thread hostListenThread;
    private volatile bool isBroadcasting;
    private volatile bool isListening;
    private float broadcastTimer;
    private float localhostPollTimer;
    private const float LOCALHOST_POLL_INTERVAL = 0.5f; // Poll localhost every 0.5 seconds
    private int actualListenPort; // Track which port we actually bound to (may differ from broadcastPort)

    // Server info for broadcasting
    private string currentServerName;
    private int currentGamePort;
    private int currentPlayerCount;
    private int currentMaxPlayers;

    // Discovered servers
    private Dictionary<string, ServerInfo> discoveredServers = new Dictionary<string, ServerInfo>();
    private readonly object serverLock = new object();

    // Thread-safe time tracking
    private float lastUpdateTime;

    // UDP Connection Reset constant for Windows
    private const int SIO_UDP_CONNRESET = -1744830452;

    // Pending server updates from background thread
    private readonly Queue<ServerInfo> pendingServerUpdates = new Queue<ServerInfo>();
    private readonly object pendingLock = new object();

    // Events
    public event Action<ServerInfo> OnServerDiscovered;
    public event Action<string> OnServerLost;
    public event Action<List<ServerInfo>> OnServerListUpdated;

    // Properties
    public bool IsBroadcasting => isBroadcasting;
    public bool IsListening => isListening;

    public List<ServerInfo> DiscoveredServers
    {
        get
        {
            lock (serverLock)
            {
                return new List<ServerInfo>(discoveredServers.Values);
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        StopBroadcasting();
        StopListening();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        lastUpdateTime = Time.realtimeSinceStartup;

        // Handle broadcasting
        if (isBroadcasting)
        {
            broadcastTimer += Time.deltaTime;
            if (broadcastTimer >= broadcastInterval)
            {
                broadcastTimer = 0f;
                BroadcastServerInfo();
            }
        }

        // Handle localhost polling (like Minecraft/Factorio - actively check localhost)
        if (isListening)
        {
            localhostPollTimer += Time.deltaTime;
            if (localhostPollTimer >= LOCALHOST_POLL_INTERVAL)
            {
                localhostPollTimer = 0f;
                PollLocalhost();
            }
        }

        // Process pending server updates from background thread
        ProcessPendingServerUpdates();

        // Clean up expired servers
        if (isListening)
        {
            CleanupExpiredServers();
        }
    }

    private void IgnoreConnectionReset(UdpClient client)
    {
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            try
            {
                client.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch { }
        }
    }

    // ============================================================================
    // SERVER BROADCASTING (Host calls these)
    // ============================================================================

    /// <summary>
    /// Start broadcasting this server's presence on LAN.
    /// Called by host when server starts.
    /// </summary>
    public void StartBroadcasting(string serverName, int gamePort, int currentPlayers, int maxPlayers)
    {
        if (isBroadcasting)
        {
            Debug.LogWarning("[LANDiscovery] Already broadcasting");
            return;
        }

        // Stop listening if we were (host doesn't need to listen for servers)
        StopListening();

        try
        {
            broadcastClient = new UdpClient();
            IgnoreConnectionReset(broadcastClient);
            broadcastClient.EnableBroadcast = true;

            // Store server info
            currentServerName = serverName;
            currentGamePort = gamePort;
            currentPlayerCount = currentPlayers;
            currentMaxPlayers = maxPlayers;

            isBroadcasting = true;
            broadcastTimer = 0f;

            // Start listening for discovery requests from clients (Minecraft-style)
            StartHostDiscoveryListener();

            // Broadcast immediately
            BroadcastServerInfo();

            Debug.Log($"[LANDiscovery] Started broadcasting: {serverName} on port {broadcastPort}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LANDiscovery] Failed to start broadcasting: {ex.Message}");
        }
    }

    /// <summary>
    /// Start listening for discovery requests from clients (host-side).
    /// When a client sends a DISCOVER request, respond with server info.
    /// </summary>
    private void StartHostDiscoveryListener()
    {
        try
        {
            // Bind to the broadcast port to receive discovery requests
            hostListenClient = new UdpClient();
            IgnoreConnectionReset(hostListenClient);
            hostListenClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            hostListenClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));

            hostListenThread = new Thread(HostListenForDiscoveryRequests);
            hostListenThread.IsBackground = true;
            hostListenThread.Start();

            Debug.Log($"[LANDiscovery] Host discovery listener started on port {broadcastPort}");
        }
        catch (SocketException ex)
        {
            Debug.LogWarning($"[LANDiscovery] Could not start host discovery listener on port {broadcastPort}: {ex.Message}");

            // Try alternative port
            try
            {
                hostListenClient = new UdpClient();
                IgnoreConnectionReset(hostListenClient);
                hostListenClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                hostListenClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort + 1));

                hostListenThread = new Thread(HostListenForDiscoveryRequests);
                hostListenThread.IsBackground = true;
                hostListenThread.Start();

                Debug.Log($"[LANDiscovery] Host discovery listener started on port {broadcastPort + 1}");
            }
            catch (Exception ex2)
            {
                Debug.LogWarning($"[LANDiscovery] Could not start host discovery listener: {ex2.Message}");
            }
        }
    }

    /// <summary>
    /// Host thread that listens for discovery requests and responds.
    /// </summary>
    private void HostListenForDiscoveryRequests()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isBroadcasting && hostListenClient != null)
        {
            try
            {
                hostListenClient.Client.ReceiveTimeout = 1000;
                byte[] data = hostListenClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);

                // Check if this is a discovery request
                if (message.StartsWith($"{gameIdentifier}|DISCOVER"))
                {
                    // Debug.Log($"[LANDiscovery] Received discovery request from {remoteEndPoint.Address}");

                    // Send server info back to the requester
                    string localIP = NetworkGameManager.Instance?.LocalIP ?? "127.0.0.1";
                    string response = $"{gameIdentifier}|{currentServerName}|{localIP}|{currentGamePort}|{currentPlayerCount}|{currentMaxPlayers}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);

                    // Respond to the sender's address on the same port they sent from
                    hostListenClient.Send(responseData, responseData.Length, remoteEndPoint);

                    // Debug.Log($"[LANDiscovery] Sent server info response to {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.TimedOut && isBroadcasting)
                {
                    Debug.LogWarning($"[LANDiscovery] Host listen socket exception: {ex.SocketErrorCode}");
                }
            }
            catch (Exception ex)
            {
                if (isBroadcasting)
                {
                    Debug.LogWarning($"[LANDiscovery] Host listen error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Update player count for broadcast.
    /// </summary>
    public void UpdatePlayerCount(int current, int max)
    {
        currentPlayerCount = current;
        currentMaxPlayers = max;
    }

    /// <summary>
    /// Stop broadcasting server presence.
    /// </summary>
    public void StopBroadcasting()
    {
        if (!isBroadcasting) return;

        isBroadcasting = false;

        try
        {
            broadcastClient?.Close();
            broadcastClient = null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LANDiscovery] Error stopping broadcast: {ex.Message}");
        }

        // Stop host discovery listener
        try
        {
            hostListenClient?.Close();
            hostListenClient = null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LANDiscovery] Error stopping host listener: {ex.Message}");
        }

        if (hostListenThread != null && hostListenThread.IsAlive)
        {
            hostListenThread.Join(500);
        }
        hostListenThread = null;

        Debug.Log("[LANDiscovery] Stopped broadcasting");
    }

    private void BroadcastServerInfo()
    {
        if (broadcastClient == null) return;

        try
        {
            string localIP = NetworkGameManager.Instance?.LocalIP ?? "127.0.0.1";

            // Format: GAMEIDENTIFIER|ServerName|IP|Port|CurrentPlayers|MaxPlayers
            string message = $"{gameIdentifier}|{currentServerName}|{localIP}|{currentGamePort}|{currentPlayerCount}|{currentMaxPlayers}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Send to broadcast address (255.255.255.255)
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            broadcastClient.Send(data, data.Length, endPoint);

            // Also try subnet broadcast for better compatibility (e.g., 192.168.1.255)
            try
            {
                string subnetBroadcast = GetSubnetBroadcast(localIP);
                if (subnetBroadcast != null && subnetBroadcast != "255.255.255.255")
                {
                    IPEndPoint subnetEndPoint = new IPEndPoint(IPAddress.Parse(subnetBroadcast), broadcastPort);
                    broadcastClient.Send(data, data.Length, subnetEndPoint);
                }
            }
            catch { }

            // IMPORTANT: Also send to localhost for same-machine testing (Editor + Build)
            // UDP broadcast does NOT reach localhost, so we need explicit unicast
            try
            {
                IPEndPoint localhostEndPoint = new IPEndPoint(IPAddress.Loopback, broadcastPort);
                broadcastClient.Send(data, data.Length, localhostEndPoint);

                // Also send to alternative port in case listener had to use fallback port
                IPEndPoint localhostAltEndPoint = new IPEndPoint(IPAddress.Loopback, broadcastPort + 1);
                broadcastClient.Send(data, data.Length, localhostAltEndPoint);
            }
            catch { }

            Debug.Log($"[LANDiscovery] Broadcast sent: {currentServerName} ({currentPlayerCount}/{currentMaxPlayers})");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LANDiscovery] Broadcast failed: {ex.Message}");
        }
    }

    private string GetSubnetBroadcast(string localIP)
    {
        try
        {
            string[] parts = localIP.Split('.');
            if (parts.Length == 4)
            {
                // Assume /24 subnet (most common for home networks)
                return $"{parts[0]}.{parts[1]}.{parts[2]}.255";
            }
        }
        catch { }
        return null;
    }

    // ============================================================================
    // CLIENT LISTENING (Clients call these)
    // ============================================================================

    /// <summary>
    /// Start listening for server broadcasts.
    /// Called by clients looking for servers.
    /// </summary>
    public void StartListening()
    {
        if (isListening)
        {
            Debug.LogWarning("[LANDiscovery] Already listening");
            return;
        }

        // Don't listen if we're broadcasting (we're the host)
        if (isBroadcasting)
        {
            Debug.LogWarning("[LANDiscovery] Cannot listen while broadcasting");
            return;
        }

        try
        {
            // Try to bind to the broadcast port
            listenClient = new UdpClient();
            IgnoreConnectionReset(listenClient);
            listenClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listenClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
            listenClient.EnableBroadcast = true;

            actualListenPort = broadcastPort;
            isListening = true;
            lastUpdateTime = Time.realtimeSinceStartup;

            // Start listen thread
            listenThread = new Thread(ListenForBroadcasts);
            listenThread.IsBackground = true;
            listenThread.Start();

            Debug.Log($"[LANDiscovery] Started listening on port {broadcastPort}");
        }
        catch (SocketException ex)
        {
            Debug.LogWarning($"[LANDiscovery] Port {broadcastPort} in use (likely host on same machine): {ex.Message}");

            // Try alternative port - this is likely same-machine testing scenario
            try
            {
                int altPort = broadcastPort + 1;
                listenClient = new UdpClient();
                IgnoreConnectionReset(listenClient);
                listenClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listenClient.Client.Bind(new IPEndPoint(IPAddress.Any, altPort));
                listenClient.EnableBroadcast = true;

                actualListenPort = altPort;
                isListening = true;
                lastUpdateTime = Time.realtimeSinceStartup;

                listenThread = new Thread(ListenForBroadcasts);
                listenThread.IsBackground = true;
                listenThread.Start();

                Debug.Log($"[LANDiscovery] Started listening on alternative port {altPort} (same-machine mode)");
            }
            catch (Exception ex2)
            {
                Debug.LogError($"[LANDiscovery] Failed to start listening on alternative port: {ex2.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LANDiscovery] Failed to start listening: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop listening for broadcasts.
    /// </summary>
    public void StopListening()
    {
        if (!isListening) return;

        isListening = false;

        try
        {
            listenClient?.Close();
            listenClient = null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LANDiscovery] Error stopping listen: {ex.Message}");
        }

        // Wait for thread to finish
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Join(500);
        }
        listenThread = null;

        // Clear discovered servers
        lock (serverLock)
        {
            discoveredServers.Clear();
        }

        Debug.Log("[LANDiscovery] Stopped listening");
    }

    /// <summary>
    /// Refresh server list (clears discovered servers).
    /// </summary>
    public void RefreshServerList()
    {
        lock (serverLock)
        {
            discoveredServers.Clear();
        }
        OnServerListUpdated?.Invoke(DiscoveredServers);
        Debug.Log("[LANDiscovery] Server list refreshed");

        // Immediately poll localhost when refreshing
        PollLocalhost();
    }

    /// <summary>
    /// Actively poll localhost to discover same-machine servers (like Minecraft/Factorio).
    /// This is necessary because UDP broadcast doesn't reliably reach localhost on Windows.
    /// </summary>
    private void PollLocalhost()
    {
        if (!isListening || listenClient == null) return;

        try
        {
            // Send a discovery request to localhost on the broadcast port
            // The host will receive this and respond with server info
            // We use the listenClient so the response comes back to our bound port
            string request = $"{gameIdentifier}|DISCOVER";
            byte[] data = Encoding.UTF8.GetBytes(request);

            // Send to localhost on broadcast port (host is listening there)
            listenClient.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, broadcastPort));

            // Also try alternative port (in case host had to use fallback port)
            listenClient.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, broadcastPort + 1));

            // Debug.Log($"[LANDiscovery] Sent localhost discovery request (listening on port {actualListenPort})");
        }
        catch (Exception ex)
        {
            // Silently fail - this is a best-effort poll
            Debug.LogWarning($"[LANDiscovery] Localhost poll failed: {ex.Message}");
        }
    }

    private void ListenForBroadcasts()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isListening && listenClient != null)
        {
            try
            {
                // Set receive timeout so we can check isListening periodically
                listenClient.Client.ReceiveTimeout = 1000;

                byte[] data = listenClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);

                // Debug.Log($"[LANDiscovery] Received broadcast from {remoteEndPoint.Address}: {message}");

                ProcessBroadcast(message, remoteEndPoint.Address.ToString());
            }
            catch (SocketException ex)
            {
                // Timeout is expected, check if we should continue
                if (ex.SocketErrorCode != SocketError.TimedOut && isListening)
                {
                    Debug.LogWarning($"[LANDiscovery] Socket exception while listening: {ex.SocketErrorCode}");
                }
            }
            catch (Exception ex)
            {
                if (isListening)
                {
                    Debug.LogWarning($"[LANDiscovery] Listen error: {ex.Message}");
                }
            }
        }
    }

    private void ProcessBroadcast(string message, string senderIP)
    {
        // Parse: GAMEIDENTIFIER|ServerName|IP|Port|CurrentPlayers|MaxPlayers
        string[] parts = message.Split('|');

        if (parts.Length < 6 || parts[0] != gameIdentifier)
        {
            // Debug.Log($"[LANDiscovery] Ignored broadcast (wrong format or identifier): {message}");
            return;
        }

        try
        {
            ServerInfo server = new ServerInfo
            {
                serverName = parts[1],
                ipAddress = parts[2],
                port = int.Parse(parts[3]),
                currentPlayers = int.Parse(parts[4]),
                maxPlayers = int.Parse(parts[5]),
                lastSeenTime = 0 // Will be set on main thread
            };

            // Queue for main thread processing
            lock (pendingLock)
            {
                pendingServerUpdates.Enqueue(server);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LANDiscovery] Failed to parse broadcast: {ex.Message}");
        }
    }

    private void ProcessPendingServerUpdates()
    {
        List<ServerInfo> updates = new List<ServerInfo>();

        lock (pendingLock)
        {
            while (pendingServerUpdates.Count > 0)
            {
                updates.Add(pendingServerUpdates.Dequeue());
            }
        }

        foreach (var server in updates)
        {
            server.lastSeenTime = lastUpdateTime;
            string key = server.ipAddress;

            bool isNew;
            lock (serverLock)
            {
                isNew = !discoveredServers.ContainsKey(key);
                discoveredServers[key] = server;
            }

            if (isNew)
            {
                Debug.Log($"[LANDiscovery] Discovered new server: {server.serverName} at {server.ipAddress}");
                OnServerDiscovered?.Invoke(server);
            }

            OnServerListUpdated?.Invoke(DiscoveredServers);
        }
    }

    private void CleanupExpiredServers()
    {
        List<string> expiredKeys = new List<string>();

        lock (serverLock)
        {
            foreach (var kvp in discoveredServers)
            {
                if (kvp.Value.IsExpired(serverTimeout, lastUpdateTime))
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                discoveredServers.Remove(key);
                Debug.Log($"[LANDiscovery] Server expired: {key}");
                OnServerLost?.Invoke(key);
            }
        }

        if (expiredKeys.Count > 0)
        {
            OnServerListUpdated?.Invoke(DiscoveredServers);
        }
    }
}