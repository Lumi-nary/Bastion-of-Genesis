using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unified manager for decorative vegetation (grass, trees).
/// - Grass: Simple GameObjects (no component overhead, static batched)
/// - Trees: Uses Vegetation component for Y-sorting
/// Both destroyed when pollution spreads to their tile.
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("Vegetation Prefabs")]
    [Tooltip("Grass prefabs for random spawning (picks randomly) - NO Vegetation component needed")]
    [SerializeField] private GameObject[] grassPrefabs;

    [Tooltip("Tree prefab - MUST have Vegetation component for Y-sorting")]
    [SerializeField] private GameObject treePrefab;

    [Header("Grass Spawning")]
    [Tooltip("Chance to spawn grass per tile (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float grassDensity = 0.3f;

    [Tooltip("Random offset range within tile for grass placement")]
    [Range(0f, 0.4f)]
    [SerializeField] private float grassPositionJitter = 0.3f;

    [Header("Performance")]
    [Tooltip("Enable static batching for grass (better FPS, uses more memory)")]
    [SerializeField] private bool useStaticBatching = true;

    [Header("Containers")]
    [Tooltip("Parent transform for grass")]
    [SerializeField] private Transform grassContainer;

    [Tooltip("Parent transform for trees")]
    [SerializeField] private Transform treeContainer;

    // Track grass by grid position - simple GameObjects
    private Dictionary<Vector2Int, List<GameObject>> grassByPosition = new Dictionary<Vector2Int, List<GameObject>>();

    // Track trees separately - with Vegetation component for Y-sorting
    private Dictionary<Vector2Int, Vegetation> trees = new Dictionary<Vector2Int, Vegetation>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create containers if not assigned
        if (grassContainer == null)
        {
            grassContainer = new GameObject("Grass").transform;
            grassContainer.SetParent(transform);
        }

        if (treeContainer == null)
        {
            treeContainer = new GameObject("Trees").transform;
            treeContainer.SetParent(transform);
        }
    }

    private void Start()
    {
        // Subscribe to ground state changes
        if (TileStateManager.Instance != null)
        {
            TileStateManager.Instance.OnGroundStateChanged += HandleGroundStateChanged;
        }

        // Spawn vegetation after GridManager initializes
        SpawnAllVegetation();

        // Apply static batching to grass only (trees need dynamic sorting)
        if (useStaticBatching && grassContainer != null)
        {
            StaticBatchingUtility.Combine(grassContainer.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (TileStateManager.Instance != null)
        {
            TileStateManager.Instance.OnGroundStateChanged -= HandleGroundStateChanged;
        }

        ClearAll();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Handle ground state changes - destroy vegetation when tiles become polluted
    /// </summary>
    private void HandleGroundStateChanged(Vector2Int gridPos, GroundState oldState, GroundState newState)
    {
        // Vegetation dies when tile becomes Polluted (withered)
        if (newState == GroundState.Polluted && oldState == GroundState.Alive)
        {
            DestroyVegetationAt(gridPos);
        }
    }

    /// <summary>
    /// Spawn all vegetation: trees from TreeProperty tiles, grass randomly in Alive tiles
    /// </summary>
    public void SpawnAllVegetation()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("[EnvironmentManager] GridManager not found!");
            return;
        }

        Debug.Log("[EnvironmentManager] Spawning vegetation...");

        // Spawn trees from TreeProperty tiles first
        SpawnTreesFromProperties();

        // Spawn random grass in grass (Alive) tiles
        SpawnRandomGrass();

        int grassCount = 0;
        foreach (var list in grassByPosition.Values)
        {
            grassCount += list.Count;
        }
        Debug.Log($"[EnvironmentManager] Spawned {grassCount} grass, {trees.Count} trees.");
    }

    /// <summary>
    /// Spawn trees based on TreeProperty tiles in the grid
    /// </summary>
    private void SpawnTreesFromProperties()
    {
        if (treePrefab == null)
        {
            Debug.LogWarning("[EnvironmentManager] Tree prefab not assigned, skipping tree spawning.");
            return;
        }

        List<Vector2Int> positions = GridManager.Instance.GetTilesWithProperty<TreeProperty>();
        Debug.Log($"[EnvironmentManager] Found {positions.Count} tiles with TreeProperty");

        foreach (Vector2Int pos in positions)
        {
            SpawnTree(pos);
        }
    }

    /// <summary>
    /// Spawn a tree at grid position (with Vegetation component for Y-sorting)
    /// </summary>
    public Vegetation SpawnTree(Vector2Int gridPos)
    {
        if (trees.ContainsKey(gridPos)) return trees[gridPos];
        if (treePrefab == null) return null;

        // Only spawn in Alive zone
        if (TileStateManager.Instance != null && !TileStateManager.Instance.IsInAliveZone(gridPos))
        {
            return null;
        }

        // Check if there's a tile at this position
        if (!GridManager.Instance.HasTileAt(gridPos))
        {
            return null;
        }

        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(gridPos);
        GameObject treeObj = Instantiate(treePrefab, worldPos, Quaternion.identity, treeContainer);
        treeObj.name = $"Tree_{gridPos.x}_{gridPos.y}";

        // Get Vegetation component for Y-sorting
        Vegetation vegetation = treeObj.GetComponent<Vegetation>();
        if (vegetation == null)
        {
            Debug.LogError("[EnvironmentManager] Tree prefab missing Vegetation component! Y-sorting won't work.");
            Destroy(treeObj);
            return null;
        }

        // Initialize
        vegetation.Initialize(gridPos);

        // Track
        trees[gridPos] = vegetation;

        return vegetation;
    }

    /// <summary>
    /// Spawn random grass in all Alive (grass) tiles
    /// </summary>
    private void SpawnRandomGrass()
    {
        if (grassPrefabs == null || grassPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnvironmentManager] No grass prefabs assigned, skipping grass spawning.");
            return;
        }

        if (GridManager.Instance == null) return;

        int grassCount = 0;
        int skipAlive = 0, skipNoTile = 0, skipTerrain = 0, skipTree = 0, skipResource = 0, skipRandom = 0;
        BoundsInt bounds = GridManager.Instance.GetTilemapBounds();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);

                // Only spawn in Alive zone
                if (TileStateManager.Instance != null && !TileStateManager.Instance.IsInAliveZone(gridPos))
                {
                    skipAlive++;
                    continue;
                }

                // Check if there's a tile at this position
                if (!GridManager.Instance.HasTileAt(gridPos))
                {
                    skipNoTile++;
                    continue;
                }

                // Get tile data for terrain and property checks
                TileData tileData = GridManager.Instance.GetTileData(gridPos);

                // Skip non-grass terrain (sand, water, etc.)
                if (tileData != null && tileData.terrainType != TerrainType.None && tileData.terrainType != TerrainType.Grass)
                {
                    skipTerrain++;
                    continue;
                }

                // Skip if tree here
                if (trees.ContainsKey(gridPos))
                {
                    skipTree++;
                    continue;
                }

                // Skip if resource node (ore mound) here - check both TileData and obstacle registry
                if (GridManager.Instance.IsObstacle(gridPos) || (tileData != null && tileData.HasProperty<ResourceNodeProperty>()))
                {
                    skipResource++;
                    continue;
                }

                // Random chance
                if (Random.value > grassDensity)
                {
                    skipRandom++;
                    continue;
                }

                SpawnGrass(gridPos);
                grassCount++;
            }
        }

        Debug.Log($"[EnvironmentManager] Grass stats - Spawned: {grassCount}, SkipAlive: {skipAlive}, SkipNoTile: {skipNoTile}, SkipTerrain: {skipTerrain}, SkipTree: {skipTree}, SkipResource: {skipResource}, SkipRandom: {skipRandom}");
    }

    /// <summary>
    /// Spawn a grass object at grid position with random offset (no Vegetation component)
    /// </summary>
    private GameObject SpawnGrass(Vector2Int gridPos)
    {
        if (grassPrefabs == null || grassPrefabs.Length == 0) return null;

        // Pick random grass prefab
        GameObject prefab = grassPrefabs[Random.Range(0, grassPrefabs.Length)];
        if (prefab == null) return null;

        // Calculate world position with jitter
        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(gridPos);
        worldPos.x += Random.Range(-grassPositionJitter, grassPositionJitter);
        worldPos.y += Random.Range(-grassPositionJitter, grassPositionJitter);

        GameObject grassObj = Instantiate(prefab, worldPos, Quaternion.identity, grassContainer);
        grassObj.name = $"Grass_{gridPos.x}_{gridPos.y}";

        // Set sorting order to render behind trees
        // Trees use Y-sorting: 200 + (-Y * 100), at Y=100 tree order = -9800
        // Use minimum int value range to guarantee grass is always behind
        SpriteRenderer sr = grassObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = -32000; // Near int16 min, always behind trees
        }

        // Mark as static for batching
        if (useStaticBatching)
        {
            grassObj.isStatic = true;
        }

        // Track
        if (!grassByPosition.TryGetValue(gridPos, out List<GameObject> list))
        {
            list = new List<GameObject>(1);
            grassByPosition[gridPos] = list;
        }
        list.Add(grassObj);

        return grassObj;
    }

    /// <summary>
    /// Destroy all vegetation at a grid position
    /// </summary>
    public void DestroyVegetationAt(Vector2Int gridPos)
    {
        // Destroy grass
        if (grassByPosition.TryGetValue(gridPos, out List<GameObject> list))
        {
            foreach (GameObject obj in list)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            grassByPosition.Remove(gridPos);
        }

        // Destroy tree
        if (trees.TryGetValue(gridPos, out Vegetation tree))
        {
            if (tree != null && tree.gameObject != null)
            {
                Destroy(tree.gameObject);
            }
            trees.Remove(gridPos);
        }
    }

    /// <summary>
    /// Check if there's vegetation at a position
    /// </summary>
    public bool HasVegetationAt(Vector2Int gridPos)
    {
        if (grassByPosition.ContainsKey(gridPos) && grassByPosition[gridPos].Count > 0)
            return true;
        if (trees.ContainsKey(gridPos))
            return true;
        return false;
    }

    /// <summary>
    /// Check if there's a tree at a position
    /// </summary>
    public bool HasTreeAt(Vector2Int gridPos)
    {
        return trees.ContainsKey(gridPos);
    }

    /// <summary>
    /// Get tree at position (if any)
    /// </summary>
    public Vegetation GetTree(Vector2Int gridPos)
    {
        trees.TryGetValue(gridPos, out Vegetation tree);
        return tree;
    }

    /// <summary>
    /// Clear all vegetation
    /// </summary>
    public void ClearAll()
    {
        // Clear grass
        foreach (var list in grassByPosition.Values)
        {
            foreach (var obj in list)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
        grassByPosition.Clear();

        // Clear trees
        foreach (var tree in trees.Values)
        {
            if (tree != null && tree.gameObject != null)
            {
                Destroy(tree.gameObject);
            }
        }
        trees.Clear();
    }

    /// <summary>
    /// Get grass count (for debugging)
    /// </summary>
    public int GetGrassCount()
    {
        int count = 0;
        foreach (var list in grassByPosition.Values)
        {
            count += list.Count;
        }
        return count;
    }

    /// <summary>
    /// Get tree count (for debugging)
    /// </summary>
    public int GetTreeCount()
    {
        return trees.Count;
    }
}
