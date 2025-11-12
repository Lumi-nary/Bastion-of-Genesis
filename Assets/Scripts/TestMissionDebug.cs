using UnityEngine;

/// <summary>
/// Press T during Play mode to start a test mission
/// </summary>
public class TestMissionDebug : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private ChapterData testChapter;
    [SerializeField] private int testMissionIndex = 0;

    private void Update()
    {
        // Press T to test mission
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartTestMission();
        }

        // Press C to complete first objective (for testing)
        if (Input.GetKeyDown(KeyCode.C))
        {
            CompleteFirstObjective();
        }
    }

    private void StartTestMission()
    {
        if (MissionChapterManager.Instance == null)
        {
            Debug.LogError("MissionChapterManager not found!");
            return;
        }

        if (testChapter == null || testMissionIndex >= testChapter.missions.Count)
        {
            Debug.LogError("Invalid test configuration!");
            return;
        }

        Debug.Log($"[TEST] Starting mission: {testChapter.missions[testMissionIndex].missionName}");
        MissionChapterManager.Instance.StartMission(testChapter.missions[testMissionIndex]);
    }

    private void CompleteFirstObjective()
    {
        if (MissionChapterManager.Instance == null || !MissionChapterManager.Instance.IsMissionActive)
        {
            Debug.LogWarning("No active mission!");
            return;
        }

        var mission = MissionChapterManager.Instance.CurrentMission;
        if (mission != null && mission.objectives.Count > 0)
        {
            var firstObjective = mission.objectives[0];

            // Simulate completing the objective
            switch (firstObjective.type)
            {
                case ObjectiveType.CollectResources:
                    MissionChapterManager.Instance.UpdateObjectiveProgress(
                        ObjectiveType.CollectResources,
                        firstObjective.targetAmount,
                        firstObjective.requiredResource
                    );
                    break;

                case ObjectiveType.BuildStructures:
                    MissionChapterManager.Instance.UpdateObjectiveProgress(
                        ObjectiveType.BuildStructures,
                        firstObjective.targetAmount
                    );
                    break;

                // Add more cases as needed
            }

            Debug.Log($"[TEST] Completed objective: {firstObjective.objectiveDescription}");
        }
    }
}
