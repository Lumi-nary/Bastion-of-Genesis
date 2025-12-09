using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window for visualizing pathfinding flow field data
/// Shows cost field, integration field, and flow directions
/// </summary>
public class PathfindingVisualizer : EditorWindow
{
    private enum VisualizationType
    {
        CostField,
        IntegrationField,
        FlowDirections,
        All
    }

    private VisualizationType visualizationType = VisualizationType.All;
    private bool showCostField = true;
    private bool showIntegrationField = false;
    private bool showFlowDirections = true;
    private bool showGridLines = true;
    private bool showOnlyAroundTarget = true;
    private int viewRadius = 15;

    private Color walkableColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    private Color impassableColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
    private Color targetColor = new Color(1f, 1f, 0f, 0.8f);
    private Color arrowColor = new Color(0f, 0.5f, 1f, 0.8f);

    private static PathfindingVisualizer window;

    [MenuItem("Tools/Pathfinding Visualizer")]
    public static void ShowWindow()
    {
        window = GetWindow<PathfindingVisualizer>("Pathfinding Visualizer");
        window.minSize = new Vector2(300, 400);
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Pathfinding Visualization", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (PathfindingManager.Instance == null)
        {
            EditorGUILayout.HelpBox("PathfindingManager not found. Enter Play mode to visualize.", MessageType.Warning);
            return;
        }

        if (!PathfindingManager.Instance.HasFlowTarget)
        {
            EditorGUILayout.HelpBox("No flow target set. Place a Command Center to see flow field.", MessageType.Info);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Display Options", EditorStyles.boldLabel);

        showCostField = EditorGUILayout.Toggle("Show Cost Field", showCostField);
        showIntegrationField = EditorGUILayout.Toggle("Show Integration Field", showIntegrationField);
        showFlowDirections = EditorGUILayout.Toggle("Show Flow Directions", showFlowDirections);
        showGridLines = EditorGUILayout.Toggle("Show Grid Lines", showGridLines);

        EditorGUILayout.Space();
        showOnlyAroundTarget = EditorGUILayout.Toggle("Limit View to Target Area", showOnlyAroundTarget);
        if (showOnlyAroundTarget)
        {
            viewRadius = EditorGUILayout.IntSlider("View Radius", viewRadius, 5, 50);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Colors", EditorStyles.boldLabel);
        walkableColor = EditorGUILayout.ColorField("Walkable", walkableColor);
        impassableColor = EditorGUILayout.ColorField("Impassable", impassableColor);
        targetColor = EditorGUILayout.ColorField("Target", targetColor);
        arrowColor = EditorGUILayout.ColorField("Flow Arrows", arrowColor);

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh View"))
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Debug Info", EditorStyles.boldLabel);

        if (PathfindingManager.Instance != null && PathfindingManager.Instance.HasFlowTarget)
        {
            var debugInfo = PathfindingManager.Instance.GetDebugInfo();
            EditorGUILayout.LabelField("Target Grid Pos:", debugInfo.targetPosition.ToString());
            EditorGUILayout.LabelField("Flow Field Size:", $"{debugInfo.fieldWidth}x{debugInfo.fieldHeight}");
            EditorGUILayout.LabelField("Grid Offset:", debugInfo.gridOffset.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reachable Cells:", debugInfo.reachableCells.ToString());
            EditorGUILayout.LabelField("Impassable Cells:", debugInfo.impassableCells.ToString());
        }

        // Force repaint
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (window == null || PathfindingManager.Instance == null || GridManager.Instance == null)
            return;

        if (!PathfindingManager.Instance.HasFlowTarget)
            return;

        var debugInfo = PathfindingManager.Instance.GetDebugInfo();
        float cellSize = GridManager.Instance.GetCellSize();

        Vector2Int targetPos = debugInfo.targetPosition;
        int minX = 0, maxX = debugInfo.fieldWidth;
        int minY = 0, maxY = debugInfo.fieldHeight;

        // Limit view area if enabled
        if (window.showOnlyAroundTarget)
        {
            Vector2Int localTarget = targetPos - debugInfo.gridOffset;
            minX = Mathf.Max(0, localTarget.x - window.viewRadius);
            maxX = Mathf.Min(debugInfo.fieldWidth, localTarget.x + window.viewRadius);
            minY = Mathf.Max(0, localTarget.y - window.viewRadius);
            maxY = Mathf.Min(debugInfo.fieldHeight, localTarget.y + window.viewRadius);
        }

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        // Draw cells
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y) + debugInfo.gridOffset;
                Vector3 worldPos = GridManager.Instance.GridToWorldPosition(gridPos);

                float cost = debugInfo.GetCost(x, y);
                float integration = debugInfo.GetIntegration(x, y);
                Vector2 flowDir = debugInfo.GetFlowDirection(x, y);

                // Draw cost field (cell background)
                if (window.showCostField)
                {
                    Color cellColor;
                    if (cost >= float.MaxValue * 0.5f)
                    {
                        cellColor = window.impassableColor;
                    }
                    else
                    {
                        // Gradient from green (low cost) to yellow (high cost)
                        float t = Mathf.Clamp01(cost / 10f);
                        cellColor = Color.Lerp(window.walkableColor, new Color(1f, 1f, 0f, 0.3f), t);
                    }

                    DrawCell(worldPos, cellSize, cellColor);
                }

                // Draw integration field values
                if (window.showIntegrationField && integration < float.MaxValue * 0.5f)
                {
                    // Color based on integration value (distance to target)
                    float maxDist = window.viewRadius * 2f;
                    float t = Mathf.Clamp01(integration / maxDist);
                    Color intColor = Color.Lerp(Color.green, Color.red, t);
                    intColor.a = 0.3f;

                    DrawCell(worldPos, cellSize * 0.8f, intColor);

                    // Draw integration value text
                    Handles.Label(worldPos + Vector3.up * 0.1f, integration.ToString("F1"),
                        new GUIStyle { fontSize = 8, normal = { textColor = Color.white } });
                }

                // Draw flow directions
                if (window.showFlowDirections && flowDir != Vector2.zero)
                {
                    DrawArrow(worldPos, flowDir, cellSize * 0.4f, window.arrowColor);
                }

                // Draw grid lines
                if (window.showGridLines)
                {
                    DrawCellOutline(worldPos, cellSize, new Color(0.5f, 0.5f, 0.5f, 0.2f));
                }
            }
        }

        // Highlight target
        Vector3 targetWorldPos = GridManager.Instance.GridToWorldPosition(targetPos);
        DrawCell(targetWorldPos, cellSize, window.targetColor);
        Handles.Label(targetWorldPos + Vector3.up * 0.3f, "TARGET",
            new GUIStyle { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } });

