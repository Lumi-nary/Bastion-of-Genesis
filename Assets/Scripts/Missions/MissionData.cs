using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMission", menuName = "Planetfall/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Info")]
    public string missionName;
    [TextArea(3, 6)]
    public string missionDescription;
    public int missionNumber; // 1-10 within the chapter

    [Header("Starting Resources")]
    public List<ResourceCost> startingResources = new List<ResourceCost>();

    [Header("Starting Workers")]
    public List<WorkerStartConfig> startingWorkers = new List<WorkerStartConfig>();

    [Header("Mission Objectives")]
    public List<MissionObjective> objectives = new List<MissionObjective>();

    [Header("Mission Rewards")]
    public List<ResourceCost> completionRewards = new List<ResourceCost>();

    [Header("Mission Settings")]
    public float timeLimit = 0f; // 0 = no time limit
    public bool failOnTimeExpired = false;

    [Header("Enemy Configuration")]
    public List<RaceType> activeRaces = new List<RaceType>();
    public float enemySpawnDelay = 30f; // Delay before first enemy spawn
    public float enemySpawnInterval = 60f; // Time between enemy waves

    public bool AreAllObjectivesComplete()
    {
        foreach (var objective in objectives)
        {
            if (!objective.isOptional && !objective.isCompleted)
            {
                return false;
            }
        }
        return true;
    }

    public int GetCompletedObjectiveCount()
    {
        int count = 0;
        foreach (var objective in objectives)
        {
            if (objective.isCompleted) count++;
        }
        return count;
    }
}
