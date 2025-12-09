using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages runtime tile states and sprite updates based on pollution/integration radius.
/// Handles dynamic transitions between Alive, Polluted (Wither), and Integrated ground states.
/// </summary>
public class TileStateManager : MonoBehaviour
{
    public static TileStateManager Instance { get; private set; }

    [Header("Tilemap Reference")]
    [SerializeField] private Tilemap terrainTilemap;

    [Header("Integration Settings")]
    [SerializeField] private Transform integrationCenter; // Command Center
    [SerializeField] private float integrationRadius = 10f;
    [SerializeField] private float witherBorderWidth = 2f; // Buffer zone width

    [Header("Transition Tilesets - Ground State (Radius-based)")]
    [Tooltip("Grass to Wither transition tileset")]
    [SerializeField] private TransitionTileset grassWitherTileset;

    [Tooltip("Wither to Integrated transition tileset")]
    [SerializeField] private TransitionTileset witherIntegratedTileset;

    [Header("Transition Tilesets - Ground to Sand")]
    [Tooltip("Integrated to Sand transition tileset")]
    [SerializeField] private TransitionTileset integratedSandTileset;

    [Tooltip("Wither to Sand transition tileset")]
    [SerializeField] private TransitionTileset witherSandTileset;

    [Tooltip("Grass to Sand transition tileset")]
    [SerializeField] private TransitionTileset grassSandTileset;

    [Header("Transition Tilesets - Sand to Water")]
    [Tooltip("Sand to Water transition tileset")]
    [SerializeField] private TransitionTileset sandWaterTileset;

    // Cached sprite arrays (populated from ScriptableObjects)
    // Ground state transitions (radius-based)
    private Sprite[] grassWitherSprites;
    private Sprite[] witherIntegratedSprites;
    private Sprite fullGrassSprite;
    private Sprite fullWitherSprite;
    private Sprite fullIntegratedSprite;

    // Ground to Sand transitions
    private Sprite[] integratedSandSprites;
    private Sprite[] witherSandSprites;
    private Sprite[] grassSandSprites;

    // Sand to Water transition
    private Sprite[] sandWaterSprites;
    private Sprite fullSandSprite;
    private Sprite fullWaterSprite;

    // Cache terrain types from TileData
    private Dictionary<Vector2Int, TerrainType> terrainTypeCache = new Dictionary<Vector2Int, TerrainType>();

    // Sprite indices for 13-sprite tileset
    private const int OUTER_CORNER_TL = 0;
    private const int EDGE_TOP = 1;
    private const int OUTER_CORNER_TR = 2;
    private const int EDGE_LEFT = 3;
    private const int CENTER = 4;
    private const int EDGE_RIGHT = 5;
    private const int OUTER_CORNER_BL = 6;
    private const int EDGE_BOTTOM = 7;
    private const int OUTER_CORNER_BR = 8;
    private const int INNER_CORNER_TL = 9;
    private const int INNER_CORNER_TR = 10;
    private const int INNER_CORNER_BL = 11;
    private const int INNER_CORNER_BR = 12;

    // Runtime state tracking
    private Dictionary<Vector2Int, GroundState> tileStates = new Dictionary<Vector2Int, GroundState>();
    private BoundsInt tilemapBounds;

    // Cached tiles to avoid creating new ScriptableObjects every update
    private Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();

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
        // Cache sprites from ScriptableObjects - Ground state transitions
        if (grassWitherTileset != null)
        {
            grassWitherSprites = grassWitherTileset.ToArray();
            fullGrassSprite = grassWitherTileset.baseFromSprite;
            fullWitherSprite = grassWitherTileset.baseToSprite;
        }

        if (witherIntegratedTileset != null)
        {
            witherIntegratedSprites = witherIntegratedTileset.ToArray();
            // fullWitherSprite already set above (or use this one if grass-wither not set)
            if (fullWitherSprite == null)
            {
                fullWitherSprite = witherIntegratedTileset.baseFromSprite;
            }
            fullIntegratedSprite = witherIntegratedTileset.baseToSprite;
        }

