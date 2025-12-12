using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Editor visualization for TreeProperty tiles.
/// Shows tree icons/indicators on tiles that have TreeProperty assigned.
/// </summary>
[InitializeOnLoad]
public static class TreePropertyVisualizer
{
    private static bool isEnabled = false;
    private static float iconSize = 0.6f;
    private static Color treeColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
    private static Color outlineColor = new Color(0.1f, 0.3f, 0.1f, 1f);

    // Cached data
    private static List<Vector3> treePositions = new List<Vector3>();
    private static Tilemap cachedTilemap;
    private static double lastRefreshTime;
    private const double REFRESH_INTERVAL = 0.5; // Refresh every 0.5 seconds

    static TreePropertyVisualizer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/Tree Visualizer/Toggle Display")]
    public static void ToggleDisplay()
    {
        isEnabled = !isEnabled;
        SceneView.RepaintAll();
        Debug.Log($"[TreePropertyVisualizer] Display {(isEnabled ? "enabled" : "disabled")}");
    }

    [MenuItem("Tools/Tree Visualizer/Refresh Now")]
    public static void RefreshNow()
    {
        RefreshTreePositions();
        SceneView.RepaintAll();
        Debug.Log($"[TreePropertyVisualizer] Found {treePositions.Count} tree tiles");
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!isEnabled) return;

        // Periodic refresh
        if (EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
        {
            RefreshTreePositions();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        // Draw tree indicators
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        foreach (Vector3 pos in treePositions)
        {
            DrawTreeIndicator(pos);
        }

        // Draw count label
        if (treePositions.Count > 0)
        {
            Handles.BeginGUI();
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.fontSize = 12;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 10, 200, 25), $"Tree Tiles: {treePositions.Count}", style);
            Handles.EndGUI();
        }
    }

    private static void RefreshTreePositions()
    {
        treePositions.Clear();

        // Find the terrain tilemap
        if (cachedTilemap == null)
        {
            // Try to find GridManager's tilemap
            GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                // Use reflection to get the private tilemap field
                var field = typeof(GridManager).GetField("terrainTilemap",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    cachedTilemap = field.GetValue(gridManager) as Tilemap;
                }
            }

            // Fallback: find any tilemap with PlanetfallTiles
            if (cachedTilemap == null)
            {
                Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                foreach (var tm in tilemaps)
                {
                    BoundsInt bounds = tm.cellBounds;
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        for (int y = bounds.yMin; y < bounds.yMax; y++)
                        {
                            Vector3Int tilePos = new Vector3Int(x, y, 0);
                            if (tm.GetTile<PlanetfallTile>(tilePos) != null)
                            {
                                cachedTilemap = tm;
                                break;
                            }
                        }
                        if (cachedTilemap != null) break;
                    }
                    if (cachedTilemap != null) break;
                }
            }
        }

        if (cachedTilemap == null) return;

        // Scan tilemap for TreeProperty tiles
        BoundsInt bounds2 = cachedTilemap.cellBounds;
        for (int x = bounds2.xMin; x < bounds2.xMax; x++)
        {
            for (int y = bounds2.yMin; y < bounds2.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                PlanetfallTile tile = cachedTilemap.GetTile<PlanetfallTile>(tilePos);

                if (tile != null && tile.tileData != null)
                {
                    // Check if tile has TreeProperty
                    TreeProperty treeProp = tile.tileData.GetProperty<TreeProperty>();
                    if (treeProp != null)
                    {
                        Vector3 worldPos = cachedTilemap.GetCellCenterWorld(tilePos);
                        treePositions.Add(worldPos);
                    }
                }
            }
        }
    }

    private static void DrawTreeIndicator(Vector3 position)
    {
        // Draw filled circle
        Handles.color = treeColor;
        Handles.DrawSolidDisc(position, Vector3.forward, iconSize * 0.4f);

        // Draw outline
        Handles.color = outlineColor;
        Handles.DrawWireDisc(position, Vector3.forward, iconSize * 0.4f);

        // Draw tree shape (simple triangle for trunk + circle for foliage)
        Vector3 trunkBase = position + Vector3.down * 0.2f;
        Vector3 trunkTop = position + Vector3.up * 0.1f;

        // Trunk
        Handles.color = new Color(0.4f, 0.25f, 0.1f, 0.9f);
        Handles.DrawLine(trunkBase + Vector3.left * 0.05f, trunkTop + Vector3.left * 0.03f);
        Handles.DrawLine(trunkBase + Vector3.right * 0.05f, trunkTop + Vector3.right * 0.03f);
        Handles.DrawLine(trunkBase + Vector3.left * 0.05f, trunkBase + Vector3.right * 0.05f);

        // Foliage (triangle)
        Handles.color = treeColor;
        Vector3 foliageTop = position + Vector3.up * 0.35f;
        Vector3 foliageLeft = position + new Vector3(-0.2f, -0.05f, 0);
        Vector3 foliageRight = position + new Vector3(0.2f, -0.05f, 0);

        Handles.DrawAAConvexPolygon(foliageTop, foliageLeft, foliageRight);

        // Label
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 10;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        Handles.Label(position + Vector3.down * 0.5f, "T", labelStyle);
    }

    // Clear cache when entering/exiting play mode
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.playModeStateChanged += (state) =>
        {
            cachedTilemap = null;
            treePositions.Clear();
        };
    }
}
