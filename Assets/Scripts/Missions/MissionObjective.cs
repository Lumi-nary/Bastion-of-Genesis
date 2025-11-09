using UnityEngine;

/// <summary>
/// Types of objectives that can be part of a mission
/// </summary>
public enum ObjectiveType
{
    SurviveTime,           // Survive for X seconds
    CollectResources,      // Collect X amount of a specific resource
    BuildStructures,       // Build X number of specific buildings
    DefeatEnemies,         // Defeat X enemies of a specific race
    MaintainPollution,     // Keep pollution below X for Y seconds
    ResearchTechnology,    // Research a specific technology
    AssignWorkers,         // Assign X workers to buildings
    ReachPollutionLevel    // Reach a specific pollution threshold (for testing)
}

[System.Serializable]
public class MissionObjective
{
    [Header("Objective Info")]
    public string objectiveDescription;
    public ObjectiveType type;
    public bool isOptional = false;

    [Header("Objective Parameters")]
    public int targetAmount;              // Generic amount needed (resources, enemies, etc.)
    public float targetTime;               // Time in seconds (for time-based objectives)
    public ResourceType requiredResource;  // For resource collection objectives
    public BuildingData requiredBuilding;  // For building construction objectives
    public RaceType targetRace;           // For enemy defeat objectives

    [Header("Completion Status")]
    [HideInInspector] public int currentAmount;
    [HideInInspector] public float currentTime;
    [HideInInspector] public bool isCompleted;

    public float GetProgress()
    {
        return type switch
        {
            ObjectiveType.SurviveTime => Mathf.Clamp01(currentTime / targetTime),
            ObjectiveType.MaintainPollution => Mathf.Clamp01(currentTime / targetTime),
            _ => Mathf.Clamp01((float)currentAmount / targetAmount)
        };
    }

    public string GetProgressText()
    {
        return type switch
        {
            ObjectiveType.SurviveTime => $"{currentTime:F0}s / {targetTime:F0}s",
            ObjectiveType.MaintainPollution => $"{currentTime:F0}s / {targetTime:F0}s",
            _ => $"{currentAmount} / {targetAmount}"
        };
    }
}
