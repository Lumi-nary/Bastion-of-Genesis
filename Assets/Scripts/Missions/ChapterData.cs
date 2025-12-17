using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewChapter", menuName = "Planetfall/Chapter Data")]
public class ChapterData : ScriptableObject
{
    [Header("Chapter Info")]
    public int chapterNumber; // 1-5
    public string chapterName;
    [TextArea(3, 6)]
    public string chapterDescription;

    [Header("Story/Narrative")]
    [TextArea(5, 10)]
    public string chapterIntroText;
    [TextArea(5, 10)]
    public string chapterOutroText;

    [Header("Dialogue")]
    [Tooltip("Dialogue to play when chapter starts (before first mission)")]
    public DialogueData introDialogue;

    [Header("Starting Resources")]
    [Tooltip("Resources given at the start of this chapter")]
    public List<ResourceCost> startingResources = new List<ResourceCost>();

    [Header("Starting Workers")]
    [Tooltip("Workers given at the start of this chapter")]
    public List<WorkerStartConfig> startingWorkers = new List<WorkerStartConfig>();

    [Header("Starting Integration Zone")]
    [Tooltip("Initial integration radius (buildable zone) for this chapter")]
    public float startingIntegrationRadius = 10f;

    [Header("Pollution Settings")]
    [Tooltip("Maximum pollution level for this chapter")]
    public float maxPollution = 1000f;

    [Tooltip("Rate at which pollution naturally decays per second")]
    public float pollutionDecayRate = 0.5f;

    [Header("Chapter Missions")]
    public List<MissionData> missions = new List<MissionData>(); // 10 missions

    [Header("Enemies")]
    [Tooltip("Which enemy races are active in this chapter")]
    public List<RaceType> activeRaces = new List<RaceType>();

    [Tooltip("All enemies that can spawn in this chapter (with pollution weights)")]
    public List<EnemyData> chapterEnemies = new List<EnemyData>();

    [Header("Map/Scene")]
    public string sceneName; // Name of the scene to load for this chapter
    public Sprite chapterThumbnail; // Preview image for chapter selection

    [Header("Audio")]
    [Tooltip("Background music for this chapter (normal gameplay)")]
    public AudioClip backgroundMusic;
    [Tooltip("Battle music when enemies are attacking")]
    public AudioClip battleMusic;

    [Header("Unlock Requirements")]
    public bool isUnlocked = false; // Will be set by progression system
    public ChapterData previousChapter; // Chapter that must be completed to unlock this one

    public bool IsChapterComplete()
    {
        foreach (var mission in missions)
        {
            if (!mission.AreMainObjectivesComplete())
            {
                return false;
            }
        }
        return true;
    }

    public int GetCompletedMissionCount()
    {
        int count = 0;
        foreach (var mission in missions)
        {
            if (mission.AreMainObjectivesComplete())
            {
                count++;
            }
        }
        return count;
    }
}