        // Draw enemies
        if (EnemyManager.Instance != null)
        {
            foreach (var enemy in EnemyManager.Instance.ActiveEnemies)
            {
                if (enemy == null) continue;

                Vector3 enemyPos = enemy.transform.position;
                Vector2Int enemyGridPos = GridManager.Instance.WorldToGridPosition(enemyPos);

                // Draw enemy position marker
                Handles.color = Color.red;
                Handles.DrawSolidDisc(enemyPos, Vector3.forward, 0.2f);

                // Draw enemy grid cell
                Handles.color = new Color(1f, 0f, 0f, 0.3f);
                Vector3 enemyCellCenter = GridManager.Instance.GridToWorldPosition(enemyGridPos);
                DrawCellOutline(enemyCellCenter, cellSize, Color.red);

                // Show enemy info
                Handles.Label(enemyPos + Vector3.up * 0.5f,
                    $"Grid: {enemyGridPos}\nPos: ({enemyPos.x:F2}, {enemyPos.y:F2})",
                    new GUIStyle { fontSize = 9, normal = { textColor = Color.white } });
            }
        }
    }

    private static void DrawCell(Vector3 center, float size, Color color)
    {
        Handles.color = color;
        float halfSize = size * 0.5f;
        Vector3[] verts = new Vector3[]
        {
            center + new Vector3(-halfSize, -halfSize, 0),
            center + new Vector3(halfSize, -halfSize, 0),
            center + new Vector3(halfSize, halfSize, 0),
            center + new Vector3(-halfSize, halfSize, 0)
        };
        Handles.DrawSolidRectangleWithOutline(verts, color, Color.clear);
    }

    private static void DrawCellOutline(Vector3 center, float size, Color color)
    {
        Handles.color = color;
        float halfSize = size * 0.5f;
        Vector3[] verts = new Vector3[]
        {
            center + new Vector3(-halfSize, -halfSize, 0),
            center + new Vector3(halfSize, -halfSize, 0),
            center + new Vector3(halfSize, halfSize, 0),
            center + new Vector3(-halfSize, halfSize, 0),
            center + new Vector3(-halfSize, -halfSize, 0)
        };
        Handles.DrawPolyLine(verts);
    }

    private static void DrawArrow(Vector3 start, Vector2 direction, float length, Color color)
    {
        Handles.color = color;
        Vector3 dir3D = new Vector3(direction.x, direction.y, 0).normalized;
        Vector3 end = start + dir3D * length;

        // Arrow shaft
        Handles.DrawLine(start, end);

        // Arrow head
        Vector3 right = Quaternion.Euler(0, 0, 150) * dir3D * length * 0.3f;
        Vector3 left = Quaternion.Euler(0, 0, -150) * dir3D * length * 0.3f;
        Handles.DrawLine(end, end + right);
        Handles.DrawLine(end, end + left);
    }
}
