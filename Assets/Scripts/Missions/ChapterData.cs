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

    [Header("Chapter Missions")]
    public List<MissionData> missions = new List<MissionData>(); // 10 missions

    [Header("Enemy Races in Chapter")]
    [Tooltip("Which enemy races are active in this chapter")]
    public List<RaceType> activeRaces = new List<RaceType>();

    [Header("Map/Scene")]
    public string sceneName; // Name of the scene to load for this chapter
    public Sprite chapterThumbnail; // Preview image for chapter selection

    [Header("Unlock Requirements")]
    public bool isUnlocked = false; // Will be set by progression system
    public ChapterData previousChapter; // Chapter that must be completed to unlock this one

    public bool IsChapterComplete()
    {
        foreach (var mission in missions)
        {
            if (!mission.AreAllObjectivesComplete())
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
            if (mission.AreAllObjectivesComplete())
            {
                count++;
            }
        }
        return count;
    }
}
