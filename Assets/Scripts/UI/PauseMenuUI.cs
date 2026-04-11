using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using FishNet.Connection;
using TMPro;

/// <summary>
/// Pause menu UI panel with buttons for resume, save, load, options, main menu, and quit.
/// Controlled by UIManager's pause system.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button openLanButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Sub Canvases")]
    [SerializeField] private Canvas optionsCanvas;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MenuScene";

    private void Awake()
    {
        // Ensure pause menu stays interactive when gameplay CanvasGroup is disabled
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        cg.ignoreParentGroups = true;

        SetupButtons();
        Hide();
    }

    private void SetupButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (saveGameButton != null)
            saveGameButton.onClick.AddListener(OnSaveGameClicked);

        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGameClicked);

        if (openLanButton != null)
            openLanButton.onClick.AddListener(OnOpenLanClicked);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResumeClicked);

        if (saveGameButton != null)
            saveGameButton.onClick.RemoveListener(OnSaveGameClicked);

        if (loadGameButton != null)
            loadGameButton.onClick.RemoveListener(OnLoadGameClicked);

        if (openLanButton != null)
            openLanButton.onClick.RemoveListener(OnOpenLanClicked);

        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OnOptionsClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    public void Show()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);

        // Hide options canvas when showing pause menu
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);

        UpdateLanButtonText();
    }

    public void Hide()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);
    }

    public bool IsVisible => pausePanel != null && pausePanel.activeSelf;
    public bool IsOptionsVisible => optionsCanvas != null && optionsCanvas.gameObject.activeSelf;

    // ============================================================================
    // Button Handlers
    // ============================================================================

    private void OnResumeClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Unpause();
        }
    }

    private void OnSaveGameClicked()
    {
        // Multiplayer guard: only host can save
        if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsOnline
            && !NetworkGameManager.Instance.IsServer)
        {
            if (ModalDialog.Instance != null)
                ModalDialog.Instance.ShowError("Only the host can save the game.");
            return;
        }

        if (SaveManager.Instance != null)
        {
            bool success = SaveManager.Instance.ManualSave();

            if (ModalDialog.Instance != null)
            {
                if (success)
                    ModalDialog.Instance.ShowInfo("Game Saved", "Your progress has been saved.");
                else
                    ModalDialog.Instance.ShowError("Failed to save game.");
            }
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] SaveManager not found");
        }
    }

    private void OnLoadGameClicked()
    {
        // Show confirmation since loading will lose current progress
        if (ModalDialog.Instance != null)
        {
            ModalDialog.Instance.ShowConfirmation(
                "Loading will lose any unsaved progress. Continue?",
                () =>
                {
                    // TODO: Show load game UI or load most recent save
                    if (SaveManager.Instance != null)
                    {
                        SaveManager.Instance.LoadGame("autosave_1.json");
                    }
                },
                null // onCancel - do nothing
            );
        }
    }

    private void OnOptionsClicked()
    {
        // Hide pause panel, show options canvas
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(true);
    }

    /// <summary>
    /// Called by options canvas back button to return to pause menu
    /// </summary>
    public void ReturnFromOptions()
    {
        // Revert audio to saved settings (sliders applied live previews)
        if (SettingsManager.Instance != null && AudioManager.Instance != null)
        {
            var saved = SettingsManager.Instance.CurrentSettings;
            AudioManager.Instance.SetMasterVolume(saved.masterVolume);
            AudioManager.Instance.SetMusicVolume(saved.musicVolume);
            AudioManager.Instance.SetSFXVolume(saved.sfxVolume);
            AudioManager.Instance.SetVoiceVolume(saved.voiceVolume);
        }

        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void OnOpenLanClicked()
    {
        if (NetworkGameManager.Instance == null)
        {
            if (ModalDialog.Instance != null)
                ModalDialog.Instance.ShowError("Network system not available.");
            return;
        }

        bool isLanOpen = NetworkGameManager.Instance.IsOnline;

        if (isLanOpen)
        {
            // Close LAN
            if (ModalDialog.Instance != null)
            {
                ModalDialog.Instance.ShowConfirmation(
                    "This will disconnect all players. Close LAN?",
                    () =>
                    {
                        // Unsubscribe from player events
                        NetworkGameManager.Instance.OnPlayerJoined -= OnLanPlayerJoined;
                        NetworkGameManager.Instance.OnPlayerLeft -= OnLanPlayerLeft;

                        // Stop LAN broadcasting
                        if (LANDiscovery.Instance != null)
                        {
                            LANDiscovery.Instance.StopBroadcasting();
                        }

                        NetworkGameManager.Instance.StopHost();

                        // Revert to singleplayer mode
                        if (SaveManager.Instance != null)
                        {
                            SaveManager.Instance.pendingMode = GameMode.Singleplayer;
                        }

                        UpdateLanButtonText();
                        Debug.Log("[PauseMenuUI] LAN closed");

                        if (ModalDialog.Instance != null)
                            ModalDialog.Instance.ShowInfo("LAN Closed", "Multiplayer disabled.");
                    },
                    null
                );
            }
        }
        else
        {
            // Open LAN — start FishNet host and LAN broadcasting
            NetworkGameManager.Instance.StartHost();

            // Start LAN broadcasting so clients can discover this server
            // Pass 1 for player count because server hasn't fully started yet
            // (PlayerCount would return 0 since IsServer is still false)
            if (LANDiscovery.Instance != null)
            {
                string serverName = SaveManager.Instance?.pendingBaseName ?? "Planetfall Server";
                LANDiscovery.Instance.StartBroadcasting(
                    serverName,
                    NetworkGameManager.Instance.Port,
                    1, // Host counts as 1 player
                    NetworkGameManager.Instance.MaxPlayers
                );
            }

            // Subscribe to player events to keep broadcast count updated
            NetworkGameManager.Instance.OnPlayerJoined -= OnLanPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft -= OnLanPlayerLeft;
            NetworkGameManager.Instance.OnPlayerJoined += OnLanPlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeft += OnLanPlayerLeft;

            // Set mode to COOP now that LAN is open
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.pendingMode = GameMode.COOP;
            }

            // Force button text immediately (don't wait for async server start)
            var tmp = openLanButton?.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "Close LAN";

            Debug.Log($"[PauseMenuUI] LAN opened on {NetworkGameManager.Instance.LocalIP}:{NetworkGameManager.Instance.Port}");

            if (ModalDialog.Instance != null)
                ModalDialog.Instance.ShowInfo("Open to LAN",
                    $"Players can join at:\n{NetworkGameManager.Instance.LocalIP}:{NetworkGameManager.Instance.Port}");
        }
    }

    private void OnLanPlayerJoined(NetworkConnection conn)
    {
        if (LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            LANDiscovery.Instance.UpdatePlayerCount(
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    private void OnLanPlayerLeft(NetworkConnection conn)
    {
        if (LANDiscovery.Instance != null && NetworkGameManager.Instance != null)
        {
            LANDiscovery.Instance.UpdatePlayerCount(
                NetworkGameManager.Instance.PlayerCount,
                NetworkGameManager.Instance.MaxPlayers
            );
        }
    }

    private void UpdateLanButtonText()
    {
        if (openLanButton == null) return;

        var tmp = openLanButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;

        bool isLanOpen = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsOnline;
        tmp.text = isLanOpen ? "Close LAN" : "Open to LAN";
    }

    private void OnMainMenuClicked()
    {
        if (ModalDialog.Instance != null)
        {
            ModalDialog.Instance.ShowConfirmation(
                "Unsaved progress will be lost. Return to Main Menu?",
                () =>
                {
                    // Unpause before loading scene
                    Time.timeScale = 1f;

                    // Stop LAN broadcasting and FishNet host/client before leaving gameplay
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.OnPlayerJoined -= OnLanPlayerJoined;
                        NetworkGameManager.Instance.OnPlayerLeft -= OnLanPlayerLeft;
                    }

                    if (LANDiscovery.Instance != null)
                    {
                        LANDiscovery.Instance.StopBroadcasting();
                    }

                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.Disconnect(); // Works for both host and client
                    }

                    // Restore audio
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.StopMusic();
                    }

                    // Load main menu scene
                    SceneManager.LoadScene(mainMenuSceneName);
                },
                null // onCancel - do nothing
            );
        }
    }

    private void OnQuitClicked()
    {
        if (ModalDialog.Instance != null)
        {
            ModalDialog.Instance.ShowConfirmation(
                "Unsaved progress will be lost. Quit to desktop?",
                () =>
                {
                    Debug.Log("[PauseMenuUI] Quitting application");

#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                },
                null // onCancel - do nothing
            );
        }
    }
}
