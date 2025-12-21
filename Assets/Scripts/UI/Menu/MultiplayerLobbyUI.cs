using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FishNet.Connection;

/// <summary>
/// MultiplayerLobbyUI - Shared lobby for both host and client.
/// Shows base name, IP, connected players, and game start controls.
/// Host can start game, clients wait for host.
/// </summary>
public class MultiplayerLobbyUI : MonoBehaviour
{
    [Header("Lobby Info")]
    [SerializeField] private TextMeshProUGUI baseNameText;
    [SerializeField] private TextMeshProUGUI ipAddressText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Player List")]
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerItemPrefab;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button cancelButton;

    [Header("Settings")]
    [SerializeField] private int minPlayersToStart = 1; // Set to 1 to allow solo host start for testing

    // State
    private bool isHost;
    private bool isConnected;
    private bool hasInitialized;
    private float buttonUpdateTimer;
    private const float BUTTON_UPDATE_INTERVAL = 0.5f;
    private Dictionary<int, GameObject> playerItems = new Dictionary<int, GameObject>();
    private Canvas parentCanvas;

    private void Awake()
    {
        // Cache the parent canvas reference
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        // Periodically update button states to catch any missed state changes
        if (hasInitialized)
        {
            buttonUpdateTimer += Time.deltaTime;
            if (buttonUpdateTimer >= BUTTON_UPDATE_INTERVAL)
            {
                buttonUpdateTimer = 0f;
                RefreshPlayerList(); // Also updates buttons
            }
        }
    }

    private void OnEnable()
    {
        // IMPORTANT: Only initialize if the Canvas is actually being shown
        // This prevents auto-starting hosting when the scene loads with Canvas disabled
        if (parentCanvas != null && !parentCanvas.enabled)
        {
            Debug.Log("[MultiplayerLobbyUI] Canvas not enabled, skipping initialization");
            return;
        }

        InitializeLobby();
    }

    /// <summary>
    /// Public method to initialize the lobby when shown by MenuManager.
    /// </summary>
    public void InitializeLobby()
    {
        // Always reinitialize - unsubscribe first to prevent double-subscription
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkGameManager.Instance.OnClientDisconnected -= OnDisconnected;
            NetworkGameManager.Instance.OnServerStarted -= OnServerStarted;
            NetworkGameManager.Instance.OnClientConnected -= OnClientConnectedToServer;
        }

        hasInitialized = true;
        Debug.Log("[MultiplayerLobbyUI] Initializing lobby...");

        // Clear state first
        ClearPlayerList();

