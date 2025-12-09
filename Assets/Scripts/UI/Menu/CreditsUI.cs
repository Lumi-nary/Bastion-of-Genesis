using UnityEngine;
using TMPro;

/// <summary>
/// CreditsUI - Controller for Credits screen display.
/// Loads credits from GameSettings and displays them.
/// Epic 6 Story 6.1 - Credits Screen.
/// Pattern 2: Scene-specific (no DontDestroyOnLoad).
/// Pattern 7: Canvas switching via MenuManager.
/// </summary>
public class CreditsUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI creditsText;

    private bool hasInitialized = false;

    /// <summary>
    /// Start - Mark as initialized, prevents loading during scene initialization.
    /// </summary>
    private void Start()
    {
        hasInitialized = true;

        // If canvas is currently active when Start runs, load now
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.enabled)
        {
            LoadAndDisplayCredits();
        }
    }

    /// <summary>
    /// OnEnable - Load and display credits when canvas is shown.
    /// Pattern 2: Initialize in OnEnable() for canvas that may be disabled on scene load.
    /// Only loads after Start() has run (prevents scene load issues).
    /// </summary>
    private void OnEnable()
    {
        // Only load after Start() has run (hasInitialized = true)
        // This prevents loading during scene initialization
        if (hasInitialized)
        {
            LoadAndDisplayCredits();
        }
    }

    /// <summary>
    /// Load credits from GameSettings and display them (AC1).
    /// </summary>
    private void LoadAndDisplayCredits()
    {
        // Validate GameSettings exists
        if (GameSettings.Instance == null)
        {
            Debug.LogError("[CreditsUI] GameSettings.Instance is null - cannot load credits");
            if (creditsText != null)
            {
                creditsText.text = "Credits data not found.\nPlease create GameSettings.asset in Resources folder.";
            }
            return;
        }

        // Get formatted credits text
        string credits = GameSettings.Instance.GetCreditsText();

        // Display credits
        if (creditsText != null)
        {
            creditsText.text = credits;
            Debug.Log($"[CreditsUI] Credits loaded successfully ({GameSettings.Instance.credits.Length} entries)");
        }
        else
        {
            Debug.LogError("[CreditsUI] creditsText reference is null - cannot display credits");
        }
    }
}
