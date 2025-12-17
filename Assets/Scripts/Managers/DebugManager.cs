using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
    private enum DebugTab { Resources, Workers, Waves, Pollution, Enemies }
    private DebugTab currentTab = DebugTab.Resources;

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
        }

        // Make the window draggable by its title bar (top 25 pixels)
        GUI.DragWindow(new Rect(0, 0, windowRect.width, 25));
    }

    private void DrawTabs()
    {
        float tabWidth = 75f;
        float tabY = 25f;
        float tabX = 10f;

        string[] tabNames = { "Resources", "Workers", "Waves", "Pollution", "Enemies" };
        DebugTab[] tabs = { DebugTab.Resources, DebugTab.Workers, DebugTab.Waves, DebugTab.Pollution, DebugTab.Enemies };

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
        }
    }
}
