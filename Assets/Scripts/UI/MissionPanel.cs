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

    [Header("Behavior")]
    [Tooltip("When true, clicking outside the panel hides it (legacy modal behavior). Leave false for drawer use.")]
    [SerializeField] private bool closeOnClickOutside = false;

    [Tooltip("Optional: if set, visibility is driven by sliding this toggle instead of GameObject.SetActive. Used for the right-edge drawer.")]
    [SerializeField] private ActionPanelToggle drawerToggle;

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

        // Hide panel initially — snap to hidden instantly so the drawer doesn't flash on-screen at scene load.
        HidePanelInstant();
    }

    private void Update()
    {
        // Click outside to close (only when configured as a modal, not a drawer)
        if (closeOnClickOutside && isVisible && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsPointerOverPanel())
            {
                HidePanel();
            }
        }

        UpdateMissionTimer();
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
    /// Show the mission panel. If drawerToggle is set, slides in; otherwise activates the GameObject.
    /// </summary>
    public void ShowPanel()
    {
        if (drawerToggle != null)
        {
            drawerToggle.Show();
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        isVisible = true;

        // Refresh display
        if (MissionChapterManager.Instance?.CurrentMission != null)
        {
            DisplayMission(MissionChapterManager.Instance.CurrentMission);
        }
    }

    /// <summary>
    /// Hide the mission panel. If drawerToggle is set, slides out; otherwise deactivates the GameObject.
    /// </summary>
    public void HidePanel()
    {
        if (drawerToggle != null)
        {
            drawerToggle.Hide();
        }
        else if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        isVisible = false;
    }

    /// <summary>
    /// Snap the drawer to its hidden position without animation (used for initial state).
    /// </summary>
    private void HidePanelInstant()
    {
        if (drawerToggle != null)
        {
            drawerToggle.HideInstant();
            isVisible = false;
        }
        else
        {
            HidePanel();
        }
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
        UpdateMissionTimer();
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

    private void UpdateMissionTimer()
    {
        if (missionTimerText == null) return;

        MissionChapterManager missionManager = MissionChapterManager.Instance;
        MissionData mission = missionManager != null ? missionManager.CurrentMission : null;

        if (mission == null)
        {
            missionTimerText.gameObject.SetActive(false);
            return;
        }

        missionTimerText.gameObject.SetActive(true);

        string missionTime = FormatMissionTime(mission, missionManager.MissionTimer);
        string waveSpawnTime = GetWaveSpawnTimeText(mission, missionManager.MissionTimer);

        missionTimerText.text =
            "<color=#7FA9BA><size=70%>MISSION TIME</size></color><pos=55%><color=#7FA9BA><size=70%>WAVE SPAWN TIME</size></color>\n" +
            $"<color=#E7F7FF>{missionTime}</color><pos=55%><color=#B5F6D0>{waveSpawnTime}</color>";
    }

    private string FormatMissionTime(MissionData mission, float missionTime)
    {
        string elapsed = FormatTime(missionTime);

        if (mission != null && mission.timeLimit > 0f)
        {
            return $"{elapsed} / {FormatTime(mission.timeLimit)}";
        }

        return elapsed;
    }

    private string GetWaveSpawnTimeText(MissionData mission, float missionTime)
    {
        if (mission == null) return string.Empty;

        bool hasScriptedWave = TryGetNextScriptedWave(mission, missionTime, out float scriptedSeconds, out bool waitingForObjective);

        if (mission.disableNaturalWaves)
        {
            if (hasScriptedWave)
            {
                return waitingForObjective ? $"{FormatTime(scriptedSeconds)} gated" : FormatTime(scriptedSeconds);
            }

            return "Paused";
        }

        WaveController waveController = WaveController.Instance;
        if (waveController == null || !waveController.IsActive)
        {
            return hasScriptedWave ? FormatTime(scriptedSeconds) : "--:--";
        }

        if (waveController.TimeUntilInitialDelayComplete > 0f)
        {
            return FormatTime(waveController.TimeUntilInitialDelayComplete);
        }

        if (waveController.TimeUntilMinimumWaveWindow > 0f)
        {
            return FormatTime(waveController.TimeUntilMinimumWaveWindow);
        }

        if (hasScriptedWave && (!waveController.HasForcedWaveTimer || scriptedSeconds <= waveController.TimeUntilForcedWave))
        {
            return FormatTime(scriptedSeconds);
        }

        if (waveController.HasForcedWaveTimer)
        {
            return FormatTime(waveController.TimeUntilForcedWave);
        }

        return $"Threat {waveController.ThreatPercentage:0}%";
    }

    private bool TryGetNextScriptedWave(MissionData mission, float missionTime, out float secondsUntilWave, out bool waitingForObjective)
    {
        secondsUntilWave = 0f;
        waitingForObjective = false;

        if (mission == null || mission.scriptedWaves == null || mission.scriptedWaves.Count == 0)
        {
            return false;
        }

        float bestReadyTime = float.MaxValue;
        float bestLockedTime = float.MaxValue;

        foreach (ScriptedWave wave in mission.scriptedWaves)
        {
            if (wave == null || wave.isTriggered) continue;

            float remaining = Mathf.Max(0f, wave.triggerTime - missionTime);
            bool objectiveReady = wave.triggerAfterObjectiveIndex < 0 ||
                (wave.triggerAfterObjectiveIndex < mission.objectives.Count &&
                 mission.objectives[wave.triggerAfterObjectiveIndex].isCompleted);

            if (objectiveReady)
            {
                bestReadyTime = Mathf.Min(bestReadyTime, remaining);
            }
            else
            {
                bestLockedTime = Mathf.Min(bestLockedTime, remaining);
            }
        }

        if (bestReadyTime < float.MaxValue)
        {
            secondsUntilWave = bestReadyTime;
            return true;
        }

        if (bestLockedTime < float.MaxValue)
        {
            secondsUntilWave = bestLockedTime;
            waitingForObjective = true;
            return true;
        }

        return false;
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int wholeSeconds = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{wholeSeconds:00}";
    }
}
