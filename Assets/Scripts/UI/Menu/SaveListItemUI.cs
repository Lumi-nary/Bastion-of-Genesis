using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// SaveListItemUI displays a single save entry in the Load Game browser.
/// Handles displaying save metadata (name, difficulty, mode, playtime, timestamp).
/// Epic 3 Story 3.2 - LoadGameUI and SaveListItemUI.
/// </summary>
public class SaveListItemUI : MonoBehaviour
{
    [Header("UI Text Components")]
    [SerializeField] private TextMeshProUGUI baseNameText;
    [SerializeField] private TextMeshProUGUI playtimeText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI chapterMissionText;
    [SerializeField] private TextMeshProUGUI modeIcon;

    [Header("Difficulty Badge")]
    [SerializeField] private Image difficultyBadge;

    [Header("Buttons")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    // Store metadata for callbacks
    private SaveMetadata metadata;

    // Reference to parent LoadGameUI for callbacks
    private LoadGameUI parentUI;

    /// <summary>
    /// Initialize save list item with metadata (AC3, AC4).
    /// Populates all UI fields and sets up button callbacks.
    /// </summary>
    /// <param name="saveMetadata">Save metadata to display</param>
    /// <param name="loadGameUI">Parent LoadGameUI for callbacks</param>
    public void Initialize(SaveMetadata saveMetadata, LoadGameUI loadGameUI)
    {
        // Store references
        metadata = saveMetadata;
        parentUI = loadGameUI;

        // Validate metadata
        if (metadata == null)
        {
            Debug.LogError("[SaveListItemUI] Initialize called with null metadata");
            return;
        }

        // AC4.3: Set base name (with autosave prefix if applicable)
        if (baseNameText != null)
        {
            if (metadata.isAutosave)
            {
                // Extract autosave number from fileName (autosave_1.json -> "Autosave 1")
                string autosaveNumber = metadata.fileName.Replace("autosave_", "").Replace(".json", "");
                baseNameText.text = $"Autosave {autosaveNumber}";
            }
            else
            {
                baseNameText.text = metadata.baseName;
            }
        }

        // AC4.6: Set difficulty badge color (Green=Easy, Yellow=Medium, Red=Hard)
        if (difficultyBadge != null)
        {
            switch (metadata.difficulty)
            {
                case Difficulty.Easy:
                    difficultyBadge.color = Color.green;
                    break;
                case Difficulty.Medium:
                    difficultyBadge.color = Color.yellow;
                    break;
                case Difficulty.Hard:
                    difficultyBadge.color = Color.red;
                    break;
            }
        }

        // AC4.4: Set mode icon ([SP] or [COOP])
        if (modeIcon != null)
        {
            modeIcon.text = metadata.GetModeIcon();
        }

        // AC4.5: Convert totalPlaytime to HH:MM:SS format
        if (playtimeText != null)
        {
            TimeSpan playtime = TimeSpan.FromSeconds(metadata.totalPlaytime);
            playtimeText.text = playtime.ToString(@"hh\:mm\:ss");
        }

        // AC4.5: Format timestamp as relative time or absolute date
        if (timestampText != null)
        {
            timestampText.text = FormatTimestamp(metadata.timestamp);
        }

        // AC4.6: Set chapter/mission progress (CH{chapter} M{mission})
        if (chapterMissionText != null)
        {
            chapterMissionText.text = $"CH{metadata.currentChapter} M{metadata.currentMission}";
        }

        // AC5, AC7: Wire button callbacks
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadButtonClick);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClick);
        }
    }

    /// <summary>
    /// Format timestamp as relative time (if recent) or absolute date.
    /// AC4.5: Relative time for <24 hours, absolute otherwise.
    /// </summary>
    private string FormatTimestamp(string timestamp)
    {
        try
        {
            DateTime saveTime = DateTime.Parse(timestamp);
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - saveTime;

            // Relative time for <24 hours
            if (elapsed.TotalHours < 24)
            {
                if (elapsed.TotalMinutes < 1)
                    return "Just now";
                else if (elapsed.TotalMinutes < 60)
                    return $"{(int)elapsed.TotalMinutes} minutes ago";
                else
                    return $"{(int)elapsed.TotalHours} hours ago";
            }
            else
            {
                // Absolute date for older saves
                return saveTime.ToString("MMM dd, yyyy h:mm tt");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveListItemUI] Failed to parse timestamp: {timestamp} - {ex.Message}");
            return timestamp; // Fallback to raw timestamp
        }
    }

    /// <summary>
    /// Load button click handler (AC5).
    /// Delegates to parent LoadGameUI.OnSaveClicked().
    /// </summary>
    private void OnLoadButtonClick()
    {
        if (parentUI != null && metadata != null)
        {
            parentUI.OnSaveClicked(metadata);
        }
        else
        {
            Debug.LogError("[SaveListItemUI] Load button clicked but parentUI or metadata is null");
        }
    }

    /// <summary>
    /// Delete button click handler (AC7).
    /// Delegates to parent LoadGameUI.OnDeleteClicked().
    /// </summary>
    private void OnDeleteButtonClick()
    {
        if (parentUI != null && metadata != null)
        {
            parentUI.OnDeleteClicked(metadata);
        }
        else
        {
            Debug.LogError("[SaveListItemUI] Delete button clicked but parentUI or metadata is null");
        }
    }
}
