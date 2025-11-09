using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class ChapterManager : MonoBehaviour
{
    public static ChapterManager Instance { get; private set; }

    [Header("Chapter Configuration")]
    [SerializeField] private List<ChapterData> chapters = new List<ChapterData>();
    
    private int currentChapterIndex = 0;
    private int currentMissionIndex = 0;
    private ChapterData currentChapter;

    // Events
    public event Action<ChapterData> OnChapterStarted;
    public event Action<ChapterData> OnChapterCompleted;
    public event Action<int> OnChapterChanged; // Passes chapter index
    public event Action<ChapterData> OnChapterUnlocked;

    public ChapterData CurrentChapter => currentChapter;
    public int CurrentChapterIndex => currentChapterIndex;
    public int CurrentMissionIndex => currentMissionIndex;
    public List<ChapterData> Chapters => chapters;

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

        // Reset chapter state (resources, pollution)
        ResetChapterState();

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
        MissionManager.Instance.StartMission(mission);
        
        Debug.Log($"Starting Mission {currentMissionIndex + 1}/{currentChapter.missions.Count}: {mission.missionName}");
    }

    /// <summary>
    /// Called when a mission is completed
    /// </summary>
    public void OnMissionCompleted()
    {
        currentMissionIndex++;

        // Check if all missions in chapter are complete
        if (currentMissionIndex >= currentChapter.missions.Count)
        {
            CompleteCurrentChapter();
        }
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
}
