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
    private Coroutine missionIntroCoroutine;
    private Coroutine objectiveDialogueCoroutine;
    private DialogueData activeObjectiveDialogue;
    private bool objectiveDialoguePending;
    private readonly HashSet<MissionObjective> playedObjectiveDialogues = new HashSet<MissionObjective>();

    // Scene transition tracking
    private bool awaitingSceneValidation = false;
    private bool isLoadingFromSave = false;
    private bool chapterStateInitialized = false;

    #region Events
    // Mission Events
    public event Action<MissionData> OnMissionStarted;
    public event Action<MissionData> OnMissionCompleted;
    public event Action<MissionData> OnMissionFailed;
    public event Action<MissionObjective> OnObjectiveCompleted;
    public event Action<MissionObjective> OnObjectiveUpdated;
    public event Action OnObjectiveDialogueStateChanged;
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
    public bool IsObjectiveDialogueBlockingTutorial => objectiveDialoguePending || activeObjectiveDialogue != null;

    // Chapter Properties
    public ChapterData CurrentChapter => currentChapter;
    public int CurrentChapterIndex => currentChapterIndex;
    public int CurrentMissionIndex => currentMissionIndex;
    public List<ChapterData> Chapters => chapters;
    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapDirectChapterScene()
    {
        if (Instance != null)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        ChapterData matchingChapter = FindChapterForScene(activeScene.name);
        if (matchingChapter == null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(MissionChapterManager));
        MissionChapterManager manager = managerObject.AddComponent<MissionChapterManager>();
        manager.LoadChaptersFromResourcesIfNeeded();
        manager.currentChapterIndex = Mathf.Max(0, manager.chapters.IndexOf(matchingChapter));
        manager.currentMissionIndex = 0;
        manager.currentChapter = matchingChapter;
        manager.currentChapter.isUnlocked = true;

        Debug.Log($"[MissionChapterManager] Bootstrapped direct chapter scene: {activeScene.name}");
        manager.StartCoroutine(manager.ValidateSceneAfterDelay());
    }

    private static ChapterData FindChapterForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return null;
        }

        ChapterData[] loadedChapters = Resources.LoadAll<ChapterData>("Data/Campaign/Chapters");
        foreach (ChapterData chapter in loadedChapters)
        {
            if (chapter != null && chapter.sceneName == sceneName)
            {
                return chapter;
            }
        }

        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadChaptersFromResourcesIfNeeded();

        // Subscribe to scene events for validation
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void LoadChaptersFromResourcesIfNeeded()
    {
        if (chapters.Count > 0)
        {
            return;
        }

        ChapterData[] loadedChapters = Resources.LoadAll<ChapterData>("Data/Campaign/Chapters");
        Array.Sort(loadedChapters, (a, b) => a.chapterNumber.CompareTo(b.chapterNumber));
        chapters.AddRange(loadedChapters);
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
        if (!awaitingSceneValidation)
        {
            if (ShouldInitializeDirectlyLoadedChapterScene(scene))
            {
                Debug.Log($"[MissionChapterManager] Direct chapter scene detected: {scene.name}, validating managers...");
                StartCoroutine(ValidateSceneAfterDelay());
            }

            return;
        }

        awaitingSceneValidation = false;
        Debug.Log($"[MissionChapterManager] Scene loaded: {scene.name}, validating managers...");

        // Delay validation to allow managers to initialize
        StartCoroutine(ValidateSceneAfterDelay());
    }

    private bool ShouldInitializeDirectlyLoadedChapterScene(Scene scene)
    {
        if (currentChapter == null || string.IsNullOrEmpty(currentChapter.sceneName))
            return false;

        if (scene.name != currentChapter.sceneName)
            return false;

        return currentMission == null && !missionActive;
    }

    private IEnumerator ValidateSceneAfterDelay()
    {
        // Wait for managers to initialize
        yield return null;
        yield return null; // Extra frame for safety

        if (ValidateChapterScene())
        {
            TutorialGuideManager.EnsureRuntimeObjects();

            // Safeguard: Do not initialize if we still don't have a chapter
            if (currentChapter == null)
            {
                Debug.LogError("[MissionChapterManager] Cannot initialize chapter state: currentChapter is NULL!");
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

        if (chapterStateInitialized)
        {
            Debug.Log("[MissionChapterManager] Chapter state already initialized, skipping duplicate initialization");
            return;
        }
        chapterStateInitialized = true;

        Debug.Log($"[MissionChapterManager] Initializing chapter state for: {currentChapter.chapterName}");

        // Loading from save: RestoreStateFromSave handles resources/workers/pollution/buildings
        if (isLoadingFromSave)
        {
            isLoadingFromSave = false;
            Debug.Log("[MissionChapterManager] Loading from save — skipping normal init, restoring state");

            // Configure pollution limits from chapter (needed before importing pollution values)
            if (PollutionManager.Instance != null)
                PollutionManager.Instance.ConfigureFromChapter(currentChapter.maxPollution, currentChapter.pollutionDecayRate, currentChapter.startingWitherRadius);

            // Scene setup that's always needed
            if (TileStateManager.Instance != null)
                TileStateManager.Instance.ConfigureFromChapter(currentChapter.startingWitherRadius, currentChapter.startingIntegrationRadius);
            if (BuildingManager.Instance != null)
                BuildingManager.Instance.OnBuildingPlaced += OnBuildingPlaced;
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.ReloadEnemyTypesFromChapter();

            // Music
            if (AudioManager.Instance != null)
            {
                SetChapterMusic();
                if (currentChapter.battleMusic != null)
                    AudioManager.Instance.SetBattleMusic(currentChapter.battleMusic);
            }

            // Restore all manager state from save
            if (SaveManager.Instance != null)
                SaveManager.Instance.RestoreStateFromSave();

            return;
        }

        // Reset and initialize resources
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetAllResources();
            InitializeChapterResources();
        }

        // Reset and initialize workers
        if (WorkerManager.Instance != null)
        {
            WorkerManager.Instance.ResetAllWorkers();
            InitializeChapterWorkers();
        }

        // Reset pollution and configure from chapter settings
        if (PollutionManager.Instance != null)
        {
            PollutionManager.Instance.ResetPollution();
            PollutionManager.Instance.ConfigureFromChapter(currentChapter.maxPollution, currentChapter.pollutionDecayRate, currentChapter.startingWitherRadius);
        }

        // Set authored starting wither and integration radii
        if (TileStateManager.Instance != null)
        {
            TileStateManager.Instance.ConfigureFromChapter(currentChapter.startingWitherRadius, currentChapter.startingIntegrationRadius);
            Debug.Log($"[MissionChapterManager] Tile states set from chapter: wither={currentChapter.startingWitherRadius}, integration={currentChapter.startingIntegrationRadius}");
        }

        // Subscribe to building events for objectives
        if (BuildingManager.Instance != null)
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
            SetChapterMusic();
            if (currentChapter.battleMusic != null)
            {
                AudioManager.Instance.SetBattleMusic(currentChapter.battleMusic);
            }
        }

        Debug.Log("[MissionChapterManager] Chapter state initialization complete");

        // Start tracking and begin missions
        if (SaveManager.Instance != null)
            SaveManager.Instance.StartGameplayTracking();

        // Play chapter intro dialogue (if set), then start first mission
        StartCoroutine(PlayChapterIntroAndStartMission());
    }

    private void SetChapterMusic()
    {
        if (AudioManager.Instance == null || currentChapter == null)
        {
            return;
        }

        if (currentChapter.backgroundMusicTracks != null && currentChapter.backgroundMusicTracks.Count > 0)
        {
            AudioManager.Instance.SetNormalMusic(currentChapter.backgroundMusicTracks);
            Debug.Log($"[MissionChapterManager] Playing chapter music playlist ({currentChapter.backgroundMusicTracks.Count} tracks)");
            return;
        }

        if (currentChapter.backgroundMusic != null)
        {
            AudioManager.Instance.SetNormalMusic(currentChapter.backgroundMusic);
            Debug.Log($"[MissionChapterManager] Playing chapter music: {currentChapter.backgroundMusic.name}");
        }
    }

    /// <summary>
    /// Play chapter intro dialogue (if any), then start the first mission
    /// </summary>
    private IEnumerator PlayChapterIntroAndStartMission()
    {
        // Play chapter intro dialogue if set
        DialogueData introDialogue = currentChapter != null ? currentChapter.introDialogue : null;
        DialogueManager dialogueManager = DialogueManager.Instance;
        bool missionStarted = false;
        void StartFirstMissionAfterIntro()
        {
            if (missionStarted)
            {
                return;
            }

            missionStarted = true;
            Debug.Log("[MissionChapterManager] Chapter intro dialogue complete; starting first mission");
            StartNextMission();
        }

        if (introDialogue != null && dialogueManager != null)
        {
            Action<DialogueData> onDialogueEnded = null;
            onDialogueEnded = endedDialogue =>
            {
                if (endedDialogue == introDialogue)
                {
                    dialogueManager.OnDialogueEnded -= onDialogueEnded;
                    StartFirstMissionAfterIntro();
                }
            };

            dialogueManager.OnDialogueEnded += onDialogueEnded;
            Debug.Log($"[MissionChapterManager] Playing chapter intro dialogue: {introDialogue.dialogueName}");
            dialogueManager.StartDialogue(introDialogue);

            while (!missionStarted && dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                yield return null;
            }

            if (!missionStarted)
            {
                if (dialogueManager != null)
                {
                    dialogueManager.OnDialogueEnded -= onDialogueEnded;
                }

                StartFirstMissionAfterIntro();
            }
            yield break;
        }

        StartFirstMissionAfterIntro();
    }

    private void Start()
    {
        // Initialize the first chapter
        if (chapters.Count > 0)
        {
            currentChapter = chapters[0];
            currentChapter.isUnlocked = true; // First chapter is always unlocked

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(currentChapter.sceneName) && activeScene.name == currentChapter.sceneName)
            {
                StartCoroutine(ValidateSceneAfterDelay());
            }
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
        chapterStateInitialized = false;

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
            LoadingScreenManager.EnsureInstance().LoadSceneAsync(chapter.sceneName);
        }

        OnChapterStarted?.Invoke(currentChapter);
        Debug.Log($"Chapter {currentChapterIndex + 1} Started: {currentChapter.chapterName}");
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
            UpdateObjectiveProgress(
                ObjectiveType.BuildStructures,
                1,
                buildingData: building.BuildingData,
                placementCell: building.gridPosition);
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

        foreach (var resourceCost in currentChapter.startingResources)
        {
            if (resourceCost.resourceType != null)
            {
                ResourceManager.Instance.RegisterResourceType(resourceCost.resourceType, resourceCost.amount);
            }
        }

        Debug.Log($"[MissionChapterManager] Chapter resources initialized: {currentChapter.startingResources.Count} resource types");
    }

    /// <summary>
    /// Initialize starting workers for the chapter (called once per chapter).
    /// Registers worker types with base capacity and sets starting counts from ChapterData.
    /// </summary>
    private void InitializeChapterWorkers()
    {
        if (currentChapter == null || WorkerManager.Instance == null) return;

        foreach (var workerConfig in currentChapter.startingWorkers)
        {
            if (workerConfig.workerData != null)
            {
                WorkerManager.Instance.RegisterWorkerType(workerConfig.workerData, workerConfig.initialCount);
            }
        }

        Debug.Log($"[MissionChapterManager] Chapter workers initialized: {currentChapter.startingWorkers.Count} worker types");
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
        missionActive = false;

        // Reset all objectives
        foreach (var objective in currentMission.objectives)
        {
            objective.isCompleted = false;
            objective.currentAmount = 0;
            objective.currentTime = 0f;
        }
        playedObjectiveDialogues.Clear();

        // Reset scripted waves
        foreach (var wave in currentMission.scriptedWaves)
        {
            wave.isTriggered = false;
        }

        if (PollutionManager.Instance != null)
        {
            PollutionManager.Instance.SetMissionPollutionLimitPercent(currentMission.pollutionLimitPercent);
            Debug.Log($"[MissionChapterManager] Pollution limit set to {currentMission.pollutionLimitPercent:F0}% for {currentMission.missionName}");
        }

        // Start coroutine to handle dialogue then activate mission
        if (missionIntroCoroutine != null)
        {
            StopCoroutine(missionIntroCoroutine);
        }
        missionIntroCoroutine = StartCoroutine(PlayMissionIntroAndActivate());
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

        TryPlayCurrentObjectiveDialogue();
        OnMissionStarted?.Invoke(currentMission);
        TutorialGuideManager.EnsureRuntimeObjects();
        TutorialGuideManager.Instance?.RefreshActiveObjective();
        TutorialHologramManager.Instance?.Refresh();
        Debug.Log($"Mission Started: {currentMission.missionName}");

        // Play mission started voice clip
        if (missionStartedVoice != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(missionStartedVoice);
        }

        missionIntroCoroutine = null;
    }

    private void UpdateTimeBasedObjectives()
    {
        foreach (var objective in currentMission.objectives)
        {
            if (objective.isCompleted) continue;

            bool updated = false;

            switch (objective.type)
            {
                case ObjectiveType.SurviveTime:
                    objective.currentTime = missionTimer;
                    updated = true;
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
                            updated = true;
                            if (objective.currentTime >= objective.targetTime)
                            {
                                CompleteObjective(objective);
                            }
                        }
                        else if (objective.currentTime > 0)
                        {
                            // Reset timer if pollution drops below threshold
                            objective.currentTime = 0f;
                            updated = true;
                        }
                    }
                    break;

                case ObjectiveType.ReachPollutionLevel:
                    if (PollutionManager.Instance != null)
                    {
                        float currentPollutionPercent = PollutionManager.Instance.PollutionNormalized * 100f;
                        int newAmount = Mathf.RoundToInt(currentPollutionPercent);
                        if (objective.currentAmount != newAmount)
                        {
                            objective.currentAmount = newAmount;
                            updated = true;
                        }
                        
                        if (currentPollutionPercent >= objective.targetAmount)
                        {
                            CompleteObjective(objective);
                        }
                    }
                    break;
            }

            if (updated && !objective.isCompleted)
            {
                OnObjectiveUpdated?.Invoke(objective);
            }
        }
    }

    public void UpdateObjectiveProgress(
        ObjectiveType type,
        int amount,
        ResourceType resourceType = null,
        RaceType? raceType = null,
        BuildingData buildingData = null,
        WorkerData workerData = null,
        Vector2Int? placementCell = null,
        Building assignmentBuilding = null,
        TechnologyData technologyData = null)
    {
        if (!missionActive || currentMission == null) return;

        foreach (var objective in currentMission.objectives)
        {
            if (objective.isCompleted) continue;
            if (!objective.MatchesProgress(type, resourceType, raceType, buildingData, workerData, placementCell, assignmentBuilding, technologyData))
                continue;

            objective.currentAmount += amount;
            OnObjectiveUpdated?.Invoke(objective);

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

        TryPlayCurrentObjectiveDialogue();
    }

    private void TryPlayCurrentObjectiveDialogue()
    {
        if (!missionActive || currentMission == null || currentMission.objectives == null)
            return;

        if (!TutorialGuideManager.IsTutorialEnabled)
            return;

        MissionObjective activeObjective = GetFirstUnfinishedRequiredObjective();
        if (activeObjective == null ||
            activeObjective.objectiveDialogue == null ||
            playedObjectiveDialogues.Contains(activeObjective))
        {
            return;
        }

        playedObjectiveDialogues.Add(activeObjective);

        if (objectiveDialogueCoroutine != null)
            StopCoroutine(objectiveDialogueCoroutine);

        SetObjectiveDialogueBlock(activeObjective.objectiveDialogue, true);
        objectiveDialogueCoroutine = StartCoroutine(PlayObjectiveDialogueWhenReady(activeObjective.objectiveDialogue));
    }

    private MissionObjective GetFirstUnfinishedRequiredObjective()
    {
        foreach (MissionObjective objective in currentMission.objectives)
        {
            if (objective == null || objective.isOptional || objective.isCompleted)
                continue;

            return objective;
        }

        return null;
    }

    private IEnumerator PlayObjectiveDialogueWhenReady(DialogueData dialogue)
    {
        if (dialogue == null || DialogueManager.Instance == null)
        {
            ClearObjectiveDialogueBlock();
            yield break;
        }

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            yield return null;

        if (DialogueManager.Instance != null && dialogue.EntryCount > 0)
        {
            objectiveDialoguePending = false;
            activeObjectiveDialogue = dialogue;
            DialogueManager.Instance.StartDialogue(dialogue);

            while (DialogueManager.Instance != null &&
                DialogueManager.Instance.IsDialogueActive &&
                DialogueManager.Instance.CurrentDialogue == dialogue)
            {
                yield return null;
            }
        }

        ClearObjectiveDialogueBlock();
        objectiveDialogueCoroutine = null;
    }

    private void SetObjectiveDialogueBlock(DialogueData dialogue, bool pending)
    {
        bool changed = activeObjectiveDialogue != dialogue || objectiveDialoguePending != pending;

        activeObjectiveDialogue = dialogue;
        objectiveDialoguePending = pending;

        if (changed)
            OnObjectiveDialogueStateChanged?.Invoke();
    }

    private void ClearObjectiveDialogueBlock()
    {
        bool changed = activeObjectiveDialogue != null || objectiveDialoguePending;

        activeObjectiveDialogue = null;
        objectiveDialoguePending = false;
        objectiveDialogueCoroutine = null;

        if (changed)
            OnObjectiveDialogueStateChanged?.Invoke();
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

    public void ForceCompleteCurrentMission(bool includeOptional = false)
    {
        if (currentMission == null)
        {
            Debug.LogWarning("[MissionChapterManager] Cannot force-complete mission: no current mission.");
            return;
        }

        if (missionIntroCoroutine != null)
        {
            StopCoroutine(missionIntroCoroutine);
            missionIntroCoroutine = null;
        }
        if (objectiveDialogueCoroutine != null)
        {
            StopCoroutine(objectiveDialogueCoroutine);
            ClearObjectiveDialogueBlock();
        }

        foreach (var objective in currentMission.objectives)
        {
            if (objective == null || objective.isCompleted)
                continue;

            if (objective.isOptional && !includeOptional)
                continue;

            objective.currentAmount = objective.targetAmount;
            objective.currentTime = objective.targetTime;
            CompleteObjective(objective);
        }

        missionActive = true;
        CompleteMission();
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
        if (PollutionManager.Instance != null)
        {
            PollutionManager.Instance.SetMissionPollutionLimitPercent(100f);
        }
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

                if (PollutionManager.Instance != null)
                {
                    PollutionManager.Instance.SetMissionPollutionLimitPercent(currentMission.pollutionLimitPercent);
                }
            }
        }

        Debug.Log($"[MissionChapterManager] State imported: chapter={currentChapterIndex}, mission={currentMissionIndex}, timer={missionTimer:F1}");
    }

    /// <summary>
    /// Force-fire mission/chapter events so UI refreshes after network state sync.
    /// </summary>
    public void NotifyUIRefresh()
    {
        if (currentChapter != null)
            OnChapterStarted?.Invoke(currentChapter);
        if (currentMission != null)
            OnMissionStarted?.Invoke(currentMission);
    }
}
