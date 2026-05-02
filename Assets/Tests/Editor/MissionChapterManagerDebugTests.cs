using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MissionChapterManagerDebugTests
{
    [Test]
    public void ForceCompleteCurrentMission_MainObjectives_AdvancesImmediately()
    {
        GameObject managerObject = new GameObject("MissionChapterManager_Test");
        MissionChapterManager manager = managerObject.AddComponent<MissionChapterManager>();

        try
        {
            ChapterData chapter = ScriptableObject.CreateInstance<ChapterData>();
            chapter.chapterName = "Test Chapter";

            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            mission.missionName = "Test Mission";
            mission.objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    objectiveDescription = "Main",
                    type = ObjectiveType.BuildStructures,
                    targetAmount = 1
                },
                new MissionObjective
                {
                    objectiveDescription = "Optional",
                    type = ObjectiveType.CollectResources,
                    targetAmount = 5,
                    isOptional = true
                }
            };

            chapter.missions = new List<MissionData> { mission };

            SetPrivateField(manager, "chapters", new List<ChapterData> { chapter });
            SetPrivateField(manager, "currentChapter", chapter);
            SetPrivateField(manager, "currentMission", mission);
            SetPrivateField(manager, "currentMissionIndex", 0);
            SetPrivateField(manager, "missionActive", true);

            int completedObjectives = 0;
            int completedMissions = 0;
            manager.OnObjectiveCompleted += _ => completedObjectives++;
            manager.OnMissionCompleted += _ => completedMissions++;

            manager.ForceCompleteCurrentMission(includeOptional: false);

            Assert.IsTrue(mission.objectives[0].isCompleted);
            Assert.AreEqual(1, mission.objectives[0].currentAmount);
            Assert.IsFalse(mission.objectives[1].isCompleted);
            Assert.AreEqual(1, completedObjectives);
            Assert.AreEqual(1, completedMissions);
            Assert.AreEqual(1, manager.CurrentMissionIndex);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
        }
    }

    private static void SetPrivateField<T>(MissionChapterManager manager, string fieldName, T value)
    {
        FieldInfo field = typeof(MissionChapterManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' to exist.");
        field.SetValue(manager, value);
    }
}
