using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Sub Canvases")]
    [SerializeField] private Canvas optionsCanvas;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MenuScene";

    private void Awake()
    {
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
    }

    public void Hide()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);
    }

    public bool IsVisible => pausePanel != null && pausePanel.activeSelf;

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
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();

            // Show confirmation
            if (ModalDialog.Instance != null)
            {
                ModalDialog.Instance.ShowInfo("Game Saved", "Your progress has been saved.");
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
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
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
