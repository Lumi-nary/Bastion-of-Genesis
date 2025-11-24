using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// WakeUpButton finalizes save file creation and transitions to GameWorld scene.
/// Called from WorldMapScene after player views chapter map.
/// Creates save file with all pending data from SaveManager (baseName, difficulty, mode, chapter).
/// AC4: Wake Up button visible, functional, with Pattern 4 logging.
/// AC5: OnClick() creates save and loads GameWorld scene.
/// </summary>
public class WakeUpButton : MonoBehaviour
{
    /// <summary>
    /// OnClick event handler - creates save file and loads GameWorld scene.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// AC5: Calls SaveManager.CreateNewSave() with pending data, then loads GameWorld.
    /// </summary>
    public void OnClick()
    {
        // Pattern 4: Logging Strategy
        Debug.Log("[WakeUpButton] Wake Up button clicked");

        // Verify SaveManager exists (AC5)
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[WakeUpButton] SaveManager.Instance not found! Cannot create save.");
            return;
        }

        // Get pending data from SaveManager (AC5)
        string baseName = SaveManager.Instance.pendingBaseName;
        Difficulty difficulty = SaveManager.Instance.pendingDifficulty;
        GameMode mode = SaveManager.Instance.pendingMode;
        int chapter = SaveManager.Instance.pendingChapter; // Always 1 for new bases

        // Validate pending data
        if (string.IsNullOrEmpty(baseName))
        {
            Debug.LogError("[WakeUpButton] pendingBaseName is null or empty! Cannot create save.");
            return;
        }

        // AC5: Create save file using SaveManager.CreateNewSave()
        // Save file created in Application.persistentDataPath/Saves/
        SaveManager.Instance.CreateNewSave(baseName, difficulty, mode);

        Debug.Log($"[WakeUpButton] Save created: {baseName}, {difficulty}, {mode}, Chapter {chapter}");
        Debug.Log("[WakeUpButton] Save created, loading GameWorld scene");

        // AC5: Load GameWorld scene via SceneManager.LoadSceneAsync (ADR-6: Scene Flow)
        // NFR-1: Scene transition completes within <2 seconds
        SceneManager.LoadSceneAsync("GameWorld");
    }
}
