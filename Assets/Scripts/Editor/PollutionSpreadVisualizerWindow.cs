using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PollutionSpreadVisualizerWindow : EditorWindow
{
    private const string DefaultChapterPath = "Assets/Resources/Data/Campaign/Chapters/Chapter1.asset";

    private ChapterData chapterData;
    private TileStateManager tileStateManager;
    private PollutionManager pollutionManager;
    private float pollutionLevel;
    private float maxAdditionalRadius = 40f;
    private bool includeBuildingIntegration = true;
    private bool showAliveCells;
    private bool showGridLines;

    private Color aliveColor = new Color(0.2f, 0.75f, 0.25f, 0.12f);
    private Color witheredColor = new Color(0.95f, 0.42f, 0.08f, 0.35f);
    private Color integratedColor = new Color(0.08f, 0.75f, 1f, 0.42f);
    private Color blockedColor = new Color(0.1f, 0.1f, 0.1f, 0.22f);

    private Tilemap terrainTilemap;
    private Transform pollutionCenter;
    private int buildingIntegrationRadius = 3;
    private BoundsInt bounds;
    private HashSet<Vector2Int> pollutedTiles = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> integratedTiles = new HashSet<Vector2Int>();
    private int affectedTileCount;
    private int blockedTileCount;
    private bool cacheDirty = true;

    private static PollutionSpreadVisualizerWindow window;

    [MenuItem("Tools/Pollution Spread Visualizer")]
    public static void Open()
    {
        window = GetWindow<PollutionSpreadVisualizerWindow>("Pollution Spread");
        window.minSize = new Vector2(330f, 420f);
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnEnable()
    {
        window = this;
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;

        if (chapterData == null)
        {
            chapterData = AssetDatabase.LoadAssetAtPath<ChapterData>(DefaultChapterPath);
            if (chapterData != null)
            {
                pollutionLevel = Mathf.Clamp(pollutionLevel, 0f, chapterData.maxPollution);
            }
        }

        AutoFindSceneReferences();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();

        chapterData = (ChapterData)EditorGUILayout.ObjectField("Chapter Data", chapterData, typeof(ChapterData), false);
        tileStateManager = (TileStateManager)EditorGUILayout.ObjectField("Tile State Manager", tileStateManager, typeof(TileStateManager), true);
        pollutionManager = (PollutionManager)EditorGUILayout.ObjectField("Pollution Manager", pollutionManager, typeof(PollutionManager), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Find Scene References"))
            {
                AutoFindSceneReferences();
                cacheDirty = true;
            }

            if (GUILayout.Button("Frame Center"))
            {
                FramePollutionCenter();
            }
        }

        EditorGUILayout.Space();

        if (chapterData == null)
        {
            EditorGUILayout.HelpBox("Assign ChapterData to calculate spread from chapter-authored values.", MessageType.Warning);
            return;
        }

        float maxPollution = Mathf.Max(1f, chapterData.maxPollution);
        pollutionLevel = EditorGUILayout.Slider("Pollution Level", pollutionLevel, 0f, maxPollution);
        float normalizedPollution = Mathf.Clamp01(pollutionLevel / maxPollution);
        EditorGUILayout.LabelField("Pollution Percent", $"{normalizedPollution * 100f:F1}%");

        maxAdditionalRadius = EditorGUILayout.FloatField("Max Additional Wither Radius", Mathf.Max(0f, maxAdditionalRadius));
        includeBuildingIntegration = EditorGUILayout.Toggle("Current Building Integration", includeBuildingIntegration);
        showAliveCells = EditorGUILayout.Toggle("Show Alive Cells", showAliveCells);
        showGridLines = EditorGUILayout.Toggle("Show Grid Lines", showGridLines);

        EditorGUILayout.Space();
        GUILayout.Label("Overlay Colors", EditorStyles.boldLabel);
        aliveColor = EditorGUILayout.ColorField("Alive", aliveColor);
        witheredColor = EditorGUILayout.ColorField("Withered", witheredColor);
        integratedColor = EditorGUILayout.ColorField("Integrated", integratedColor);
        blockedColor = EditorGUILayout.ColorField("Blocked", blockedColor);

        if (EditorGUI.EndChangeCheck())
        {
            cacheDirty = true;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();

        if (!ResolveSceneData())
        {
            EditorGUILayout.HelpBox("Open a map scene with TileStateManager and terrain tilemap to preview spread.", MessageType.Warning);
            return;
        }

        RebuildCacheIfNeeded();

        float finalRadius = GetFinalWitherRadius();
        EditorGUILayout.LabelField("Final Wither Radius", finalRadius.ToString("F1"));
        EditorGUILayout.LabelField("Starting Integration Radius", chapterData.startingIntegrationRadius.ToString("F1"));
        EditorGUILayout.LabelField("Affected Tiles", affectedTileCount.ToString());
        EditorGUILayout.LabelField("Withered Tiles", Mathf.Max(0, pollutedTiles.Count - integratedTiles.Count).ToString());
        EditorGUILayout.LabelField("Integrated Tiles", integratedTiles.Count.ToString());
        EditorGUILayout.LabelField("Blocked Tiles", blockedTileCount.ToString());

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh Preview"))
        {
            cacheDirty = true;
            RebuildCacheIfNeeded();
            SceneView.RepaintAll();
        }
    }

    private void AutoFindSceneReferences()
    {
        tileStateManager = UnityEngine.Object.FindFirstObjectByType<TileStateManager>();
        pollutionManager = UnityEngine.Object.FindFirstObjectByType<PollutionManager>();

        if (pollutionManager != null)
        {
            SerializedObject serializedPollution = new SerializedObject(pollutionManager);
            SerializedProperty additionalRadius = serializedPollution.FindProperty("maxAdditionalRadius");
            if (additionalRadius != null)
            {
                maxAdditionalRadius = Mathf.Max(0f, additionalRadius.floatValue);
            }
        }

        ResolveSceneData();
    }

    private bool ResolveSceneData()
    {
        if (tileStateManager == null)
        {
            tileStateManager = UnityEngine.Object.FindFirstObjectByType<TileStateManager>();
        }

        if (tileStateManager == null)
        {
            return false;
        }

        SerializedObject serializedTileState = new SerializedObject(tileStateManager);
        terrainTilemap = serializedTileState.FindProperty("terrainTilemap")?.objectReferenceValue as Tilemap;
        pollutionCenter = serializedTileState.FindProperty("pollutionCenter")?.objectReferenceValue as Transform;

        SerializedProperty buildingRadius = serializedTileState.FindProperty("buildingIntegrationRadius");
        if (buildingRadius != null)
        {
            buildingIntegrationRadius = Mathf.Max(0, buildingRadius.intValue);
        }

        if (pollutionCenter == null)
        {
            GameObject commandCenter = GameObject.Find("CommandCenter");
            if (commandCenter == null)
            {
                commandCenter = GameObject.Find("CommandCenter(Clone)");
            }
            pollutionCenter = commandCenter != null ? commandCenter.transform : null;
        }

        if (terrainTilemap == null || pollutionCenter == null)
        {
            return false;
        }

        bounds = terrainTilemap.cellBounds;
        return true;
    }

    private void RebuildCacheIfNeeded()
    {
        if (!cacheDirty || !ResolveSceneData())
        {
            return;
        }

        cacheDirty = false;
        pollutedTiles.Clear();
        integratedTiles.Clear();
        affectedTileCount = 0;
        blockedTileCount = 0;

        int depthCap = Mathf.CeilToInt(GetFinalWitherRadius());
        BuildPollutedSet(depthCap);
        BuildIntegratedSet();
        affectedTileCount = pollutedTiles.Count;
        blockedTileCount = CountBlockedTiles();
    }

    private float GetFinalWitherRadius()
    {
        if (chapterData == null)
        {
            return 0f;
        }

        float normalizedPollution = Mathf.Clamp01(pollutionLevel / Mathf.Max(1f, chapterData.maxPollution));
        return Mathf.Max(0f, chapterData.startingWitherRadius + (maxAdditionalRadius * normalizedPollution));
    }

    private void BuildPollutedSet(int depthCap)
    {
        if (depthCap <= 0)
        {
            return;
        }

        Vector3Int seedCell = terrainTilemap.WorldToCell(pollutionCenter.position);
        Vector2Int seed = new Vector2Int(seedCell.x, seedCell.y);

        if (!InBounds(seed) || IsPollutionBlocker(seed))
        {
            return;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> depthByCell = new Dictionary<Vector2Int, int>();
        queue.Enqueue(seed);
        depthByCell[seed] = 0;

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            int depth = depthByCell[cell];

            if (depth <= depthCap)
            {
                pollutedTiles.Add(cell);
            }

            if (depth >= depthCap)
            {
                continue;
            }

            foreach (Vector2Int direction in Neighbors4)
            {
                Vector2Int next = cell + direction;
                if (!InBounds(next)) continue;
                if (depthByCell.ContainsKey(next)) continue;
                if (IsPollutionBlocker(next)) continue;

                depthByCell[next] = depth + 1;
                queue.Enqueue(next);
            }
        }
    }

    private void BuildIntegratedSet()
    {
        Vector2 center = pollutionCenter.position;

        foreach (Vector2Int cell in pollutedTiles)
        {
            Vector3 world = terrainTilemap.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + new Vector3(0.5f, 0.5f, 0f);
            float chebyshev = Mathf.Max(Mathf.Abs(world.x - center.x), Mathf.Abs(world.y - center.y));

            if (chebyshev < chapterData.startingIntegrationRadius)
            {
                integratedTiles.Add(cell);
            }
        }

        if (!includeBuildingIntegration)
        {
            return;
        }

        foreach (Building building in UnityEngine.Object.FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (building == null || building.BuildingData == null || !building.BuildingData.spreadsIntegration)
            {
                continue;
            }

            int width = building.width > 0 ? building.width : building.BuildingData.width;
            int height = building.height > 0 ? building.height : building.BuildingData.height;
            Vector2Int origin = GetBuildingGridOrigin(building, width, height);

            for (int dx = -buildingIntegrationRadius; dx < width + buildingIntegrationRadius; dx++)
            {
                for (int dy = -buildingIntegrationRadius; dy < height + buildingIntegrationRadius; dy++)
                {
                    Vector2Int cell = origin + new Vector2Int(dx, dy);
                    if (pollutedTiles.Contains(cell))
                    {
                        integratedTiles.Add(cell);
                    }
                }
            }
        }
    }

    private Vector2Int GetBuildingGridOrigin(Building building, int width, int height)
    {
        if (building.gridPosition != Vector2Int.zero)
        {
            return building.gridPosition;
        }

        Vector3Int cell = terrainTilemap.WorldToCell(building.transform.position);
        return new Vector2Int(cell.x - width / 2, cell.y - height / 2);
    }

    private bool InBounds(Vector2Int cell)
    {
        return cell.x >= bounds.xMin && cell.x < bounds.xMax
            && cell.y >= bounds.yMin && cell.y < bounds.yMax
            && terrainTilemap.HasTile(new Vector3Int(cell.x, cell.y, 0));
    }

    private bool IsPollutionBlocker(Vector2Int cell)
    {
        PlanetfallTile tile = terrainTilemap.GetTile<PlanetfallTile>(new Vector3Int(cell.x, cell.y, 0));
        if (tile == null || tile.tileData == null)
        {
            return false;
        }

        return !tile.tileData.pollutionAffected;
    }

    private int CountBlockedTiles()
    {
        int count = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!terrainTilemap.HasTile(new Vector3Int(cell.x, cell.y, 0))) continue;
                if (IsPollutionBlocker(cell))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void FramePollutionCenter()
    {
        if (!ResolveSceneData())
        {
            return;
        }

        SceneView.lastActiveSceneView?.Frame(new Bounds(pollutionCenter.position, Vector3.one * 12f), false);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (window == null || !window.ResolveSceneData())
        {
            return;
        }

        window.RebuildCacheIfNeeded();
        window.DrawPreviewOverlay();
    }

    private void DrawPreviewOverlay()
    {
        float cellSize = terrainTilemap.layoutGrid != null ? terrainTilemap.layoutGrid.cellSize.x : 1f;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        if (showAliveCells)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!InBounds(cell) || pollutedTiles.Contains(cell)) continue;
                    DrawCell(cell, cellSize, aliveColor, showGridLines ? aliveColor : Color.clear);
                }
            }
        }

        foreach (Vector2Int cell in pollutedTiles)
        {
            Color fill = integratedTiles.Contains(cell) ? integratedColor : witheredColor;
            DrawCell(cell, cellSize, fill, showGridLines ? Color.black : Color.clear);
        }

        Vector3 center = pollutionCenter.position;
        Handles.color = Color.white;
        Handles.DrawWireDisc(center, Vector3.forward, cellSize * 0.6f);
        Handles.Label(center + Vector3.up * cellSize, "Pollution Center");
    }

    private void DrawCell(Vector2Int cell, float size, Color fill, Color outline)
    {
        Vector3 center = terrainTilemap.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) + new Vector3(size * 0.5f, size * 0.5f, 0f);
        float half = size * 0.5f;
        Vector3[] vertices =
        {
            center + new Vector3(-half, -half, 0f),
            center + new Vector3(half, -half, 0f),
            center + new Vector3(half, half, 0f),
            center + new Vector3(-half, half, 0f)
        };

        Handles.DrawSolidRectangleWithOutline(vertices, fill, outline);
    }

    private static readonly Vector2Int[] Neighbors4 =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };
}
