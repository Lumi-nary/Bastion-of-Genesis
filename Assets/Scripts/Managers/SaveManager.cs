using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// SaveManager handles all save file operations: create, load, delete, scan.
/// Singleton pattern with DontDestroyOnLoad for persistence across scenes.
/// ADR-4: DontDestroyOnLoad manager (unlike MenuManager which was scene-specific).
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ============================================================================
    // SINGLETON PATTERN
    // ============================================================================

    /// <summary>
    /// Singleton instance (accessible from any scene).
    /// </summary>
    public static SaveManager Instance { get; private set; }

    // ============================================================================
    // PENDING DATA FOR SCENE HANDOFF (ADR-7)
    // ============================================================================

    /// <summary>
    /// Pending base name set by NewBaseUI, read by WorldMapManager/WakeUpButton.
    /// </summary>
    public string pendingBaseName { get; set; }

    /// <summary>
    /// Pending difficulty set by NewBaseUI.
    /// </summary>
    public Difficulty pendingDifficulty { get; set; } = Difficulty.Medium;

    /// <summary>
    /// Pending game mode set by NewBaseUI.
    /// </summary>
    public GameMode pendingMode { get; set; } = GameMode.Singleplayer;

    /// <summary>
    /// Pending chapter set by NewBaseUI or LoadGameUI (for World Map display).
    /// Default: 1 for new bases.
    /// </summary>
    public int pendingChapter { get; set; } = 1;

    // ============================================================================
    // SAVE DIRECTORY PATH
    // ============================================================================

    /// <summary>
    /// Platform-agnostic path to Saves directory.
    /// Windows: C:\Users\<name>\AppData\LocalLow\<company>\Planetfall\Saves\
    /// Mac: ~/Library/Application Support/<company>/Planetfall/Saves/
    /// Linux: ~/.config/unity3d/<company>/Planetfall/Saves/
    /// </summary>
    private string savesPath => Path.Combine(Application.persistentDataPath, "Saves");

    // ============================================================================
    // LIFECYCLE METHODS
    // ============================================================================

    private void Awake()
    {
        // Singleton initialization
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SaveManager] Duplicate SaveManager detected, destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure Saves directory exists
        if (!Directory.Exists(savesPath))
        {
            Directory.CreateDirectory(savesPath);
            Debug.Log($"[SaveManager] Created Saves directory at: {savesPath}");
        }

        Debug.Log($"[SaveManager] SaveManager singleton initialized. Saves directory: {savesPath}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[SaveManager] SaveManager singleton destroyed.");
            Instance = null;
        }
    }

    // ============================================================================
    // PUBLIC API: SAVE OPERATIONS
    // ============================================================================

    /// <summary>
    /// Create new save file with atomic write pattern (prevents corruption).
    /// Called by WakeUpButton after WorldMapScene.
    /// </summary>
    /// <param name="baseName">User-visible base name</param>
    /// <param name="difficulty">Difficulty level</param>
    /// <param name="mode">Singleplayer or COOP</param>
    public void CreateNewSave(string baseName, Difficulty difficulty, GameMode mode)
    {
        try
        {
            // Create SaveData instance
            SaveData saveData = new SaveData
            {
                version = "1.0.0",
                baseName = baseName,
                difficulty = difficulty,
                mode = mode,
                timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                totalPlaytime = 0f, // New save starts at zero playtime
                currentChapter = 1,
                currentMission = 1,
                missionCompletions = new bool[0], // Empty for new save
                gridState = "{}", // Empty world state
                pollutionLevel = 0f,
                hostPlayerName = mode == GameMode.COOP ? baseName : "",
                connectedPlayers = new string[0]
            };

            // Serialize to JSON (pretty-print for human readability)
            string json = JsonUtility.ToJson(saveData, true);

            // Get save file path (always autosave_1.json for new bases)
            string fileName = "autosave_1.json";
            string savePath = GetSaveFilePath(fileName);

            // Atomic write: temp file → rename (prevents corruption on crash)
            string tempPath = savePath + ".tmp";
            File.WriteAllText(tempPath, json);

            // Delete existing file if present (File.Move doesn't support overwrite in Unity .NET)
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            File.Move(tempPath, savePath);

            Debug.Log($"[SaveManager] Save created: {fileName} at {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Failed to create save: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get all save files in Saves directory with metadata.
    /// Used by LoadGameUI to display save list (Epic 3).
    /// </summary>
    /// <returns>List of SaveMetadata sorted by timestamp (newest first)</returns>
    public List<SaveMetadata> GetAllSaves()
    {
        List<SaveMetadata> saves = new List<SaveMetadata>();

        try
        {
            // Get all .json files in Saves directory
            if (!Directory.Exists(savesPath))
            {
                Debug.LogWarning("[SaveManager] Saves directory does not exist, returning empty list.");
                return saves;
            }

            string[] saveFiles = Directory.GetFiles(savesPath, "*.json");

            foreach (string filePath in saveFiles)
            {
                try
                {
                    // Read and deserialize save file
                    string json = File.ReadAllText(filePath);
                    SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                    // Convert to SaveMetadata
                    string fileName = Path.GetFileName(filePath);
                    SaveMetadata metadata = new SaveMetadata
                    {
                        fileName = fileName,
                        baseName = saveData.baseName,
                        difficulty = saveData.difficulty,
                        mode = saveData.mode,
                        timestamp = saveData.timestamp,
                        totalPlaytime = saveData.totalPlaytime,
                        currentChapter = saveData.currentChapter,
                        currentMission = saveData.currentMission,
                        isAutosave = fileName.StartsWith("autosave_") // Detect autosave pattern
                    };

                    saves.Add(metadata);
                }
                catch (Exception ex)
                {
                    // Skip corrupted files, log warning
                    Debug.LogWarning($"[SaveManager] Skipping corrupted save file {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            // Sort by timestamp descending (newest first)
            saves = saves.OrderByDescending(s => s.timestamp).ToList();

            Debug.Log($"[SaveManager] Found {saves.Count} valid save files.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Failed to scan saves: {ex.Message}");
        }

        return saves;
    }

    /// <summary>
    /// Delete save file by name.
    /// Used by LoadGameUI delete button (Epic 3).
    /// </summary>
    /// <param name="fileName">File name (e.g., "autosave_1.json")</param>
    /// <returns>True if deletion successful, false if file not found or locked</returns>
    public bool DeleteSave(string fileName)
    {
        try
        {
            // Validate file name (security check)
            string filePath = GetSaveFilePath(fileName);

            // Check if file exists (AC7)
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] Save file not found: {fileName}");
                return false;
            }

            // Delete file
            File.Delete(filePath);
            Debug.Log($"[SaveManager] Save deleted: {fileName}");
            return true;
        }
        catch (IOException ex)
        {
            // File locked or I/O error (AC5)
            Debug.LogError($"[SaveManager] Failed to delete save {fileName}: {ex.Message}");
            return false;
        }
        catch (ArgumentException ex)
        {
            // Invalid file name (path traversal attempt)
            Debug.LogError($"[SaveManager] Invalid file name: {fileName} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // Other errors
            Debug.LogError($"[SaveManager] Failed to delete save {fileName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load save game from file and restore game state (Epic 3 - LoadGameUI).
    /// Deserializes SaveData JSON and applies state to game managers.
    /// </summary>
    /// <param name="fileName">File name to load (e.g., "Colony Alpha.json", "autosave_1.json")</param>
    /// <returns>True if load successful, false if corrupted or missing</returns>
    public bool LoadGame(string fileName)
    {
        try
        {
            string filePath = GetSaveFilePath(fileName);

            // Check if file exists
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] Save file not found: {fileName}");
                return false;
            }

            // Read and deserialize JSON
            string json = File.ReadAllText(filePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // COOP detection logging (AC4)
            if (saveData.mode == GameMode.COOP)
            {
                Debug.Log($"[SaveManager] COOP save loaded: {saveData.baseName}");
            }
            else
            {
                Debug.Log($"[SaveManager] Save loaded successfully: {saveData.baseName}");
            }

            // TODO: Apply state to ISaveable managers (Epic 7+)
            // For MVP, store loaded data in SaveManager for later use
            // Future: FindObjectsOfType<ISaveable>() and call LoadSaveData()

            return true;
        }
        catch (Exception ex)
        {
            // Corrupted save handling (AC6)
            Debug.LogError($"[SaveManager] Corrupted save file: {fileName} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Autosave during gameplay (Epic 7 - stub for now).
    /// Will be called by gameplay managers every 60 seconds + mission/chapter completion.
    /// </summary>
    public void AutoSave()
    {
        // TODO: Epic 7 - Implement autosave rotation (autosave_1/2/3.json)
        Debug.LogWarning("[SaveManager] AutoSave() not yet implemented (Epic 7).");
    }

    /// <summary>
    /// Get next autosave file name with rotation (Epic 7 - stub).
    /// </summary>
    /// <returns>Next autosave file name</returns>
    public string GetNextAutosaveName()
    {
        // TODO: Epic 7 - Implement rotation logic
        return "autosave_1.json";
    }

    /// <summary>
    /// Check if save file exists.
    /// </summary>
    /// <param name="fileName">File name to check</param>
    /// <returns>True if file exists</returns>
    public bool SaveExists(string fileName)
    {
        try
        {
            string filePath = GetSaveFilePath(fileName);
            return File.Exists(filePath);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // ============================================================================
    // PRIVATE METHODS: FILE PATH SECURITY
    // ============================================================================

    /// <summary>
    /// Get full save file path with security validation.
    /// Prevents directory traversal attacks (e.g., "../../../etc/passwd").
    /// All saves forced into Application.persistentDataPath/Saves/ directory.
    /// </summary>
    /// <param name="fileName">File name (e.g., "autosave_1.json")</param>
    /// <returns>Full validated file path</returns>
    /// <exception cref="ArgumentException">Thrown if fileName contains path traversal characters</exception>
    private string GetSaveFilePath(string fileName)
    {
        // Reject null or empty
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("[SaveManager] File name cannot be null or empty.", nameof(fileName));
        }

        // Reject path separators (/, \)
        if (fileName.Contains("/") || fileName.Contains("\\"))
        {
            throw new ArgumentException($"[SaveManager] File name cannot contain path separators: {fileName}", nameof(fileName));
        }

        // Reject parent directory traversal (..)
        if (fileName.Contains(".."))
        {
            throw new ArgumentException($"[SaveManager] File name cannot contain ..: {fileName}", nameof(fileName));
        }

        // Reject absolute paths (C:\, /etc/, etc.)
        if (Path.IsPathRooted(fileName))
        {
            throw new ArgumentException($"[SaveManager] File name cannot be an absolute path: {fileName}", nameof(fileName));
        }

        // Combine with saves path (forces into correct directory)
        string fullPath = Path.Combine(savesPath, fileName);

        return fullPath;
    }
}
