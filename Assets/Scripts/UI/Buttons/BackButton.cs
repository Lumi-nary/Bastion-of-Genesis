using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BackButton provides generic back navigation to main menu from any subscreen or scene.
/// Works in conjunction with ESC key navigation (handled by MenuManager in MenuScene).
/// Follows Pattern 7 (UI Canvas Management) - calls MenuManager, never manipulates canvas directly.
/// Special handling: On NewBaseCanvas, checks if form is dirty and shows confirmation dialog.
/// Cross-scene support: In WorldMapScene, loads MenuScene directly (cancels base creation).
/// </summary>
public class BackButton : MonoBehaviour
{
    /// <summary>
    /// Optional reference to NewBaseUI for confirmation dialog on NewBaseCanvas.
    /// If assigned, will check for unsaved changes before navigating back.
    /// </summary>
    [SerializeField] private NewBaseUI newBaseUI;

    /// <summary>
    /// OnClick event handler - returns to main menu canvas or MenuScene.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// Provides consistent back navigation from all subscreens and scenes.
    /// Special case: On NewBaseCanvas with unsaved changes, shows confirmation dialog.
    /// Cross-scene: In WorldMapScene, loads MenuScene (no save created).
    /// AC6: Back button returns to MenuScene, cancels base creation, no save file created.
    /// </summary>
    public void OnClick()
    {
        // Special handling for NewBaseCanvas with unsaved changes
        if (newBaseUI != null && newBaseUI.IsFormDirty())
        {
            Debug.Log("[BackButton] Form has unsaved changes, showing confirmation dialog");
            newBaseUI.ShowConfirmationDialog();
            return;
        }

        // Check if MenuManager exists (we're in MenuScene)
        if (MenuManager.Instance != null)
        {
            // Epic 1 behavior: Switch canvas within MenuScene
            Debug.Log("[BackButton] Returning to main menu canvas");
            MenuManager.Instance.ShowMainMenuCanvas();
        }
        else
        {
            // Cross-scene behavior: Load MenuScene (used in WorldMapScene, CutsceneScene, etc.)
            // AC6: Returning to MenuScene, base creation cancelled, no save file created
            Debug.Log("[BackButton] Returning to MenuScene, base creation cancelled");
            SceneManager.LoadSceneAsync("MenuScene");
        }
    }
}
