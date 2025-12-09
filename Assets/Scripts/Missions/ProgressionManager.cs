using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages game progression save/load across chapters
/// Saves: Chapter unlocks, mission completion, research state, enemy hostility
/// Does NOT save: Resources, pollution level (these reset each chapter)
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    private const string SAVE_KEY = "PlanetfallProgression";

    [Header("Auto Save Settings")]
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveInterval = 60f; // Auto-save every 60 seconds
    private float autoSaveTimer = 0f;

    // Events
    public event Action OnProgressSaved;
    public event Action OnProgressLoaded;

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
        // Auto-load progress on start
        LoadProgress();

        // Subscribe to events for auto-save
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnChapterCompleted += OnChapterCompleted;
            MissionChapterManager.Instance.OnMissionCompleted += OnMissionCompleted;
        }
    }

    private void Update()
    {
        if (autoSaveEnabled)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                SaveProgress();
            }
        }
    }

    private void OnChapterCompleted(ChapterData chapter)
    {
        SaveProgress();
    }

    private void OnMissionCompleted(MissionData mission)
    {
        SaveProgress();
    }

    /// <summary>
    /// Save current progression state to PlayerPrefs
    /// </summary>
    public void SaveProgress()
    {
        ProgressionData data = new ProgressionData();

        // Save chapter progression
        if (MissionChapterManager.Instance != null)
        {
            data.currentChapterIndex = MissionChapterManager.Instance.CurrentChapterIndex;
            data.currentMissionIndex = MissionChapterManager.Instance.CurrentMissionIndex;

            // Save chapter unlock states
            data.unlockedChapters = new List<bool>();
            foreach (var chapter in MissionChapterManager.Instance.Chapters)
            {
                data.unlockedChapters.Add(chapter.isUnlocked);
            }

            // Save mission completion states for each chapter
            data.completedMissions = new List<List<bool>>();
            foreach (var chapter in MissionChapterManager.Instance.Chapters)
            {
                List<bool> chapterMissions = new List<bool>();
                foreach (var mission in chapter.missions)
                {
                    chapterMissions.Add(mission.AreMainObjectivesComplete());
                }
                data.completedMissions.Add(chapterMissions);
            }
        }

        // Note: Enemy hostility is no longer tracked - difficulty is based on current pollution tier
        // Pollution resets each chapter, so difficulty always starts at Tier 1

        // TODO: Save research/technology state when TechnologyManager is implemented
        // data.unlockedTechnologies = TechnologyManager.Instance.GetUnlockedTechnologies();

        // Serialize to JSON and save
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        OnProgressSaved?.Invoke();
        Debug.Log("Progress saved successfully!");
    }

    /// <summary>
    /// Load progression state from PlayerPrefs
    /// </summary>
    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log("No saved progress found. Starting new game.");
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        ProgressionData data = JsonUtility.FromJson<ProgressionData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to load progression data!");
            return;
        }

        // Restore chapter progression
        if (MissionChapterManager.Instance != null)
        {
            // Restore chapter unlock states
            if (data.unlockedChapters != null)
            {
                for (int i = 0; i < Mathf.Min(data.unlockedChapters.Count, MissionChapterManager.Instance.Chapters.Count); i++)
                {
                    MissionChapterManager.Instance.Chapters[i].isUnlocked = data.unlockedChapters[i];
                }
            }

            // Restore mission completion states
            if (data.completedMissions != null)
            {
                for (int i = 0; i < Mathf.Min(data.completedMissions.Count, MissionChapterManager.Instance.Chapters.Count); i++)
                {
                    var chapter = MissionChapterManager.Instance.Chapters[i];
                    for (int j = 0; j < Mathf.Min(data.completedMissions[i].Count, chapter.missions.Count); j++)
                    {
                        // Restore objective completion state
                        foreach (var objective in chapter.missions[j].objectives)
                        {
                            objective.isCompleted = data.completedMissions[i][j];
                        }
                    }
                }
            }
        }

        // Note: Enemy hostility is no longer tracked - difficulty is based on current pollution tier

        // TODO: Restore research/technology state when TechnologyManager is implemented
        // TechnologyManager.Instance.RestoreUnlockedTechnologies(data.unlockedTechnologies);

        OnProgressLoaded?.Invoke();
        Debug.Log("Progress loaded successfully!");
    }

    /// <summary>
    /// Delete all saved progress and start fresh
    /// </summary>
    public void DeleteProgress()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("Progress deleted. Starting new game.");
    }

    /// <summary>
    /// Check if there is saved progress available
    /// </summary>
    public bool HasSavedProgress()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnChapterCompleted -= OnChapterCompleted;
            MissionChapterManager.Instance.OnMissionCompleted -= OnMissionCompleted;
        }
    }
}

/// <summary>
/// Serializable data structure for saving progression
/// </summary>
[Serializable]
public class ProgressionData
{
    public int currentChapterIndex;
    public int currentMissionIndex;

    // Chapter unlocks
    public List<bool> unlockedChapters = new List<bool>();

    // Mission completion (per chapter, per mission)
    public List<List<bool>> completedMissions = new List<List<bool>>();

    // Legacy hostility fields (kept for save compatibility, no longer used)
    [Obsolete("Hostility system replaced by pollution tier system")]
    public bool humansHostile;
    [Obsolete("Hostility system replaced by pollution tier system")]
    public bool elvesHostile;
    [Obsolete("Hostility system replaced by pollution tier system")]
    public bool dwarvesHostile;
    [Obsolete("Hostility system replaced by pollution tier system")]
    public bool demonsHostile;

    // TODO: Add research/technology data when TechnologyManager is implemented
    // public List<string> unlockedTechnologies = new List<string>();
}
