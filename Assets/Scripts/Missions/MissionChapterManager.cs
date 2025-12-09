using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class MissionChapterManager : MonoBehaviour
{
    public static MissionChapterManager Instance { get; private set; }

    [Header("Chapter Configuration")]
    [SerializeField] private List<ChapterData> chapters = new List<ChapterData>();

    [Header("Current State")]
    [SerializeField] private MissionData currentMission;

    private int currentChapterIndex = 0;
    private int currentMissionIndex = 0;
    private ChapterData currentChapter;

    private float missionTimer = 0f;
    private bool missionActive = false;

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

        // Reset chapter state (resources, pollution)
        ResetChapterState();

        // Initialize starting resources and workers for the chapter
        InitializeChapterResources();
        InitializeChapterWorkers();

        // Load the chapter's scene
        if (!string.IsNullOrEmpty(chapter.sceneName))
        {
            SceneManager.LoadScene(chapter.sceneName);
        }

        OnChapterStarted?.Invoke(currentChapter);
        Debug.Log($"Chapter {currentChapterIndex + 1} Started: {currentChapter.chapterName}");
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
    /// Reset state that doesn't persist between chapters
    /// Resources and Pollution reset, but Research/Technology persists
    /// Enemy hostility does NOT reset (cumulative)
    /// </summary>
    private void ResetChapterState()
    {
        // Reset resources to 0
        if (ResourceManager.Instance != null)
        {
            ResetAllResources();
        }

        // Reset pollution to 0
        if (PollutionManager.Instance != null)
        {
            PollutionManager.Instance.ResetPollution();
        }

        // NOTE: Enemy hostility is NOT reset - it persists across chapters
        // This is handled by PollutionManager maintaining cumulative hostility

        // NOTE: Research/Technology state persists - nothing to reset
        // TechnologyManager (to be implemented) will maintain research progress

        Debug.Log("Chapter state reset: Resources and Pollution cleared, Research and Hostility persist");
    }

    /// <summary>
    /// Reset all resources to 0
    /// </summary>
    private void ResetAllResources()
    {
        if (ResourceManager.Instance == null) return;

        // Get all resource types and set them to 0
        var resources = ResourceManager.Instance.GetAllResources();
        foreach (var resourceType in resources.Keys)
        {
            ResourceManager.Instance.RemoveResource(resourceType, resources[resourceType]);
        }
    }

    /// <summary>
    /// Initialize starting resources for the chapter (called once per chapter)
    /// </summary>
    private void InitializeChapterResources()
    {
        if (currentChapter == null || ResourceManager.Instance == null) return;

        foreach (var resourceCost in currentChapter.startingResources)
        {
            if (resourceCost.resourceType != null)
            {
                ResourceManager.Instance.AddResource(resourceCost.resourceType, resourceCost.amount);
            }
        }

        Debug.Log($"Chapter resources initialized: {currentChapter.startingResources.Count} resource types");
    }

    /// <summary>
    /// Initialize starting workers for the chapter (called once per chapter)
    /// </summary>
    private void InitializeChapterWorkers()
    {
        if (currentChapter == null || WorkerManager.Instance == null) return;

        foreach (var workerConfig in currentChapter.startingWorkers)
        {
            if (workerConfig.workerData != null)
            {
                for (int i = 0; i < workerConfig.initialCount; i++)
                {
                    WorkerManager.Instance.TrainWorker(workerConfig.workerData);
                }
            }
        }

        Debug.Log($"Chapter workers initialized: {currentChapter.startingWorkers.Count} worker types");
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
        missionActive = true;

        // Reset all objectives
        foreach (var objective in currentMission.objectives)
        {
            objective.isCompleted = false;
            objective.currentAmount = 0;
            objective.currentTime = 0f;
        }

        OnMissionStarted?.Invoke(currentMission);
        Debug.Log($"Mission Started: {currentMission.missionName}");
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
                        // Check if pollution is within acceptable range
                        if (PollutionManager.Instance.CurrentPollution <= objective.targetAmount)
                        {
                            objective.currentTime += Time.deltaTime;
                            if (objective.currentTime >= objective.targetTime)
                            {
                                CompleteObjective(objective);
                            }
                        }
                        else
                        {
                            // Reset timer if pollution goes over limit
                            objective.currentTime = 0f;
                        }
                    }
                    break;
            }
        }
    }

    public void UpdateObjectiveProgress(ObjectiveType type, int amount, ResourceType resourceType = null, RaceType? raceType = null)
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

            objective.currentAmount += amount;

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

        // Advance to next mission in chapter
        currentMissionIndex++;

        // Check if all missions in chapter are complete
        if (currentMissionIndex >= currentChapter.missions.Count)
        {
            CompleteCurrentChapter();
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
}
