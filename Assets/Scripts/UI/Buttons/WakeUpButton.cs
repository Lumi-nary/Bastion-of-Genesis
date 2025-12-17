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

        // Start chapter via MissionChapterManager (handles scene loading AND chapter initialization)
        // This ensures ChapterData values (resources, workers, enemies, integration) are applied
        if (MissionChapterManager.Instance != null)
        {
            int chapterIndex = chapter - 1; // Convert 1-indexed to 0-indexed
            Debug.Log($"[WakeUpButton] Starting Chapter {chapter} via MissionChapterManager");
            MissionChapterManager.Instance.StartChapter(chapterIndex);
        }
        else
        {
            // Fallback: Direct scene load if MissionChapterManager not available
            Debug.LogWarning("[WakeUpButton] MissionChapterManager not found! Loading scene directly (chapter data won't be applied)");
            string sceneToLoad = GetChapterSceneName(chapter);
            SceneManager.LoadSceneAsync(sceneToLoad);
        }
    }

    /// <summary>
    /// Get the scene name for a specific chapter.
    /// Queries MissionChapterManager for chapter data if available,
    /// otherwise falls back to default naming convention.
    /// </summary>
    private string GetChapterSceneName(int chapterNumber)
    {
        // Try to get scene name from MissionChapterManager
        if (MissionChapterManager.Instance != null && MissionChapterManager.Instance.Chapters.Count > 0)
        {
            // Chapter numbers are 1-indexed, list is 0-indexed
            int chapterIndex = chapterNumber - 1;

            if (chapterIndex >= 0 && chapterIndex < MissionChapterManager.Instance.Chapters.Count)
            {
                ChapterData chapterData = MissionChapterManager.Instance.Chapters[chapterIndex];
                if (!string.IsNullOrEmpty(chapterData.sceneName))
                {
                    return chapterData.sceneName;
                }
            }
        }

        // Fallback: Use default naming convention if ChapterData not available
        // Assumes scenes named: Chapter1Map, Chapter2Map, etc.
        return $"Chapter{chapterNumber}Map";
    }
}
