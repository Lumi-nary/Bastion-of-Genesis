using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// COOPServerSetupUI controls the COOP server configuration panel.
/// Displays local IP/port and provides server start button.
/// Uses NetworkGameManager for FishNet integration.
/// </summary>
public class COOPServerSetupUI : MonoBehaviour
{
    [Header("COOP Server UI")]
    [SerializeField] private TextMeshProUGUI ipPortText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    [Header("Buttons")]
    [SerializeField] private Button startServerButton;
    [SerializeField] private Button startGameButton;

    [Header("Settings")]
    [SerializeField] private int minPlayersToStart = 1;

    private bool serverStarted = false;

    private void OnEnable()
    {
        // Reset state when panel becomes visible
        serverStarted = false;

        // Display local IP/port from NetworkGameManager
        UpdateIPDisplay();
        UpdateStatusText("Ready to Host");
        UpdateButtonStates();

        // Subscribe to network events
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnServerStarted += OnServerStarted;
            NetworkGameManager.Instance.OnServerStopped += OnServerStopped;
            NetworkGameManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }

        Debug.Log("[COOPServerSetupUI] COOP server panel enabled");
    }

    private void OnDisable()
    {
        // Unsubscribe from network events
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnServerStarted -= OnServerStarted;
            NetworkGameManager.Instance.OnServerStopped -= OnServerStopped;
            NetworkGameManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft -= OnPlayerLeft;
        }
    }

    private void UpdateIPDisplay()
    {
        if (ipPortText != null)
        {
            if (NetworkGameManager.Instance != null)
            {
                ipPortText.text = $"{NetworkGameManager.Instance.LocalIP}:{NetworkGameManager.Instance.Port}";
            }
            else
            {
                ipPortText.text = "Network not initialized";
            }
        }
    }

    private void UpdateStatusText(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText != null && NetworkGameManager.Instance != null)
        {
            playerCountText.text = $"Players: {NetworkGameManager.Instance.PlayerCount}/{NetworkGameManager.Instance.MaxPlayers}";
        }
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        // Start Server button: enabled when not hosting
        if (startServerButton != null)
        {
            startServerButton.interactable = !serverStarted;
        }

        // Start Game button: enabled when hosting and enough players
        if (startGameButton != null)
        {
            bool canStartGame = serverStarted &&
                NetworkGameManager.Instance != null &&
                NetworkGameManager.Instance.PlayerCount >= minPlayersToStart;
            startGameButton.interactable = canStartGame;
        }
    }

    // ============================================================================
    // NETWORK EVENT HANDLERS
    // ============================================================================

    private void OnServerStarted()
    {
        serverStarted = true;
        UpdateStatusText("Server Running - Waiting for players...");
        UpdatePlayerCount();
        UpdateButtonStates();

        // Start LAN broadcasting so clients can discover this server
        if (LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            string serverName = SaveManager.Instance?.pendingBaseName ?? "Planetfall Server";
            LANDiscovery.Instance.StartBroadcasting(
                serverName,
                NetworkGameManager.Instance.Port,
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    private void OnServerStopped()
    {
        serverStarted = false;
        UpdateStatusText("Server Stopped");
        UpdateButtonStates();

        // Stop LAN broadcasting
        if (LANDiscovery.Instance != null)
        {
            LANDiscovery.Instance.StopBroadcasting();
        }
    }

    private void OnPlayerJoined(FishNet.Connection.NetworkConnection conn)
    {
        UpdatePlayerCount();
        UpdateStatusText($"Player {conn.ClientId} joined!");

        // Update broadcast with new player count
        if (LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            LANDiscovery.Instance.UpdatePlayerCount(
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    private void OnPlayerLeft(FishNet.Connection.NetworkConnection conn)
    {
        UpdatePlayerCount();
        UpdateStatusText($"Player {conn.ClientId} left");

        // Update broadcast with new player count
        if (LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            LANDiscovery.Instance.UpdatePlayerCount(
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    // ============================================================================
    // PUBLIC API (Called by UI Buttons)
    // ============================================================================

    /// <summary>
    /// Start COOP server using NetworkGameManager.
    /// Called by "Start Server" button onClick event.
    /// </summary>
    public void OnStartServerClicked()
    {
        if (serverStarted)
        {
            Debug.LogWarning("[COOPServerSetupUI] Server already started");
            return;
        }

        if (NetworkGameManager.Instance == null)
        {
            Debug.LogError("[COOPServerSetupUI] NetworkGameManager not found!");
            UpdateStatusText("Error: Network not initialized");
            return;
        }

        UpdateStatusText("Starting server...");
        NetworkGameManager.Instance.StartHost();

        Debug.Log("[COOPServerSetupUI] Starting FishNet host");
    }

    /// <summary>
    /// Stop COOP server.
    /// Called by Back button or mode toggle.
    /// </summary>
    public void StopServer()
    {
        if (!serverStarted) return;

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.StopHost();
        }

        serverStarted = false;
        UpdateStatusText("Ready to Host");
        UpdateButtonStates();

        Debug.Log("[COOPServerSetupUI] Server stopped");
    }

    /// <summary>
    /// Start the game (load game scene for all players)
    /// </summary>
    public void OnStartGameClicked()
    {
        if (!serverStarted)
        {
            Debug.LogWarning("[COOPServerSetupUI] Server not running");
            return;
        }

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.LoadGameScene();
        }
    }
}
