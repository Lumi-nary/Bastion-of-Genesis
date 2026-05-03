using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // Auto-stop gameplay tracking when returning to menu
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Ensure Saves directory exists
        if (!Directory.Exists(savesPath))
        {
            Directory.CreateDirectory(savesPath);
            Debug.Log($"[SaveManager] Created Saves directory at: {savesPath}");
        }

        Debug.Log($"[SaveManager] SaveManager singleton initialized. Saves directory: {savesPath}");
    }

    // ============================================================================
    // AUTOSAVE & GAMEPLAY STATE
    // ============================================================================

    private const float AUTOSAVE_INTERVAL = 300f; // 5 minutes
    private const int AUTOSAVE_SLOT_COUNT = 3;
    private float autosaveTimer = 0f;
    private int nextAutosaveSlot = 1; // rotates 1→2→3→1
    private bool isGameplayActive = false;
    private float sessionPlaytime = 0f; // playtime accumulated this session
    private float loadedPlaytime = 0f; // playtime from loaded save

    /// <summary>
    /// SaveData loaded from file, waiting to be applied after scene loads.
    /// </summary>
    public float TotalPlaytime => loadedPlaytime + sessionPlaytime;

    private SaveData pendingSaveData;

    /// <summary>
    /// True if a save is being loaded and state needs to be restored after scene load.
    /// </summary>
    public bool HasPendingSaveData => pendingSaveData != null;

    /// <summary>
    /// Currently active save file name (for overwriting on manual save).
    /// </summary>
    public string CurrentSaveFileName { get; private set; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Stop autosave when returning to menu scene
        if (scene.name == "MenuScene" && isGameplayActive)
        {
            Debug.Log("[SaveManager] Menu scene loaded, stopping gameplay tracking");
            StopGameplayTracking();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Unsubscribe from events
            if (MissionChapterManager.Instance != null)
                MissionChapterManager.Instance.OnMissionCompleted -= OnMissionCompleted;

            Debug.Log("[SaveManager] SaveManager singleton destroyed.");
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isGameplayActive) return;

        sessionPlaytime += Time.unscaledDeltaTime;

        autosaveTimer += Time.unscaledDeltaTime;
        if (autosaveTimer >= AUTOSAVE_INTERVAL)
        {
            autosaveTimer = 0f;
            PerformAutosave();
        }
    }

    /// <summary>
    /// Call when gameplay begins (after scene loads and state is ready).
    /// </summary>
    public void StartGameplayTracking()
    {
        isGameplayActive = true;
        autosaveTimer = 0f;

        // Subscribe to mission complete for autosave
        if (MissionChapterManager.Instance != null)
            MissionChapterManager.Instance.OnMissionCompleted += OnMissionCompleted;
    }

    /// <summary>
    /// Call when leaving gameplay (returning to menu, etc.).
    /// </summary>
    public void StopGameplayTracking()
    {
        isGameplayActive = false;

        if (MissionChapterManager.Instance != null)
            MissionChapterManager.Instance.OnMissionCompleted -= OnMissionCompleted;
    }

    private void OnMissionCompleted(MissionData mission)
    {
        Debug.Log($"[SaveManager] Mission completed, performing autosave");
        PerformAutosave();
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
                version = "2.0.0",
                baseName = baseName,
                difficulty = difficulty,
                mode = mode,
                timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                totalPlaytime = 0f, // New save starts at zero playtime
                currentChapter = 1,
                currentMission = 1,
                missionCompletions = new bool[0], // Empty for new save
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

    // Track corrupted saves for user notification (Story 3.3)
    private int lastCorruptedCount = 0;

    /// <summary>
    /// Get count of corrupted saves from last GetAllSaves() call (Story 3.3).
    /// </summary>
    public int GetLastCorruptedCount()
    {
        return lastCorruptedCount;
    }

    /// <summary>
    /// Get all save files in Saves directory with metadata.
    /// Used by LoadGameUI to display save list (Epic 3).
    /// Story 3.3: Tracks corrupted saves for error notification.
    /// </summary>
    /// <returns>List of SaveMetadata sorted by timestamp (newest first)</returns>
    public List<SaveMetadata> GetAllSaves()
    {
        List<SaveMetadata> saves = new List<SaveMetadata>();
        lastCorruptedCount = 0; // Reset counter

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
                    // Skip corrupted files, log warning, increment counter (Story 3.3)
                    lastCorruptedCount++;
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
    /// Load save game from file. Deserializes and stores pendingSaveData
    /// for restoration after the chapter scene loads.
    /// Sets pending metadata for scene handoff.
    /// </summary>
    /// <param name="fileName">File name to load</param>
    /// <returns>True if file read and deserialized successfully</returns>
    public bool LoadGame(string fileName)
    {
        try
        {
            string filePath = GetSaveFilePath(fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] Save file not found: {fileName}");
                return false;
            }

            string json = File.ReadAllText(filePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            // Set pending metadata for scene handoff
            pendingBaseName = saveData.baseName;
            pendingDifficulty = saveData.difficulty;
            pendingMode = saveData.mode;
            pendingChapter = saveData.currentChapter;
            CurrentSaveFileName = fileName;
            loadedPlaytime = saveData.totalPlaytime;
            sessionPlaytime = 0f;

            // Store for post-scene-load restoration
            if (saveData.version == "1.0.0" || saveData.resources == null)
            {
                // Old format: no manager state, treat as new game at this chapter
                pendingSaveData = null;
                Debug.Log($"[SaveManager] v1.0.0 save loaded (no world state): {saveData.baseName}");
            }
            else
            {
                pendingSaveData = saveData;
                Debug.Log($"[SaveManager] Save loaded: {saveData.baseName}, pending restore after scene load");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Corrupted save file: {fileName} - {ex.Message}");
            return false;
        }
    }

    // ============================================================================
    // SAVE GAME (CORE)
    // ============================================================================

    /// <summary>
    /// Save current game state to file. Collects state from all managers.
    /// </summary>
    public bool SaveGame(string fileName)
    {

        try
        {
            SaveData saveData = new SaveData
            {
                version = "2.0.0",
                baseName = pendingBaseName ?? "Unnamed",
                difficulty = pendingDifficulty,
                mode = pendingMode,
                timestamp = DateTime.UtcNow.ToString("o"),
                totalPlaytime = loadedPlaytime + sessionPlaytime,
                hostPlayerName = pendingMode == GameMode.COOP ? (pendingBaseName ?? "") : "",
                connectedPlayers = new string[0]
            };

            // Export state from each manager
            if (ResourceManager.Instance != null)
                saveData.resources = ResourceManager.Instance.ExportState();
            if (WorkerManager.Instance != null)
                saveData.workers = WorkerManager.Instance.ExportState();
            if (BuildingManager.Instance != null)
                saveData.buildings = BuildingManager.Instance.ExportState();
            if (ResearchManager.Instance != null)
                saveData.research = ResearchManager.Instance.ExportState();
            if (PollutionManager.Instance != null)
                saveData.pollution = PollutionManager.Instance.ExportState();
            if (MissionChapterManager.Instance != null)
                saveData.mission = MissionChapterManager.Instance.ExportState();
            if (GridManager.Instance != null)
                saveData.oreMounds = GridManager.Instance.ExportOreMoundState();
            if (EnemyManager.Instance != null)
                saveData.enemies = EnemyManager.Instance.ExportState();

            // Set metadata from mission export for UI display
            if (saveData.mission != null)
            {
                saveData.currentChapter = saveData.mission.currentChapterIndex + 1;
                saveData.currentMission = saveData.mission.currentMissionIndex + 1;
            }

            // Atomic write
            string savePath = GetSaveFilePath(fileName);
            string tempPath = savePath + ".tmp";
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(tempPath, json);

            if (File.Exists(savePath))
                File.Delete(savePath);
            File.Move(tempPath, savePath);

            CurrentSaveFileName = fileName;
            Debug.Log($"[SaveManager] Game saved: {fileName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Failed to save: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Manual save from pause menu. Uses base name as filename.
    /// </summary>
    public bool ManualSave()
    {
        string safeName = SanitizeFileName(pendingBaseName ?? "manual_save");
        string fileName = $"{safeName}.json";
        return SaveGame(fileName);
    }

    /// <summary>
    /// Perform autosave with 3-slot rotation.
    /// </summary>
    public void PerformAutosave()
    {
        string fileName = $"autosave_{nextAutosaveSlot}.json";
        if (SaveGame(fileName))
        {
            nextAutosaveSlot = (nextAutosaveSlot % AUTOSAVE_SLOT_COUNT) + 1;
            Debug.Log($"[SaveManager] Autosave complete: {fileName}, next slot: {nextAutosaveSlot}");
        }
    }

    /// <summary>
    /// Alias for PerformAutosave (backwards compatibility with PauseMenuUI).
    /// </summary>
    public void AutoSave()
    {
        PerformAutosave();
    }

    // ============================================================================
    // RESTORE STATE (CALLED AFTER SCENE LOAD)
    // ============================================================================

    /// <summary>
    /// Restore game state from pending save data.
    /// Called by MissionChapterManager after the chapter scene loads.
    /// Import order matters for dependencies.
    /// </summary>
    public void RestoreStateFromSave()
    {
        if (pendingSaveData == null)
        {
            Debug.LogWarning("[SaveManager] No pending save data to restore.");
            return;
        }

        SaveData data = pendingSaveData;
        pendingSaveData = null;

        Debug.Log("[SaveManager] Restoring game state from save...");

        // 1. Resources (must be first — capacities needed by workers and buildings)
        if (ResourceManager.Instance != null && data.resources != null)
            ResourceManager.Instance.ImportState(data.resources);

        // 2. Workers (must be before buildings for worker pool)
        if (WorkerManager.Instance != null && data.workers != null)
            WorkerManager.Instance.ImportState(data.workers);

        // 3. Research (effects modify capacities and unlock buildings)
        if (ResearchManager.Instance != null && data.research != null)
            ResearchManager.Instance.ImportState(data.research);

        // 4. Buildings (depends on resources, workers, research)
        if (BuildingManager.Instance != null && data.buildings != null)
            BuildingManager.Instance.ImportState(data.buildings);

        // 5. Pollution
        if (PollutionManager.Instance != null && data.pollution != null)
            PollutionManager.Instance.ImportState(data.pollution);

        // 6. Mission state (restore objective progress)
        if (MissionChapterManager.Instance != null && data.mission != null)
            MissionChapterManager.Instance.ImportState(data.mission);

        // 7. Ore mounds (scene objects matched by position)
        if (GridManager.Instance != null && data.oreMounds != null)
            GridManager.Instance.ImportOreMoundState(data.oreMounds);

        // 8. Enemy counters
        if (EnemyManager.Instance != null && data.enemies != null)
            EnemyManager.Instance.ImportState(data.enemies);

        // 9. Energy recalculates from building state (never saved)
        if (EnergyManager.Instance != null)
            EnergyManager.Instance.ForceUpdate();

        // Start tracking gameplay
        StartGameplayTracking();

        Debug.Log("[SaveManager] Game state restored successfully.");
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
    // HELPERS
    // ============================================================================

    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "save";

        // Replace spaces and invalid chars
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = name;
        foreach (char c in invalid)
            sanitized = sanitized.Replace(c, '_');
        sanitized = sanitized.Replace(' ', '_');
        return sanitized;
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
