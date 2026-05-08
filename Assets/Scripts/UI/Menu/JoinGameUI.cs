using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// LAN server browser UI on the joinGameCanvas in MenuScene.
/// Discovers servers via LANBroadcaster and allows joining.
/// </summary>
public class JoinGameUI : MonoBehaviour
{
    [Header("Server List")]
    [SerializeField] private Transform serverListContent;
    [SerializeField] private GameObject serverItemPrefab;

    [Header("Manual Connect")]
    [SerializeField] private TMP_InputField manualIPInput;
    [SerializeField] private Button manualConnectButton;

    [Header("Controls")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly Dictionary<string, GameObject> serverItems = new();
    private LANBroadcaster broadcaster;

    private void OnEnable()
    {
        broadcaster = LANBroadcaster.Instance;
        if (broadcaster == null)
        {
            // Try to find or create one
            broadcaster = FindAnyObjectByType<LANBroadcaster>();
        }

        if (broadcaster != null)
        {
            broadcaster.OnServerDiscovered += OnServerDiscovered;
            broadcaster.OnServerLost += OnServerLost;
            broadcaster.StartListening();
        }

        ClearServerList();
        SetStatus("Searching for LAN games...");

        // Re-populate from any already discovered servers
        if (broadcaster != null)
        {
            foreach (var kvp in broadcaster.DiscoveredServers)
            {
                OnServerDiscovered(kvp.Value);
            }
        }
    }

    private void OnDisable()
    {
        if (broadcaster != null)
        {
            broadcaster.OnServerDiscovered -= OnServerDiscovered;
            broadcaster.OnServerLost -= OnServerLost;
            broadcaster.StopListening();
        }

        ClearServerList();
    }

    private void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        if (manualConnectButton != null)
            manualConnectButton.onClick.AddListener(OnManualConnectClicked);
    }

    private void OnServerDiscovered(ServerInfo info)
    {
        string key = $"{info.ip}:{info.port}";

        if (serverItems.ContainsKey(key))
        {
            // Update existing entry
            UpdateServerItem(serverItems[key], info);
            return;
        }

        if (serverItemPrefab == null || serverListContent == null) return;

        GameObject item = Instantiate(serverItemPrefab, serverListContent);
        serverItems[key] = item;
        UpdateServerItem(item, info);

        // Wire up join button
        Button joinBtn = item.GetComponentInChildren<Button>();
        if (joinBtn != null)
        {
            string capturedIP = info.ip;
            int capturedPort = info.port;
            joinBtn.onClick.AddListener(() => JoinServer(capturedIP, capturedPort));
            if (MenuManager.Instance != null)
                MenuManager.Instance.RegisterButtonClickSfx(joinBtn);
        }

        SetStatus($"Found {serverItems.Count} server(s)");
    }

    private void OnServerLost(string key)
    {
        if (serverItems.TryGetValue(key, out GameObject item))
        {
            Destroy(item);
            serverItems.Remove(key);
        }

        if (serverItems.Count == 0)
            SetStatus("Searching for LAN games...");
        else
            SetStatus($"Found {serverItems.Count} server(s)");
    }

    private void UpdateServerItem(GameObject item, ServerInfo info)
    {
        // Find text components in the server item prefab
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 1)
            texts[0].text = info.serverName;
        if (texts.Length >= 2)
            texts[1].text = $"{info.ip}:{info.port}";
        if (texts.Length >= 3)
            texts[2].text = $"{info.playerCount}/{info.maxPlayers}";
    }

    private void JoinServer(string ip, int port)
    {
        SetStatus($"Connecting to {ip}:{port}...");
        Debug.Log($"[JoinGameUI] Joining server at {ip}:{port}");

        if (InstanceFinder.NetworkManager == null)
        {
            SetStatus("Error: NetworkManager not found");
            return;
        }

        // Set the transport address and port
        var transport = InstanceFinder.NetworkManager.TransportManager.Transport;
        if (transport is FishNet.Transporting.Tugboat.Tugboat tugboat)
        {
            tugboat.SetClientAddress(ip);
            tugboat.SetPort((ushort)port);
        }

        // Listen for scene load to unload MenuScene after gameplay scene arrives
        InstanceFinder.SceneManager.OnLoadEnd += OnFishNetSceneLoaded;
        LoadingScreenManager.EnsureInstance().ShowIndeterminate("Connecting...");
        InstanceFinder.ClientManager.StartConnection();
    }

    private void OnFishNetSceneLoaded(SceneLoadEndEventArgs args)
    {
        // Unsubscribe immediately
        if (InstanceFinder.SceneManager != null)
            InstanceFinder.SceneManager.OnLoadEnd -= OnFishNetSceneLoaded;

        // Unload MenuScene now that gameplay scene is loaded
        var menuScene = UnitySceneManager.GetSceneByName("MenuScene");
        if (menuScene.isLoaded)
        {
            UnitySceneManager.UnloadSceneAsync(menuScene);
            Debug.Log("[JoinGameUI] Unloaded MenuScene after joining server");
        }

        LoadingScreenManager.Instance?.Hide();
    }

    private void OnManualConnectClicked()
    {
        if (manualIPInput == null || string.IsNullOrWhiteSpace(manualIPInput.text)) return;

        string input = manualIPInput.text.Trim();
        string ip;
        int port = 7770; // Default FishNet Tugboat port

        // Parse ip:port format
        if (input.Contains(":"))
        {
            string[] parts = input.Split(':');
            ip = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out int parsedPort))
                port = parsedPort;
        }
        else
        {
            ip = input;
        }

        JoinServer(ip, port);
    }

    private void OnRefreshClicked()
    {
        ClearServerList();
        SetStatus("Searching for LAN games...");

        if (broadcaster != null)
        {
            broadcaster.StopListening();
            broadcaster.StartListening();
        }
    }

    private void OnBackClicked()
    {
        if (MenuManager.Instance != null)
            MenuManager.Instance.ShowMainMenuCanvas();
    }

    private void ClearServerList()
    {
        foreach (var item in serverItems.Values)
        {
            if (item != null) Destroy(item);
        }
        serverItems.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
