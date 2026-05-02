using System.Collections.Generic;
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
    ReachPollutionLevel,   // Reach a specific pollution threshold (for testing)
    DefeatBoss             // Defeat a boss enemy
}

public enum TutorialGateMode
{
    HardGate
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
    public WorkerData requiredWorker;     // For worker assembly/assignment objectives
    public RaceType targetRace;           // For enemy defeat objectives

    [Header("Tutorial Gate")]
    public bool isTutorialStep = false;
    public TutorialGateMode gateMode = TutorialGateMode.HardGate;
    public List<Vector2Int> allowedPlacementCells = new List<Vector2Int>();
    public TechnologyData requiredTechnology;
    public List<TechnologyData> requiredTechnologies = new List<TechnologyData>();
    public BuildingData requiredAssignmentBuilding;
    [TextArea(2, 4)] public string tutorialInstruction;
    public bool focusCameraOnTarget = true;

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

    public bool MatchesProgress(
        ObjectiveType progressType,
        ResourceType resourceType = null,
        RaceType? raceType = null,
        BuildingData buildingData = null,
        WorkerData workerData = null,
        Vector2Int? placementCell = null,
        Building assignmentBuilding = null,
        TechnologyData technologyData = null)
    {
        if (type != progressType)
            return false;

        if (progressType == ObjectiveType.CollectResources && requiredResource != resourceType)
            return false;

        if (progressType == ObjectiveType.DefeatEnemies && raceType.HasValue && targetRace != raceType.Value)
            return false;

        if (progressType == ObjectiveType.BuildStructures)
        {
            if (requiredBuilding != null && buildingData != null && requiredBuilding != buildingData)
                return false;

            if (isTutorialStep)
            {
                if (requiredBuilding != null && requiredBuilding != buildingData)
                    return false;

                if (allowedPlacementCells != null && allowedPlacementCells.Count > 0)
                {
                    if (!placementCell.HasValue || !allowedPlacementCells.Contains(placementCell.Value))
                        return false;
                }
            }
        }

        if (progressType == ObjectiveType.AssignWorkers)
        {
            if (requiredWorker != null && requiredWorker != workerData)
                return false;

            if (isTutorialStep)
            {
                if (requiredWorker != null && requiredWorker != workerData)
                    return false;

                if (requiredAssignmentBuilding != null)
                {
                    if (assignmentBuilding == null || assignmentBuilding.BuildingData != requiredAssignmentBuilding)
                        return false;
                }
            }
        }

        if (progressType == ObjectiveType.ResearchTechnology && isTutorialStep)
        {
            if (requiredTechnologies != null && requiredTechnologies.Count > 0)
                return technologyData != null && requiredTechnologies.Contains(technologyData);

            if (requiredTechnology != null && requiredTechnology != technologyData)
                return false;
        }

        return true;
    }
}
