using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    /// <summary>
    /// Assign this method to the "Return to Main Menu" button's OnClick() in inspector
    /// </summary>
    public void OnMainMenuClicked()
    {
        Debug.Log("Returning to main menu...");

        // Unpause game
        Time.timeScale = 1f;

        // Save progress
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.SaveProgress();
        }

        // End current mission
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.EndMission();
        }

        // Load main menu scene
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }
}
