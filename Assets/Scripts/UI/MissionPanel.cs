using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI Panel that displays the current mission and its objectives.
/// Click outside the panel to close it.
/// </summary>
public class MissionPanel : MonoBehaviour
{
    public static MissionPanel Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform panelRect;

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
    private bool isVisible;

    public bool IsVisible => isVisible;

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
        HidePanel();
    }

    private void Update()
    {
        // Click outside to close
        if (isVisible && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsPointerOverPanel())
            {
                HidePanel();
            }
        }
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

    /// <summary>
    /// Show the mission panel
    /// </summary>
    public void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        isVisible = true;

        // Refresh display
        if (MissionChapterManager.Instance?.CurrentMission != null)
        {
            DisplayMission(MissionChapterManager.Instance.CurrentMission);
        }
    }

    /// <summary>
    /// Hide the mission panel
    /// </summary>
    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        isVisible = false;
    }

    /// <summary>
    /// Toggle panel visibility. Wire this to Button OnClick in Inspector.
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
            HidePanel();
        else
            ShowPanel();
    }

    /// <summary>
    /// Check if pointer is over the panel rect
    /// </summary>
    private bool IsPointerOverPanel()
    {
        // Check if mouse is over any UI element (like the toggle button)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        if (panelRect == null) return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos);
    }

    private void OnMissionStarted(MissionData mission)
    {
        // Don't auto-show, just refresh if visible
        if (isVisible)
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
