using NUnit.Framework;
using UnityEngine;

public class TutorialObjectiveFilterTests
{
    [Test]
    public void TutorialBuildObjective_RequiresBuildingAndAllowedCell()
    {
        BuildingData requiredBuilding = ScriptableObject.CreateInstance<BuildingData>();
        BuildingData wrongBuilding = ScriptableObject.CreateInstance<BuildingData>();

        MissionObjective objective = new MissionObjective
        {
            type = ObjectiveType.BuildStructures,
            isTutorialStep = true,
            requiredBuilding = requiredBuilding
        };
        objective.allowedPlacementCells.Add(new Vector2Int(3, 4));

        Assert.IsTrue(objective.MatchesProgress(ObjectiveType.BuildStructures, buildingData: requiredBuilding, placementCell: new Vector2Int(3, 4)));
        Assert.IsFalse(objective.MatchesProgress(ObjectiveType.BuildStructures, buildingData: requiredBuilding, placementCell: new Vector2Int(4, 4)));
        Assert.IsFalse(objective.MatchesProgress(ObjectiveType.BuildStructures, buildingData: wrongBuilding, placementCell: new Vector2Int(3, 4)));

        Object.DestroyImmediate(requiredBuilding);
        Object.DestroyImmediate(wrongBuilding);
    }

    [Test]
    public void TutorialWorkerObjective_RequiresWorkerAndTargetBuilding()
    {
        WorkerData requiredWorker = ScriptableObject.CreateInstance<WorkerData>();
        WorkerData wrongWorker = ScriptableObject.CreateInstance<WorkerData>();
        BuildingData requiredBuilding = ScriptableObject.CreateInstance<BuildingData>();
        BuildingData wrongBuilding = ScriptableObject.CreateInstance<BuildingData>();

        Building matchingBuilding = CreateBuilding(requiredBuilding);
        Building otherBuilding = CreateBuilding(wrongBuilding);

        MissionObjective objective = new MissionObjective
        {
            type = ObjectiveType.AssignWorkers,
            isTutorialStep = true,
            requiredWorker = requiredWorker,
            requiredAssignmentBuilding = requiredBuilding
        };

        Assert.IsTrue(objective.MatchesProgress(ObjectiveType.AssignWorkers, workerData: requiredWorker, assignmentBuilding: matchingBuilding));
        Assert.IsFalse(objective.MatchesProgress(ObjectiveType.AssignWorkers, workerData: wrongWorker, assignmentBuilding: matchingBuilding));
        Assert.IsFalse(objective.MatchesProgress(ObjectiveType.AssignWorkers, workerData: requiredWorker, assignmentBuilding: otherBuilding));

        Object.DestroyImmediate(matchingBuilding.gameObject);
        Object.DestroyImmediate(otherBuilding.gameObject);
        Object.DestroyImmediate(requiredWorker);
        Object.DestroyImmediate(wrongWorker);
        Object.DestroyImmediate(requiredBuilding);
        Object.DestroyImmediate(wrongBuilding);
    }

    [Test]
    public void TutorialResearchObjective_RequiresAuthoredTechnology()
    {
        TechnologyData requiredTech = ScriptableObject.CreateInstance<TechnologyData>();
        TechnologyData wrongTech = ScriptableObject.CreateInstance<TechnologyData>();

        MissionObjective objective = new MissionObjective
        {
            type = ObjectiveType.ResearchTechnology,
            isTutorialStep = true,
            requiredTechnology = requiredTech
        };

        Assert.IsTrue(objective.MatchesProgress(ObjectiveType.ResearchTechnology, technologyData: requiredTech));
        Assert.IsFalse(objective.MatchesProgress(ObjectiveType.ResearchTechnology, technologyData: wrongTech));

        Object.DestroyImmediate(requiredTech);
        Object.DestroyImmediate(wrongTech);
    }

    [Test]
    public void TutorialResearchObjective_WithAllowedTechnologyList_AcceptsAnyListedTech()
    {
        TechnologyData requiredTechA = ScriptableObject.CreateInstance<TechnologyData>();
        TechnologyData requiredTechB = ScriptableObject.CreateInstance<TechnologyData>();
        TechnologyData wrongTech = ScriptableObject.CreateInstance<TechnologyData>();

        MissionObjective objective = new MissionObjective
        {
            type = ObjectiveType.ResearchTechnology,
            isTutorialStep = true,
            targetAmount = 2
        };
        objective.requiredTechnologies.Add(requiredTechA);
        objective.requiredTechnologies.Add(requiredTechB);

        Assert.IsTrue(objective.MatchesProgress(ObjectiveType.ResearchTechnology, technologyData: requiredTechA));
        Assert.IsTrue(objective.MatchesProgress(ObjectiveType.ResearchTechnology, technologyData: requiredTechB));
        Assert.IsFalse(objective.MatchesProgress(ObjectiveType.ResearchTechnology, technologyData: wrongTech));

        Object.DestroyImmediate(requiredTechA);
        Object.DestroyImmediate(requiredTechB);
        Object.DestroyImmediate(wrongTech);
    }

    [Test]
    public void NonTutorialObjectives_PreserveBroadResearchBehavior()
    {
        TechnologyData arbitraryTech = ScriptableObject.CreateInstance<TechnologyData>();

        MissionObjective objective = new MissionObjective
        {
            type = ObjectiveType.ResearchTechnology,
            isTutorialStep = false
        };

        Assert.IsTrue(objective.MatchesProgress(ObjectiveType.ResearchTechnology, technologyData: arbitraryTech));

        Object.DestroyImmediate(arbitraryTech);
    }

    [Test]
    public void ActiveTutorialObjective_WaitsBehindEarlierRequiredObjective()
    {
        MissionData mission = ScriptableObject.CreateInstance<MissionData>();

        MissionObjective completedTutorial = new MissionObjective
        {
            type = ObjectiveType.BuildStructures,
            isTutorialStep = true,
            isCompleted = true
        };

        MissionObjective requiredResourceObjective = new MissionObjective
        {
            type = ObjectiveType.CollectResources,
            isTutorialStep = false,
            isCompleted = false
        };

        MissionObjective futureTutorial = new MissionObjective
        {
            type = ObjectiveType.AssignWorkers,
            isTutorialStep = true,
            isCompleted = false
        };

        mission.objectives.Add(completedTutorial);
        mission.objectives.Add(requiredResourceObjective);
        mission.objectives.Add(futureTutorial);

        Assert.IsNull(TutorialGuideManager.FindActiveTutorialObjective(mission));

        requiredResourceObjective.isCompleted = true;

        Assert.AreSame(futureTutorial, TutorialGuideManager.FindActiveTutorialObjective(mission));

        Object.DestroyImmediate(mission);
    }

    private static Building CreateBuilding(BuildingData buildingData)
    {
        GameObject go = new GameObject("Tutorial Objective Test Building");
        Building building = go.AddComponent<Building>();
        typeof(Building)
            .GetField("buildingData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(building, buildingData);
        return building;
    }
}
