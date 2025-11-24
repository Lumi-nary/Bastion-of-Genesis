using UnityEngine;
using TMPro;

/// <summary>
/// MainMenuUI controls the main menu canvas display.
/// Displays game title from GameSettings ScriptableObject.
/// Attached to MainMenuCanvas GameObject.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;

    /// <summary>
    /// Initialize main menu display.
    /// Sets game title from GameSettings.Instance.
    /// </summary>
    private void Start()
    {
        DisplayGameTitle();
    }

    /// <summary>
    /// Optional hook for future animations when menu is enabled.
    /// Currently unused but reserved for Epic enhancements.
    /// </summary>
    private void OnEnable()
    {
        // Future: Add title animation, fade-in effects, etc.
    }

    /// <summary>
    /// Display game title from GameSettings configuration.
    /// Handles null checks for graceful degradation.
    /// </summary>
    private void DisplayGameTitle()
    {
        if (titleText == null)
        {
            Debug.LogWarning("[MainMenuUI] titleText reference is not assigned. Please assign TextMeshProUGUI in Inspector.");
            return;
        }

        if (GameSettings.Instance == null)
        {
            Debug.LogError("[MainMenuUI] GameSettings.Instance is null. Cannot display game title.");
            titleText.text = "Game Title Not Set";
            return;
        }

        titleText.text = GameSettings.Instance.gameTitle;
        Debug.Log($"[MainMenuUI] Game title displayed: {GameSettings.Instance.gameTitle}");
    }
}
