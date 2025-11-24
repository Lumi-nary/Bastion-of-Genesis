using UnityEngine;

/// <summary>
/// QuitButton exits the application gracefully.
/// Handles both Unity Editor (stops play mode) and standalone builds (quits application).
/// </summary>
public class QuitButton : MonoBehaviour
{
    /// <summary>
    /// OnClick event handler - quits the application.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// In Editor: Stops play mode (UnityEditor.EditorApplication.isPlaying = false)
    /// In Build: Calls Application.Quit() for graceful shutdown
    /// </summary>
    public void OnClick()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Legacy method - quits with save progress.
    /// Retained for backward compatibility but not used in Story 1.2.
    /// Note: Epic 7 will handle autosave, manual save at quit may not be needed.
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("Quitting game...");

        // Save progress before quitting
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.SaveProgress();
        }

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
