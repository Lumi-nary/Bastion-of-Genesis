using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MissionPanel : MonoBehaviour
{
    [Header("Mission Info")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionDescriptionText;
    [SerializeField] private TextMeshProUGUI missionTimerText;

    [Header("Objectives")]
    [SerializeField] private Transform objectivesContainer;
    [SerializeField] private GameObject objectiveSlotPrefab;

    [Header("Chapter Info")]
    [SerializeField] private TextMeshProUGUI chapterInfoText;

    private Dictionary<MissionObjective, MissionObjectiveSlotUI> objectiveSlots = new Dictionary<MissionObjective, MissionObjectiveSlotUI>();

    private void Start()
    {
        // Subscribe to mission and chapter events
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted += OnMissionStarted;
            MissionChapterManager.Instance.OnMissionCompleted += OnMissionCompleted;
            MissionChapterManager.Instance.OnMissionFailed += OnMissionFailed;
            MissionChapterManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
            MissionChapterManager.Instance.OnMissionTimerUpdate += OnMissionTimerUpdate;
            MissionChapterManager.Instance.OnChapterStarted += OnChapterStarted;
        }

        // Hide panel initially
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (MissionChapterManager.Instance != null)
        {
            MissionChapterManager.Instance.OnMissionStarted -= OnMissionStarted;
            MissionChapterManager.Instance.OnMissionCompleted -= OnMissionCompleted;
            MissionChapterManager.Instance.OnMissionFailed -= OnMissionFailed;
            MissionChapterManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
            MissionChapterManager.Instance.OnMissionTimerUpdate -= OnMissionTimerUpdate;
            MissionChapterManager.Instance.OnChapterStarted -= OnChapterStarted;
        }
    }

    private void OnChapterStarted(ChapterData chapter)
    {
        UpdateChapterInfo(chapter);
    }

    private void OnMissionStarted(MissionData mission)
    {
        gameObject.SetActive(true);
        DisplayMission(mission);
    }

    private void OnMissionCompleted(MissionData mission)
    {
        if (missionNameText != null)
            missionNameText.text = $"<color=green>Mission Complete!</color>";
    }

    private void OnMissionFailed(MissionData mission)
    {
        if (missionNameText != null)
            missionNameText.text = $"<color=red>Mission Failed!</color>";
    }

    private void OnObjectiveCompleted(MissionObjective objective)
    {
        if (objectiveSlots.ContainsKey(objective))
        {
            objectiveSlots[objective].UpdateObjective(objective);
        }
    }

    private void OnMissionTimerUpdate(float time)
    {
        UpdateMissionTimer(time);
    }

    private void DisplayMission(MissionData mission)
    {
        // Clear previous objectives
        ClearObjectives();

        // Display mission info
        if (missionNameText != null)
            missionNameText.text = mission.missionName;

        if (missionDescriptionText != null)
            missionDescriptionText.text = mission.missionDescription;

        // Create objective slots
        foreach (var objective in mission.objectives)
        {
            CreateObjectiveSlot(objective);
        }

        // Update chapter info
        if (MissionChapterManager.Instance != null && MissionChapterManager.Instance.CurrentChapter != null)
        {
            UpdateChapterInfo(MissionChapterManager.Instance.CurrentChapter);
        }
    }

    private void CreateObjectiveSlot(MissionObjective objective)
    {
        if (objectiveSlotPrefab == null || objectivesContainer == null) return;

        GameObject slotObj = Instantiate(objectiveSlotPrefab, objectivesContainer);
        MissionObjectiveSlotUI slot = slotObj.GetComponent<MissionObjectiveSlotUI>();

        if (slot != null)
        {
            slot.Setup(objective);
            objectiveSlots.Add(objective, slot);
        }
    }

    private void ClearObjectives()
    {
        foreach (var slot in objectiveSlots.Values)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        objectiveSlots.Clear();
    }

    private void UpdateChapterInfo(ChapterData chapter)
    {
        if (chapterInfoText != null && MissionChapterManager.Instance != null)
        {
            int chapterNum = MissionChapterManager.Instance.CurrentChapterIndex + 1;
            int missionNum = MissionChapterManager.Instance.CurrentMissionIndex + 1;
            int totalMissions = chapter.missions.Count;

            chapterInfoText.text = $"Chapter {chapterNum}: {chapter.chapterName} - Mission {missionNum}/{totalMissions}";
        }
    }

    private void UpdateMissionTimer(float time)
    {
        if (missionTimerText == null) return;

        MissionData mission = MissionChapterManager.Instance.CurrentMission;
        if (mission != null && mission.timeLimit > 0)
        {
            float remaining = mission.timeLimit - time;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);

            Color timerColor = Color.white;
            if (remaining < 30f)
                timerColor = Color.red;
            else if (remaining < 60f)
                timerColor = Color.yellow;

            missionTimerText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(timerColor)}>Time: {minutes:00}:{seconds:00}</color>";
        }
        else
        {
            // No time limit, just show elapsed time
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            missionTimerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
