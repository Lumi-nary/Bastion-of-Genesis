using UnityEngine;
using System;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Current Mission")]
    [SerializeField] private MissionData currentMission;

    private float missionTimer = 0f;
    private bool missionActive = false;

    // Events
    public event Action<MissionData> OnMissionStarted;
    public event Action<MissionData> OnMissionCompleted;
    public event Action<MissionData> OnMissionFailed;
    public event Action<MissionObjective> OnObjectiveCompleted;
    public event Action<float> OnMissionTimerUpdate; // For time-based missions

    public MissionData CurrentMission => currentMission;
    public bool IsMissionActive => missionActive;
    public float MissionTimer => missionTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!missionActive || currentMission == null) return;

        // Update mission timer
        missionTimer += Time.deltaTime;
        OnMissionTimerUpdate?.Invoke(missionTimer);

        // Check time limit
        if (currentMission.timeLimit > 0 && missionTimer >= currentMission.timeLimit)
        {
            if (currentMission.failOnTimeExpired)
            {
                FailMission();
            }
        }

        // Update time-based objectives
        UpdateTimeBasedObjectives();

        // Check if all objectives are complete
        if (currentMission.AreAllObjectivesComplete())
        {
            CompleteMission();
        }
    }

    public void StartMission(MissionData mission)
    {
        if (mission == null)
        {
            Debug.LogError("Cannot start null mission!");
            return;
        }

        currentMission = mission;
        missionTimer = 0f;
        missionActive = true;

        // Reset all objectives
        foreach (var objective in currentMission.objectives)
        {
            objective.isCompleted = false;
            objective.currentAmount = 0;
            objective.currentTime = 0f;
        }

        // Initialize starting resources
        InitializeStartingResources();

        // Initialize starting workers
        InitializeStartingWorkers();

        OnMissionStarted?.Invoke(currentMission);
        Debug.Log($"Mission Started: {currentMission.missionName}");
    }

    private void InitializeStartingResources()
    {
        // Reset all resources to 0 first
        if (ResourceManager.Instance != null)
        {
            // This assumes ResourceManager has a method to clear resources
            // You may need to add this method to ResourceManager
        }

        // Add starting resources
        foreach (var resourceCost in currentMission.startingResources)
        {
            if (ResourceManager.Instance != null && resourceCost.resourceType != null)
            {
                ResourceManager.Instance.AddResource(resourceCost.resourceType, resourceCost.amount);
            }
        }
    }

    private void InitializeStartingWorkers()
    {
        // This assumes WorkerManager can reset and reinitialize workers
        // You may need to add methods to WorkerManager for this
        foreach (var workerConfig in currentMission.startingWorkers)
        {
            if (WorkerManager.Instance != null && workerConfig.workerData != null)
            {
                // Add starting workers
                for (int i = 0; i < workerConfig.initialCount; i++)
                {
                    WorkerManager.Instance.TrainWorker(workerConfig.workerData);
                }
            }
        }
    }

    private void UpdateTimeBasedObjectives()
    {
        foreach (var objective in currentMission.objectives)
        {
            if (objective.isCompleted) continue;

            switch (objective.type)
            {
                case ObjectiveType.SurviveTime:
                    objective.currentTime = missionTimer;
                    if (objective.currentTime >= objective.targetTime)
                    {
                        CompleteObjective(objective);
                    }
                    break;

                case ObjectiveType.MaintainPollution:
                    if (PollutionManager.Instance != null)
                    {
                        // Check if pollution is within acceptable range
                        if (PollutionManager.Instance.CurrentPollution <= objective.targetAmount)
                        {
                            objective.currentTime += Time.deltaTime;
                            if (objective.currentTime >= objective.targetTime)
                            {
                                CompleteObjective(objective);
                            }
                        }
                        else
                        {
                            // Reset timer if pollution goes over limit
                            objective.currentTime = 0f;
                        }
                    }
                    break;
            }
        }
    }

    public void UpdateObjectiveProgress(ObjectiveType type, int amount, ResourceType resourceType = null, RaceType? raceType = null)
    {
        if (!missionActive || currentMission == null) return;

        foreach (var objective in currentMission.objectives)
        {
            if (objective.isCompleted) continue;
            if (objective.type != type) continue;

            // Check if resource type matches (for resource objectives)
            if (type == ObjectiveType.CollectResources && objective.requiredResource != resourceType)
                continue;

            // Check if race type matches (for enemy defeat objectives)
            if (type == ObjectiveType.DefeatEnemies && raceType.HasValue && objective.targetRace != raceType.Value)
                continue;

            objective.currentAmount += amount;

            if (objective.currentAmount >= objective.targetAmount)
            {
                CompleteObjective(objective);
            }
        }
    }

    private void CompleteObjective(MissionObjective objective)
    {
        if (objective.isCompleted) return;

        objective.isCompleted = true;
        OnObjectiveCompleted?.Invoke(objective);
        Debug.Log($"Objective Completed: {objective.objectiveDescription}");
    }

    private void CompleteMission()
    {
        if (!missionActive) return;

        missionActive = false;

        // Award completion rewards
        foreach (var reward in currentMission.completionRewards)
        {
            if (ResourceManager.Instance != null && reward.resourceType != null)
            {
                ResourceManager.Instance.AddResource(reward.resourceType, reward.amount);
            }
        }

        OnMissionCompleted?.Invoke(currentMission);
        Debug.Log($"Mission Completed: {currentMission.missionName}");
    }

    public void FailMission()
    {
        if (!missionActive) return;

        missionActive = false;
        OnMissionFailed?.Invoke(currentMission);
        Debug.Log($"Mission Failed: {currentMission.missionName}");
    }

    public void EndMission()
    {
        missionActive = false;
        currentMission = null;
        missionTimer = 0f;
    }
}
