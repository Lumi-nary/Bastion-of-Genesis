using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// LoadGameButton navigates to the Load Game browser screen.
/// Follows Pattern 7 (UI Canvas Management) - calls MenuManager, never manipulates canvas directly.
/// </summary>
public class LoadGameButton : MonoBehaviour
{
    [SerializeField] private string defaultSceneName = "SampleScene";
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Disable button if no saved progress
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (button != null && ProgressionManager.Instance != null)
        {
            button.interactable = ProgressionManager.Instance.HasSavedProgress();
        }
    }

    /// <summary>
    /// OnClick event handler - navigates to Load Game canvas.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// Story 1.2: Shows Load Game canvas (placeholder screen).
    /// Epic 3: Will implement save browser and load functionality.
    /// </summary>
    public void OnClick()
    {
        MenuManager.Instance.ShowLoadGameCanvas();
    }

    /// <summary>
    /// Legacy method - loads game directly (bypasses menu flow).
    /// Retained for backward compatibility but not used in Story 1.2.
    /// Will be integrated with Load Game browser in Epic 3.
    /// </summary>
    public void OnLoadGameClicked()
    {
        Debug.Log("Loading saved game...");

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasSavedProgress())
        {
            ProgressionManager.Instance.LoadProgress();

            // Load the current chapter's scene
            string sceneName = defaultSceneName;
            if (MissionChapterManager.Instance != null && MissionChapterManager.Instance.CurrentChapter != null)
            {
                sceneName = MissionChapterManager.Instance.CurrentChapter.sceneName;
            }

            SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            Debug.LogWarning("No saved progress found!");
        }
    }
}
