using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CreateBaseButton triggers the New Base creation flow.
/// Sets SaveManager pending data and loads CutsceneScene.
/// Multiplayer is enabled in-game via "Open to LAN" in the pause menu.
/// </summary>
public class CreateBaseButton : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private NewBaseUI newBaseUI;

    /// <summary>
    /// Handle Create Base button click.
    /// Always starts as singleplayer. LAN can be opened in-game.
    /// </summary>
    public void OnClick()
    {
        if (newBaseUI == null)
        {
            Debug.LogError("[CreateBaseButton] NewBaseUI reference not assigned!");
            return;
        }

        if (!newBaseUI.ValidateForm())
        {
            Debug.LogWarning("[CreateBaseButton] Form validation failed");
            return;
        }

        // Get form values
        string baseName = newBaseUI.GetBaseName();
        Difficulty difficulty = newBaseUI.GetDifficulty();

        Debug.Log($"[CreateBaseButton] Creating new base: {baseName}, {difficulty}");

        // Set SaveManager pending data
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[CreateBaseButton] SaveManager.Instance not found!");
            return;
        }

        SaveManager.Instance.pendingBaseName = baseName;
        SaveManager.Instance.pendingDifficulty = difficulty;
        SaveManager.Instance.pendingMode = GameMode.Singleplayer;
        SaveManager.Instance.pendingChapter = 1;

        Debug.Log("[CreateBaseButton] Loading CutsceneScene");
        SceneManager.LoadSceneAsync("CutsceneScene");
    }
}
