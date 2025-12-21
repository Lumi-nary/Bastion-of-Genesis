using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// JoinGameUI - UI for joining a COOP game as client.
/// Shows discovered LAN servers and allows connection.
/// Uses MenuManager canvas switching pattern.
/// </summary>
public class JoinGameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform serverListContent;
    [SerializeField] private GameObject serverItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject noServersPanel;

    [Header("Manual Connection (Fallback)")]
    [SerializeField] private TMP_InputField manualIPInput;
    [SerializeField] private Button manualConnectButton;

    [Header("Settings")]
    [SerializeField] private float searchTimeout = 3f;

    private bool isConnecting;
    private bool isSearching;
    private bool isSubscribedToNetwork;
    private bool isSubscribedToLANDiscovery;
    private bool connectionSucceeded; // Track if we connected successfully (don't disconnect on disable)
    private string lastJoinedServerName; // Track which server we're joining (for lobby display)
    private Dictionary<string, GameObject> serverItems = new Dictionary<string, GameObject>();
    private Coroutine searchTimeoutCoroutine;

    private void OnEnable()
    {
        // Reset state
        isConnecting = false;
        isSearching = true;
        connectionSucceeded = false;
        UpdateStatus("Searching for games...");

        // Clear existing server items
        ClearServerList();

        // Hide no servers panel during search
        if (noServersPanel != null)
            noServersPanel.SetActive(false);

        // Subscribe to LANDiscovery events (may fail if not ready yet)
        SubscribeToLANDiscovery();

        // Subscribe to network events (may fail if NetworkGameManager not ready yet)
        SubscribeToNetworkEvents();

        // Setup button listeners
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (manualConnectButton != null)
        {
            manualConnectButton.onClick.RemoveAllListeners();
            manualConnectButton.onClick.AddListener(OnManualConnectClicked);
        }

        // Start search timeout coroutine
        if (searchTimeoutCoroutine != null)
            StopCoroutine(searchTimeoutCoroutine);
        searchTimeoutCoroutine = StartCoroutine(SearchTimeoutRoutine());

        Debug.Log("[JoinGameUI] Panel enabled, listening for servers");
    }

    /// <summary>
    /// Subscribe to NetworkGameManager events. Safe to call multiple times.
    /// </summary>
    private void SubscribeToNetworkEvents()
    {
        if (isSubscribedToNetwork) return;

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnClientConnected += OnConnected;
            NetworkGameManager.Instance.OnClientDisconnected += OnDisconnected;
            isSubscribedToNetwork = true;
            Debug.Log("[JoinGameUI] Subscribed to network events");
        }
        else
        {
            Debug.LogWarning("[JoinGameUI] NetworkGameManager.Instance is null, will retry subscription later");
        }
    }

    /// <summary>
    /// Unsubscribe from NetworkGameManager events.
    /// </summary>
    private void UnsubscribeFromNetworkEvents()
    {
        if (!isSubscribedToNetwork) return;

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnClientConnected -= OnConnected;
            NetworkGameManager.Instance.OnClientDisconnected -= OnDisconnected;
        }
        isSubscribedToNetwork = false;
    }

    /// <summary>
    /// Subscribe to LANDiscovery events. Safe to call multiple times.
    /// </summary>
    private void SubscribeToLANDiscovery()
    {
        if (isSubscribedToLANDiscovery) return;

        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.OnServerListUpdated += OnServerListUpdated;
            LANDiscovery.Instance.StartListening();
            isSubscribedToLANDiscovery = true;
            Debug.Log("[JoinGameUI] Subscribed to LANDiscovery events");
        }
        else
        {
            Debug.LogWarning("[JoinGameUI] LANDiscovery.Instance is null, will retry later");
        }
    }

    /// <summary>
    /// Unsubscribe from LANDiscovery events.
    /// </summary>
    private void UnsubscribeFromLANDiscovery()
    {
        if (!isSubscribedToLANDiscovery) return;

        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.OnServerListUpdated -= OnServerListUpdated;
            LANDiscovery.Instance.StopListening();
        }
        isSubscribedToLANDiscovery = false;
    }

    private void OnDisable()
    {
        // Stop timeout coroutine
        if (searchTimeoutCoroutine != null)
        {
            StopCoroutine(searchTimeoutCoroutine);
            searchTimeoutCoroutine = null;
        }

        // Unsubscribe from events
        UnsubscribeFromLANDiscovery();
        UnsubscribeFromNetworkEvents();

        // Only disconnect if we were connecting and connection did NOT succeed
        // (If connection succeeded, we're transitioning to lobby - don't disconnect!)
        if (isConnecting && !connectionSucceeded && NetworkGameManager.Instance != null)
        {
            Debug.Log("[JoinGameUI] OnDisable - disconnecting (connection was in progress but not completed)");
            NetworkGameManager.Instance.Disconnect();
        }

        ClearServerList();
    }

    private IEnumerator SearchTimeoutRoutine()
    {
        yield return new WaitForSeconds(searchTimeout);

        isSearching = false;

        // If no servers found after timeout, show the no servers panel
        if (serverItems.Count == 0)
        {
            if (noServersPanel != null)
                noServersPanel.SetActive(true);
            UpdateStatus("No games found. Ask host to start a server.");
        }

        searchTimeoutCoroutine = null;
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    private void ClearServerList()
    {
        foreach (var item in serverItems.Values)
        {
            if (item != null)
                Destroy(item);
        }
        serverItems.Clear();
    }

    // ============================================================================
    // LAN DISCOVERY CALLBACKS
    // ============================================================================

    private void OnServerListUpdated(List<LANDiscovery.ServerInfo> servers)
    {
        // If we found servers, stop searching and cancel timeout
        if (servers.Count > 0)
        {
            isSearching = false;
            if (searchTimeoutCoroutine != null)
            {
                StopCoroutine(searchTimeoutCoroutine);
                searchTimeoutCoroutine = null;
            }
        }

        // Update no servers panel visibility (only show if not searching and no servers)
        if (noServersPanel != null)
        {
            noServersPanel.SetActive(!isSearching && servers.Count == 0);
        }

        if (servers.Count == 0)
        {
            if (isSearching)
            {
                UpdateStatus("Searching for games...");
            }
            else
            {
                UpdateStatus("No games found. Ask host to start a server.");
            }
        }
        else
        {
            UpdateStatus($"Found {servers.Count} game(s)");
        }

        // Remove items for servers no longer in list
        List<string> toRemove = new List<string>();
        foreach (var key in serverItems.Keys)
        {
            bool found = false;
            foreach (var server in servers)
            {
                if (server.ipAddress == key)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                toRemove.Add(key);
            }
        }

        foreach (var key in toRemove)
        {
            if (serverItems.TryGetValue(key, out GameObject item))
            {
                Destroy(item);
                serverItems.Remove(key);
            }
        }

        // Add or update server items
        foreach (var server in servers)
        {
            if (serverItems.ContainsKey(server.ipAddress))
            {
                // Update existing item
                UpdateServerItem(serverItems[server.ipAddress], server);
            }
            else
            {
                // Create new item
                CreateServerItem(server);
            }
        }
    }

    private void CreateServerItem(LANDiscovery.ServerInfo server)
    {
        if (serverListContent == null || serverItemPrefab == null) return;

        GameObject item = Instantiate(serverItemPrefab, serverListContent);
        serverItems[server.ipAddress] = item;

        UpdateServerItem(item, server);

        // Setup join button
        Button joinBtn = item.GetComponentInChildren<Button>();
        if (joinBtn != null)
        {
            string ip = server.ipAddress; // Capture for lambda
            string serverName = server.serverName; // Capture for lambda
            joinBtn.onClick.AddListener(() => OnJoinServerClicked(ip, serverName));
        }
    }

    private void UpdateServerItem(GameObject item, LANDiscovery.ServerInfo server)
    {
        if (item == null) return;

        // Find and update text components
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (var text in texts)
        {
            string name = text.gameObject.name.ToLower();

            if (name.Contains("name") || name.Contains("server"))
            {
                text.text = server.serverName;
            }
            else if (name.Contains("player") || name.Contains("count"))
            {
                text.text = $"{server.currentPlayers}/{server.maxPlayers}";
            }
            else if (name.Contains("ip") || name.Contains("address"))
            {
                text.text = server.ipAddress;
            }
        }

        // Disable join button if server is full
        Button joinBtn = item.GetComponentInChildren<Button>();
        if (joinBtn != null)
        {
            joinBtn.interactable = server.currentPlayers < server.maxPlayers;
        }
    }

    // ============================================================================
    // NETWORK EVENT HANDLERS
    // ============================================================================

    private void OnConnected()
    {
        Debug.Log("[JoinGameUI] OnConnected callback fired!");

        // Mark connection as successful BEFORE transitioning
        // This prevents OnDisable from disconnecting when we switch canvases
        connectionSucceeded = true;
        isConnecting = false;

        UpdateStatus("Connected! Joining lobby...");

        // Stop listening for servers
        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.StopListening();
        }

        // Set pending mode to COOP so lobby knows this is multiplayer
        // Also set the base name from the server we joined (client doesn't have this otherwise)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.pendingMode = GameMode.COOP;

            // Use the server name we stored when clicking join
            if (!string.IsNullOrEmpty(lastJoinedServerName))
            {
                SaveManager.Instance.pendingBaseName = lastJoinedServerName;
                Debug.Log($"[JoinGameUI] Set pendingBaseName to '{lastJoinedServerName}'");
            }

            Debug.Log("[JoinGameUI] Set pendingMode to COOP");
        }

        // Transition to lobby immediately (don't use coroutine - it can be interrupted)
        TransitionToLobby();
    }

    private void TransitionToLobby()
    {
        Debug.Log("[JoinGameUI] TransitionToLobby called");

        // Transition to multiplayer lobby
        if (MenuManager.Instance != null)
        {
            Debug.Log("[JoinGameUI] Switching to multiplayer canvas...");
            MenuManager.Instance.ShowMultiplayerCanvas();
        }
        else
        {
            Debug.LogError("[JoinGameUI] MenuManager.Instance is null!");
        }
    }

    private void OnDisconnected()
    {
        isConnecting = false;
        UpdateStatus("Disconnected from server");
        Debug.Log("[JoinGameUI] Disconnected from server");
    }

    // ============================================================================
    // BUTTON HANDLERS
    // ============================================================================

    private void OnJoinServerClicked(string ipAddress, string serverName = null)
    {
        if (isConnecting)
        {
            Debug.LogWarning("[JoinGameUI] Already connecting");
            return;
        }

        if (NetworkGameManager.Instance == null)
        {
            UpdateStatus("Error: Network not initialized");
            return;
        }

        // CRITICAL: Ensure we're subscribed to network events before connecting
        // (Subscription may have failed in OnEnable if NetworkGameManager wasn't ready)
        SubscribeToNetworkEvents();

        if (!isSubscribedToNetwork)
        {
            UpdateStatus("Error: Failed to subscribe to network events");
            Debug.LogError("[JoinGameUI] Cannot connect - failed to subscribe to network events");
            return;
        }

        isConnecting = true;
        connectionSucceeded = false; // Reset for new connection attempt
        lastJoinedServerName = serverName ?? $"Game at {ipAddress}"; // Store for lobby display
        UpdateStatus($"Connecting to {ipAddress}...");

        NetworkGameManager.Instance.JoinGame(ipAddress);

        Debug.Log($"[JoinGameUI] Joining game at {ipAddress} (server: {lastJoinedServerName})");
    }

    private void OnManualConnectClicked()
    {
        if (manualIPInput == null) return;

        string ip = manualIPInput.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            UpdateStatus("Please enter an IP address");
            return;
        }

        if (NetworkGameManager.Instance != null && !NetworkGameManager.Instance.IsValidIPAddress(ip))
        {
            UpdateStatus("Invalid IP address format");
            return;
        }

        OnJoinServerClicked(ip);
    }

    private void OnRefreshClicked()
    {
        // Ensure we're subscribed to LANDiscovery (may have failed at startup)
        SubscribeToLANDiscovery();

        // Restart search
        isSearching = true;
        UpdateStatus("Searching for games...");
        ClearServerList();

        // Hide no servers panel during search
        if (noServersPanel != null)
            noServersPanel.SetActive(false);

        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.RefreshServerList();
        }
        else
        {
            Debug.LogWarning("[JoinGameUI] LANDiscovery not available for refresh");
        }

        // Restart timeout coroutine
        if (searchTimeoutCoroutine != null)
            StopCoroutine(searchTimeoutCoroutine);
        searchTimeoutCoroutine = StartCoroutine(SearchTimeoutRoutine());
    }

    private void OnBackClicked()
    {
        // Disconnect if connecting
        if (isConnecting && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.Disconnect();
        }

        // Return to main menu via MenuManager (canvas switching pattern)
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowMainMenuCanvas();
        }
    }

    /// <summary>
    /// Disconnect from server.
    /// </summary>
    public void Disconnect()
    {
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.Disconnect();
        }
        isConnecting = false;
        UpdateStatus("Disconnected");
    }
}
