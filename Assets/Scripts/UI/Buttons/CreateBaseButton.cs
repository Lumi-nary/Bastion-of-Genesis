using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CreateBaseButton triggers the New Base creation flow.
/// For Singleplayer: Sets SaveManager pending data and loads CutsceneScene.
/// For COOP: Sets pending data and opens MultiplayerCanvas lobby.
/// </summary>
public class CreateBaseButton : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private NewBaseUI newBaseUI;

    /// <summary>
    /// Handle Create Base button click.
    /// For SP: Load CutsceneScene immediately.
    /// For COOP: Open multiplayer lobby to wait for players.
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
        GameMode mode = newBaseUI.GetMode();

        Debug.Log($"[CreateBaseButton] Creating new base: {baseName}, {difficulty}, {mode}");

        // Set SaveManager pending data
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[CreateBaseButton] SaveManager.Instance not found!");
            return;
        }

        SaveManager.Instance.pendingBaseName = baseName;
        SaveManager.Instance.pendingDifficulty = difficulty;
        SaveManager.Instance.pendingMode = mode;
        SaveManager.Instance.pendingChapter = 1;

        Debug.Log($"[CreateBaseButton] SaveManager pending data set");

        if (mode == GameMode.Singleplayer)
        {
            // Singleplayer: Load cutscene immediately
            Debug.Log("[CreateBaseButton] Singleplayer - Loading CutsceneScene");
            SceneManager.LoadSceneAsync("CutsceneScene");
        }
        else
        {
            // COOP: Open multiplayer lobby
            Debug.Log("[CreateBaseButton] COOP - Opening multiplayer lobby as host");

            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.ShowMultiplayerCanvas();
            }
            else
            {
                Debug.LogError("[CreateBaseButton] MenuManager not found!");
            }
        }
    }
}
