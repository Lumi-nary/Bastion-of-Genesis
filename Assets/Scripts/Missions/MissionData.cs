using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mission type classification
/// </summary>
public enum MissionType
{
    CompleteObjectives,   // Complete the listed objectives
    SurviveWaves,         // Survive X waves of enemies
    ReachPollution,       // Reach pollution threshold
    KillCount,            // Kill X enemies
    ResearchUnderFire,    // Research tech while under attack
    ResourceGoal,         // Accumulate X resources
    MaintainPollution     // Maintain pollution above threshold for duration
}

/// <summary>
/// Mission reward types
/// </summary>
public enum RewardType
{
    Resources,          // Resource rewards (instant)
    TechUnlock,        // Unlock tech for research (still costs resources/time)
    BuildingUnlock     // Unlock building (instant, no research needed)
}

/// <summary>
/// Mission reward definition
/// </summary>
[System.Serializable]
public class MissionReward
{
    [Header("Reward Info")]
    public RewardType rewardType;

    [Header("Resource Rewards")]
    [Tooltip("Resources awarded (for RewardType.Resources)")]
    public List<ResourceCost> resourceRewards = new List<ResourceCost>();

    [Header("Tech Unlocks")]
    [Tooltip("Technologies unlocked for research (for RewardType.TechUnlock)")]
    public List<TechnologyData> techUnlocks = new List<TechnologyData>();

    [Header("Building Unlocks")]
    [Tooltip("Buildings instantly unlocked (for RewardType.BuildingUnlock)")]
    public List<BuildingData> buildingUnlocks = new List<BuildingData>();

    [Header("Optional Reward")]
    [Tooltip("Is this reward for optional objective completion?")]
    public bool isOptionalReward = false;
}

