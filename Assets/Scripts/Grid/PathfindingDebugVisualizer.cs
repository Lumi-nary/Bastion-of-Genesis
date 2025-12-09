using UnityEngine;

/// <summary>
/// Runtime debug visualizer for pathfinding flow field
/// Attach to any GameObject in the scene and enable to see flow field visualization
/// Uses OnDrawGizmos for reliable Scene view rendering
/// </summary>
public class PathfindingDebugVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showVisualization = true;
    [SerializeField] private bool showCostField = true;
    [SerializeField] private bool showFlowDirections = true;
    [SerializeField] private bool showIntegrationValues = false;
    [SerializeField] private bool showGridLines = true;

    [Header("View Settings")]
    [SerializeField] private bool limitToArea = true;
    [SerializeField] private int viewRadius = 15;

    [Header("Colors")]
    [SerializeField] private Color walkableColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private Color impassableColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color targetColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private Color arrowColor = new Color(0f, 0.5f, 1f, 1f);
    [SerializeField] private Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    private void OnDrawGizmos()
    {
        if (!showVisualization) return;
        if (!Application.isPlaying) return;
        if (PathfindingManager.Instance == null || GridManager.Instance == null) return;
        if (!PathfindingManager.Instance.HasFlowTarget) return;

        var debugInfo = PathfindingManager.Instance.GetDebugInfo();
        float cellSize = GridManager.Instance.GetCellSize();

        Vector2Int targetPos = debugInfo.targetPosition;
        int minX = 0, maxX = debugInfo.fieldWidth;
        int minY = 0, maxY = debugInfo.fieldHeight;

        // Limit view area if enabled
        if (limitToArea)
        {
            Vector2Int localTarget = targetPos - debugInfo.gridOffset;
            minX = Mathf.Max(0, localTarget.x - viewRadius);
            maxX = Mathf.Min(debugInfo.fieldWidth, localTarget.x + viewRadius);
            minY = Mathf.Max(0, localTarget.y - viewRadius);
            maxY = Mathf.Min(debugInfo.fieldHeight, localTarget.y + viewRadius);
        }

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
                if (showCostField)
                {
                    Color cellColor;
                    if (cost >= float.MaxValue * 0.5f)
                    {
                        cellColor = impassableColor;
                    }
                    else
                    {
                        // Gradient from green (low cost) to yellow (high cost)
                        float t = Mathf.Clamp01(cost / 10f);
                        cellColor = Color.Lerp(walkableColor, new Color(1f, 1f, 0f, 0.3f), t);
                    }

                    DrawCell(worldPos, cellSize * 0.95f, cellColor);
                }

                // Draw grid lines
                if (showGridLines)
                {
                    DrawCellOutline(worldPos, cellSize, gridLineColor);
                }

                // Draw flow directions
                if (showFlowDirections && flowDir != Vector2.zero)
                {
                    DrawArrow(worldPos, flowDir, cellSize * 0.4f, arrowColor);
                }
            }
        }

        // Highlight target
        Vector3 targetWorldPos = GridManager.Instance.GridToWorldPosition(targetPos);
        DrawCell(targetWorldPos, cellSize, targetColor);

        // Draw enemies
        if (EnemyManager.Instance != null)
        {
            foreach (var enemy in EnemyManager.Instance.ActiveEnemies)
            {
                if (enemy == null) continue;

                Vector3 enemyPos = enemy.transform.position;

                // Draw enemy position marker
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(enemyPos, 0.2f);

                // Draw line to enemy's current target cell
                Gizmos.color = Color.magenta;
                Vector2Int enemyGridPos = GridManager.Instance.WorldToGridPosition(enemyPos);
                Vector3 enemyCellCenter = GridManager.Instance.GridToWorldPosition(enemyGridPos);
                Gizmos.DrawLine(enemyPos, enemyCellCenter);
            }
        }
    }

    private void DrawCell(Vector3 center, float size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawCube(center, new Vector3(size, size, 0.01f));
    }

    private void DrawCellOutline(Vector3 center, float size, Color color)
    {
        Gizmos.color = color;
        float halfSize = size * 0.5f;

        Vector3 bl = center + new Vector3(-halfSize, -halfSize, 0);
        Vector3 br = center + new Vector3(halfSize, -halfSize, 0);
        Vector3 tr = center + new Vector3(halfSize, halfSize, 0);
        Vector3 tl = center + new Vector3(-halfSize, halfSize, 0);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    private void DrawArrow(Vector3 start, Vector2 direction, float length, Color color)
    {
        Gizmos.color = color;
        Vector3 dir3D = new Vector3(direction.x, direction.y, 0).normalized;
        Vector3 end = start + dir3D * length;

        // Arrow shaft
        Gizmos.DrawLine(start, end);

        // Arrow head
        Vector3 right = Quaternion.Euler(0, 0, 150) * dir3D * length * 0.3f;
        Vector3 left = Quaternion.Euler(0, 0, -150) * dir3D * length * 0.3f;
        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }

    // Debug info display in Inspector
    [Header("Debug Info (Runtime)")]
    [SerializeField] private string targetPosition = "N/A";
    [SerializeField] private int reachableCells = 0;
    [SerializeField] private int impassableCells = 0;
    [SerializeField] private int flowFieldVersion = 0;

    [Header("Manual Controls")]
    [SerializeField] private bool forceRecalculate = false;
    [SerializeField] private bool continuousRepaint = true;

    // Track last values to detect changes
    private int lastFlowFieldVersion = -1;

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (PathfindingManager.Instance == null) return;

        // Manual recalculation trigger
        if (forceRecalculate)
        {
            forceRecalculate = false;
            PathfindingManager.Instance.RequestRecalculation();
            Debug.Log("[PathfindingVisualizer] Forced flow field recalculation");
        }

        // Always repaint if continuous repaint is enabled
#if UNITY_EDITOR
        if (continuousRepaint)
        {
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        if (!PathfindingManager.Instance.HasFlowTarget) return;

        var debugInfo = PathfindingManager.Instance.GetDebugInfo();
        targetPosition = debugInfo.targetPosition.ToString();
        reachableCells = debugInfo.reachableCells;
        impassableCells = debugInfo.impassableCells;
        flowFieldVersion = PathfindingManager.Instance.FlowFieldVersion;

        // Detect when flow field has changed
        if (flowFieldVersion != lastFlowFieldVersion)
        {
            lastFlowFieldVersion = flowFieldVersion;
            Debug.Log($"[PathfindingVisualizer] Flow field updated to version {flowFieldVersion}");
#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }
    }

    // Force continuous repaint in editor for smooth visualization
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }
}
