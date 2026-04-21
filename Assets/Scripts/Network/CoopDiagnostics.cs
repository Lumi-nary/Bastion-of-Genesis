using System.Text;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// In-game diagnostic overlay that shows multiplayer sync status for testing.
/// Press F9 to toggle. Server writes to manager state, clients should see identical values.
/// Verifies: connectivity, scene, resources, buildings, enemies, workers, research, missions.
/// </summary>
public class CoopDiagnostics : MonoBehaviour
{
    [SerializeField] private Key toggleKey = Key.F9;
    [SerializeField] private bool showOnStart = false;

    private bool visible;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private Vector2 scroll;
    private string lastTestResult = "";

    private void Start()
    {
        visible = showOnStart;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (boxStyle == null) InitStyles();

        const int width = 460;
        const int height = 620;
        GUILayout.BeginArea(new Rect(10, 10, width, height), "Co-op Diagnostics (F9 to toggle)", boxStyle);
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label(BuildReport(), labelStyle);

        GUILayout.Space(8);
        if (GUILayout.Button("Run Sync Self-Test"))
        {
            lastTestResult = RunSelfTest();
            Debug.Log(lastTestResult);
        }

        if (!string.IsNullOrEmpty(lastTestResult))
        {
            GUILayout.Space(4);
            GUILayout.Label(lastTestResult, labelStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void InitStyles()
    {
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.padding = new RectOffset(20, 10, 25, 10);
        boxStyle.normal.textColor = Color.white;
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;
        labelStyle.richText = true;
    }

    private string BuildReport()
    {
        var sb = new StringBuilder();

        // --- Connectivity ---
        sb.AppendLine("<b>[1] Connectivity</b>");
        if (CoopBootstrap.Instance == null)
        {
            sb.AppendLine("  CoopBootstrap: <color=#ff6666>MISSING</color>");
        }
        else
        {
            sb.AppendLine($"  IsOnline: {Tag(CoopBootstrap.Instance.IsOnline)}");
            sb.AppendLine($"  IsHost: {Tag(CoopBootstrap.Instance.IsHost)}");
            sb.AppendLine($"  IsClientOnly: {Tag(CoopBootstrap.Instance.IsClientOnly)}");
            sb.AppendLine($"  IsServer: {Tag(CoopBootstrap.Instance.IsServer)}");
            sb.AppendLine($"  PlayerCount: {CoopBootstrap.Instance.PlayerCount}/{CoopBootstrap.Instance.MaxPlayers}");
            sb.AppendLine($"  LocalIP:Port: {CoopBootstrap.Instance.LocalIP}:{CoopBootstrap.Instance.Port}");
        }

        bool coopMgr = CoopManager.Instance != null;
        sb.AppendLine($"  CoopManager spawned: {Tag(coopMgr)}");

        // --- Scene ---
        sb.AppendLine();
        sb.AppendLine("<b>[2] Scene Sync</b>");
        sb.AppendLine($"  Active scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"  Scenes loaded: {UnityEngine.SceneManagement.SceneManager.sceneCount}");

        // --- Resources ---
        sb.AppendLine();
        sb.AppendLine("<b>[3] Resources</b>");
        if (ResourceManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else
        {
            var res = ResourceManager.Instance.GetAllResources();
            sb.AppendLine($"  Types: {res.Count}");
            int shown = 0;
            foreach (var kvp in res)
            {
                if (shown >= 4) { sb.AppendLine("  ..."); break; }
                int cap = ResourceManager.Instance.GetResourceCapacity(kvp.Key);
                sb.AppendLine($"  {kvp.Key.ResourceName}: {kvp.Value}/{cap}");
                shown++;
            }
        }

        // --- Buildings ---
        sb.AppendLine();
        sb.AppendLine("<b>[4] Buildings</b>");
        if (BuildingManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else sb.AppendLine($"  Count: {BuildingManager.Instance.AllBuildings.Count}");

        // --- Workers ---
        sb.AppendLine();
        sb.AppendLine("<b>[5] Workers</b>");
        if (WorkerManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else
        {
            var workers = WorkerManager.Instance.AvailableWorkers;
            sb.AppendLine($"  Types: {workers.Count}");
            int shown = 0;
            foreach (var kvp in workers)
            {
                if (shown >= 3) break;
                int cap = WorkerManager.Instance.GetWorkerCapacity(kvp.Key);
                sb.AppendLine($"  {kvp.Key.workerName}: {kvp.Value}/{cap}");
                shown++;
            }
        }

        // --- Enemies ---
        sb.AppendLine();
        sb.AppendLine("<b>[6] Enemies</b>");
        if (EnemyManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else sb.AppendLine($"  Active: {EnemyManager.Instance.ActiveEnemyCount}, Wave: {EnemyManager.Instance.CurrentWave}, Killed: {EnemyManager.Instance.EnemiesKilled}");

        // --- Research ---
        sb.AppendLine();
        sb.AppendLine("<b>[7] Research</b>");
        if (ResearchManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else
        {
            var current = ResearchManager.Instance.CurrentResearch;
            sb.AppendLine($"  Current: {(current != null ? current.techName : "none")}");
            sb.AppendLine($"  Progress: {ResearchManager.Instance.CurrentResearchProgress:P0}");
        }

        // --- Missions ---
        sb.AppendLine();
        sb.AppendLine("<b>[8] Missions</b>");
        if (MissionChapterManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else
        {
            var ch = MissionChapterManager.Instance.CurrentChapter;
            var m = MissionChapterManager.Instance.CurrentMission;
            sb.AppendLine($"  Chapter: {(ch != null ? ch.chapterName : "none")}");
            sb.AppendLine($"  Mission: {(m != null ? m.missionName : "none")}");
            if (m != null && m.objectives != null)
            {
                int done = 0;
                foreach (var obj in m.objectives) if (obj.isCompleted) done++;
                sb.AppendLine($"  Objectives: {done}/{m.objectives.Count}");
            }
        }

        // --- Pollution ---
        sb.AppendLine();
        sb.AppendLine("<b>[9] Pollution</b>");
        if (PollutionManager.Instance == null) sb.AppendLine("  <color=#ff6666>MISSING</color>");
        else sb.AppendLine($"  Level: {PollutionManager.Instance.CurrentPollution:F1}/{PollutionManager.Instance.MaxPollution:F0}  Tier: {PollutionManager.Instance.CurrentTier}");

        return sb.ToString();
    }

    /// <summary>
    /// Runs a self-test that checks all sync systems are wired correctly.
    /// Compare the output between host and client — values should match after sync.
    /// </summary>
    private string RunSelfTest()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== COOP SYNC SELF-TEST ===");

        int passed = 0, failed = 0;

        // 1. Connectivity
        bool connectivity = CoopBootstrap.Instance != null && CoopManager.Instance != null && InstanceFinder.IsServerStarted;
        bool connectivityClient = CoopBootstrap.Instance != null && CoopManager.Instance != null && InstanceFinder.IsClientStarted;
        sb.AppendLine($"[1] Connectivity: {Pass(connectivity || connectivityClient, ref passed, ref failed)}");

        // 2. Scene
        bool scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MenuScene";
        sb.AppendLine($"[2] Gameplay scene loaded: {Pass(scene, ref passed, ref failed)}");

        // 3. Resources
        bool resources = ResourceManager.Instance != null && ResourceManager.Instance.GetAllResources().Count > 0;
        sb.AppendLine($"[3] Resources populated: {Pass(resources, ref passed, ref failed)}");

        // 4. Buildings
        bool buildings = BuildingManager.Instance != null;
        sb.AppendLine($"[4] BuildingManager ready: {Pass(buildings, ref passed, ref failed)}");

        // 5. Workers
        bool workers = WorkerManager.Instance != null && WorkerManager.Instance.AvailableWorkers.Count > 0;
        sb.AppendLine($"[5] Workers populated: {Pass(workers, ref passed, ref failed)}");

        // 6. Enemies
        bool enemies = EnemyManager.Instance != null;
        sb.AppendLine($"[6] EnemyManager ready: {Pass(enemies, ref passed, ref failed)}");

        // 7. Research
        bool research = ResearchManager.Instance != null;
        sb.AppendLine($"[7] ResearchManager ready: {Pass(research, ref passed, ref failed)}");

        // 8. Missions
        bool missions = MissionChapterManager.Instance != null && MissionChapterManager.Instance.CurrentChapter != null;
        sb.AppendLine($"[8] Mission/Chapter loaded: {Pass(missions, ref passed, ref failed)}");

        sb.AppendLine();
        sb.AppendLine($"Result: {passed} passed, {failed} failed");
        return sb.ToString();
    }

    private string Pass(bool ok, ref int passed, ref int failed)
    {
        if (ok) { passed++; return "<color=#66ff66>PASS</color>"; }
        failed++; return "<color=#ff6666>FAIL</color>";
    }

    private string Tag(bool b) => b ? "<color=#66ff66>true</color>" : "<color=#ff6666>false</color>";
}
