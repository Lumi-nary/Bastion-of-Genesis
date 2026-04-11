using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public class MissionChapterManager : MonoBehaviour
{
    public static MissionChapterManager Instance { get; private set; }

    [Header("Chapter Configuration")]
    [SerializeField] private List<ChapterData> chapters = new List<ChapterData>();

    [Header("Mission Voice Clips")]
    [Tooltip("Plays when any mission starts")]
    [SerializeField] private AudioClip missionStartedVoice;
    [Tooltip("Plays when any objective is completed")]
    [SerializeField] private AudioClip objectiveUpdatedVoice;
    [Tooltip("Plays when any mission is completed")]
    [SerializeField] private AudioClip missionAccomplishedVoice;

    [Header("Current State")]
    [SerializeField] private MissionData currentMission;

    private int currentChapterIndex = 0;
    private int currentMissionIndex = 0;
    private ChapterData currentChapter;

    private float missionTimer = 0f;
    private bool missionActive = false;

    // Scene transition tracking
    private bool awaitingSceneValidation = false;
    private bool isLoadingFromSave = false;

    #region Events
    // Mission Events
    public event Action<MissionData> OnMissionStarted;
    public event Action<MissionData> OnMissionCompleted;
    public event Action<MissionData> OnMissionFailed;
    public event Action<MissionObjective> OnObjectiveCompleted;
    public event Action<float> OnMissionTimerUpdate;

    // Chapter Events
    public event Action<ChapterData> OnChapterStarted;
    public event Action<ChapterData> OnChapterCompleted;
    public event Action<int> OnChapterChanged;
    public event Action<ChapterData> OnChapterUnlocked;
    #endregion

    #region Properties
    // Mission Properties
    public MissionData CurrentMission => currentMission;
    public bool IsMissionActive => missionActive;
    public float MissionTimer => missionTimer;

    // Chapter Properties
    public ChapterData CurrentChapter => currentChapter;
    public int CurrentChapterIndex => currentChapterIndex;
    public int CurrentMissionIndex => currentMissionIndex;
    public List<ChapterData> Chapters => chapters;
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to scene events for validation
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called when a new scene is loaded - validates required managers exist
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // COOP Client: FishNet loads the game scene directly (StartChapter is never called),
        // so awaitingSceneValidation is never set. Detect this and trigger validation anyway.
        bool isCoopClient = SaveManager.Instance != null
            && SaveManager.Instance.pendingMode == GameMode.COOP
            && NetworkGameManager.Instance != null
            && NetworkGameManager.Instance.IsClient
            && !NetworkGameManager.Instance.IsHost;

        if (!awaitingSceneValidation && isCoopClient && scene.name != "MenuScene" && scene.name != "CutsceneScene")
        {
            Debug.Log($"[MissionChapterManager] COOP Client scene loaded by FishNet: {scene.name}, triggering validation...");
            StartCoroutine(ValidateSceneAfterDelay());
            return;
        }

        if (!awaitingSceneValidation) return;

        awaitingSceneValidation = false;
        Debug.Log($"[MissionChapterManager] Scene loaded: {scene.name}, validating managers...");

        // Delay validation to allow managers to initialize
        StartCoroutine(ValidateSceneAfterDelay());
    }

    private IEnumerator ValidateSceneAfterDelay()
    {
        // Wait for managers to initialize
        yield return null;
        yield return null; // Extra frame for safety

        // If networked, wait for mission manager to have valid data
        bool isCoopClient = NetworkGameManager.Instance != null
            && NetworkGameManager.Instance.IsClient
            && !NetworkGameManager.Instance.IsServer;

        if (isCoopClient)
        {
            Debug.Log("[MissionChapterManager] COOP Client - waiting for mission data from host...");

            // Wait for NetworkedMissionManager to exist and have a valid chapter index
            float timeout = 10.0f;
            while (timeout > 0)
            {
                if (NetworkedMissionManager.Instance != null && NetworkedMissionManager.Instance.CurrentChapterIndex > 0)
                    break;

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (NetworkedMissionManager.Instance != null && NetworkedMissionManager.Instance.CurrentChapterIndex > 0)
            {
                int netChapterNum = NetworkedMissionManager.Instance.CurrentChapterIndex;
                int netIndex = netChapterNum - 1;

                if (netIndex >= 0 && netIndex < chapters.Count)
                {
                    currentChapterIndex = netIndex;
                    currentChapter = chapters[netIndex];

                    int netMissionNum = NetworkedMissionManager.Instance.CurrentMissionIndex;
                    currentMissionIndex = Mathf.Max(0, netMissionNum - 1);

                    Debug.Log($"[SYNC SUCCESS] Client synced chapter/mission from host - Chapter: {currentChapter.chapterName}, Mission Index: {currentMissionIndex}");
                }
                else
                {
                    Debug.LogError($"[MissionChapterManager] Client received invalid chapter number: {netChapterNum}");
                }
            }
            else
            {
                Debug.LogWarning($"[MissionChapterManager] COOP Client mission sync timeout. Using chapter 0 as fallback.");
                if (chapters.Count > 0)
                {
                    currentChapterIndex = 0;
                    currentChapter = chapters[0];
                }
            }
        }
        else if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsOnline)
        {
            // Host: wait for network managers briefly then continue
            yield return null;
            yield return null;
        }

        if (ValidateChapterScene())
        {
            // Safeguard: Do not initialize if we still don't have a chapter (prevents UI/Resource corruption)
            if (currentChapter == null && (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsOnline))
            {
                Debug.LogError("[MissionChapterManager] Cannot initialize chapter state: currentChapter is still NULL after timeout!");
                yield break;
            }

            // Initialize chapter values AFTER scene managers are ready
            InitializeChapterState();
        }
    }

    /// <summary>
    /// Initialize all chapter state after scene loads
    /// Called after scene validation passes to ensure managers exist
    /// </summary>
    private void InitializeChapterState()
    {
        if (currentChapter == null)
        {
            Debug.LogError("[MissionChapterManager] No current chapter to initialize! This is critical for setup.");
            return;
        }

        Debug.Log($"[MissionChapterManager] Initializing chapter state for: {currentChapter.chapterName}");

        // Loading from save: RestoreStateFromSave handles resources/workers/pollution/buildings
        if (isLoadingFromSave)
        {
            isLoadingFromSave = false;
            Debug.Log("[MissionChapterManager] Loading from save — skipping normal init, restoring state");

            // Configure pollution limits from chapter (needed before importing pollution values)
            if (PollutionManager.Instance != null)
                PollutionManager.Instance.ConfigureFromChapter(currentChapter.maxPollution, currentChapter.pollutionDecayRate);

            // Scene setup that's always needed
            if (TileStateManager.Instance != null)
                TileStateManager.Instance.SetIntegrationRadius(currentChapter.startingIntegrationRadius);
            if (BuildingManager.Instance != null)
                BuildingManager.Instance.OnBuildingPlaced += OnBuildingPlaced;
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.ReloadEnemyTypesFromChapter();

            // Music
            if (AudioManager.Instance != null)
            {
                if (currentChapter.backgroundMusic != null)
                    AudioManager.Instance.SetNormalMusic(currentChapter.backgroundMusic);
                if (currentChapter.battleMusic != null)
                    AudioManager.Instance.SetBattleMusic(currentChapter.battleMusic);
            }

            // Restore all manager state from save
            if (SaveManager.Instance != null)
                SaveManager.Instance.RestoreStateFromSave();

            return;
        }

        bool isClientOnly = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsClient && !NetworkGameManager.Instance.IsServer;

        // Reset and initialize resources (Server only, or singleplayer)
        // Clients should NOT reset, as they receive synced values from the server
        if (ResourceManager.Instance != null)
        {
            if (!isClientOnly)
            {
                ResourceManager.Instance.ResetAllResources();
                InitializeChapterResources();
            }
            else
            {
                Debug.Log("[MissionChapterManager] Client: Skipping resource reset to preserve networked state.");

                // Force a sync from the network to local manager now that the scene is ready
                if (NetworkedResourceManager.Instance != null)
                {
                    // Request fresh state from server to guarantee UI has the latest values
                    NetworkedResourceManager.Instance.RequestFullSyncServerRpc();
                }
            }
        }

        // Reset and initialize workers
        if (WorkerManager.Instance != null)
        {
            if (!isClientOnly)
            {
                WorkerManager.Instance.ResetAllWorkers();
                InitializeChapterWorkers();
            }
            else
            {
                Debug.Log("[MissionChapterManager] Client: Skipping worker reset to preserve networked state.");

                if (NetworkedWorkerManager.Instance != null)
                {
                    NetworkedWorkerManager.Instance.RequestFullSyncServerRpc();
                }
            }
        }

        // Reset pollution and configure from chapter settings
        // Clients skip reset to preserve synced values from host
        if (PollutionManager.Instance != null)
        {
            if (!isClientOnly)
            {
                PollutionManager.Instance.ResetPollution();
                PollutionManager.Instance.ConfigureFromChapter(currentChapter.maxPollution, currentChapter.pollutionDecayRate);
            }
            else
            {
                Debug.Log("[MissionChapterManager] Client: Skipping pollution reset to preserve networked state.");
                // Still configure limits so UI displays correctly
                PollutionManager.Instance.ConfigureFromChapter(currentChapter.maxPollution, currentChapter.pollutionDecayRate);
            }
        }

        // Set starting integration radius
        if (TileStateManager.Instance != null)
        {
            TileStateManager.Instance.SetIntegrationRadius(currentChapter.startingIntegrationRadius);
            Debug.Log($"[MissionChapterManager] Integration radius set to: {currentChapter.startingIntegrationRadius}");
        }

        // Subscribe to building events for objectives (host/singleplayer only)
        // Clients receive objective progress from the host via NetworkedMissionManager
        if (!isClientOnly && BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced += OnBuildingPlaced;
        }

        // Reload enemy types from chapter data
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ReloadEnemyTypesFromChapter();
        }

        // Play chapter background music
        if (AudioManager.Instance != null)
        {
            if (currentChapter.backgroundMusic != null)
            {
                AudioManager.Instance.SetNormalMusic(currentChapter.backgroundMusic);
                Debug.Log($"[MissionChapterManager] Playing chapter music: {currentChapter.backgroundMusic.name}");
            }
            if (currentChapter.battleMusic != null)
            {
                AudioManager.Instance.SetBattleMusic(currentChapter.battleMusic);
            }
        }

        Debug.Log("[MissionChapterManager] Chapter state initialization complete");

        if (!isClientOnly)
        {
            // Host/Singleplayer: Start tracking and begin missions
            if (SaveManager.Instance != null)
                SaveManager.Instance.StartGameplayTracking();

            // Play chapter intro dialogue (if set), then start first mission
            StartCoroutine(PlayChapterIntroAndStartMission());
        }
        else
        {
            // Client: Activate the current mission locally for UI display.
            // The mission index was already synced from the host in ValidateSceneAfterDelay.
            if (currentChapter != null && currentMissionIndex < currentChapter.missions.Count)
            {
                currentMission = currentChapter.missions[currentMissionIndex];

                // Reset objectives to clean state first
                foreach (var objective in currentMission.objectives)
                {
                    objective.isCompleted = false;
                    objective.currentAmount = 0;
                    objective.currentTime = 0f;
                }

                // Sync objective progress from host via NetworkedMissionManager
                if (NetworkedMissionManager.Instance != null)
                {
                    NetworkedMissionManager.Instance.SyncObjectivesToLocal(currentMission);
                }

                missionActive = true;
                OnMissionStarted?.Invoke(currentMission);
                Debug.Log($"[SYNC] Client activated mission for UI: {currentMission.missionName}");
            }

            Debug.Log("[SYNC] Client chapter state initialization complete.");
        }
    }

    /// <summary>
    /// Play chapter intro dialogue (if any), then start the first mission
    /// </summary>
    private IEnumerator PlayChapterIntroAndStartMission()
    {
        // Play chapter intro dialogue if set
        if (currentChapter.introDialogue != null && DialogueManager.Instance != null)
        {
            Debug.Log($"[MissionChapterManager] Playing chapter intro dialogue: {currentChapter.introDialogue.dialogueName}");
            DialogueManager.Instance.StartDialogue(currentChapter.introDialogue);

            // Wait for dialogue to finish
            while (DialogueManager.Instance.IsDialogueActive)
            {
                yield return null;
            }
        }

        // Start first mission
        StartNextMission();
    }

    private void Start()
    {
        // Initialize the first chapter
        if (chapters.Count > 0)
        {
            currentChapter = chapters[0];
            currentChapter.isUnlocked = true; // First chapter is always unlocked
        }
    }

    private void Update()
    {
        if (!missionActive || currentMission == null) return;

        // Update mission timer
        missionTimer += Time.deltaTime;
        OnMissionTimerUpdate?.Invoke(missionTimer);

        // Check scripted waves
        foreach (var wave in currentMission.scriptedWaves)
        {
            // Check objective prerequisite if set
            bool objectiveReady = wave.triggerAfterObjectiveIndex < 0 ||
                (wave.triggerAfterObjectiveIndex < currentMission.objectives.Count &&
                 currentMission.objectives[wave.triggerAfterObjectiveIndex].isCompleted);

            if (!wave.isTriggered && missionTimer >= wave.triggerTime && objectiveReady)
            {
                wave.isTriggered = true;
                if (!string.IsNullOrEmpty(wave.waveMessage))
                {
                    Debug.Log($"[Mission] Scripted Wave Message: {wave.waveMessage}");
                    // TODO: Show on UI
                }

                if (WaveController.Instance != null)
                {
                    // Use the extended overload if edges or spawnList are specified
                    if ((wave.spawnEdges != null && wave.spawnEdges.Count > 0) ||
                        (wave.spawnList != null && wave.spawnList.Count > 0))
                    {
                        WaveController.Instance.TriggerScriptedWave(wave.enemyCount, wave.spawnEdges, wave.spawnList);
                    }
                    else
                    {
                        WaveController.Instance.TriggerScriptedWave(wave.enemyCount);
                    }
                }
            }
        }

        // Check time limit
        if (currentMission.timeLimit > 0 && missionTimer >= currentMission.timeLimit)
        {
            if (currentMission.failOnTimeExpired)
            {
                FailMission();
            }
        }

        // Update time-based objectives
        UpdateTimeBasedObjectives();

        // Check if all main objectives are complete
        if (currentMission.AreMainObjectivesComplete())
        {
            CompleteMission();
        }
    }

    #region Chapter Management
    /// <summary>
    /// Start a specific chapter by index
    /// </summary>
    /// <summary>
    /// Start a chapter from a loaded save file.
    /// Skips normal state initialization — SaveManager.RestoreStateFromSave() handles it.
    /// </summary>
    public void StartChapterFromLoad(int chapterIndex)
    {
        isLoadingFromSave = true;
        StartChapter(chapterIndex);
    }

    public void StartChapter(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Count)
        {
            Debug.LogError($"Invalid chapter index: {chapterIndex}");
            return;
        }

        ChapterData chapter = chapters[chapterIndex];

        if (!chapter.isUnlocked)
        {
            Debug.LogWarning($"Chapter {chapterIndex + 1} is locked!");
            return;
        }

        currentChapterIndex = chapterIndex;
        currentChapter = chapter;
        currentMissionIndex = 0;

        // Notify listeners that chapter changed
        OnChapterChanged?.Invoke(currentChapterIndex);

        // Pre-load cleanup: Clear existing enemies and reset pathfinding
        CleanupBeforeSceneLoad();

        // Load the chapter's scene
        // NOTE: Chapter state (resources, workers, integration) is initialized
        // AFTER scene loads in InitializeChapterState() via ValidateSceneAfterDelay()
        if (!string.IsNullOrEmpty(chapter.sceneName))
        {
            awaitingSceneValidation = true;

            // Check if we're in COOP mode - use networked scene loading
            bool isCoop = SaveManager.Instance != null && SaveManager.Instance.pendingMode == GameMode.COOP;
            bool isHost = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsHost;

            if (isCoop && isHost)
            {
                // COOP Host: Load scene for all players via FishNet
                Debug.Log($"[MissionChapterManager] COOP Host - Loading {chapter.sceneName} via FishNet for all players");
                NetworkGameManager.Instance.LoadNetworkedScene(chapter.sceneName);
            }
            else if (isCoop && !isHost)
            {
                // COOP Client: FishNet handles scene loading automatically.
                // The host registered the game scene as a global scene when opening LAN.
                // Do NOT load the scene locally — FishNet will load it.
                Debug.Log($"[MissionChapterManager] COOP Client - Scene will be loaded by FishNet (not locally)");
            }
            else
            {
                // Singleplayer: Load scene directly
                SceneManager.LoadScene(chapter.sceneName);
            }
        }

        OnChapterStarted?.Invoke(currentChapter);
        Debug.Log($"Chapter {currentChapterIndex + 1} Started: {currentChapter.chapterName}");

        // EXPLICIT NETWORK SYNC (Push)
        if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
        {
            if (NetworkedMissionManager.Instance != null)
            {
                NetworkedMissionManager.Instance.ServerSetChapter(currentChapter);
            }
        }
    }

    /// <summary>
    /// Cleanup persistent managers before loading a new chapter scene
    /// Ensures no stale references or enemies carry over
    /// </summary>
    private void CleanupBeforeSceneLoad()
    {
        // Unsubscribe from events
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced -= OnBuildingPlaced;
        }

        // Clear all active enemies
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ClearAllEnemies();
            EnemyManager.Instance.ResetForNewMission();
            Debug.Log("[MissionChapterManager] Cleared enemies before scene load");
        }

        // Note: PathfindingManager and GridManager handle their own cleanup via OnSceneLoaded
        // They will reinitialize when the new scene loads
    }

    private void OnBuildingPlaced(Building building)
    {
        if (building != null)
        {
            UpdateObjectiveProgress(ObjectiveType.BuildStructures, 1, buildingData: building.BuildingData);
        }
    }

    /// <summary>
    /// Validates that all required managers exist in the chapter scene
    /// Called after scene loads to ensure proper setup
    /// </summary>
    /// <returns>True if all required managers are present</returns>
    private bool ValidateChapterScene()
    {
        bool isValid = true;
        var missingManagers = new List<string>();

        // Check for required scene-specific managers
        if (GridManager.Instance == null)
        {
            missingManagers.Add("GridManager");
            isValid = false;
        }

        if (BuildingManager.Instance == null)
        {
            missingManagers.Add("BuildingManager");
            isValid = false;
        }

        if (WorkerManager.Instance == null)
        {
            missingManagers.Add("WorkerManager");
            isValid = false;
        }

        if (PollutionManager.Instance == null)
        {
            missingManagers.Add("PollutionManager");
            isValid = false;
        }

        // Log results
        if (isValid)
        {
            Debug.Log($"[MissionChapterManager] Scene validation PASSED - all required managers present");
        }
        else
        {
            Debug.LogError($"[MissionChapterManager] Scene validation FAILED - Missing managers: {string.Join(", ", missingManagers)}");
            Debug.LogError("[MissionChapterManager] Ensure the chapter scene has GameObjects with the required manager components!");
        }

        return isValid;
    }

    /// <summary>
    /// Start the next mission in the current chapter
    /// </summary>
    public void StartNextMission()
    {
        if (currentChapter == null)
        {
            Debug.LogError("No active chapter!");
            return;
        }

        if (currentMissionIndex >= currentChapter.missions.Count)
        {
            Debug.LogWarning("All missions in this chapter are complete!");
            CompleteCurrentChapter();
            return;
        }

        MissionData mission = currentChapter.missions[currentMissionIndex];
        StartMission(mission);

        Debug.Log($"Starting Mission {currentMissionIndex + 1}/{currentChapter.missions.Count}: {mission.missionName}");
    }

    /// <summary>
    /// Complete the current chapter and unlock the next one
    /// </summary>
    private void CompleteCurrentChapter()
    {
        if (currentChapter == null) return;

        Debug.Log($"Chapter {currentChapterIndex + 1} Completed: {currentChapter.chapterName}");
        OnChapterCompleted?.Invoke(currentChapter);

        // Unlock next chapter
        if (currentChapterIndex + 1 < chapters.Count)
        {
            ChapterData nextChapter = chapters[currentChapterIndex + 1];
            if (!nextChapter.isUnlocked)
            {
                nextChapter.isUnlocked = true;
                OnChapterUnlocked?.Invoke(nextChapter);
                Debug.Log($"Unlocked Chapter {currentChapterIndex + 2}: {nextChapter.chapterName}");
            }
        }
        else
        {
            Debug.Log("Game Complete! All chapters finished!");
        }
    }

    /// <summary>
    /// Initialize starting resources for the chapter (called once per chapter).
    /// Registers resource types with base capacity and sets starting amounts from ChapterData.
    /// </summary>
    private void InitializeChapterResources()
    {
        if (currentChapter == null || ResourceManager.Instance == null) return;

        // Check if we are the server to update authoritative network state
        bool useNetwork = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer && NetworkedResourceManager.Instance != null;

        foreach (var resourceCost in currentChapter.startingResources)
        {
            if (resourceCost.resourceType != null)
            {
                // Register type with base capacity and starting amount locally
                ResourceManager.Instance.RegisterResourceType(resourceCost.resourceType, resourceCost.amount);

                // If networked, update the authoritative server state
                if (useNetwork)
                {
                    NetworkedResourceManager.Instance.ServerSetResource(resourceCost.resourceType, resourceCost.amount);
                }
            }
        }

        Debug.Log($"[MissionChapterManager] Chapter resources initialized: {currentChapter.startingResources.Count} resource types. Network Sync: {useNetwork}");
    }

    /// <summary>
    /// Initialize starting workers for the chapter (called once per chapter).
    /// Registers worker types with base capacity and sets starting counts from ChapterData.
    /// </summary>
    private void InitializeChapterWorkers()
    {
        if (currentChapter == null || WorkerManager.Instance == null) return;

        // Check if we are the server to update authoritative network state
        bool useNetwork = NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer && NetworkedWorkerManager.Instance != null;

        foreach (var workerConfig in currentChapter.startingWorkers)
        {
            if (workerConfig.workerData != null)
            {
                // Register type with base capacity and starting count
                WorkerManager.Instance.RegisterWorkerType(workerConfig.workerData, workerConfig.initialCount);

                // If networked, update the authoritative server state
                if (useNetwork)
                {
                    NetworkedWorkerManager.Instance.ServerSetWorkers(workerConfig.workerData, workerConfig.initialCount);
                }
            }
        }

        Debug.Log($"[MissionChapterManager] Chapter workers initialized: {currentChapter.startingWorkers.Count} worker types. Network Sync: {useNetwork}");
    }

    /// <summary>
    /// Check if a chapter is unlocked
    /// </summary>
    public bool IsChapterUnlocked(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Count)
            return false;

        return chapters[chapterIndex].isUnlocked;
    }

    /// <summary>
    /// Get completion percentage for a chapter (0-1)
    /// </summary>
    public float GetChapterProgress(int chapterIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= chapters.Count)
            return 0f;

        ChapterData chapter = chapters[chapterIndex];
        if (chapter.missions.Count == 0) return 0f;

        int completedCount = chapter.GetCompletedMissionCount();
        return (float)completedCount / chapter.missions.Count;
    }

    /// <summary>
    /// Restart the current chapter
    /// </summary>
    public void RestartCurrentChapter()
    {
        if (currentChapter != null)
        {
            StartChapter(currentChapterIndex);
        }
    }

    /// <summary>
    /// Get the active enemy races for the current chapter
    /// </summary>
    public List<RaceType> GetActiveRaces()
    {
        return currentChapter?.activeRaces ?? new List<RaceType>();
    }
    #endregion

    #region Mission Management
    public void StartMission(MissionData mission)
    {
        if (mission == null)
        {
            Debug.LogError("Cannot start null mission!");
            return;
        }

        currentMission = mission;
        missionTimer = 0f;

        // Reset all objectives
        foreach (var objective in currentMission.objectives)
        {
            objective.isCompleted = false;
            objective.currentAmount = 0;
            objective.currentTime = 0f;
        }

        // Reset scripted waves
        foreach (var wave in currentMission.scriptedWaves)
        {
            wave.isTriggered = false;
        }

        // Start coroutine to handle dialogue then activate mission
        StartCoroutine(PlayMissionIntroAndActivate());
    }

    /// <summary>
    /// Play mission intro dialogue (if any), then activate the mission
    /// </summary>
    private IEnumerator PlayMissionIntroAndActivate()
    {
        // Play mission intro dialogue if set
        if (currentMission.introDialogue != null && DialogueManager.Instance != null)
        {
            Debug.Log($"[MissionChapterManager] Playing mission intro dialogue: {currentMission.introDialogue.dialogueName}");
            DialogueManager.Instance.StartDialogue(currentMission.introDialogue);

            // Wait for dialogue to finish
            while (DialogueManager.Instance.IsDialogueActive)
            {
                yield return null;
            }
        }

        // Apply wave settings
        if (WaveController.Instance != null)
        {
            WaveController.Instance.SetPaused(currentMission.disableNaturalWaves);
            if (currentMission.disableNaturalWaves)
            {
                Debug.Log("[MissionChapterManager] Natural waves paused for this mission");
            }
        }

        // Now activate the mission
        missionActive = true;

        OnMissionStarted?.Invoke(currentMission);
        Debug.Log($"Mission Started: {currentMission.missionName}");

        // Play mission started voice clip
        if (missionStartedVoice != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(missionStartedVoice);
        }
    }

    private void UpdateTimeBasedObjectives()
    {
        foreach (var objective in currentMission.objectives)
        {
            if (objective.isCompleted) continue;

            switch (objective.type)
            {
                case ObjectiveType.SurviveTime:
                    objective.currentTime = missionTimer;
                    if (objective.currentTime >= objective.targetTime)
                    {
                        CompleteObjective(objective);
                    }
                    break;

                case ObjectiveType.MaintainPollution:
                    if (PollutionManager.Instance != null)
                    {
                        float pollutionPercent = PollutionManager.Instance.PollutionNormalized * 100f;
                        // Track time while pollution is at or above the target threshold
                        if (pollutionPercent >= objective.targetAmount)
                        {
                            objective.currentTime += Time.deltaTime;
                            if (objective.currentTime >= objective.targetTime)
                            {
                                CompleteObjective(objective);
                            }
                        }
                        else
                        {
                            // Reset timer if pollution drops below threshold
                            objective.currentTime = 0f;
                        }
                    }
                    break;

                case ObjectiveType.ReachPollutionLevel:
                    if (PollutionManager.Instance != null)
                    {
                        float currentPollutionPercent = PollutionManager.Instance.PollutionNormalized * 100f;
                        objective.currentAmount = Mathf.RoundToInt(currentPollutionPercent);
                        if (currentPollutionPercent >= objective.targetAmount)
                        {
                            CompleteObjective(objective);
                        }
                    }
                    break;
            }
        }
    }

    public void UpdateObjectiveProgress(ObjectiveType type, int amount, ResourceType resourceType = null, RaceType? raceType = null, BuildingData buildingData = null, WorkerData workerData = null)
    {
        if (!missionActive || currentMission == null) return;

        foreach (var objective in currentMission.objectives)
        {
            if (objective.isCompleted) continue;
            if (objective.type != type) continue;

            // Check if resource type matches (for resource objectives)
            if (type == ObjectiveType.CollectResources && objective.requiredResource != resourceType)
                continue;

            // Check if race type matches (for enemy defeat objectives)
            if (type == ObjectiveType.DefeatEnemies && raceType.HasValue && objective.targetRace != raceType.Value)
                continue;

            // Check if building type matches (for build objectives)
            if (type == ObjectiveType.BuildStructures && buildingData != null && objective.requiredBuilding != buildingData)
                continue;

            // Check if worker type matches (for worker assembly/assignment objectives)
            if (type == ObjectiveType.AssignWorkers && objective.requiredWorker != null && objective.requiredWorker != workerData)
                continue;

            objective.currentAmount += amount;

            // Sync progress to network so clients see it
            if (NetworkedMissionManager.Instance != null && NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsServer)
            {
                int objIndex = currentMission.objectives.IndexOf(objective);
                if (objIndex >= 0)
                {
                    NetworkedMissionManager.Instance.ServerUpdateObjectiveProgress(objIndex, objective.currentAmount);
                }
            }

            if (objective.currentAmount >= objective.targetAmount)
            {
                CompleteObjective(objective);
            }
        }
    }

    private void CompleteObjective(MissionObjective objective)
    {
        if (objective.isCompleted) return;

        objective.isCompleted = true;
        OnObjectiveCompleted?.Invoke(objective);
        Debug.Log($"Objective Completed: {objective.objectiveDescription}");

        // Play objective updated voice clip
        if (objectiveUpdatedVoice != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(objectiveUpdatedVoice);
        }
    }

    private void CompleteMission()
    {
        if (!missionActive) return;

        missionActive = false;

        // Award completion rewards (main + optional if all optional objectives complete)
        bool includeOptional = currentMission.AreOptionalObjectivesComplete();
        currentMission.ApplyRewards(includeOptional);

        OnMissionCompleted?.Invoke(currentMission);
        Debug.Log($"Mission Completed: {currentMission.missionName}");

        // Play mission accomplished voice clip
        if (missionAccomplishedVoice != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(missionAccomplishedVoice);
        }

        // Advance to next mission in chapter
        currentMissionIndex++;

        // Check if all missions in chapter are complete
        if (currentMissionIndex >= currentChapter.missions.Count)
        {
            CompleteCurrentChapter();
        }
        else
        {
            // Auto-start next mission
            StartNextMission();
        }
    }

    public void FailMission()
    {
        if (!missionActive) return;

        missionActive = false;
        OnMissionFailed?.Invoke(currentMission);
        Debug.Log($"Mission Failed: {currentMission.missionName}");
    }

    public void EndMission()
    {
        missionActive = false;
        currentMission = null;
        missionTimer = 0f;
    }
    #endregion

    // ============================================================================
    // SAVE/LOAD
    // ============================================================================

    public MissionSaveData ExportState()
    {
        var data = new MissionSaveData
        {
            currentChapterIndex = currentChapterIndex,
            currentMissionIndex = currentMissionIndex,
            missionTimer = missionTimer,
            missionActive = missionActive
        };

        // Export objective progress
        if (currentMission != null && currentMission.objectives != null)
        {
            data.objectives = new ObjectiveSaveData[currentMission.objectives.Count];
            for (int i = 0; i < currentMission.objectives.Count; i++)
            {
                var obj = currentMission.objectives[i];
                data.objectives[i] = new ObjectiveSaveData
                {
                    isCompleted = obj.isCompleted,
                    currentAmount = obj.currentAmount,
                    currentTime = obj.currentTime
                };
            }
        }

        // Export scripted wave triggers
        if (currentMission != null && currentMission.scriptedWaves != null)
        {
            data.scriptedWaves = new ScriptedWaveSaveData[currentMission.scriptedWaves.Count];
            for (int i = 0; i < currentMission.scriptedWaves.Count; i++)
            {
                data.scriptedWaves[i] = new ScriptedWaveSaveData
                {
                    isTriggered = currentMission.scriptedWaves[i].isTriggered
                };
            }
        }

        return data;
    }

    public void ImportState(MissionSaveData data)
    {
        if (data == null) return;

        currentChapterIndex = data.currentChapterIndex;
        currentMissionIndex = data.currentMissionIndex;
        missionTimer = data.missionTimer;
        missionActive = data.missionActive;

        // Restore chapter and mission references
        if (currentChapterIndex >= 0 && currentChapterIndex < chapters.Count)
        {
            currentChapter = chapters[currentChapterIndex];

            if (currentMissionIndex >= 0 && currentMissionIndex < currentChapter.missions.Count)
            {
                currentMission = currentChapter.missions[currentMissionIndex];

                // Restore objective progress
                if (data.objectives != null && currentMission.objectives != null)
                {
                    for (int i = 0; i < Mathf.Min(data.objectives.Length, currentMission.objectives.Count); i++)
                    {
                        currentMission.objectives[i].isCompleted = data.objectives[i].isCompleted;
                        currentMission.objectives[i].currentAmount = data.objectives[i].currentAmount;
                        currentMission.objectives[i].currentTime = data.objectives[i].currentTime;
                    }
                }

                // Restore scripted wave triggers
                if (data.scriptedWaves != null && currentMission.scriptedWaves != null)
                {
                    for (int i = 0; i < Mathf.Min(data.scriptedWaves.Length, currentMission.scriptedWaves.Count); i++)
                    {
                        currentMission.scriptedWaves[i].isTriggered = data.scriptedWaves[i].isTriggered;
                    }
                }
            }
        }

        Debug.Log($"[MissionChapterManager] State imported: chapter={currentChapterIndex}, mission={currentMissionIndex}, timer={missionTimer:F1}");
    }
}
