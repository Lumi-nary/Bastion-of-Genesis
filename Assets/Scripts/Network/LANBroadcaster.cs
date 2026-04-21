using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// UDP broadcast/listen for LAN server discovery.
/// Server broadcasts game info on a fixed port, clients listen and maintain a discovered server list.
/// </summary>
public class LANBroadcaster : MonoBehaviour
{
    public static LANBroadcaster Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int broadcastPort = 47777;
    [SerializeField] private float broadcastInterval = 1f;
    [SerializeField] private float serverTimeout = 5f;

    private const string GAME_IDENTIFIER = "BASTION_OF_GENESIS";

    // Broadcasting (server side)
    private UdpClient broadcastClient;
    private bool isBroadcasting;
    private float broadcastTimer;
    private ServerInfo localServerInfo;

    // Listening (client side)
    private UdpClient listenClient;
    private bool isListening;
    private readonly Dictionary<string, ServerInfo> discoveredServers = new();
    private readonly Dictionary<string, float> serverLastSeen = new();

    // Events
    public event Action<ServerInfo> OnServerDiscovered;
    public event Action<string> OnServerLost;

    public IReadOnlyDictionary<string, ServerInfo> DiscoveredServers => discoveredServers;

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
        StopBroadcasting();
        StopListening();
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (isBroadcasting)
        {
            broadcastTimer -= Time.unscaledDeltaTime;
            if (broadcastTimer <= 0f)
            {
                broadcastTimer = broadcastInterval;
                SendBroadcast();
            }
        }

        if (isListening)
        {
            ReceiveBroadcasts();
            CleanupStaleServers();
        }
    }

    // ========================================================================
    // SERVER SIDE - Broadcasting
    // ========================================================================

    public void StartBroadcasting(string serverName, int gamePort, int playerCount, int maxPlayers)
    {
        StopBroadcasting();

        localServerInfo = new ServerInfo
        {
            serverName = serverName,
            ip = GetLocalIP(),
            port = gamePort,
            playerCount = playerCount,
            maxPlayers = maxPlayers
        };

        try
        {
            broadcastClient = new UdpClient();
            broadcastClient.EnableBroadcast = true;
            isBroadcasting = true;
            broadcastTimer = 0f; // Send immediately
            Debug.Log($"[LANBroadcaster] Broadcasting started: {localServerInfo.serverName} on port {broadcastPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LANBroadcaster] Failed to start broadcasting: {e.Message}");
        }
    }

    public void StopBroadcasting()
    {
        isBroadcasting = false;
        if (broadcastClient != null)
        {
            try { broadcastClient.Close(); } catch { }
            broadcastClient = null;
        }
    }

    public void UpdatePlayerCount(int current, int max)
    {
        if (localServerInfo != null)
        {
            localServerInfo.playerCount = current;
            localServerInfo.maxPlayers = max;
        }
    }

    private void SendBroadcast()
    {
        if (broadcastClient == null || localServerInfo == null) return;

        try
        {
            string message = $"{GAME_IDENTIFIER}|{localServerInfo.serverName}|{localServerInfo.ip}|{localServerInfo.port}|{localServerInfo.playerCount}|{localServerInfo.maxPlayers}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            broadcastClient.Send(data, data.Length, endPoint);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LANBroadcaster] Broadcast send failed: {e.Message}");
        }
    }

    // ========================================================================
    // CLIENT SIDE - Listening
    // ========================================================================

    public void StartListening()
    {
        StopListening();
        discoveredServers.Clear();
        serverLastSeen.Clear();

        try
        {
            // SO_REUSEADDR allows multiple instances (editor + build) to listen on the same port
            listenClient = new UdpClient();
            listenClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listenClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
            listenClient.EnableBroadcast = true;
            listenClient.Client.Blocking = false;
            isListening = true;
            Debug.Log($"[LANBroadcaster] Listening for servers on port {broadcastPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LANBroadcaster] Failed to start listening: {e.Message}");
        }
    }

    public void StopListening()
    {
        isListening = false;
        if (listenClient != null)
        {
            try { listenClient.Close(); } catch { }
            listenClient = null;
        }
        discoveredServers.Clear();
        serverLastSeen.Clear();
    }

    private void ReceiveBroadcasts()
    {
        if (listenClient == null) return;

        try
        {
            while (listenClient.Available > 0)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = listenClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);
                ParseBroadcast(message, remoteEP.Address.ToString());
            }
        }
        catch (SocketException)
        {
            // No data available - normal for non-blocking
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LANBroadcaster] Receive error: {e.Message}");
        }
    }

    private void ParseBroadcast(string message, string senderIP)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 6 || parts[0] != GAME_IDENTIFIER) return;

        string ip = parts[2];
        // Use sender IP if broadcast reports localhost
        if (ip == "127.0.0.1" || ip == "0.0.0.0")
            ip = senderIP;

        string key = $"{ip}:{parts[3]}";

        var info = new ServerInfo
        {
            serverName = parts[1],
            ip = ip,
            port = int.Parse(parts[3]),
            playerCount = int.Parse(parts[4]),
            maxPlayers = int.Parse(parts[5])
        };

        bool isNew = !discoveredServers.ContainsKey(key);
        discoveredServers[key] = info;
        serverLastSeen[key] = Time.unscaledTime;

        if (isNew)
        {
            OnServerDiscovered?.Invoke(info);
        }
    }

    private void CleanupStaleServers()
    {
        List<string> stale = null;
        foreach (var kvp in serverLastSeen)
        {
            if (Time.unscaledTime - kvp.Value > serverTimeout)
            {
                stale ??= new List<string>();
                stale.Add(kvp.Key);
            }
        }

        if (stale == null) return;

        foreach (string key in stale)
        {
            discoveredServers.Remove(key);
            serverLastSeen.Remove(key);
            OnServerLost?.Invoke(key);
        }
    }

    // ========================================================================
    // UTILITY
    // ========================================================================

    public static string GetLocalIP()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}

/// <summary>
/// Data about a discovered LAN server.
/// </summary>
[Serializable]
public class ServerInfo
{
    public string serverName;
    public string ip;
    public int port;
    public int playerCount;
    public int maxPlayers;
}
