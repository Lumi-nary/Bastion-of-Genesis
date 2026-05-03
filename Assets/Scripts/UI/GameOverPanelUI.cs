using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameOverPanelUI : MonoBehaviour
{
    public static GameOverPanelUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject panelContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI playtimeText;
    [SerializeField] private TextMeshProUGUI missionText;
    [SerializeField] private TextMeshProUGUI resourcesText;
    
    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button shareButton;

    [Header("Settings")]
    [SerializeField] private string menuSceneName = "MenuScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (panelContainer != null) panelContainer.SetActive(false);
        
        if (mainMenuButton != null) 
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        if (shareButton != null) 
        {
            shareButton.onClick.RemoveAllListeners();
            shareButton.onClick.AddListener(OnShareClicked);
        }
    }

    public void Show(bool isWin, float totalPlaytime, string currentMission, IReadOnlyDictionary<ResourceType, int> resources)
    {
        Debug.Log($"[GameOverPanelUI] Showing panel. Win: {isWin}");
        if (panelContainer != null) panelContainer.SetActive(true);
        else Debug.LogError("[GameOverPanelUI] panelContainer is null!");

        if (titleText != null)
        {
            titleText.text = isWin ? "VICTORY" : "DEFEAT";
            titleText.color = isWin ? Color.green : Color.red;
        }

        if (playtimeText != null)
        {
            playtimeText.text = $"Playtime: {FormatTime(totalPlaytime)}";
        }

        if (missionText != null)
        {
            missionText.text = $"Mission: {currentMission}";
        }

        if (resourcesText != null)
        {
            string resStr = "Resources Collected:\n";
            if (resources != null)
            {
                foreach (var kvp in resources)
                {
                    if (kvp.Key != null)
                        resStr += $"{kvp.Key.ResourceName}: {kvp.Value}\n";
                }
            }
            resourcesText.text = resStr;
        }
        
        // Ensure time is stopped
        Time.timeScale = 0f;
    }

    private string FormatTime(float seconds)
    {
        System.TimeSpan t = System.TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverPanelUI] Main Menu button clicked.");
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnShareClicked()
    {
        Debug.Log("[GameOverPanelUI] Share button clicked!");
        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private System.Collections.IEnumerator CaptureScreenshotCoroutine()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Planetfall_Stats_{timestamp}.png";
        string fullPath = System.IO.Path.Combine(Application.persistentDataPath, filename);

        // Wait for end of frame to ensure UI is rendered
        yield return new WaitForEndOfFrame();

        ScreenCapture.CaptureScreenshot(filename);
        
        // On Windows Standalone, filename without path goes to game root.
        // On Editor, it goes to project root.
        Debug.Log($"[GameOverPanelUI] Screenshot capture requested as: {filename}");
        
        // Wait a bit for the file to actually be written (CaptureScreenshot is async)
        float startWait = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < startWait + 1f)
        {
            yield return null;
        }

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        string projectRoot = System.IO.Directory.GetCurrentDirectory();
        string editorPath = System.IO.Path.Combine(projectRoot, filename);
        
        if (System.IO.File.Exists(editorPath))
        {
            Debug.Log($"[GameOverPanelUI] Screenshot found at: {editorPath}");
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{editorPath.Replace("/", "\\")}\"");
        }
        else if (System.IO.File.Exists(fullPath))
        {
            Debug.Log($"[GameOverPanelUI] Screenshot found at: {fullPath}");
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath.Replace("/", "\\")}\"");
        }
        else
        {
            Debug.LogWarning("[GameOverPanelUI] Screenshot file not detected yet. It might still be saving or saved to a different location.");
        }
        #endif
    }
}
