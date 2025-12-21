using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NetworkedMissionManager - Syncs mission/chapter state across network.
/// Server-authoritative: Only server progresses missions.
/// Works alongside MissionChapterManager for host, syncs state to clients.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedMissionManager : NetworkBehaviour
{
    public static NetworkedMissionManager Instance { get; private set; }

    // Synced state (FishNet 4.x SyncVar<T>)
    private readonly SyncVar<int> _currentChapterIndex = new SyncVar<int>(-1);
    private readonly SyncVar<int> _currentMissionIndex = new SyncVar<int>(-1);
    private readonly SyncVar<string> _currentMissionName = new SyncVar<string>("");
    private readonly SyncVar<string> _currentChapterName = new SyncVar<string>("");
    private readonly SyncVar<bool> _missionActive = new SyncVar<bool>(false);
    private readonly SyncVar<float> _missionTimer = new SyncVar<float>(0f);

    // Synced objective progress (FishNet 4.x - no attribute needed)
    private readonly SyncDictionary<int, int> objectiveProgress = new SyncDictionary<int, int>();
    private readonly SyncDictionary<int, bool> objectiveCompleted = new SyncDictionary<int, bool>();

    // Events
    public event Action<int> OnChapterChanged;
    public event Action<int> OnMissionChanged;
    public event Action<bool> OnMissionStateChanged;
    public event Action<int, int> OnObjectiveUpdated;

    // Properties
    public int CurrentChapterIndex => _currentChapterIndex.Value;
    public int CurrentMissionIndex => _currentMissionIndex.Value;
    public string CurrentMissionName => _currentMissionName.Value;
    public string CurrentChapterName => _currentChapterName.Value;
    public bool IsMissionActive => _missionActive.Value;
    public float MissionTimer => _missionTimer.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _currentChapterIndex.OnChange += OnChapterIndexChanged;
        _currentMissionIndex.OnChange += OnMissionIndexChanged;
        _missionActive.OnChange += OnMissionActiveChanged;
        objectiveProgress.OnChange += OnObjectiveProgressChanged;
        objectiveCompleted.OnChange += OnObjectiveCompletedChanged;

        Debug.Log("[NetworkedMissionManager] Network started");
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        _currentChapterIndex.OnChange -= OnChapterIndexChanged;
        _currentMissionIndex.OnChange -= OnMissionIndexChanged;
        _missionActive.OnChange -= OnMissionActiveChanged;
        objectiveProgress.OnChange -= OnObjectiveProgressChanged;
        objectiveCompleted.OnChange -= OnObjectiveCompletedChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Subscribe to MissionChapterManager events
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted += ServerOnMissionStarted;
            MissionChapterManager.Instance.OnMissionCompleted += ServerOnMissionCompleted;
            MissionChapterManager.Instance.OnChapterStarted += ServerOnChapterStarted;
            MissionChapterManager.Instance.OnObjectiveCompleted += ServerOnObjectiveCompleted;

            // If a chapter is already active, sync its state immediately
            if (MissionChapterManager.Instance.CurrentChapter != null)
            {
                Debug.Log("[NetworkedMissionManager] Syncing existing chapter state to network...");
                _currentChapterIndex.Value = MissionChapterManager.Instance.CurrentChapter.chapterNumber;
                _currentChapterName.Value = MissionChapterManager.Instance.CurrentChapter.chapterName;
                
                // Also sync current mission index (convert 0-based to 1-based)
                _currentMissionIndex.Value = MissionChapterManager.Instance.CurrentMissionIndex + 1;
                
                if (MissionChapterManager.Instance.CurrentMission != null)
                {
                    _currentMissionName.Value = MissionChapterManager.Instance.CurrentMission.missionName;
                    _missionActive.Value = MissionChapterManager.Instance.IsMissionActive;
                    _missionTimer.Value = MissionChapterManager.Instance.MissionTimer;
                }
            }
        }

        Debug.Log("[NetworkedMissionManager] Server initialized");
    }

    private void Update()
    {
        // Server updates mission timer
        if (!IsServerStarted) return;
        if (!_missionActive.Value) return;

        _missionTimer.Value += Time.deltaTime;
    }

    // ============================================================================
    // SYNC CALLBACKS (FishNet 4.x)
    // ============================================================================

    private void OnChapterIndexChanged(int prev, int next, bool asServer)
    {
        OnChapterChanged?.Invoke(next);
    }

    private void OnMissionIndexChanged(int prev, int next, bool asServer)
    {
        OnMissionChanged?.Invoke(next);
    }

    private void OnMissionActiveChanged(bool prev, bool next, bool asServer)
    {
        OnMissionStateChanged?.Invoke(next);
    }

    private void OnObjectiveProgressChanged(SyncDictionaryOperation op, int key, int value, bool asServer)
    {
        OnObjectiveUpdated?.Invoke(key, value);
    }

    private void OnObjectiveCompletedChanged(SyncDictionaryOperation op, int key, bool value, bool asServer)
    {
        if (value)
        {
            Debug.Log($"[NetworkedMissionManager] Objective {key} completed");
        }
    }

    // ============================================================================
    // SERVER EVENT HANDLERS (From MissionChapterManager)
    // ============================================================================

    /// <summary>
    /// Explicitly set the current chapter from the server.
    /// Called by MissionChapterManager when starting a chapter.
    /// </summary>
    [Server]
    public void ServerSetChapter(ChapterData chapter)
    {
        if (chapter == null) return;
        
        _currentChapterIndex.Value = chapter.chapterNumber;
        _currentChapterName.Value = chapter.chapterName;
        _currentMissionIndex.Value = 1;

        objectiveProgress.Clear();
        objectiveCompleted.Clear();

        Debug.Log($"[NetworkedMissionManager] Server explicitly set chapter: {chapter.chapterName} ({chapter.chapterNumber})");
    }

    [Server]
    private void ServerOnChapterStarted(ChapterData chapter)
    {
        ServerSetChapter(chapter);
    }

    [Server]
    private void ServerOnMissionStarted(MissionData mission)
    {
        // Use missionNumber (1-based) if available, otherwise fallback to current index + 1
        int missionNum = mission.missionNumber > 0 ? mission.missionNumber : (MissionChapterManager.Instance != null ? MissionChapterManager.Instance.CurrentMissionIndex + 1 : 1);
        
        _currentMissionIndex.Value = missionNum;
        _currentMissionName.Value = mission.missionName;
        _missionActive.Value = true;
        _missionTimer.Value = 0f;

        // Initialize objective tracking
        objectiveProgress.Clear();
        objectiveCompleted.Clear();

        for (int i = 0; i < mission.objectives.Count; i++)
        {
            objectiveProgress.Add(i, 0);
            objectiveCompleted.Add(i, false);
        }

        Debug.Log($"[NetworkedMissionManager] Mission started: {mission.missionName}");
    }

    [Server]
    private void ServerOnMissionCompleted(MissionData mission)
    {
        _missionActive.Value = false;
        Debug.Log($"[NetworkedMissionManager] Mission completed: {mission.missionName}");
    }

    [Server]
    private void ServerOnObjectiveCompleted(MissionObjective objective)
    {
        // Find objective index and mark complete
        if (MissionChapterManager.Instance?.CurrentMission != null)
        {
            var objectives = MissionChapterManager.Instance.CurrentMission.objectives;
            int index = objectives.IndexOf(objective);
            if (index >= 0 && objectiveCompleted.ContainsKey(index))
            {
                objectiveCompleted[index] = true;
            }
        }
    }

    // ============================================================================
    // SERVER METHODS
    // ============================================================================

    /// <summary>
    /// Update objective progress (server only)
    /// </summary>
    [Server]
    public void ServerUpdateObjectiveProgress(int objectiveIndex, int amount)
    {
        if (objectiveProgress.ContainsKey(objectiveIndex))
        {
            objectiveProgress[objectiveIndex] = amount;
        }
    }

    /// <summary>
    /// Start chapter (server only) - triggers MissionChapterManager
    /// </summary>
    [Server]
    public void ServerStartChapter(int chapterIndex)
    {
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.StartChapter(chapterIndex);
        }
    }

    /// <summary>
    /// Start next mission (server only)
    /// </summary>
    [Server]
    public void ServerStartNextMission()
    {
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.StartNextMission();
        }
    }

    // ============================================================================
    // CLIENT API
    // ============================================================================

    /// <summary>
    /// Get objective progress (works on all clients)
    /// </summary>
    public int GetObjectiveProgress(int objectiveIndex)
    {
        if (objectiveProgress.ContainsKey(objectiveIndex))
            return objectiveProgress[objectiveIndex];
        return 0;
    }

    /// <summary>
    /// Check if objective is complete
    /// </summary>
    public bool IsObjectiveComplete(int objectiveIndex)
    {
        if (objectiveCompleted.ContainsKey(objectiveIndex))
            return objectiveCompleted[objectiveIndex];
        return false;
    }

    /// <summary>
    /// Get all objectives completed status
    /// </summary>
    public List<bool> GetAllObjectiveStatus()
    {
        List<bool> status = new List<bool>();
        foreach (var kvp in objectiveCompleted)
        {
            status.Add(kvp.Value);
        }
        return status;
    }
}
