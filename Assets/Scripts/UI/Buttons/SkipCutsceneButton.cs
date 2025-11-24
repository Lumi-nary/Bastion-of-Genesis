using UnityEngine;

/// <summary>
/// SkipCutsceneButton provides immediate cutscene skip functionality.
/// Works in conjunction with ESC key navigation (handled by CutsceneManager).
/// Follows Pattern 4 (Logging Strategy) for consistent debug output.
/// AC3: Skip Button Visible and Functional.
/// </summary>
public class SkipCutsceneButton : MonoBehaviour
{
    /// <summary>
    /// OnClick event handler - immediately skips cutscene.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// Instant action (Factorio/Rimworld UX philosophy - no delay or confirmation).
    /// AC3: OnClick() calls CutsceneManager.SkipCutscene().
    /// </summary>
    public void OnClick()
    {
        // Pattern 4: Logging Strategy
        Debug.Log("[SkipCutsceneButton] Skip button clicked");

        // Call CutsceneManager to handle skip logic
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.SkipCutscene();
        }
        else
        {
            Debug.LogError("[SkipCutsceneButton] CutsceneManager.Instance not found!");
        }
    }
}