        // Subscribe to network events
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft += OnPlayerLeft;
            NetworkGameManager.Instance.OnClientDisconnected += OnDisconnected;
            NetworkGameManager.Instance.OnServerStarted += OnServerStarted;
            NetworkGameManager.Instance.OnClientConnected += OnClientConnectedToServer;
        }

        // Setup button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // Check if we're already online (client joining existing lobby)
        isHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsHost;
        isConnected = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsClient;

        if (isConnected)
        {
            // Already connected (client joining)
            UpdateLobbyInfo();
            UpdateButtonStates();
            RefreshPlayerList();
        }
        else
        {
            // Starting fresh (host creating lobby)
            UpdateStatus("Starting server...");
            UpdateButtonStates();
            StartHosting();
        }

        Debug.Log($"[MultiplayerLobbyUI] Lobby opened - IsHost: {isHost}, IsConnected: {isConnected}");
    }

    private void OnDisable()
    {
        // Reset initialization flag so we can reinitialize next time
        hasInitialized = false;

        // Unsubscribe from events
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkGameManager.Instance.OnClientDisconnected -= OnDisconnected;
            NetworkGameManager.Instance.OnServerStarted -= OnServerStarted;
            NetworkGameManager.Instance.OnClientConnected -= OnClientConnectedToServer;
        }

        ClearPlayerList();
    }

    private void OnServerStarted()
    {
        Debug.Log("[MultiplayerLobbyUI] Server started - updating status");
        isHost = true;
        isConnected = true;
        UpdateLobbyInfo();
        UpdateButtonStates();
        RefreshPlayerList();
    }

    private void OnClientConnectedToServer()
    {
        Debug.Log("[MultiplayerLobbyUI] Client connected to server");
        isConnected = true;
        UpdateLobbyInfo();
        UpdateButtonStates();
        RefreshPlayerList();
    }

    private void StartHosting()
    {
        if (NetworkGameManager.Instance == null) return;

        // Start the server if not already running
        if (!NetworkGameManager.Instance.IsServer)
        {
            NetworkGameManager.Instance.StartHost();
        }

        // Start LAN broadcasting with initial count of 1 (host counts as 1 player)
        // The actual PlayerCount will be updated once the host fully connects
        if (LANDiscovery.Instance != null)
        {
            string serverName = SaveManager.Instance?.pendingBaseName ?? "Planetfall Server";
            LANDiscovery.Instance.StartBroadcasting(
                serverName,
                NetworkGameManager.Instance.Port,
                1, // Host counts as 1 player immediately
                NetworkGameManager.Instance.MaxPlayers
            );
        }

        UpdateStatus("Waiting for players to join...");
    }

    private void UpdateLobbyInfo()
    {
        // Base name
        if (baseNameText != null)
        {
            string baseName = SaveManager.Instance?.pendingBaseName ?? "Unknown Base";
            baseNameText.text = baseName;
        }

        // IP Address
        if (ipAddressText != null)
        {
            if (NetworkGameManager.Instance != null)
            {
                string ip = NetworkGameManager.Instance.LocalIP;
                ushort port = NetworkGameManager.Instance.Port;
                ipAddressText.text = $"{ip}:{port}";
            }
            else
            {
                ipAddressText.text = "Not connected";
            }
        }

        // Status
        if (isHost)
        {
            UpdateStatus("Hosting - Waiting for players...");
        }
        else if (isConnected)
        {
            UpdateStatus("Connected - Waiting for host to start...");
        }
        else
        {
            UpdateStatus("Connecting...");
        }
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    private void UpdateButtonStates()
    {
        // Start Game button - only visible and enabled for host with enough players
        if (startGameButton != null)
        {
            // Check NetworkGameManager directly instead of local variables (more reliable)
            bool isActuallyHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsHost;
            int playerCount = NetworkGameManager.Instance?.PlayerCount ?? 0;

            bool canStart = isActuallyHost && playerCount >= minPlayersToStart;

            startGameButton.gameObject.SetActive(isActuallyHost);
            startGameButton.interactable = canStart;

            Debug.Log($"[MultiplayerLobbyUI] UpdateButtonStates - isHost:{isActuallyHost}, playerCount:{playerCount}, minPlayers:{minPlayersToStart}, canStart:{canStart}");
        }

        // Cancel button - always available
        if (cancelButton != null)
        {
            cancelButton.interactable = true;
        }
    }

    private void ClearPlayerList()
    {
        foreach (var item in playerItems.Values)
        {
            if (item != null)
                Destroy(item);
        }
        playerItems.Clear();
    }

    private void RefreshPlayerList()
    {
        ClearPlayerList();

        if (NetworkGameManager.Instance == null) return;

        // Always show host first if we're hosting or connected
        if (isHost || isConnected)
        {
            AddPlayerItem(0, "Player 1 (Host)");
        }

        if (isHost)
        {
            // Host: Show connected remote players (clients who joined)
            int remotePlayerCount = NetworkGameManager.Instance.RemotePlayerCount;
            for (int i = 0; i < remotePlayerCount; i++)
            {
                AddPlayerItem(i + 1, $"Player {i + 2}");
            }
        }
        else if (isConnected)
        {
            // Client: We know we're connected, so show ourselves as Player 2
            // Note: In a full implementation, this would come from networked state
            AddPlayerItem(1, "Player 2 (You)");
        }

        UpdateButtonStates();
    }

    private void AddPlayerItem(int playerId, string playerName)
    {
        if (playerListContent == null) return;

        // Create simple text if no prefab
        if (playerItemPrefab == null)
        {
            GameObject item = new GameObject($"Player_{playerId}");
            item.transform.SetParent(playerListContent, false);

            TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
            text.text = playerName;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Left;

            playerItems[playerId] = item;
        }
        else
        {
            GameObject item = Instantiate(playerItemPrefab, playerListContent);
            item.name = $"Player_{playerId}";

            // Try to set player name in text component
            TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = playerName;
            }

            playerItems[playerId] = item;
        }
    }

    // ============================================================================
    // NETWORK EVENT HANDLERS
    // ============================================================================

    private void OnPlayerJoined(NetworkConnection conn)
    {
        Debug.Log($"[MultiplayerLobbyUI] Player joined: {conn.ClientId}");

        RefreshPlayerList();

        // Don't show status message for host's own connection
        bool isLocal = NetworkGameManager.Instance != null &&
                       NetworkGameManager.Instance.IsHost &&
                       NetworkGameManager.Instance.IsServer && // Ensure we are server
                       conn.ClientId == 0; // Host is usually 0, but check against known local ID if possible

        // Actually, NetworkGameManager handles filtering, but if it leaks through during race:
        if (NetworkGameManager.Instance != null && conn.ClientId == NetworkGameManager.Instance.LocalIP.GetHashCode()) { /* hypothetical */ }

        // Better check: Use the ID from NetworkGameManager if available
        int hostId = -1;
        // Use reflection or just trust the ClientId logic
        // For now, let's just make the message human-friendly (1-based)
        int playerNum = conn.ClientId + 1;

        UpdateStatus($"Player {playerNum} joined! ({NetworkGameManager.Instance?.PlayerCount}/{NetworkGameManager.Instance?.MaxPlayers})");

        // Update broadcast
        if (isHost && LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            LANDiscovery.Instance.UpdatePlayerCount(
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    private void OnPlayerLeft(NetworkConnection conn)
    {
        Debug.Log($"[MultiplayerLobbyUI] Player left: {conn.ClientId}");

        RefreshPlayerList();

        int playerNum = conn.ClientId + 1;
        UpdateStatus($"Player {playerNum} left ({NetworkGameManager.Instance?.PlayerCount}/{NetworkGameManager.Instance?.MaxPlayers})");

        // Update broadcast
        if (isHost && LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            LANDiscovery.Instance.UpdatePlayerCount(
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    private void OnDisconnected()
    {
        Debug.Log("[MultiplayerLobbyUI] Disconnected from server");

        UpdateStatus("Disconnected from server");
        isConnected = false;

        // Reset mode
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.pendingMode = GameMode.Singleplayer;
        }

        // Return to main menu after a moment
        Invoke(nameof(ReturnToMainMenu), 1f);
    }

    // ============================================================================
    // BUTTON HANDLERS
    // ============================================================================

    private void OnStartGameClicked()
    {
        if (!isHost)
        {
            Debug.LogWarning("[MultiplayerLobbyUI] Only host can start game");
            return;
        }

        if (NetworkGameManager.Instance == null)
        {
            Debug.LogError("[MultiplayerLobbyUI] NetworkGameManager not found");
            return;
        }

        // Ensure COOP mode is locked in
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.pendingMode = GameMode.COOP;
        }

        // Stop broadcasting (game is starting)
        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.StopBroadcasting();
        }

        UpdateStatus("Starting game...");
        startGameButton.interactable = false;

        // Load the cutscene scene for all players
        Debug.Log("[MultiplayerLobbyUI] Host starting game - loading CutsceneScene");
        NetworkGameManager.Instance.LoadGameScene();
    }

    private void OnCancelClicked()
    {
        // Stop hosting/broadcasting
        if (isHost)
        {
            if (LANDiscovery.Instance != null)
            {
                LANDiscovery.Instance.StopBroadcasting();
            }
        }

        // Reset mode
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.pendingMode = GameMode.Singleplayer;
        }

        // Disconnect
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.Disconnect();
        }

        ReturnToMainMenu();
    }

    private void ReturnToMainMenu()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ShowMainMenuCanvas();
        }
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Called by client after successfully connecting to show lobby.
    /// </summary>
    public void ShowAsClient()
    {
        isHost = false;
        isConnected = true;
        
        // Ensure SaveManager knows we are in COOP mode (Story 2.4/Mission Sync)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.pendingMode = GameMode.COOP;
            Debug.Log("[MultiplayerLobbyUI] Setting pendingMode to COOP (Client)");
        }

        UpdateLobbyInfo();
        UpdateButtonStates();
        RefreshPlayerList();
    }

    /// <summary>
    /// Called by host to initialize lobby.
    /// </summary>
    public void ShowAsHost()
    {
        isHost = true;
        isConnected = true;

        // Ensure SaveManager knows we are in COOP mode
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.pendingMode = GameMode.COOP;
            Debug.Log("[MultiplayerLobbyUI] Setting pendingMode to COOP (Host)");
        }

        UpdateLobbyInfo();
        UpdateButtonStates();
        RefreshPlayerList();
    }
}
