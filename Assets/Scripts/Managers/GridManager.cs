using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private Grid sceneGrid;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    private float cellSize;

    [Header("Tilemap References")]
    [Tooltip("Gameplay layer - contains PlanetfallTiles with TileData")]
    [SerializeField] private Tilemap terrainTilemap;

    [Tooltip("Visual background layer - decorative only")]
    [SerializeField] private Tilemap groundTilemap;

    [Header("Ore Mound Settings")]
    [Tooltip("How often to check for mound discoveries (seconds)")]
    [SerializeField] private float moundDiscoveryCheckInterval = 5f;
    private float moundDiscoveryCheckTimer = 0f;

    // Building placement tracking
    private Dictionary<Vector2Int, Building> placedBuildings = new Dictionary<Vector2Int, Building>();

    // Obstacles that block pathfinding (ore mounds, rocks, etc.)
    private HashSet<Vector2Int> obstacles = new HashSet<Vector2Int>();

    // Tiles that need OnUpdate called (trees checking pollution, etc.)
    private HashSet<Vector2Int> activeTiles = new HashSet<Vector2Int>();

    // Ore mound tracking (merged from OreMoundManager)
    private List<OreMound> allMounds = new List<OreMound>();
    private List<OreMound> discoveredMounds = new List<OreMound>();
    private List<OreMound> undiscoveredMounds = new List<OreMound>();

    // Ore mound events
    public delegate void MoundDiscoveredEvent(OreMound mound);
    public event MoundDiscoveredEvent OnMoundDiscovered;

    // Public ore mound properties
    public IReadOnlyList<OreMound> AllMounds => allMounds;
    public IReadOnlyList<OreMound> DiscoveredMounds => discoveredMounds;
    public IReadOnlyList<OreMound> UndiscoveredMounds => undiscoveredMounds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (sceneGrid != null)
        {
            cellSize = sceneGrid.cellSize.x; // Assuming square cells
        }
        else
        {
            Debug.LogError("GridManager Error: Scene Grid is not assigned!");
            cellSize = 1f; // Fallback
        }

        // Subscribe to ore mound events
        OreMound.OnMoundDiscovered += HandleMoundDiscovered;
    }

    private void OnDestroy()
    {
        // Unsubscribe from ore mound events
        OreMound.OnMoundDiscovered -= HandleMoundDiscovered;
    }

    private void Update()
    {
        // Update active tiles (trees, hazards, etc.)
        foreach (var tilePos in activeTiles)
        {
            TileData tileData = GetTileData(tilePos);
            if (tileData != null)
            {
                tileData.UpdateProperties(tilePos);
            }
        }

        // Check for mound discoveries based on pollution
        UpdateMoundDiscovery();
    }

    private void UpdateMoundDiscovery()
    {
        moundDiscoveryCheckTimer += Time.deltaTime;
        if (moundDiscoveryCheckTimer >= moundDiscoveryCheckInterval)
        {
            moundDiscoveryCheckTimer = 0f;
            CheckForMoundDiscoveries();
        }
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    /// <summary>
    /// Get the bounds of the terrain tilemap in grid coordinates
    /// </summary>
    public BoundsInt GetTilemapBounds()
    {
        if (terrainTilemap == null) return new BoundsInt();
        return terrainTilemap.cellBounds;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize + cellSize / 2f, gridPosition.y * cellSize + cellSize / 2f, 0) + gridOrigin;
    }

    public bool IsCellOccupied(Vector2Int cellPosition)
    {
        return placedBuildings.ContainsKey(cellPosition);
    }

    /// <summary>
    /// Get TileData at grid position
    /// </summary>
    public TileData GetTileData(Vector2Int gridPos)
    {
        if (terrainTilemap == null) return null;

        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);
        PlanetfallTile tile = terrainTilemap.GetTile<PlanetfallTile>(tilePos);

        return tile?.tileData;
    }

    /// <summary>
    /// Check if any tile exists at grid position (checks ground tilemap)
    /// </summary>
    public bool HasTileAt(Vector2Int gridPos)
    {
        if (groundTilemap == null) return false;

        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);
        return groundTilemap.HasTile(tilePos);
    }

    /// <summary>
    /// Check if tile is buildable
    /// </summary>
    public bool IsBuildable(Vector2Int gridPos)
    {
        // Check if already occupied by another building
        if (IsCellOccupied(gridPos))
        {
            return false;
        }

        // Check integration zone using world position
        if (TileStateManager.Instance != null)
        {
            Vector3 worldPos = GridToWorldPosition(gridPos);
            if (!TileStateManager.Instance.IsPositionBuildable(worldPos))
            {
                return false;
            }
        }

        // Check TileData for additional restrictions (water, etc.)
        TileData tileData = GetTileData(gridPos);

        // No tile data = no additional restrictions
        if (tileData == null)
        {
            return true;
        }

        // Tile data exists, check if it allows building
        return tileData.IsBuildable;
    }

    /// <summary>
    /// Check if tile is walkable for pathfinding
    /// Default: walkable (ground tiles without TileData are walkable)
    /// Only blocked if TileData explicitly sets IsWalkable = false (water, walls, etc.)
    /// </summary>
    public bool IsWalkable(Vector2Int gridPos)
    {
        // Check if blocked by a building
        if (IsCellOccupied(gridPos))
        {
            return false;
        }

        // Check if blocked by an obstacle (ore mounds, rocks, etc.)
        if (obstacles.Contains(gridPos))
        {
            return false;
        }

        TileData tileData = GetTileData(gridPos);

        // No TileData = basic ground tile = walkable
        if (tileData == null) return true;

        return tileData.IsWalkable;
    }

    /// <summary>
    /// Register an obstacle that blocks pathfinding (ore mounds, rocks, etc.)
    /// </summary>
    public void RegisterObstacle(Vector2Int gridPos)
    {
        obstacles.Add(gridPos);

        // Trigger pathfinding recalculation
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }

    /// <summary>
    /// Get obstacle count
    /// </summary>
    public int GetObstacleCount()
    {
        return obstacles.Count;
    }

    /// <summary>
    /// Check if a cell is an obstacle
    /// </summary>
    public bool IsObstacle(Vector2Int gridPos)
    {
        return obstacles.Contains(gridPos);
    }

    /// <summary>
    /// Unregister an obstacle (when destroyed or removed)
    /// </summary>
    public void UnregisterObstacle(Vector2Int gridPos)
    {
        obstacles.Remove(gridPos);

        // Trigger pathfinding recalculation
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }

    /// <summary>
    /// Get movement cost for pathfinding (1.0 = normal, higher = slower, MaxValue = impassable)
    /// Default: 1.0 (ground tiles without TileData have normal movement cost)
    /// </summary>
    public float GetMovementCost(Vector2Int gridPos)
    {
        TileData tileData = GetTileData(gridPos);

        // No TileData = basic ground tile = normal movement cost
        if (tileData == null) return 1f;

        return tileData.MovementCost;
    }

    /// <summary>
    /// Get build cost multiplier for this tile
    /// </summary>
    public float GetBuildCostMultiplier(Vector2Int gridPos)
    {
        TileData tileData = GetTileData(gridPos);
        if (tileData == null) return 1.0f;

        return tileData.BuildCostMultiplier;
    }

    /// <summary>
    /// Place building on grid, notify tile properties
    /// </summary>
    public void PlaceBuilding(Building building, Vector2Int startCell, int width, int height)
    {
        // Track building on grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(startCell.x + x, startCell.y + y);
                placedBuildings[cell] = building;

                // Notify tile properties
                TileData tileData = GetTileData(cell);
                if (tileData != null)
                {
                    tileData.NotifyBuildingPlaced(building, cell);
                }
            }
        }

        // Trigger pathfinding recalculation
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }

    /// <summary>
    /// Remove building from grid, notify tile properties
    /// </summary>
    public void RemoveBuilding(Building building, Vector2Int startCell, int width, int height)
    {
        // Remove from tracking
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(startCell.x + x, startCell.y + y);
                placedBuildings.Remove(cell);

                // Notify tile properties
                TileData tileData = GetTileData(cell);
                if (tileData != null)
                {
                    tileData.NotifyBuildingRemoved(building, cell);
                }
            }
        }

        // Trigger pathfinding recalculation
        if (PathfindingManager.Instance != null)
        {
            PathfindingManager.Instance.RequestRecalculation();
        }
    }

    /// <summary>
    /// Notify tile properties that enemy entered tile
    /// </summary>
    public void NotifyEnemyEnter(Enemy enemy, Vector2Int gridPos)
    {
        TileData tileData = GetTileData(gridPos);
        if (tileData != null)
        {
            tileData.NotifyEnemyEnter(enemy, gridPos);
        }
    }

    /// <summary>
    /// Notify tile properties that enemy exited tile
    /// </summary>
    public void NotifyEnemyExit(Enemy enemy, Vector2Int gridPos)
    {
        TileData tileData = GetTileData(gridPos);
        if (tileData != null)
        {
            tileData.NotifyEnemyExit(enemy, gridPos);
        }
    }

    /// <summary>
    /// Get all tiles with specific property type
    /// </summary>
    public List<Vector2Int> GetTilesWithProperty<T>() where T : TileProperty
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        if (terrainTilemap == null) return tiles;

        BoundsInt bounds = terrainTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                TileData tileData = GetTileData(gridPos);

                if (tileData != null && tileData.HasProperty<T>())
                {
                    tiles.Add(gridPos);
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// Register tile for active updates (trees, hazards, etc.)
    /// </summary>
    public void RegisterActiveTile(Vector2Int gridPos)
    {
        activeTiles.Add(gridPos);
    }

    /// <summary>
    /// Unregister tile from active updates
    /// </summary>
    public void UnregisterActiveTile(Vector2Int gridPos)
    {
        activeTiles.Remove(gridPos);
    }

    /// <summary>
    /// Swap tile sprite (for resource nodes, tree states, etc.)
    /// </summary>
    public void SwapTileSprite(Vector2Int gridPos, Sprite newSprite)
    {
        if (terrainTilemap == null || newSprite == null) return;

        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);
        PlanetfallTile tile = terrainTilemap.GetTile<PlanetfallTile>(tilePos);

        if (tile != null && tile.tileData != null)
        {
            tile.sprite = newSprite;
            terrainTilemap.RefreshTile(tilePos);
        }
    }

    /// <summary>
    /// Restore tile sprite to original (from TileData)
    /// </summary>
    public void RestoreTileSprite(Vector2Int gridPos)
    {
        if (terrainTilemap == null) return;

        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);
        PlanetfallTile tile = terrainTilemap.GetTile<PlanetfallTile>(tilePos);

        if (tile != null && tile.tileData != null && tile.tileData.tileSprite != null)
        {
            tile.sprite = tile.tileData.tileSprite;
            terrainTilemap.RefreshTile(tilePos);
        }
    }

    #region Ore Mound System (merged from OreMoundManager)

    /// <summary>
    /// Register an ore mound (called by OreMound.Start)
    /// </summary>
    public void RegisterOreMound(OreMound mound)
    {
        if (mound == null || allMounds.Contains(mound)) return;

        allMounds.Add(mound);

        if (mound.IsDiscovered)
        {
            discoveredMounds.Add(mound);
        }
        else
        {
            undiscoveredMounds.Add(mound);
        }

        Debug.Log($"[GridManager] Registered {mound.GetMoundTypeName()} at {mound.Position}");
    }

    /// <summary>
    /// Unregister an ore mound (called by OreMound.OnDestroy)
    /// </summary>
    public void UnregisterOreMound(OreMound mound)
    {
        if (mound == null) return;

        allMounds.Remove(mound);
        discoveredMounds.Remove(mound);
        undiscoveredMounds.Remove(mound);
    }

    /// <summary>
    /// Check for mound discoveries based on current pollution level
    /// </summary>
    private void CheckForMoundDiscoveries()
    {
        if (PollutionManager.Instance == null) return;

        float currentPollution = PollutionManager.Instance.CurrentPollution;

        // Check all undiscovered mounds
        foreach (OreMound mound in undiscoveredMounds.ToList())
        {
            // Check if mound is spatially within pollution zone
            bool isInPollutionZone = true;
            if (TileStateManager.Instance != null)
            {
                Vector2Int moundGridPos = WorldToGridPosition(mound.Position);
                // IsInAliveZone returns true if OUTSIDE pollution radius.
                // We want to discover only if INSIDE pollution radius (so NOT in AliveZone).
                isInPollutionZone = !TileStateManager.Instance.IsInAliveZone(moundGridPos);
            }

            if (isInPollutionZone && mound.CanDiscoverAtPollution(currentPollution))
            {
                mound.Discover();
            }
        }
    }

    /// <summary>
    /// Handle mound discovery event
    /// </summary>
    private void HandleMoundDiscovered(OreMound mound)
    {
        if (undiscoveredMounds.Contains(mound))
        {
            undiscoveredMounds.Remove(mound);
            discoveredMounds.Add(mound);

            OnMoundDiscovered?.Invoke(mound);

            Debug.Log($"[GridManager] {mound.GetMoundTypeName()} discovered! ({discoveredMounds.Count}/{allMounds.Count})");
        }
    }

    /// <summary>
    /// Get all mounds of a specific type
    /// </summary>
    public List<OreMound> GetMoundsByType(OreMoundType type)
    {
        return allMounds.Where(m => m.moundType == type).ToList();
    }

    /// <summary>
    /// Get all discovered mounds of a specific type
    /// </summary>
    public List<OreMound> GetDiscoveredMoundsByType(OreMoundType type)
    {
        return discoveredMounds.Where(m => m.moundType == type).ToList();
    }

    /// <summary>
    /// Get nearest available mound of a specific type
    /// </summary>
    public OreMound GetNearestAvailableMound(OreMoundType type, Vector3 position)
    {
        OreMound nearestMound = null;
        float nearestDistance = float.MaxValue;

        foreach (OreMound mound in discoveredMounds)
        {
            if (mound.moundType != type) continue;
            if (mound.HasExtractor) continue; // Skip occupied mounds

            float distance = Vector3.Distance(position, mound.Position);
            if (distance < nearestDistance)
            {
                nearestMound = mound;
                nearestDistance = distance;
            }
        }

        return nearestMound;
    }

    /// <summary>
    /// Check if a position is on an ore mound
    /// </summary>
    public OreMound GetMoundAtPosition(Vector3 position, float tolerance = 0.5f)
    {
        foreach (OreMound mound in allMounds)
        {
            float distance = Vector3.Distance(position, mound.Position);
            if (distance <= tolerance)
            {
                return mound;
            }
        }

        return null;
    }

    /// <summary>
    /// Get mound discovery progress as percentage
    /// </summary>
    public float GetMoundDiscoveryProgress()
    {
        if (allMounds.Count == 0) return 0f;

        return (float)discoveredMounds.Count / allMounds.Count;
    }

    /// <summary>
    /// Get mound discovery stats string
    /// </summary>
    public string GetMoundDiscoveryStatsString()
    {
        return $"Ore Mounds: {discoveredMounds.Count}/{allMounds.Count} discovered";
    }

    /// <summary>
    /// Force discover all mounds (debug/cheat)
    /// </summary>
    public void DiscoverAllMounds()
    {
        foreach (OreMound mound in undiscoveredMounds.ToList())
        {
            mound.Discover();
        }

        Debug.Log("[GridManager] All mounds discovered (cheat)");
    }

    /// <summary>
    /// Reset all mound discoveries (for new mission)
    /// </summary>
    public void ResetAllMounds()
    {
        discoveredMounds.Clear();
        undiscoveredMounds.Clear();
        allMounds.Clear();

        Debug.Log("[GridManager] Reset all mound data");
    }

    #endregion
}