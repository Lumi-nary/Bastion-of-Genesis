using System.Collections.Generic;
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

    // Building placement tracking
    private Dictionary<Vector2Int, Building> placedBuildings = new Dictionary<Vector2Int, Building>();

    // Obstacles that block pathfinding (ore mounds, rocks, etc.)
    private HashSet<Vector2Int> obstacles = new HashSet<Vector2Int>();

    // Tiles that need OnUpdate called (trees checking pollution, etc.)
    private HashSet<Vector2Int> activeTiles = new HashSet<Vector2Int>();

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
}