        // Cache sprites from ScriptableObjects - Ground to Sand transitions
        if (integratedSandTileset != null)
        {
            integratedSandSprites = integratedSandTileset.ToArray();
            if (fullIntegratedSprite == null)
            {
                fullIntegratedSprite = integratedSandTileset.baseFromSprite;
            }
            fullSandSprite = integratedSandTileset.baseToSprite;
        }

        if (witherSandTileset != null)
        {
            witherSandSprites = witherSandTileset.ToArray();
            if (fullWitherSprite == null)
            {
                fullWitherSprite = witherSandTileset.baseFromSprite;
            }
            if (fullSandSprite == null)
            {
                fullSandSprite = witherSandTileset.baseToSprite;
            }
        }

        if (grassSandTileset != null)
        {
            grassSandSprites = grassSandTileset.ToArray();
            if (fullGrassSprite == null)
            {
                fullGrassSprite = grassSandTileset.baseFromSprite;
            }
            if (fullSandSprite == null)
            {
                fullSandSprite = grassSandTileset.baseToSprite;
            }
        }

        // Cache sprites - Sand to Water transition
        if (sandWaterTileset != null)
        {
            sandWaterSprites = sandWaterTileset.ToArray();
            if (fullSandSprite == null)
            {
                fullSandSprite = sandWaterTileset.baseFromSprite;
            }
            fullWaterSprite = sandWaterTileset.baseToSprite;
        }

        // If no center assigned, try to find Command Center FIRST
        if (integrationCenter == null)
        {
            GameObject commandCenter = GameObject.Find("CommandCenter");
            if (commandCenter != null)
            {
                integrationCenter = commandCenter.transform;
            }
        }

