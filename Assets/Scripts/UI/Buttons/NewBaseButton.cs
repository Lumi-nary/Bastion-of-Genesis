using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NewBaseButton navigates to the New Base creation screen.
/// Follows Pattern 7 (UI Canvas Management) - calls MenuManager, never manipulates canvas directly.
/// </summary>
public class NewBaseButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    /// <summary>
    /// OnClick event handler - navigates to New Base canvas.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// Story 1.2: Shows New Base canvas (placeholder screen).
    /// Epic 2: Will implement actual new base creation flow.
    /// </summary>
    public void OnClick()
    {
        MenuManager.Instance.ShowNewBaseCanvas();
    }

    /// <summary>
    /// Legacy method - starts new game directly (bypasses menu flow).
    /// Retained for backward compatibility but not used in Story 1.2.
    /// Will be integrated with New Base flow in Epic 2.
    /// </summary>
    public void OnNewBaseClicked()
    {
        Debug.Log("Starting new game...");

        // Delete existing progress
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.DeleteProgress();
        }

        // Load game scene
        SceneManager.LoadSceneAsync(gameSceneName);
    }
}
