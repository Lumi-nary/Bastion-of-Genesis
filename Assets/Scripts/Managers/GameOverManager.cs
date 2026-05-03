using UnityEngine;
using System.Collections;

/// <summary>
/// GameOverManager monitors game state conditions (Command Center destruction, Mission completion)
/// and triggers the Game Over UI panel.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to events
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingDestroyedEvent += OnBuildingDestroyed;
        }

        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionCompleted += OnMissionCompleted;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingDestroyedEvent -= OnBuildingDestroyed;
        }

        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionCompleted -= OnMissionCompleted;
        }
    }

    private void OnBuildingDestroyed(Building building)
    {
        if (building == null || building.BuildingData == null) return;

        // Loss condition: Command Center is destroyed
        // BuildingCategory.Command is the category for the base/core
        if (building.BuildingData.category == BuildingCategory.Command)
        {
            TriggerGameOver(false);
        }
    }

    private void OnMissionCompleted(MissionData mission)
    {
        if (mission == null) return;

        // Win condition: Mission 10 is completed (as per requirements)
        if (mission.missionNumber == 10)
        {
            TriggerGameOver(true);
        }
    }

    public void TriggerGameOver(bool isWin)
    {
        Debug.Log($"[GameOverManager] Game Over Triggered! Win: {isWin}");
        
        // Gather stats
        float playtime = SaveManager.Instance != null ? SaveManager.Instance.TotalPlaytime : 0f;
        
        string missionName = "Unknown Mission";
        if (MissionChapterManager.Instance != null && MissionChapterManager.Instance.CurrentMission != null)
        {
            missionName = MissionChapterManager.Instance.CurrentMission.missionName;
        }

        var resources = ResourceManager.Instance != null ? ResourceManager.Instance.ResourceAmounts : null;

        // Freeze the game
        Time.timeScale = 0f;

        // Show the panel
        if (GameOverPanelUI.Instance != null)
        {
            GameOverPanelUI.Instance.Show(isWin, playtime, missionName, resources);
        }
        else
        {
            Debug.LogError("[GameOverManager] GameOverPanelUI.Instance is null! Make sure the prefab is in the scene.");
        }
    }
}
