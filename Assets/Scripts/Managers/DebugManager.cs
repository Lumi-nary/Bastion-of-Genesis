using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Unified Debug System for Planetfall
/// Press F12 to toggle the debug panel
/// Includes tabs for: Resources, Workers, Waves, Pollution, Enemies
/// </summary>
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Debug Settings")]
    public bool showDebugMenu = false;

    // Tab system
    private enum DebugTab { Resources, Workers, Waves, Pollution, Enemies, Mission, Visuals }
    private DebugTab currentTab = DebugTab.Resources;

    // Visual debug toggles
    public static bool ShowTurretRange { get; private set; } = false;
    public static bool ShowEnemyPaths { get; private set; } = false;
    public static bool ShowEnemyTargets { get; private set; } = false;

    // Input caches
    private Dictionary<ResourceType, string> resourceInputAmounts = new Dictionary<ResourceType, string>();
    private Dictionary<WorkerData, string> workerInputAmounts = new Dictionary<WorkerData, string>();
    private List<ResourceType> cachedResourceTypes = new List<ResourceType>();
    private List<WorkerData> cachedWorkerTypes = new List<WorkerData>();

    // Wave debug inputs
    private string threatRateInput = "1";
    private string threatThresholdInput = "100";
    private string attackChanceInput = "50";
    private string maxWaitTimeInput = "120";

    // Pollution debug inputs
    private string pollutionAddInput = "100";

    // Mission debug inputs
    private string missionJumpInput = "1";
    private Vector2 missionScrollPos = Vector2.zero;

    // Panel dimensions
    private const float PANEL_WIDTH = 400f;
    private const float PANEL_HEIGHT = 550f;

    // Draggable window
    private Rect windowRect = new Rect(10f, 10f, PANEL_WIDTH, PANEL_HEIGHT);
    private int windowId = 12345;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void Start()
    {
        InitializeInputAmounts();
        InitializeWaveInputs();
    }

    private void InitializeInputAmounts()
    {
        if (ResourceManager.Instance != null)
        {
            foreach (var resourceType in ResourceManager.Instance.ResourceAmounts.Keys)
            {
                if (!resourceInputAmounts.ContainsKey(resourceType))
                {
                    resourceInputAmounts[resourceType] = "100";
                    cachedResourceTypes.Add(resourceType);
                }
            }
        }

        if (WorkerManager.Instance != null)
        {
            foreach (var workerData in WorkerManager.Instance.AvailableWorkers.Keys)
            {
                if (!workerInputAmounts.ContainsKey(workerData))
                {
                    workerInputAmounts[workerData] = "1";
                    cachedWorkerTypes.Add(workerData);
                }
            }
        }
    }

    private void InitializeWaveInputs()
    {
        if (WaveController.Instance != null)
        {
            // Values will be read directly from WaveController
        }
    }

    private void Update()
    {
        // Toggle debug menu with F12 key
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            showDebugMenu = !showDebugMenu;
        }
    }

    private void OnGUI()
    {
        if (!showDebugMenu) return;

        // Refresh data if needed
        if (cachedResourceTypes.Count == 0 || cachedWorkerTypes.Count == 0)
        {
            InitializeInputAmounts();
        }

        // Clamp window position to screen bounds
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);

        // Draggable window
        windowRect = GUI.Window(windowId, windowRect, DrawDebugWindow, "Debug Panel (F12) - Drag to move");
    }

    private void DrawDebugWindow(int id)
    {
        // Tab buttons (relative to window)
        DrawTabs();

        // Tab content
        float contentY = 60f;
        switch (currentTab)
        {
            case DebugTab.Resources:
                DrawResourcesTab(contentY);
                break;
            case DebugTab.Workers:
                DrawWorkersTab(contentY);
                break;
            case DebugTab.Waves:
                DrawWavesTab(contentY);
                break;
            case DebugTab.Pollution:
                DrawPollutionTab(contentY);
                break;
            case DebugTab.Enemies:
                DrawEnemiesTab(contentY);
                break;
            case DebugTab.Mission:
                DrawMissionTab(contentY);
                break;
            case DebugTab.Visuals:
                DrawVisualsTab(contentY);
                break;
        }

        // Make the window draggable by its title bar (top 25 pixels)
        GUI.DragWindow(new Rect(0, 0, windowRect.width, 25));
    }

    private void DrawTabs()
    {
        float tabWidth = 55f;
        float tabY = 25f;
        float tabX = 5f;

        string[] tabNames = { "Resources", "Workers", "Waves", "Pollution", "Enemies", "Mission", "Visuals" };
        DebugTab[] tabs = { DebugTab.Resources, DebugTab.Workers, DebugTab.Waves, DebugTab.Pollution, DebugTab.Enemies, DebugTab.Mission, DebugTab.Visuals };

        for (int i = 0; i < tabNames.Length; i++)
        {
            bool isSelected = currentTab == tabs[i];
            GUI.color = isSelected ? Color.cyan : Color.white;

            if (GUI.Button(new Rect(tabX + (i * tabWidth), tabY, tabWidth - 2f, 25f), tabNames[i]))
            {
                currentTab = tabs[i];
            }
        }
        GUI.color = Color.white;
    }

    #region Resources Tab

    private void DrawResourcesTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 300, 20), "<b>Resource Management</b>");
        y += 30f;

        if (ResourceManager.Instance == null)
        {
            GUI.Label(new Rect(20, y, 300, 20), "ResourceManager not found!");
            return;
        }

        // Header
        GUI.Label(new Rect(20, y, 80, 20), "Resource");
        GUI.Label(new Rect(110, y, 80, 20), "Amount");
        GUI.Label(new Rect(190, y, 50, 20), "Input");
        y += 25f;

        foreach (var resource in cachedResourceTypes)
        {
            int current = ResourceManager.Instance.GetResourceAmount(resource);
            int capacity = ResourceManager.Instance.GetResourceCapacity(resource);

            GUI.Label(new Rect(20, y, 90, 20), resource.ResourceName);
            GUI.Label(new Rect(110, y, 80, 20), $"{current}/{capacity}");

            string inputValue = GUI.TextField(new Rect(190, y, 50, 20), resourceInputAmounts[resource]);
            resourceInputAmounts[resource] = inputValue;

            if (GUI.Button(new Rect(250, y, 45, 20), "Add"))
            {
                if (int.TryParse(inputValue, out int amount))
                {
                    ResourceManager.Instance.AddResource(resource, amount);
                }
            }
            if (GUI.Button(new Rect(300, y, 60, 20), "Remove"))
            {
                if (int.TryParse(inputValue, out int amount))
                {
                    ResourceManager.Instance.RemoveResource(resource, amount);
                }
            }
            y += 25f;
        }

        y += 10f;

        // Quick actions
        if (GUI.Button(new Rect(20, y, 120, 25), "Max All"))
        {
            foreach (var resource in cachedResourceTypes)
            {
                int capacity = ResourceManager.Instance.GetResourceCapacity(resource);
                int current = ResourceManager.Instance.GetResourceAmount(resource);
                ResourceManager.Instance.AddResource(resource, capacity - current);
            }
        }
        if (GUI.Button(new Rect(150, y, 120, 25), "Clear All"))
        {
            ResourceManager.Instance.ResetAllResources();
        }
    }

    #endregion

    #region Workers Tab

    private void DrawWorkersTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 300, 20), "<b>Worker Management</b>");
        y += 30f;

        if (WorkerManager.Instance == null)
        {
            GUI.Label(new Rect(20, y, 300, 20), "WorkerManager not found!");
            return;
        }

        // Header
        GUI.Label(new Rect(20, y, 80, 20), "Worker");
        GUI.Label(new Rect(110, y, 80, 20), "Available");
        GUI.Label(new Rect(190, y, 50, 20), "Input");
        y += 25f;

        foreach (var worker in cachedWorkerTypes)
        {
            int current = WorkerManager.Instance.GetAvailableWorkerCount(worker);
            int capacity = WorkerManager.Instance.GetWorkerCapacity(worker);

            GUI.Label(new Rect(20, y, 90, 20), worker.workerName);
            GUI.Label(new Rect(110, y, 80, 20), $"{current}/{capacity}");

            string inputValue = GUI.TextField(new Rect(190, y, 50, 20), workerInputAmounts[worker]);
            workerInputAmounts[worker] = inputValue;

            if (GUI.Button(new Rect(250, y, 45, 20), "Add"))
            {
                if (int.TryParse(inputValue, out int amount))
                {
                    for (int i = 0; i < amount; i++)
                    {
                        WorkerManager.Instance.ReturnWorker(worker);
                    }
                }
            }
            if (GUI.Button(new Rect(300, y, 60, 20), "Remove"))
            {
                if (int.TryParse(inputValue, out int amount))
                {
                    for (int i = 0; i < amount; i++)
                    {
                        WorkerManager.Instance.AssignWorker(worker);
                    }
                }
            }
            y += 25f;
        }
    }

    #endregion

    #region Waves Tab

    private void DrawWavesTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 300, 20), "<b>Wave Controller</b>");
        y += 30f;

        if (WaveController.Instance == null)
        {
            GUI.Label(new Rect(20, y, 300, 20), "WaveController not found!");
            return;
        }

        var wc = WaveController.Instance;

        // Status
        GUI.Label(new Rect(20, y, 350, 20),
            $"Wave: {wc.CurrentWave} | Active: {wc.IsActive} | Paused: {wc.IsPaused}");
        y += 25f;

        // Threat bar
        GUI.Label(new Rect(20, y, 60, 20), "Threat:");
        float threatPercent = wc.CurrentThreat / wc.ThreatThreshold;
        GUI.Box(new Rect(80, y, 200, 18), "");
        GUI.color = Color.Lerp(Color.green, Color.red, threatPercent);
        GUI.Box(new Rect(81, y + 1, 198 * Mathf.Clamp01(threatPercent), 16), "");
        GUI.color = Color.white;
        GUI.Label(new Rect(290, y, 100, 20), $"{wc.CurrentThreat:F0}/{wc.ThreatThreshold}");
        y += 25f;

        // Pollution effects on waves
        float pollution = PollutionManager.Instance != null ? PollutionManager.Instance.PollutionNormalized : 0f;
        float threatMult = 1f + (pollution * 1.5f);
        float attackChance = 0.5f + (pollution * 0.3f);
        GUI.Label(new Rect(20, y, 350, 20),
            $"Pollution: {pollution * 100:F0}% | Threat x{threatMult:F2} | Chance: {attackChance * 100:F0}%");
        y += 30f;

        // Editable parameters (read from serialized fields via reflection or use local tracking)
        GUI.Label(new Rect(20, y, 120, 20), "Threat Rate:");
        threatRateInput = GUI.TextField(new Rect(140, y, 60, 20), threatRateInput);
        y += 25f;

        GUI.Label(new Rect(20, y, 120, 20), "Threshold:");
        threatThresholdInput = GUI.TextField(new Rect(140, y, 60, 20), threatThresholdInput);
        y += 25f;

        GUI.Label(new Rect(20, y, 120, 20), "Base Attack %:");
        attackChanceInput = GUI.TextField(new Rect(140, y, 60, 20), attackChanceInput);
        y += 25f;

        GUI.Label(new Rect(20, y, 120, 20), "Max Wait (s):");
        maxWaitTimeInput = GUI.TextField(new Rect(140, y, 60, 20), maxWaitTimeInput);
        y += 30f;

        // Action buttons row 1
        if (GUI.Button(new Rect(20, y, 80, 25), "Force Wave"))
        {
            wc.ForceWave();
        }
        if (GUI.Button(new Rect(110, y, 80, 25), "+25 Threat"))
        {
            AddThreatToWaveController(25f);
        }
        if (GUI.Button(new Rect(200, y, 90, 25), "Reset Threat"))
        {
            ResetWaveControllerThreat();
        }
        y += 30f;

        // Action buttons row 2
        if (GUI.Button(new Rect(20, y, 80, 25), wc.IsPaused ? "Resume" : "Pause"))
        {
            wc.SetPaused(!wc.IsPaused);
        }
        if (GUI.Button(new Rect(110, y, 80, 25), wc.IsActive ? "Stop" : "Start"))
        {
            if (wc.IsActive) wc.StopWaveSystem();
            else wc.StartWaveSystem();
        }
        if (GUI.Button(new Rect(200, y, 90, 25), "Clear Enemies"))
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.ClearAllEnemies();
            }
        }
    }

    private void AddThreatToWaveController(float amount)
    {
        // Use reflection to modify private field
        var field = typeof(WaveController).GetField("currentThreat",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && WaveController.Instance != null)
        {
            float current = (float)field.GetValue(WaveController.Instance);
            field.SetValue(WaveController.Instance, current + amount);
        }
    }

    private void ResetWaveControllerThreat()
    {
        var field = typeof(WaveController).GetField("currentThreat",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && WaveController.Instance != null)
        {
            field.SetValue(WaveController.Instance, 0f);
        }
    }

    #endregion

    #region Pollution Tab

    private void DrawPollutionTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 300, 20), "<b>Pollution & Difficulty</b>");
        y += 30f;

        if (PollutionManager.Instance == null)
        {
            GUI.Label(new Rect(20, y, 300, 20), "PollutionManager not found!");
            return;
        }

        var pm = PollutionManager.Instance;

        // Current pollution bar
        GUI.Label(new Rect(20, y, 60, 20), "Pollution:");
        float pollPercent = pm.PollutionNormalized;
        GUI.Box(new Rect(80, y, 200, 18), "");
        GUI.color = Color.Lerp(Color.green, Color.red, pollPercent);
        GUI.Box(new Rect(81, y + 1, 198 * pollPercent, 16), "");
        GUI.color = Color.white;
        GUI.Label(new Rect(290, y, 100, 20), $"{pm.CurrentPollution:F0}/{pm.MaxPollution}");
        y += 25f;

        // Stats
        GUI.Label(new Rect(20, y, 350, 20),
            $"Tier: {pm.CurrentTier} | Menu Difficulty: {pm.MenuDifficulty}");
        y += 25f;

        GUI.Label(new Rect(20, y, 350, 20),
            $"Integration Radius: {pm.IntegrationRadius:F1}");
        y += 30f;

        // Multiplier info
        GUI.Label(new Rect(20, y, 350, 20), "<b>Current Multipliers:</b>");
        y += 22f;
        GUI.Label(new Rect(30, y, 350, 20),
            $"Spawn Count: {pm.GetSpawnCountMultiplier():F2}x");
        y += 20f;
        GUI.Label(new Rect(30, y, 350, 20),
            $"Wave Interval: {pm.GetWaveIntervalMultiplier():F2}x");
        y += 20f;
        GUI.Label(new Rect(30, y, 350, 20),
            $"Enemy HP: {pm.GetEnemyHPModifier():F2}x");
        y += 20f;
        GUI.Label(new Rect(30, y, 350, 20),
            $"Enemy Damage: {pm.GetEnemyDamageModifier():F2}x");
        y += 30f;

        // Pollution controls
        GUI.Label(new Rect(20, y, 60, 20), "Amount:");
        pollutionAddInput = GUI.TextField(new Rect(80, y, 60, 20), pollutionAddInput);

        if (GUI.Button(new Rect(150, y, 50, 20), "Add"))
        {
            if (float.TryParse(pollutionAddInput, out float amount))
            {
                pm.AddPollution(amount);
            }
        }
        if (GUI.Button(new Rect(210, y, 70, 20), "Remove"))
        {
            if (float.TryParse(pollutionAddInput, out float amount))
            {
                pm.RemovePollution(amount);
            }
        }
        y += 30f;

        // Quick set buttons
        if (GUI.Button(new Rect(20, y, 60, 25), "0%"))
        {
            pm.SetPollution(0);
        }
        if (GUI.Button(new Rect(85, y, 60, 25), "25%"))
        {
            pm.SetPollution(pm.MaxPollution * 0.25f);
        }
        if (GUI.Button(new Rect(150, y, 60, 25), "50%"))
        {
            pm.SetPollution(pm.MaxPollution * 0.5f);
        }
        if (GUI.Button(new Rect(215, y, 60, 25), "75%"))
        {
            pm.SetPollution(pm.MaxPollution * 0.75f);
        }
        if (GUI.Button(new Rect(280, y, 60, 25), "100%"))
        {
            pm.SetPollution(pm.MaxPollution);
        }
    }

    #endregion

    #region Enemies Tab

    private void DrawEnemiesTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 300, 20), "<b>Enemy Management</b>");
        y += 30f;

        if (EnemyManager.Instance == null)
        {
            GUI.Label(new Rect(20, y, 300, 20), "EnemyManager not found!");
            return;
        }

        var em = EnemyManager.Instance;

        // Stats
        GUI.Label(new Rect(20, y, 350, 20),
            $"Active Enemies: {em.ActiveEnemyCount}");
        y += 22f;
        GUI.Label(new Rect(20, y, 350, 20),
            $"Enemies Killed: {em.EnemiesKilled}");
        y += 22f;
        GUI.Label(new Rect(20, y, 350, 20),
            $"Current Wave: {em.CurrentWave}");
        y += 22f;
        GUI.Label(new Rect(20, y, 350, 20),
            $"Wave Active: {em.IsWaveActive}");
        y += 30f;

        // Actions
        if (GUI.Button(new Rect(20, y, 120, 25), "Clear All Enemies"))
        {
            em.ClearAllEnemies();
        }
        if (GUI.Button(new Rect(150, y, 120, 25), "Reset for Mission"))
        {
            em.ResetForNewMission();
        }
        y += 35f;

        // Enemy breakdown by race
        GUI.Label(new Rect(20, y, 300, 20), "<b>Active by Race:</b>");
        y += 22f;

        var enemies = em.GetAllActiveEnemies();
        Dictionary<EnemyRace, int> raceCounts = new Dictionary<EnemyRace, int>();

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.Data != null)
            {
                if (!raceCounts.ContainsKey(enemy.Data.race))
                    raceCounts[enemy.Data.race] = 0;
                raceCounts[enemy.Data.race]++;
            }
        }

        foreach (var kvp in raceCounts)
        {
            GUI.Label(new Rect(30, y, 300, 20), $"{kvp.Key}: {kvp.Value}");
            y += 20f;
        }

        if (raceCounts.Count == 0)
        {
            GUI.Label(new Rect(30, y, 300, 20), "No active enemies");
        }
    }

    #endregion

    #region Mission Tab

    private void DrawMissionTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 350, 20), "<b>Mission Debug</b>");
        y += 25f;

        var mcm = MissionChapterManager.Instance;
        if (mcm == null)
        {
            GUI.Label(new Rect(20, y, 300, 20), "MissionChapterManager not found!");
            return;
        }

        // Chapter info
        string chapterName = mcm.CurrentChapter != null ? mcm.CurrentChapter.chapterName : "None";
        GUI.Label(new Rect(20, y, 350, 20), $"Chapter: {mcm.CurrentChapterIndex + 1} - {chapterName}");
        y += 22f;

        // Mission info
        string missionName = mcm.CurrentMission != null ? mcm.CurrentMission.missionName : "None";
        int missionNum = mcm.CurrentMission != null ? mcm.CurrentMission.missionNumber : 0;
        GUI.Label(new Rect(20, y, 350, 20), $"Mission: {missionNum} - {missionName}");
        y += 22f;

        GUI.Label(new Rect(20, y, 350, 20),
            $"Active: {mcm.IsMissionActive} | Timer: {mcm.MissionTimer:F1}s");
        y += 28f;

        // --- Objectives ---
        if (mcm.CurrentMission != null && mcm.CurrentMission.objectives.Count > 0)
        {
            GUI.Label(new Rect(20, y, 350, 20), "<b>Objectives:</b>");
            y += 22f;

            foreach (var obj in mcm.CurrentMission.objectives)
            {
                // Status icon
                string status = obj.isCompleted ? "<color=green>[DONE]</color>" :
                                obj.isOptional ? "<color=yellow>[OPT]</color>" : "<color=red>[    ]</color>";

                GUI.Label(new Rect(20, y, 250, 20),
                    $"{status} {obj.objectiveDescription}");

                // Progress
                GUI.Label(new Rect(270, y, 80, 20), obj.GetProgressText());

                // Force complete button
                if (!obj.isCompleted)
                {
                    if (GUI.Button(new Rect(345, y, 40, 18), "Done"))
                    {
                        ForceCompleteObjective(obj);
                    }
                }
                y += 22f;
            }
        }
        else
        {
            GUI.Label(new Rect(20, y, 300, 20), "No active mission objectives.");
            y += 22f;
        }

        y += 10f;

        // --- Action Buttons ---
        GUI.Label(new Rect(20, y, 350, 20), "<b>Actions:</b>");
        y += 25f;

        // Skip mission (force complete all main objectives)
        if (GUI.Button(new Rect(20, y, 110, 25), "Skip Mission"))
        {
            SkipCurrentMission();
        }

        // Complete all optional objectives too
        if (GUI.Button(new Rect(140, y, 130, 25), "Complete All Obj."))
        {
            CompleteAllObjectives();
        }
        y += 30f;

        // Jump to mission number
        GUI.Label(new Rect(20, y, 80, 20), "Jump to M#:");
        missionJumpInput = GUI.TextField(new Rect(100, y, 40, 20), missionJumpInput);

        if (GUI.Button(new Rect(150, y, 60, 20), "Go"))
        {
            if (int.TryParse(missionJumpInput, out int targetMission))
            {
                JumpToMission(targetMission);
            }
        }
        y += 28f;

        // Restart current mission
        if (GUI.Button(new Rect(20, y, 110, 25), "Restart Mission"))
        {
            RestartCurrentMission();
        }

        // Fail mission
        if (GUI.Button(new Rect(140, y, 110, 25), "Fail Mission"))
        {
            if (mcm.IsMissionActive)
            {
                mcm.FailMission();
                Debug.Log("[DebugManager] Mission force-failed");
            }
        }
        y += 30f;

        // Spawn boss for testing
        if (GUI.Button(new Rect(20, y, 130, 25), "Spawn Warden"))
        {
            SpawnWardenBoss();
        }
    }

    private void ForceCompleteObjective(MissionObjective objective)
    {
        if (objective == null || objective.isCompleted) return;

        var mcm = MissionChapterManager.Instance;
        int remaining = objective.targetAmount - objective.currentAmount;

        switch (objective.type)
        {
            case ObjectiveType.CollectResources:
                // Actually grant the resources so the ResourceManager event fires naturally
                if (objective.requiredResource != null && ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddResource(objective.requiredResource, remaining);
                    Debug.Log($"[DebugManager] Granted {remaining} {objective.requiredResource.ResourceName}");
                }
                else
                {
                    // No specific resource — just force the objective done
                    ForceObjectiveDone(objective);
                }
                break;

            case ObjectiveType.AssignWorkers:
                // Pass worker type so the filter matches the correct objective
                if (mcm != null)
                {
                    mcm.UpdateObjectiveProgress(ObjectiveType.AssignWorkers, remaining, workerData: objective.requiredWorker);
                }
                break;

            case ObjectiveType.BuildStructures:
                // Building objectives filter by requiredBuilding — pass it through
                if (mcm != null && objective.requiredBuilding != null)
                {
                    mcm.UpdateObjectiveProgress(ObjectiveType.BuildStructures, remaining, buildingData: objective.requiredBuilding);
                }
                else
                {
                    ForceObjectiveDone(objective);
                }
                break;

            case ObjectiveType.DefeatEnemies:
                if (mcm != null)
                {
                    mcm.UpdateObjectiveProgress(ObjectiveType.DefeatEnemies, remaining);
                }
                break;

            case ObjectiveType.DefeatBoss:
                if (mcm != null)
                {
                    mcm.UpdateObjectiveProgress(ObjectiveType.DefeatBoss, remaining);
                }
                break;

            case ObjectiveType.ResearchTechnology:
                if (mcm != null)
                {
                    mcm.UpdateObjectiveProgress(ObjectiveType.ResearchTechnology, remaining);
                }
                break;

            case ObjectiveType.ReachPollutionLevel:
                // Set pollution to the target level
                if (PollutionManager.Instance != null)
                {
                    float targetPollution = PollutionManager.Instance.MaxPollution * (objective.targetAmount / 100f);
                    PollutionManager.Instance.SetPollution(targetPollution);
                    Debug.Log($"[DebugManager] Set pollution to {targetPollution:F0} ({objective.targetAmount}%)");
                }
                break;

            case ObjectiveType.SurviveTime:
            case ObjectiveType.MaintainPollution:
                // Time-based — force complete directly
                ForceObjectiveDone(objective);
                break;

            default:
                ForceObjectiveDone(objective);
                break;
        }

        Debug.Log($"[DebugManager] Force completed objective: {objective.objectiveDescription}");
    }

    /// <summary>
    /// Directly mark an objective as complete, bypassing UpdateObjectiveProgress filters.
    /// Used when the normal event path can't be triggered from debug.
    /// </summary>
    private void ForceObjectiveDone(MissionObjective objective)
    {
        objective.currentAmount = objective.targetAmount;
        objective.currentTime = objective.targetTime;
        objective.isCompleted = true;
    }

    private void SkipCurrentMission()
    {
        var mcm = MissionChapterManager.Instance;
        if (mcm == null || mcm.CurrentMission == null) return;

        string skippedMissionName = mcm.CurrentMission.missionName;
        mcm.ForceCompleteCurrentMission(includeOptional: false);
        Debug.Log($"[DebugManager] Skipped mission: {skippedMissionName}");
    }

    private void CompleteAllObjectives()
    {
        var mcm = MissionChapterManager.Instance;
        if (mcm == null || mcm.CurrentMission == null) return;

        mcm.ForceCompleteCurrentMission(includeOptional: true);
        Debug.Log("[DebugManager] All objectives force-completed");
    }

    private void JumpToMission(int missionNumber)
    {
        var mcm = MissionChapterManager.Instance;
        if (mcm == null || mcm.CurrentChapter == null) return;

        // Mission numbers are 1-based, index is 0-based
        int targetIndex = missionNumber - 1;

        if (targetIndex < 0 || targetIndex >= mcm.CurrentChapter.missions.Count)
        {
            Debug.LogWarning($"[DebugManager] Invalid mission number: {missionNumber} (chapter has {mcm.CurrentChapter.missions.Count} missions)");
            return;
        }

        // Use reflection to set the private currentMissionIndex
        var indexField = typeof(MissionChapterManager).GetField("currentMissionIndex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var activeField = typeof(MissionChapterManager).GetField("missionActive",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (indexField != null)
        {
            // Deactivate current mission
            if (activeField != null)
            {
                activeField.SetValue(mcm, false);
            }

            // Set the index and start
            indexField.SetValue(mcm, targetIndex);
            mcm.StartNextMission();

            Debug.Log($"[DebugManager] Jumped to Mission {missionNumber}: {mcm.CurrentChapter.missions[targetIndex].missionName}");
        }
    }

    private void RestartCurrentMission()
    {
        var mcm = MissionChapterManager.Instance;
        if (mcm == null || mcm.CurrentMission == null) return;

        // Deactivate then restart the same mission
        var activeField = typeof(MissionChapterManager).GetField("missionActive",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (activeField != null)
        {
            activeField.SetValue(mcm, false);
        }

        mcm.StartMission(mcm.CurrentMission);
        Debug.Log($"[DebugManager] Restarted mission: {mcm.CurrentMission.missionName}");
    }

    private void SpawnWardenBoss()
    {
        if (EnemyManager.Instance == null) return;

        var wardenData = Resources.Load<EnemyData>("Data/Enemies/Enemy_Warden");
        if (wardenData == null)
        {
            Debug.LogWarning("[DebugManager] Could not load Enemy_Warden data!");
            return;
        }

        // Spawn near a random edge
        Vector3 spawnPos = Vector3.zero;
        if (Camera.main != null)
        {
            spawnPos = Camera.main.transform.position + new Vector3(15f, 0f, 0f);
        }

        Enemy boss = EnemyManager.Instance.SpawnEnemy(wardenData, spawnPos);
        if (boss != null)
        {
            Debug.Log($"[DebugManager] Spawned Warden boss at {spawnPos}. IsBossEnemy: {boss is BossEnemy}");
        }
    }

    #endregion

    #region Visuals Tab

    private void DrawVisualsTab(float startY)
    {
        float y = startY;

        GUI.Label(new Rect(20, y, 300, 20), "<b>Visual Debug Toggles</b>");
        y += 30f;

        // Turret Range toggle
        GUI.color = ShowTurretRange ? Color.green : Color.white;
        if (GUI.Button(new Rect(20, y, 350, 25), ShowTurretRange ? "[ON]  Turret Attack Range" : "[OFF] Turret Attack Range"))
        {
            ShowTurretRange = !ShowTurretRange;
        }
        GUI.color = Color.white;
        y += 30f;

        // Enemy Paths toggle
        GUI.color = ShowEnemyPaths ? Color.green : Color.white;
        if (GUI.Button(new Rect(20, y, 350, 25), ShowEnemyPaths ? "[ON]  Enemy Flow Field Path" : "[OFF] Enemy Flow Field Path"))
        {
            ShowEnemyPaths = !ShowEnemyPaths;
        }
        GUI.color = Color.white;
        y += 30f;

        // Enemy Targets toggle
        GUI.color = ShowEnemyTargets ? Color.green : Color.white;
        if (GUI.Button(new Rect(20, y, 350, 25), ShowEnemyTargets ? "[ON]  Enemy Target Lines" : "[OFF] Enemy Target Lines"))
        {
            ShowEnemyTargets = !ShowEnemyTargets;
        }
        GUI.color = Color.white;
        y += 40f;

        // Info text
        GUI.Label(new Rect(20, y, 360, 60),
            "Turret Range: Red circle around turrets\n" +
            "Enemy Path: Yellow dots showing flow field direction\n" +
            "Enemy Target: Red line to targeted building");
    }

    #endregion

    #region Visual Debug Rendering

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Only render debug visuals for the main camera
        if (camera != Camera.main) return;

        if (ShowTurretRange) DrawTurretRanges();
        if (ShowEnemyPaths) DrawEnemyPaths();
        if (ShowEnemyTargets) DrawEnemyTargetLines();
    }

    private static Material _debugLineMat;
    private static Material DebugLineMat
    {
        get
        {
            if (_debugLineMat == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _debugLineMat = new Material(shader);
                _debugLineMat.hideFlags = HideFlags.HideAndDontSave;
                _debugLineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _debugLineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _debugLineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _debugLineMat.SetInt("_ZWrite", 0);
                _debugLineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            return _debugLineMat;
        }
    }

    private void DrawTurretRanges()
    {
        if (BuildingManager.Instance == null) return;

        DebugLineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        foreach (var building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDestroyed) continue;

            Turret turret = building.GetComponent<Turret>();
            if (turret == null) continue;

            float range = turret.GetEffectiveRange();
            Vector3 center = building.transform.position;

            // Draw range circle
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.2f, 0.2f, 0.6f));
            int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                GL.Vertex3(center.x + Mathf.Cos(angle1) * range, center.y + Mathf.Sin(angle1) * range, 0f);
                GL.Vertex3(center.x + Mathf.Cos(angle2) * range, center.y + Mathf.Sin(angle2) * range, 0f);
            }
            GL.End();

            // Draw line to current target
            if (turret.CurrentTarget != null && !turret.CurrentTarget.IsDead)
            {
                GL.Begin(GL.LINES);
                GL.Color(new Color(1f, 1f, 0f, 0.8f));
                GL.Vertex3(center.x, center.y, 0f);
                Vector3 targetPos = turret.CurrentTarget.transform.position;
                GL.Vertex3(targetPos.x, targetPos.y, 0f);
                GL.End();
            }
        }

        GL.PopMatrix();
    }

    private void DrawEnemyPaths()
    {
        if (EnemyManager.Instance == null || PathfindingManager.Instance == null) return;

        DebugLineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        foreach (var enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            Vector3 pos = enemy.transform.position;

            // Draw flow field path preview (next several cells)
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.9f, 0.2f, 0.5f));

            // Get enemy's movement type for correct flow field lookup
            MovementType moveType = enemy.Data != null ? enemy.Data.movementType : MovementType.Ground;

            Vector3 current = pos;
            for (int step = 0; step < 10; step++)
            {
                Vector3 flowDir = PathfindingManager.Instance.GetFlowDirection(current, moveType);
                if (flowDir == Vector3.zero) break;

                Vector3 next = current + flowDir * 1f;
                GL.Vertex3(current.x, current.y, 0f);
                GL.Vertex3(next.x, next.y, 0f);
                current = next;
            }
            GL.End();
        }

        GL.PopMatrix();
    }

    private void DrawEnemyTargetLines()
    {
        if (EnemyManager.Instance == null) return;

        DebugLineMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        foreach (var enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            // Get the enemy's current building target via reflection
            var targetField = typeof(Enemy).GetField("currentTarget",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetField == null) continue;

            Building target = targetField.GetValue(enemy) as Building;
            if (target == null || target.IsDestroyed) continue;

            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 0.1f, 0.1f, 0.7f));
            Vector3 enemyPos = enemy.transform.position;
            Vector3 targetPos = target.transform.position;
            GL.Vertex3(enemyPos.x, enemyPos.y, 0f);
            GL.Vertex3(targetPos.x, targetPos.y, 0f);
            GL.End();
        }

        GL.PopMatrix();
    }

    #endregion

    /// <summary>
    /// Public method to toggle debug menu from other scripts
    /// </summary>
    public void ToggleDebugMenu()
    {
        showDebugMenu = !showDebugMenu;
    }

    /// <summary>
    /// Public method to set specific tab
    /// </summary>
    public void SetTab(string tabName)
    {
        switch (tabName.ToLower())
        {
            case "resources": currentTab = DebugTab.Resources; break;
            case "workers": currentTab = DebugTab.Workers; break;
            case "waves": currentTab = DebugTab.Waves; break;
            case "pollution": currentTab = DebugTab.Pollution; break;
            case "enemies": currentTab = DebugTab.Enemies; break;
            case "mission": currentTab = DebugTab.Mission; break;
            case "visuals": currentTab = DebugTab.Visuals; break;
        }
    }
}
