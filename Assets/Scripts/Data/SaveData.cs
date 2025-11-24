using System;
using UnityEngine;

/// <summary>
/// Complete save file data structure for Planetfall.
/// Serialized to JSON format for human-readable save files.
/// ADR-2: Single JSON file per save with all game state.
/// </summary>
[Serializable]
public class SaveData
{
    // ============================================================================
    // METADATA
    // ============================================================================

    /// <summary>
    /// Save format version for migration support (Epic 7+).
    /// </summary>
    public string version = "1.0.0";

    /// <summary>
    /// User-visible base name (e.g., "Colony Alpha", "Genesis Outpost").
    /// </summary>
    public string baseName;

    /// <summary>
    /// Game difficulty level.
    /// </summary>
    public Difficulty difficulty;

    /// <summary>
    /// Game mode: Singleplayer or COOP.
    /// </summary>
    public GameMode mode;

    /// <summary>
    /// Save creation/modification timestamp (ISO 8601 format).
    /// </summary>
    public string timestamp;

    /// <summary>
    /// Total playtime in seconds (Epic 3 - for Load Game UI display).
    /// </summary>
    public float totalPlaytime = 0f;

    // ============================================================================
    // PROGRESSION STATE
    // ============================================================================

    /// <summary>
    /// Current chapter number (1-based).
    /// </summary>
    public int currentChapter = 1;

    /// <summary>
    /// Current mission number within chapter (1-based).
    /// </summary>
    public int currentMission = 1;

    /// <summary>
    /// Track which missions have been completed (Epic 7+).
    /// </summary>
    public bool[] missionCompletions = new bool[0];

    // ============================================================================
    // WORLD STATE (Placeholder - Epic 7)
    // ============================================================================

    /// <summary>
    /// Grid state: buildings, resources, workers (Epic 7).
    /// Using string placeholder for MVP - will be proper GridState class later.
    /// </summary>
    public string gridState = "{}";

    /// <summary>
    /// Current pollution level (0.0 - 100.0).
    /// </summary>
    public float pollutionLevel = 0f;

    // ============================================================================
    // COOP STATE (Placeholder - Epic 9)
    // ============================================================================

    /// <summary>
    /// Host player name (empty for Singleplayer).
    /// </summary>
    public string hostPlayerName = "";

    /// <summary>
    /// Connected player names (empty for Singleplayer).
    /// </summary>
    public string[] connectedPlayers = new string[0];
}

/// <summary>
/// Game difficulty levels.
/// </summary>
public enum Difficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

/// <summary>
/// Game mode options.
/// </summary>
public enum GameMode
{
    Singleplayer = 0,
    COOP = 1
}

/// <summary>
/// Lightweight save metadata for Load Game UI display.
/// Extracted from SaveData without loading full world state.
/// </summary>
[Serializable]
public class SaveMetadata
{
    /// <summary>
    /// File name (e.g., "autosave_1.json", "colony_alpha.json").
    /// </summary>
    public string fileName;

    /// <summary>
    /// User-visible base name for display.
    /// </summary>
    public string baseName;

    /// <summary>
    /// Difficulty level.
    /// </summary>
    public Difficulty difficulty;

    /// <summary>
    /// Game mode.
    /// </summary>
    public GameMode mode;

    /// <summary>
    /// Last modified timestamp (ISO 8601).
    /// </summary>
    public string timestamp;

    /// <summary>
    /// Current chapter for progress display.
    /// </summary>
    public int currentChapter;

    /// <summary>
    /// Current mission for progress display.
    /// </summary>
    public int currentMission;

    /// <summary>
    /// Total playtime in seconds (converted to HH:MM:SS for UI).
    /// </summary>
    public float totalPlaytime;

    /// <summary>
    /// True if this is an autosave file (autosave_N.json pattern).
    /// </summary>
    public bool isAutosave;

    // ============================================================================
    // HELPER METHODS FOR UI DISPLAY
    // ============================================================================

    /// <summary>
    /// Get formatted display name for Load Game list.
    /// Format: "baseName (CH# M#)"
    /// Example: "Colony Alpha (CH1 M3)"
    /// </summary>
    public string GetDisplayName()
    {
        return $"{baseName} (CH{currentChapter} M{currentMission})";
    }

    /// <summary>
    /// Get mode badge for UI display.
    /// Returns "[SP]" for Singleplayer, "[COOP]" for Cooperative.
    /// </summary>
    public string GetModeIcon()
    {
        return mode == GameMode.COOP ? "[COOP]" : "[SP]";
    }
}
