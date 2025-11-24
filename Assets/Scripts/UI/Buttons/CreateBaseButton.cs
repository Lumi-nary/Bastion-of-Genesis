using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CreateBaseButton triggers the New Base creation flow.
/// Validates form, sets SaveManager pending data (ADR-7), and loads CutsceneScene.
/// </summary>
public class CreateBaseButton : MonoBehaviour
{
    // ============================================================================
    // SERIALIZED FIELDS (Assigned in Unity Inspector)
    // ============================================================================

    [Header("Required References")]
    [Tooltip("Reference to NewBaseUI canvas controller")]
    [SerializeField] private NewBaseUI newBaseUI;

    // ============================================================================
    // PUBLIC API (Called by Button onClick Event)
    // ============================================================================

    /// <summary>
    /// Handle Create Base button click.
    /// Validates form, sets SaveManager pending data, loads CutsceneScene.
    /// ADR-7: Uses SaveManager pending properties for scene-to-scene data handoff.
    /// </summary>
    public void OnClick()
    {
        // Validate form (always valid with defaults in MVP)
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

        // Get form values from NewBaseUI
        string baseName = newBaseUI.GetBaseName();
        Difficulty difficulty = newBaseUI.GetDifficulty();
        GameMode mode = newBaseUI.GetMode();

        Debug.Log($"[CreateBaseButton] Creating new base: {baseName}, {difficulty}, {mode}");

        // Set SaveManager pending data (ADR-7 scene handoff pattern)
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[CreateBaseButton] SaveManager.Instance not found!");
            return;
        }

        SaveManager.Instance.pendingBaseName = baseName;
        SaveManager.Instance.pendingDifficulty = difficulty;
        SaveManager.Instance.pendingMode = mode;
        SaveManager.Instance.pendingChapter = 1; // Always Chapter 1 for new bases

        Debug.Log($"[CreateBaseButton] SaveManager pending data set: baseName={baseName}, difficulty={difficulty}, mode={mode}, chapter=1");

        // Load CutsceneScene (Story 2.3)
        Debug.Log("[CreateBaseButton] Loading CutsceneScene for Chapter 1");
        SceneManager.LoadSceneAsync("CutsceneScene");
    }
}