        if (terrainTilemap != null)
        {
            tilemapBounds = terrainTilemap.cellBounds;
            CacheTerrainTypes();
            InitializeTileStates();
        }
    }

    /// <summary>
    /// Cache terrain types from all tiles in the tilemap
    /// </summary>
    private void CacheTerrainTypes()
    {
        terrainTypeCache.Clear();

        for (int x = tilemapBounds.xMin; x < tilemapBounds.xMax; x++)
        {
            for (int y = tilemapBounds.yMin; y < tilemapBounds.yMax; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                PlanetfallTile tile = terrainTilemap.GetTile<PlanetfallTile>(tilePos);
                if (tile != null && tile.tileData != null)
                {
                    terrainTypeCache[gridPos] = tile.tileData.terrainType;
                }
            }
        }

        Debug.Log($"[TileStateManager] Cached {terrainTypeCache.Count} terrain type tiles");
    }

    /// <summary>
    /// Get terrain type at position (from cache)
    /// </summary>
    public TerrainType GetTerrainType(Vector2Int gridPos)
    {
        if (terrainTypeCache.TryGetValue(gridPos, out TerrainType terrainType))
        {
            return terrainType;
        }
        return TerrainType.None;
    }

    /// <summary>
    /// Initialize all tile states based on current integration radius
    /// </summary>
    private void InitializeTileStates()
    {
        UpdateAllTileStates();
    }

    /// <summary>
    /// Update integration radius (called when pollution changes)
    /// </summary>
    public void SetIntegrationRadius(float newRadius)
    {
        if (Mathf.Approximately(integrationRadius, newRadius)) return;

        integrationRadius = newRadius;
        UpdateAllTileStates();
    }

    /// <summary>
    /// Get current integration radius
    /// </summary>
    public float GetIntegrationRadius()
    {
        return integrationRadius;
    }

    /// <summary>
    /// Update all tile states based on square distance from integration center
    /// Optimized to only update tiles that changed state
    /// </summary>
    public void UpdateAllTileStates()
    {
        if (terrainTilemap == null || integrationCenter == null) return;

        Vector2 centerPos = integrationCenter.position;
        float innerRadius = integrationRadius - witherBorderWidth;

        // Track tiles that changed state (need sprite update)
        HashSet<Vector2Int> changedTiles = new HashSet<Vector2Int>();

        // First pass: calculate all states, track changes
        for (int x = tilemapBounds.xMin; x < tilemapBounds.xMax; x++)
        {
            for (int y = tilemapBounds.yMin; y < tilemapBounds.yMax; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = terrainTilemap.CellToWorld(new Vector3Int(x, y, 0)) + new Vector3(0.5f, 0.5f, 0);

                // Square distance (Chebyshev) = max of X and Y distance
                float dx = Mathf.Abs(worldPos.x - centerPos.x);
                float dy = Mathf.Abs(worldPos.y - centerPos.y);
                float distance = Mathf.Max(dx, dy);

                GroundState newState;
                if (distance < innerRadius)
                {
                    newState = GroundState.Integrated;
                }
                else if (distance < integrationRadius)
                {
                    newState = GroundState.Polluted; // Wither zone
                }
                else
                {
                    newState = GroundState.Alive; // Grass zone
                }

                // Check if state changed
                if (!tileStates.TryGetValue(gridPos, out GroundState oldState) || oldState != newState)
                {
                    tileStates[gridPos] = newState;
                    changedTiles.Add(gridPos);

                    // Also mark neighbors as needing update (for transition sprites)
                    changedTiles.Add(gridPos + Vector2Int.up);
                    changedTiles.Add(gridPos + Vector2Int.down);
                    changedTiles.Add(gridPos + Vector2Int.left);
                    changedTiles.Add(gridPos + Vector2Int.right);
                }
            }
        }

        // Second pass: only update sprites for changed tiles and their neighbors
        foreach (Vector2Int gridPos in changedTiles)
        {
            UpdateTileSprite(gridPos);
        }
    }

    /// <summary>
    /// Get the ground state at a specific position
    /// </summary>
    public GroundState GetGroundState(Vector2Int gridPos)
    {
        if (tileStates.TryGetValue(gridPos, out GroundState state))
        {
            return state;
        }
        return GroundState.Alive; // Default
    }

    /// <summary>
    /// Check if a tile is buildable based on its ground state
    /// </summary>
    public bool IsTileBuildable(Vector2Int gridPos)
    {
        // If no tile states calculated yet, allow building (fallback)
        if (tileStates.Count == 0)
        {
            return true;
        }
        return GetGroundState(gridPos) == GroundState.Integrated;
    }

    /// <summary>
    /// Check if a world position is within the integrated (buildable) zone
    /// Uses direct distance check - doesn't rely on tile state dictionary
    /// </summary>
    public bool IsPositionBuildable(Vector3 worldPos)
    {
        if (integrationCenter == null) return true; // No center = allow all

        Vector2 centerPos = integrationCenter.position;
        float innerRadius = integrationRadius - witherBorderWidth;

        // Square distance (Chebyshev) = max of X and Y distance
        float dx = Mathf.Abs(worldPos.x - centerPos.x);
        float dy = Mathf.Abs(worldPos.y - centerPos.y);
        float distance = Mathf.Max(dx, dy);

        bool isBuildable = distance < innerRadius;

        Debug.Log($"[TileState] Check pos {worldPos}, center {centerPos}, radius {innerRadius}, distance {distance}, buildable: {isBuildable}");

        return isBuildable;
    }

    /// <summary>
    /// Update a single tile's sprite based on its state and neighbors
    /// </summary>
    private void UpdateTileSprite(Vector2Int gridPos)
    {
        if (terrainTilemap == null) return;

        Vector3Int tilePos = new Vector3Int(gridPos.x, gridPos.y, 0);

        // Only update tiles that already exist in the tilemap
        TileBase existingTile = terrainTilemap.GetTile(tilePos);
        if (existingTile == null) return;

        Sprite newSprite = null;

        // Check terrain type first - fixed terrain uses terrain transitions
        TerrainType terrainType = GetTerrainType(gridPos);
        if (terrainType != TerrainType.None)
        {
            newSprite = GetSpriteForTerrainType(gridPos, terrainType);
        }
        else
        {
            // No terrain type - use ground state (radius-based) transitions
            GroundState currentState = GetGroundState(gridPos);
            newSprite = GetSpriteForGroundState(gridPos, currentState);
        }

        if (newSprite != null)
        {
            terrainTilemap.SetTile(tilePos, GetCachedTile(newSprite));
        }
    }

    /// <summary>
    /// Get the appropriate sprite for a tile based on terrain type.
    /// Inner terrain draws transitions toward outer terrain.
    /// Hierarchy: Grass (inner) -> Sand (middle) -> Water (outer)
    /// </summary>
    private Sprite GetSpriteForTerrainType(Vector2Int gridPos, TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Grass:
                // Grass draws transitions toward Sand (outer)
                return GetTerrainTransitionSprite(gridPos,
                    TerrainType.Grass,
                    new[] { TerrainType.Sand, TerrainType.Water },
                    grassSandSprites,
                    fullGrassSprite);

            case TerrainType.Sand:
                // Sand draws transitions toward Water (outer)
                return GetTerrainTransitionSprite(gridPos,
                    TerrainType.Sand,
                    new[] { TerrainType.Water },
                    sandWaterSprites,
                    fullSandSprite);

            case TerrainType.Water:
                // Water is outermost - no transitions to draw
                return fullWaterSprite;
        }

        return null;
    }

    /// <summary>
    /// Get transition sprite for terrain type based on neighbor analysis
    /// </summary>
    private Sprite GetTerrainTransitionSprite(Vector2Int gridPos, TerrainType currentTerrain,
        TerrainType[] outerTerrains, Sprite[] sprites, Sprite fallbackSprite)
    {
        if (sprites == null || sprites.Length < 13) return fallbackSprite;

        // Get 8-directional neighbors - check if neighbor is outer terrain
        bool top = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.up), outerTerrains);
        bool bottom = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.down), outerTerrains);
        bool left = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.left), outerTerrains);
        bool right = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.right), outerTerrains);
        bool topLeft = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.up + Vector2Int.left), outerTerrains);
        bool topRight = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.up + Vector2Int.right), outerTerrains);
        bool bottomLeft = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.down + Vector2Int.left), outerTerrains);
        bool bottomRight = IsOuterTerrain(GetTerrainType(gridPos + Vector2Int.down + Vector2Int.right), outerTerrains);

        // If no cardinal neighbors are outer terrain, check diagonals for inner corners
        if (!top && !bottom && !left && !right)
        {
            if (topLeft) return sprites[INNER_CORNER_TL];
            if (topRight) return sprites[INNER_CORNER_TR];
            if (bottomLeft) return sprites[INNER_CORNER_BL];
            if (bottomRight) return sprites[INNER_CORNER_BR];
            return fallbackSprite;
        }

        // Outer corners: two adjacent cardinals are outer terrain
        if (top && left && !bottom && !right) return sprites[OUTER_CORNER_TL];
        if (top && right && !bottom && !left) return sprites[OUTER_CORNER_TR];
        if (bottom && left && !top && !right) return sprites[OUTER_CORNER_BL];
        if (bottom && right && !top && !left) return sprites[OUTER_CORNER_BR];

        // Edges: one cardinal is outer terrain
        if (top && !bottom && !left && !right) return sprites[EDGE_TOP];
        if (bottom && !top && !left && !right) return sprites[EDGE_BOTTOM];
        if (left && !right && !top && !bottom) return sprites[EDGE_LEFT];
        if (right && !left && !top && !bottom) return sprites[EDGE_RIGHT];

        // Three sides - use edge sprite
        if (top && left && right && !bottom) return sprites[EDGE_TOP];
        if (bottom && left && right && !top) return sprites[EDGE_BOTTOM];
        if (top && bottom && left && !right) return sprites[EDGE_LEFT];
        if (top && bottom && right && !left) return sprites[EDGE_RIGHT];

        // Completely surrounded
        if (top && bottom && left && right) return sprites[CENTER];

        return fallbackSprite;
    }

    /// <summary>
    /// Check if terrain type is in the outer terrains array
    /// </summary>
    private bool IsOuterTerrain(TerrainType terrain, TerrainType[] outerTerrains)
    {
        foreach (var outer in outerTerrains)
        {
            if (terrain == outer) return true;
        }
        return false;
    }

    /// <summary>
    /// Get the appropriate sprite for a tile based on ground state and neighbors.
    /// Checks for Sand terrain borders first, then ground state transitions.
    /// Hierarchy: Integrated (innermost) -> Polluted (middle) -> Alive (outermost) -> Sand -> Water
    /// </summary>
    private Sprite GetSpriteForGroundState(Vector2Int gridPos, GroundState state)
    {
        // First check if this tile borders Sand terrain
        bool bordersSand = HasSandNeighbor(gridPos);

        switch (state)
        {
            case GroundState.Alive:
                // Grass - check for Sand border first
                if (bordersSand)
                {
                    return GetGroundToSandSprite(gridPos, grassSandSprites, fullGrassSprite);
                }
                // Grass is outermost ground state - no ground state transitions
                return fullGrassSprite;

            case GroundState.Polluted:
                // Wither - check for Sand border first
                if (bordersSand)
                {
                    return GetGroundToSandSprite(gridPos, witherSandSprites, fullWitherSprite);
                }
                // Wither draws transition towards Grass (outer ground state)
                return GetTransitionSprite(gridPos, GroundState.Polluted,
                    new[] { GroundState.Alive },
                    grassWitherSprites, fullWitherSprite, true);

            case GroundState.Integrated:
                // Integrated - check for Sand border first
                if (bordersSand)
                {
                    return GetGroundToSandSprite(gridPos, integratedSandSprites, fullIntegratedSprite);
                }
                // Integrated draws transition towards Wither/Grass
                return GetTransitionSprite(gridPos, GroundState.Integrated,
                    new[] { GroundState.Polluted, GroundState.Alive },
                    witherIntegratedSprites, fullIntegratedSprite, true);
        }

        return null;
    }

    /// <summary>
    /// Check if any neighbor is Sand terrain
    /// </summary>
    private bool HasSandNeighbor(Vector2Int gridPos)
    {
        // Check all 8 directions for Sand terrain
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            Vector2Int.up + Vector2Int.left, Vector2Int.up + Vector2Int.right,
            Vector2Int.down + Vector2Int.left, Vector2Int.down + Vector2Int.right
        };

        foreach (var dir in directions)
        {
            if (GetTerrainType(gridPos + dir) == TerrainType.Sand)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get transition sprite for ground state bordering Sand terrain
    /// </summary>
    private Sprite GetGroundToSandSprite(Vector2Int gridPos, Sprite[] sprites, Sprite fallbackSprite)
    {
        if (sprites == null || sprites.Length < 13) return fallbackSprite;

        // Check neighbors for Sand terrain
        bool top = GetTerrainType(gridPos + Vector2Int.up) == TerrainType.Sand;
        bool bottom = GetTerrainType(gridPos + Vector2Int.down) == TerrainType.Sand;
        bool left = GetTerrainType(gridPos + Vector2Int.left) == TerrainType.Sand;
        bool right = GetTerrainType(gridPos + Vector2Int.right) == TerrainType.Sand;
        bool topLeft = GetTerrainType(gridPos + Vector2Int.up + Vector2Int.left) == TerrainType.Sand;
        bool topRight = GetTerrainType(gridPos + Vector2Int.up + Vector2Int.right) == TerrainType.Sand;
        bool bottomLeft = GetTerrainType(gridPos + Vector2Int.down + Vector2Int.left) == TerrainType.Sand;
        bool bottomRight = GetTerrainType(gridPos + Vector2Int.down + Vector2Int.right) == TerrainType.Sand;

        // If no cardinal neighbors are Sand, check diagonals for inner corners
        if (!top && !bottom && !left && !right)
        {
            if (topLeft) return sprites[INNER_CORNER_TL];
            if (topRight) return sprites[INNER_CORNER_TR];
            if (bottomLeft) return sprites[INNER_CORNER_BL];
            if (bottomRight) return sprites[INNER_CORNER_BR];
            return fallbackSprite;
        }

        // Outer corners: two adjacent cardinals are Sand
        if (top && left && !bottom && !right) return sprites[OUTER_CORNER_TL];
        if (top && right && !bottom && !left) return sprites[OUTER_CORNER_TR];
        if (bottom && left && !top && !right) return sprites[OUTER_CORNER_BL];
        if (bottom && right && !top && !left) return sprites[OUTER_CORNER_BR];

        // Edges: one cardinal is Sand
        if (top && !bottom && !left && !right) return sprites[EDGE_TOP];
        if (bottom && !top && !left && !right) return sprites[EDGE_BOTTOM];
        if (left && !right && !top && !bottom) return sprites[EDGE_LEFT];
        if (right && !left && !top && !bottom) return sprites[EDGE_RIGHT];

        // Three sides - use edge sprite
        if (top && left && right && !bottom) return sprites[EDGE_TOP];
        if (bottom && left && right && !top) return sprites[EDGE_BOTTOM];
        if (top && bottom && left && !right) return sprites[EDGE_LEFT];
        if (top && bottom && right && !left) return sprites[EDGE_RIGHT];

        // Completely surrounded by Sand
        if (top && bottom && left && right) return sprites[CENTER];

        return fallbackSprite;
    }

    /// <summary>
    /// Get transition sprite based on neighbor analysis
    /// </summary>
    /// <param name="gridPos">Current tile position</param>
    /// <param name="currentState">State of current tile</param>
    /// <param name="borderStates">States that count as "other" for border detection</param>
    /// <param name="sprites">Sprite array to use</param>
    /// <param name="fallbackSprite">Sprite to use if no border detected</param>
    /// <param name="invertPerspective">True if this tile is the "filled" side of the transition</param>
    private Sprite GetTransitionSprite(Vector2Int gridPos, GroundState currentState,
        GroundState[] borderStates, Sprite[] sprites, Sprite fallbackSprite, bool invertPerspective)
    {
        if (sprites == null || sprites.Length < 13) return fallbackSprite;

        // Get 8-directional neighbors - check if neighbor is a DIFFERENT state (border state)
        bool top = IsBorderState(GetGroundState(gridPos + Vector2Int.up), borderStates);
        bool bottom = IsBorderState(GetGroundState(gridPos + Vector2Int.down), borderStates);
        bool left = IsBorderState(GetGroundState(gridPos + Vector2Int.left), borderStates);
        bool right = IsBorderState(GetGroundState(gridPos + Vector2Int.right), borderStates);
        bool topLeft = IsBorderState(GetGroundState(gridPos + Vector2Int.up + Vector2Int.left), borderStates);
        bool topRight = IsBorderState(GetGroundState(gridPos + Vector2Int.up + Vector2Int.right), borderStates);
        bool bottomLeft = IsBorderState(GetGroundState(gridPos + Vector2Int.down + Vector2Int.left), borderStates);
        bool bottomRight = IsBorderState(GetGroundState(gridPos + Vector2Int.down + Vector2Int.right), borderStates);

        // First check: if no cardinal neighbors are border states, use fallback (no transition needed)
        if (!top && !bottom && !left && !right)
        {
            return fallbackSprite;
        }

        // From here, we know at least one cardinal neighbor is a different state
        // Now determine which transition sprite to use

        if (invertPerspective)
        {
            // Inverted: we're the "filled" side looking at "empty" neighbors
            // top=true means neighbor above is empty/different

            // Inner corners: all 4 cardinals are same as us, but one diagonal is different
            if (!top && !bottom && !left && !right)
            {
                // Already handled above
            }

            // Outer corners: two adjacent cardinals are different
            if (top && left && !bottom && !right) return sprites[OUTER_CORNER_TL];
            if (top && right && !bottom && !left) return sprites[OUTER_CORNER_TR];
            if (bottom && left && !top && !right) return sprites[OUTER_CORNER_BL];
            if (bottom && right && !top && !left) return sprites[OUTER_CORNER_BR];

            // Edges: one cardinal is different
            if (top && !bottom && !left && !right) return sprites[EDGE_TOP];
            if (bottom && !top && !left && !right) return sprites[EDGE_BOTTOM];
            if (left && !right && !top && !bottom) return sprites[EDGE_LEFT];
            if (right && !left && !top && !bottom) return sprites[EDGE_RIGHT];

            // Inner corners: all cardinals same, one diagonal different
            if (!top && !bottom && !left && !right)
            {
                if (topLeft) return sprites[INNER_CORNER_TL];
                if (topRight) return sprites[INNER_CORNER_TR];
                if (bottomLeft) return sprites[INNER_CORNER_BL];
                if (bottomRight) return sprites[INNER_CORNER_BR];
            }

            // Three sides - use edge sprite
            if (top && left && right && !bottom) return sprites[EDGE_TOP];
            if (bottom && left && right && !top) return sprites[EDGE_BOTTOM];
            if (top && bottom && left && !right) return sprites[EDGE_LEFT];
            if (top && bottom && right && !left) return sprites[EDGE_RIGHT];
        }
        else
        {
            // Non-inverted: we're the "empty" side looking at "filled" neighbors
            // top=true means neighbor above is filled/different

            // Outer corners: two adjacent cardinals are different (filled)
            if (top && left && !bottom && !right) return sprites[OUTER_CORNER_BR];
            if (top && right && !bottom && !left) return sprites[OUTER_CORNER_BL];
            if (bottom && left && !top && !right) return sprites[OUTER_CORNER_TR];
            if (bottom && right && !top && !left) return sprites[OUTER_CORNER_TL];

            // Edges: one cardinal is different (filled)
            if (top && !bottom && !left && !right) return sprites[EDGE_BOTTOM];
            if (bottom && !top && !left && !right) return sprites[EDGE_TOP];
            if (left && !right && !top && !bottom) return sprites[EDGE_RIGHT];
            if (right && !left && !top && !bottom) return sprites[EDGE_LEFT];

            // Inner corners: all cardinals filled, one diagonal empty (same as us)
            if (top && bottom && left && right)
            {
                if (!topLeft) return sprites[INNER_CORNER_TL];
                if (!topRight) return sprites[INNER_CORNER_TR];
                if (!bottomLeft) return sprites[INNER_CORNER_BL];
                if (!bottomRight) return sprites[INNER_CORNER_BR];
                return sprites[CENTER];
            }

            // Three sides - use edge sprite
            if (top && left && right && !bottom) return sprites[EDGE_BOTTOM];
            if (bottom && left && right && !top) return sprites[EDGE_TOP];
            if (top && bottom && left && !right) return sprites[EDGE_RIGHT];
            if (top && bottom && right && !left) return sprites[EDGE_LEFT];
        }

        return fallbackSprite;
    }

    /// <summary>
    /// Check if a state is in the border states array
    /// </summary>
    private bool IsBorderState(GroundState state, GroundState[] borderStates)
    {
        foreach (var s in borderStates)
        {
            if (state == s) return true;
        }
        return false;
    }

    /// <summary>
    /// Get or create a cached tile with the given sprite
    /// </summary>
    private Tile GetCachedTile(Sprite sprite)
    {
        if (sprite == null) return null;

        if (!tileCache.TryGetValue(sprite, out Tile tile))
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tileCache[sprite] = tile;
        }
        return tile;
    }

    /// <summary>
    /// Force refresh all tiles (call after major changes)
    /// </summary>
    public void RefreshAllTiles()
    {
        UpdateAllTileStates();
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || integrationCenter == null) return;

        Vector3 center = integrationCenter.position;

        // Draw integration radius (outer square - wither border)
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f); // Orange for wither
        Gizmos.DrawWireCube(center, new Vector3(integrationRadius * 2, integrationRadius * 2, 0));

        // Draw inner radius (buildable zone square)
        float innerRadius = integrationRadius - witherBorderWidth;
        Gizmos.color = new Color(0, 1, 0, 0.5f); // Green for integrated
        Gizmos.DrawWireCube(center, new Vector3(innerRadius * 2, innerRadius * 2, 0));
    }
#endif
}