[CreateAssetMenu(fileName = "NewMission", menuName = "Planetfall/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Info")]
    public string missionName;
    [TextArea(3, 6)]
    public string missionDescription;
    public int missionNumber; // 1-10 within the chapter
    public MissionType missionType;

    [Header("Dialogue")]
    [Tooltip("Dialogue to play when mission starts")]
    public DialogueData introDialogue;

    [Header("Mission Objectives")]
    [Tooltip("Main objectives (required) and optional objectives")]
    public List<MissionObjective> objectives = new List<MissionObjective>();

    [Header("Mission Rewards")]
    [Tooltip("Rewards for completing main objectives")]
    public List<MissionReward> mainRewards = new List<MissionReward>();

    [Tooltip("Rewards for completing optional objectives")]
    public List<MissionReward> optionalRewards = new List<MissionReward>();

    [Header("Mission Settings")]
    [Tooltip("Time limit in seconds. 0 = no time limit")]
    public float timeLimit = 0f;
    public bool failOnTimeExpired = false;

    [Tooltip("If true, standard pollution-based waves will be paused.")]
    public bool disableNaturalWaves = false;

    [Tooltip("Scripted waves that trigger at specific times.")]
    public List<ScriptedWave> scriptedWaves = new List<ScriptedWave>();

    /// <summary>
    /// Check if all main objectives are complete
    /// </summary>
    public bool AreMainObjectivesComplete()
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

    /// <summary>
    /// Check if all optional objectives are complete
    /// </summary>
    public bool AreOptionalObjectivesComplete()
    {
        foreach (var objective in objectives)
        {
            if (objective.isOptional && !objective.isCompleted)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Get count of completed main objectives
    /// </summary>
    public int GetCompletedMainObjectiveCount()
    {
        int count = 0;
        foreach (var objective in objectives)
        {
            if (!objective.isOptional && objective.isCompleted) count++;
        }
        return count;
    }

    /// <summary>
    /// Get count of completed optional objectives
    /// </summary>
    public int GetCompletedOptionalObjectiveCount()
    {
        int count = 0;
        foreach (var objective in objectives)
        {
            if (objective.isOptional && objective.isCompleted) count++;
        }
        return count;
    }

    /// <summary>
    /// Get total objective count
    /// </summary>
    public int GetTotalObjectiveCount()
    {
        return objectives.Count;
    }

    /// <summary>
    /// Get main objective count
    /// </summary>
    public int GetMainObjectiveCount()
    {
        int count = 0;
        foreach (var objective in objectives)
        {
            if (!objective.isOptional) count++;
        }
        return count;
    }

    /// <summary>
    /// Get optional objective count
    /// </summary>
    public int GetOptionalObjectiveCount()
    {
        int count = 0;
        foreach (var objective in objectives)
        {
            if (objective.isOptional) count++;
        }
        return count;
    }

    /// <summary>
    /// Apply mission rewards (called when mission is completed)
    /// </summary>
    public void ApplyRewards(bool includeOptional = false)
    {
        // Apply main rewards
        foreach (MissionReward reward in mainRewards)
        {
            ApplyReward(reward);
        }

        // Apply optional rewards if applicable
        if (includeOptional && AreOptionalObjectivesComplete())
        {
            foreach (MissionReward reward in optionalRewards)
            {
                ApplyReward(reward);
            }
        }
    }

    /// <summary>
    /// Apply a single reward
    /// </summary>
    private void ApplyReward(MissionReward reward)
    {
        switch (reward.rewardType)
        {
            case RewardType.Resources:
                ApplyResourceRewards(reward.resourceRewards);
                break;

            case RewardType.TechUnlock:
                ApplyTechUnlocks(reward.techUnlocks);
                break;

            case RewardType.BuildingUnlock:
                ApplyBuildingUnlocks(reward.buildingUnlocks);
                break;
        }
    }

    /// <summary>
    /// Apply resource rewards
    /// </summary>
    private void ApplyResourceRewards(List<ResourceCost> resources)
    {
        if (ResourceManager.Instance == null) return;

        foreach (ResourceCost resource in resources)
        {
            if (resource.resourceType != null)
            {
                ResourceManager.Instance.AddResource(resource.resourceType, resource.amount);
                Debug.Log($"[Mission] Reward: {resource.amount} {resource.resourceType.ResourceName}");
            }
        }
    }

    /// <summary>
    /// Apply tech unlocks (make available for research)
    /// </summary>
    private void ApplyTechUnlocks(List<TechnologyData> techs)
    {
        if (ResearchManager.Instance == null) return;

        foreach (TechnologyData tech in techs)
        {
            if (tech != null)
            {
                ResearchManager.Instance.UnlockTechnologyForResearch(tech);
                Debug.Log($"[Mission] Tech unlocked for research: {tech.techName}");
            }
        }
    }

    /// <summary>
    /// Apply building unlocks (instantly available)
    /// </summary>
    public void ApplyBuildingUnlocks(List<BuildingData> buildings)
    {
        foreach (BuildingData building in buildings)
        {
            if (building != null)
            {
                if (BuildingManager.Instance != null)
                {
                    BuildingManager.Instance.UnlockBuilding(building);
                }
                Debug.Log($"[Mission] Building unlocked: {building.buildingName}");
            }
        }
    }
}

/// <summary>
/// Edge of the map for directional spawning
/// </summary>
public enum SpawnEdge { North, South, East, West }

/// <summary>
/// Specific enemy type + count for scripted waves
/// </summary>
[System.Serializable]
public class ScriptedSpawnEntry
{
    public EnemyData enemyData;
    public int count = 1;
}

/// <summary>
/// Definition for a scripted wave event in a mission
/// </summary>
[System.Serializable]
public class ScriptedWave
{
    [Tooltip("Time in seconds from mission start to trigger this wave")]
    public float triggerTime;

    [Tooltip("Number of enemies to spawn (0 = use standard wave calculation). Ignored if spawnList is set.")]
    public int enemyCount = 0;

    [Tooltip("Optional message to display when wave triggers")]
    public string waveMessage;

    [Tooltip("Only trigger after this objective index is completed (-1 = no requirement, time-based only).")]
    public int triggerAfterObjectiveIndex = -1;

    [Tooltip("Which map edges to spawn from. Empty = random (pollution-based).")]
    public List<SpawnEdge> spawnEdges = new List<SpawnEdge>();

    [Tooltip("Specific enemies to spawn. Empty = use weighted random selection with enemyCount.")]
    public List<ScriptedSpawnEntry> spawnList = new List<ScriptedSpawnEntry>();

    // Runtime state
    [System.NonSerialized]
    public bool isTriggered = false;

    /// <summary>
    /// Get total enemy count from spawnList entries, or fallback to enemyCount field
    /// </summary>
    public int GetTotalEnemyCount()
    {
        if (spawnList != null && spawnList.Count > 0)
        {
            int total = 0;
            foreach (var entry in spawnList)
            {
                total += entry.count;
            }
            return total;
        }
        return enemyCount;
    }
}
