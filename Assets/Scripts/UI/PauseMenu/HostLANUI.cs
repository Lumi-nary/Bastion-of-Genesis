using UnityEngine;
using UnityEngine.UI;
using FishNet.Connection;
using TMPro;

/// <summary>
/// UI component for "Open to LAN" functionality in the pause menu.
/// Uses CoopBootstrap (always available) for server control.
/// </summary>
public class HostLANUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button openLanButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI infoText;

    private void Start()
    {
        if (openLanButton != null)
            openLanButton.onClick.AddListener(OnOpenLanClicked);

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (openLanButton != null)
            openLanButton.onClick.RemoveListener(OnOpenLanClicked);

        if (CoopBootstrap.Instance != null)
        {
            CoopBootstrap.Instance.OnPlayerJoined -= OnPlayerChanged;
            CoopBootstrap.Instance.OnPlayerLeft -= OnPlayerChanged;
        }
    }

    private void OnOpenLanClicked()
    {
        if (CoopBootstrap.Instance == null)
        {
            if (ModalDialog.Instance != null)
                ModalDialog.Instance.ShowError("Network system not available.");
            return;
        }

        bool isOnline = CoopBootstrap.Instance.IsOnline;

        if (isOnline)
        {
            // Close LAN
            if (ModalDialog.Instance != null)
            {
                ModalDialog.Instance.ShowConfirmation(
                    "This will disconnect all players. Close LAN?",
                    () =>
                    {
                        CloseLAN();
                        UpdateUI();

                        if (ModalDialog.Instance != null)
                            ModalDialog.Instance.ShowInfo("LAN Closed", "Multiplayer disabled.");
                    },
                    null
                );
            }
        }
        else
        {
            OpenLAN();
        }
    }

    private void OpenLAN()
    {
        if (CoopBootstrap.Instance == null) return;

        CoopBootstrap.Instance.StartServer();

        // Start LAN broadcasting
        if (LANBroadcaster.Instance != null)
        {
            string serverName = SaveManager.Instance != null ? SaveManager.Instance.pendingBaseName : null;
            if (string.IsNullOrWhiteSpace(serverName))
            {
                string playerName = SettingsManager.Instance != null
                    ? SettingsManager.Instance.CurrentSettings?.playerName
                    : null;
                serverName = $"{SettingsData.NormalizePlayerName(playerName)}'s Game";
            }

            LANBroadcaster.Instance.StartBroadcasting(
                serverName,
                CoopBootstrap.Instance.Port,
                1, // Host counts as 1
                CoopBootstrap.Instance.MaxPlayers
            );
        }

        // Subscribe to player events
        CoopBootstrap.Instance.OnPlayerJoined -= OnPlayerChanged;
        CoopBootstrap.Instance.OnPlayerLeft -= OnPlayerChanged;
        CoopBootstrap.Instance.OnPlayerJoined += OnPlayerChanged;
        CoopBootstrap.Instance.OnPlayerLeft += OnPlayerChanged;

        UpdateUI();

        Debug.Log($"[HostLANUI] LAN opened on {CoopBootstrap.Instance.LocalIP}:{CoopBootstrap.Instance.Port}");

        if (ModalDialog.Instance != null)
            ModalDialog.Instance.ShowInfo("Open to LAN",
                $"Players can join at:\n{CoopBootstrap.Instance.LocalIP}:{CoopBootstrap.Instance.Port}");
    }

    private void CloseLAN()
    {
        if (CoopBootstrap.Instance != null)
        {
            CoopBootstrap.Instance.OnPlayerJoined -= OnPlayerChanged;
            CoopBootstrap.Instance.OnPlayerLeft -= OnPlayerChanged;
            CoopBootstrap.Instance.StopServer();
        }

        if (LANBroadcaster.Instance != null)
            LANBroadcaster.Instance.StopBroadcasting();

        if (CoopSceneLoader.Instance != null)
            CoopSceneLoader.Instance.UnregisterScene();

        Debug.Log("[HostLANUI] LAN closed");
    }

    private void OnPlayerChanged(NetworkConnection conn)
    {
        if (LANBroadcaster.Instance != null && CoopBootstrap.Instance != null)
        {
            LANBroadcaster.Instance.UpdatePlayerCount(
                CoopBootstrap.Instance.PlayerCount,
                CoopBootstrap.Instance.MaxPlayers
            );
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        bool isOnline = CoopBootstrap.Instance != null && CoopBootstrap.Instance.IsOnline;

        if (buttonText != null)
            buttonText.text = isOnline ? "Close LAN" : "Open to LAN";

        if (infoText != null)
        {
            if (isOnline)
            {
                string ip = CoopBootstrap.Instance.LocalIP;
                int players = CoopBootstrap.Instance.PlayerCount;
                int max = CoopBootstrap.Instance.MaxPlayers;
                infoText.text = $"{ip}:{CoopBootstrap.Instance.Port}  |  Players: {players}/{max}";
                infoText.gameObject.SetActive(true);
            }
            else
            {
                infoText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called by PauseMenuUI when returning to main menu to clean up networking.
    /// </summary>
    public void CleanupOnSceneExit()
    {
        CloseLAN();
    }
}
