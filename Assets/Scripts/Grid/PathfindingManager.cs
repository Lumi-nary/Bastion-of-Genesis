using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages pathfinding for enemies navigating the grid
/// Supports both A* (single path) and Flow Field (many enemies to one target)
/// Flow fields are cached and recalculated when buildings/walls change
/// </summary>
public class PathfindingManager : MonoBehaviour
{
    public static PathfindingManager Instance { get; private set; }

    [Header("Pathfinding Settings")]
    [Tooltip("Allow diagonal movement")]
    [SerializeField] private bool allowDiagonals = true;

    [Tooltip("Smooth paths by removing unnecessary waypoints")]
    [SerializeField] private bool smoothPaths = true;

    [Header("Flow Field Settings")]
    [Tooltip("Grid dimensions for flow field (auto-calculated from tilemap if 0)")]
    [SerializeField] private int flowFieldWidth = 0;
    [SerializeField] private int flowFieldHeight = 0;

    // Diagonal cost (sqrt(2) ≈ 1.414)
    private const float DIAGONAL_COST = 1.414f;
    private const float IMPASSABLE_COST = float.MaxValue;

    // Flow field data for each movement type
    private FlowFieldData groundFlowField;
    private FlowFieldData tunnelingFlowField;

    // Flow field target (usually Command Center)
    private Vector2Int flowTarget;
    private bool hasFlowTarget = false;

    // Recalculation management
    private bool needsRecalculation = false;
    private float recalculationCooldown = 0.1f;
    private float lastRecalculationTime = 0f;

    // Version tracking for visualization updates
    private int flowFieldVersion = 0;
    public int FlowFieldVersion => flowFieldVersion;

    // Grid offset for flow field indexing
    private Vector2Int gridOffset;

    // Public property to check if flow target is set
    public bool HasFlowTarget => hasFlowTarget;

    // 8-directional lookup
    private static readonly Vector2Int[] DIRECTIONS = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // N
        new Vector2Int(1, 1),   // NE
        new Vector2Int(1, 0),   // E
        new Vector2Int(1, -1),  // SE
        new Vector2Int(0, -1),  // S
        new Vector2Int(-1, -1), // SW
        new Vector2Int(-1, 0),  // W
        new Vector2Int(-1, 1)   // NW
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Defer initialization to Start() so GridManager is ready
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Track which BuildingManager we're subscribed to
    private BuildingManager subscribedBuildingManager = null;
    private bool flowFieldsInitialized = false;

    // Track which GridManager we initialized from
    private GridManager initializedFromGrid = null;

    private void Start()
    {
        InitializeFlowFields();
        TrySubscribeToBuildingManager();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Reinitialize when new scene loads (GridManager might be different)
        Debug.Log($"[PathfindingManager] Scene loaded: {scene.name}, reinitializing flow fields...");
        flowFieldWidth = 0;
        flowFieldHeight = 0;
        flowFieldsInitialized = false;
        initializedFromGrid = null;
        hasFlowTarget = false;

        // Delay init to let GridManager awake
        StartCoroutine(DelayedInit());
    }

    private System.Collections.IEnumerator DelayedInit()
    {
        yield return null; // Wait one frame
        InitializeFlowFields();
    }

    private void OnDestroy()
    {
        UnsubscribeFromBuildingManager();
    }

    private void Update()
    {
        // Re-subscribe if BuildingManager changed (scene reload, etc.)
        if (BuildingManager.Instance != null && BuildingManager.Instance != subscribedBuildingManager)
        {
            UnsubscribeFromBuildingManager();
            TrySubscribeToBuildingManager();
        }

        // Reinitialize if GridManager changed
        if (GridManager.Instance != null && GridManager.Instance != initializedFromGrid)
        {
            Debug.Log("[PathfindingManager] GridManager changed, reinitializing flow fields...");
            flowFieldWidth = 0;
            flowFieldHeight = 0;
            InitializeFlowFields();
            initializedFromGrid = GridManager.Instance;
        }

        // Handle deferred recalculation
        if (needsRecalculation && Time.time - lastRecalculationTime >= recalculationCooldown)
        {
            RecalculateFlowFields();
            needsRecalculation = false;
            lastRecalculationTime = Time.time;
        }
    }

    private void TrySubscribeToBuildingManager()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingPlaced += OnBuildingChanged;
            BuildingManager.Instance.OnBuildingDestroyedEvent += OnBuildingChanged;
            subscribedBuildingManager = BuildingManager.Instance;
            Debug.Log("[PathfindingManager] Subscribed to BuildingManager events");
        }
    }

    private void UnsubscribeFromBuildingManager()
    {
        if (subscribedBuildingManager != null)
        {
            subscribedBuildingManager.OnBuildingPlaced -= OnBuildingChanged;
            subscribedBuildingManager.OnBuildingDestroyedEvent -= OnBuildingChanged;
            subscribedBuildingManager = null;
        }
    }

    private void InitializeFlowFields()
    {
        // Auto-calculate from GridManager tilemap if dimensions are 0
        if ((flowFieldWidth == 0 || flowFieldHeight == 0) && GridManager.Instance != null)
        {
            BoundsInt tileBounds = GridManager.Instance.GetTilemapBounds();
            flowFieldWidth = tileBounds.size.x;
            flowFieldHeight = tileBounds.size.y;
            gridOffset = new Vector2Int(tileBounds.xMin, tileBounds.yMin);

            Debug.Log($"[PathfindingManager] Flow field auto-sized from tilemap: {flowFieldWidth}x{flowFieldHeight}, offset: {gridOffset}");
        }
        else
        {
            // Fallback to default centered bounds
            if (flowFieldWidth == 0) flowFieldWidth = 200;
            if (flowFieldHeight == 0) flowFieldHeight = 200;
            gridOffset = new Vector2Int(-flowFieldWidth / 2, -flowFieldHeight / 2);

            Debug.Log($"[PathfindingManager] Flow field using default size: {flowFieldWidth}x{flowFieldHeight}, offset: {gridOffset}");
        }

        groundFlowField = new FlowFieldData(flowFieldWidth, flowFieldHeight);
        tunnelingFlowField = new FlowFieldData(flowFieldWidth, flowFieldHeight);
        flowFieldsInitialized = true;
        initializedFromGrid = GridManager.Instance;
    }

    /// <summary>
    /// Reinitialize flow fields (call if GridManager wasn't ready at Start)
    /// </summary>
    public void ReinitializeFlowFields()
    {
        flowFieldsInitialized = false;
        flowFieldWidth = 0;
        flowFieldHeight = 0;
        InitializeFlowFields();

        if (hasFlowTarget)
        {
            RecalculateFlowFields();
        }
    }

    #region Flow Field API

    /// <summary>
    /// Set flow field target (usually the Command Center)
    /// </summary>
    public void SetFlowTarget(Vector2Int target)
    {
        flowTarget = target;
        hasFlowTarget = true;
        RecalculateFlowFields();
        Debug.Log($"[PathfindingManager] Flow target set to {target}");
    }

    /// <summary>
    /// Set flow target from world position
    /// </summary>
    public void SetFlowTargetFromWorld(Vector3 worldPosition)
    {
        if (GridManager.Instance != null)
        {
            SetFlowTarget(GridManager.Instance.WorldToGridPosition(worldPosition));
        }
    }

    /// <summary>
    /// Request flow field recalculation (batched with cooldown)
    /// </summary>
    public void RequestRecalculation()
    {
        needsRecalculation = true;
        Debug.Log($"[PathfindingManager] Recalculation requested (version {flowFieldVersion})");
    }

    /// <summary>
    /// Get flow direction at world position for a movement type
    /// Returns direction toward target as fallback if outside flow field or no valid flow
    /// </summary>
    public Vector3 GetFlowDirection(Vector3 worldPosition, MovementType movementType)
    {
        if (!hasFlowTarget || GridManager.Instance == null)
            return Vector3.zero;

        // Flying enemies go straight to target
        if (movementType == MovementType.Flying)
        {
            Vector3 targetWorld = GridManager.Instance.GridToWorldPosition(flowTarget);
            return (targetWorld - worldPosition).normalized;
        }

        Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPosition);
        Vector2Int localPos = GridToLocal(gridPos);

        // Fallback: direct path toward target (used when outside flow field or stuck)
        Vector3 targetWorldPos = GridManager.Instance.GridToWorldPosition(flowTarget);
        Vector3 fallbackDir = (targetWorldPos - worldPosition).normalized;

        if (!IsValidLocal(localPos))
        {
            // Outside flow field bounds - use direct path to target
            Debug.LogWarning($"[PathfindingManager] Position {gridPos} outside flow field bounds, using direct path");
            return fallbackDir;
        }

        FlowFieldData field = (movementType == MovementType.Tunneling) ? tunnelingFlowField : groundFlowField;
        Vector2 dir = field.flowDirections[localPos.x, localPos.y];

        if (dir == Vector2.zero)
        {
            // Current cell has no flow - try to find direction to nearest walkable cell with flow
            Vector2 escapeDir = FindEscapeDirection(localPos, field, gridPos);
            if (escapeDir != Vector2.zero)
            {
                return new Vector3(escapeDir.x, escapeDir.y, 0).normalized;
            }

            // Still stuck - use direct path toward target as last resort
            Debug.Log($"[PathfindingManager] No flow at {gridPos}, using direct path toward target");
            return fallbackDir;
        }

        return new Vector3(dir.x, dir.y, 0).normalized;
    }

    /// <summary>
    /// Find direction to escape from a cell with no flow (e.g., stuck on obstacle)
    /// Searches neighboring cells for valid flow and returns direction to nearest one
    /// </summary>
    private Vector2 FindEscapeDirection(Vector2Int localPos, FlowFieldData field, Vector2Int gridPos)
    {
        float bestCost = float.MaxValue;
        Vector2 bestDir = Vector2.zero;

        foreach (Vector2Int dir in DIRECTIONS)
        {
            Vector2Int neighborLocal = localPos + dir;
            if (!IsValidLocal(neighborLocal)) continue;

            // Check if neighbor has valid flow (not impassable)
            float neighborCost = field.integrationField[neighborLocal.x, neighborLocal.y];
            if (neighborCost < IMPASSABLE_COST && neighborCost < bestCost)
            {
                bestCost = neighborCost;
                bestDir = new Vector2(dir.x, dir.y).normalized;
            }
        }

        return bestDir;
    }

    /// <summary>
    /// Check if position has valid flow
    /// </summary>
    public bool HasValidFlow(Vector2Int gridPos, MovementType movementType)
    {
        Vector2Int localPos = GridToLocal(gridPos);
        if (!IsValidLocal(localPos)) return false;

        FlowFieldData field = (movementType == MovementType.Tunneling) ? tunnelingFlowField : groundFlowField;
        return field.integrationField[localPos.x, localPos.y] < IMPASSABLE_COST;
    }

    #endregion

    #region Flow Field Calculation

    private void RecalculateFlowFields()
    {
        if (!hasFlowTarget) return;

        flowFieldVersion++;
        Debug.Log($"[PathfindingManager] Recalculating flow fields (version {flowFieldVersion})");

        Vector2Int localTarget = GridToLocal(flowTarget);

        CalculateCostField(groundFlowField, MovementType.Ground);
        CalculateCostField(tunnelingFlowField, MovementType.Tunneling);

        // Force target building cells to be walkable (buildings occupy multiple cells but enemies need to path TO them)
        // This allows Dijkstra to propagate outward from the target
        ForceTargetBuildingWalkable(localTarget);

        CalculateIntegrationField(groundFlowField);
        CalculateIntegrationField(tunnelingFlowField);

        CalculateFlowDirections(groundFlowField);
        CalculateFlowDirections(tunnelingFlowField);

        // Count reachable cells for debugging
        int reachable = 0;
        for (int x = 0; x < flowFieldWidth; x++)
            for (int y = 0; y < flowFieldHeight; y++)
                if (groundFlowField.integrationField[x, y] < IMPASSABLE_COST)
                    reachable++;

        Debug.Log($"[PathfindingManager] Flow field v{flowFieldVersion} complete: {reachable} reachable cells");
    }

    /// <summary>
    /// Force all cells of the target building to be walkable so Dijkstra can propagate
    /// </summary>
    private void ForceTargetBuildingWalkable(Vector2Int localTarget)
    {
        if (!IsValidLocal(localTarget)) return;

        // Find the building at the target position
        Building targetBuilding = GetBuildingAt(flowTarget);
        if (targetBuilding != null)
        {
            // Force all cells of this building to be walkable
            for (int x = 0; x < targetBuilding.width; x++)
            {
                for (int y = 0; y < targetBuilding.height; y++)
                {
                    Vector2Int buildingCell = new Vector2Int(
                        targetBuilding.gridPosition.x + x,
                        targetBuilding.gridPosition.y + y
                    );
                    Vector2Int localCell = GridToLocal(buildingCell);

                    if (IsValidLocal(localCell))
                    {
                        groundFlowField.costField[localCell.x, localCell.y] = 1f;
                        tunnelingFlowField.costField[localCell.x, localCell.y] = 1f;
                    }
                }
            }
        }
        else
        {
            // No building found, just force the target cell
            groundFlowField.costField[localTarget.x, localTarget.y] = 1f;
            tunnelingFlowField.costField[localTarget.x, localTarget.y] = 1f;
        }
    }

    private void CalculateCostField(FlowFieldData field, MovementType moveType)
    {
        int wallCellCount = 0;
        int impassableCellCount = 0;

        for (int x = 0; x < flowFieldWidth; x++)
        {
            for (int y = 0; y < flowFieldHeight; y++)
            {
                Vector2Int gridPos = LocalToGrid(new Vector2Int(x, y));
                float cost = GetCostForMovementType(gridPos, moveType);
                field.costField[x, y] = cost;

                // Debug: count wall vs impassable cells
                if (cost >= WALL_MIN_COST && cost < IMPASSABLE_COST)
                {
                    wallCellCount++;
                }
                else if (cost >= IMPASSABLE_COST)
                {
                    impassableCellCount++;
                }
            }
        }

        Debug.Log($"[PathfindingManager] Cost field calculated: {wallCellCount} wall cells (passable), {impassableCellCount} impassable cells");
    }

    // Wall cost configuration
    private const float WALL_TIER_IRON_COST = 100f;
    private const float WALL_TIER_STEEL_COST = 200f;
    private const float WALL_TIER_NULLMAGIC_COST = 300f;
    private const float WALL_MIN_COST = 10f; // Minimum cost even for nearly destroyed walls

    private float GetCostForMovementType(Vector2Int gridPos, MovementType moveType)
    {
        if (GridManager.Instance == null) return 1f;

        // Check if occupied by building
        if (GridManager.Instance.IsCellOccupied(gridPos))
        {
            Building building = GetBuildingAt(gridPos);

            if (building != null && building.BuildingData != null)
            {
                // Check if it's a wall (walls have special tier-based costs)
                if (building.BuildingData.HasFeature<WallFeature>())
                {
                    WallFeature wallFeature = building.BuildingData.GetFeature<WallFeature>();

                    // Tunneling enemies can bypass Iron walls
                    if (moveType == MovementType.Tunneling && !wallFeature.BlocksTunneling())
                    {
                        return 1f; // Can tunnel through Iron walls
                    }

                    // Calculate wall cost based on tier and health
                    return CalculateWallCost(building, wallFeature);
                }
                else
                {
                    // All buildings are destroyable - calculate cost based on health
                    // Lower health = lower cost = enemies prefer attacking damaged buildings
                    return CalculateBuildingCost(building);
                }
            }
            else
            {
                // Cell is occupied but we couldn't find the building - treat as high-cost obstacle
                // This ensures pathfinding doesn't completely break due to sync issues
                return WALL_TIER_IRON_COST;
            }
        }

        // Check for obstacles (ore mounds, etc.)
        if (GridManager.Instance.IsObstacle(gridPos))
        {
            return IMPASSABLE_COST;
        }

        // For pathfinding purposes, check terrain - but only truly impassable terrain (water)
        // Note: IsWalkable returns false for occupied cells, but we handled those above
        TileData tileData = GridManager.Instance.GetTileData(gridPos);
        if (tileData != null && !tileData.IsWalkable)
        {
            return IMPASSABLE_COST;
        }

        // Default ground cost
        return GridManager.Instance.GetMovementCost(gridPos);
    }

    /// <summary>
    /// Calculate pathfinding cost for a wall based on tier and current health
    /// Damaged walls are cheaper = enemies prefer attacking weak points
    /// </summary>
    private float CalculateWallCost(Building building, WallFeature wallFeature)
    {
        // Base cost by tier
        float tierCost;
        switch (wallFeature.wallTier)
        {
            case WallTier.Iron:
                tierCost = WALL_TIER_IRON_COST;
                break;
            case WallTier.Steel:
                tierCost = WALL_TIER_STEEL_COST;
                break;
            case WallTier.NullMagic:
                tierCost = WALL_TIER_NULLMAGIC_COST;
                break;
            default:
                tierCost = WALL_TIER_IRON_COST;
                break;
        }

        // Health percentage (0.0 to 1.0)
        float healthPercent = 1f;
        if (building.BuildingData.maxHealth > 0)
        {
            healthPercent = Mathf.Clamp01(building.CurrentHealth / building.BuildingData.maxHealth);
        }

        // Final cost: damaged walls are cheaper to path through
        // Formula: (tierCost * healthPercent) + minCost
        float wallCost = (tierCost * healthPercent) + WALL_MIN_COST;

        return wallCost;
    }

    // Building cost configuration
    private const float BUILDING_BASE_COST = 150f; // Higher than walls - prefer attacking walls first
    private const float BUILDING_MIN_COST = 20f;

    /// <summary>
    /// Calculate pathfinding cost for a non-wall building based on health
    /// Damaged buildings are cheaper = enemies prefer attacking weak buildings
    /// </summary>
    private float CalculateBuildingCost(Building building)
    {
        // Health percentage (0.0 to 1.0)
        float healthPercent = 1f;
        if (building.BuildingData.maxHealth > 0)
        {
            healthPercent = Mathf.Clamp01(building.CurrentHealth / building.BuildingData.maxHealth);
        }

        // Final cost: damaged buildings are cheaper to path through
        float buildingCost = (BUILDING_BASE_COST * healthPercent) + BUILDING_MIN_COST;

        return buildingCost;
    }

    private Building GetBuildingAt(Vector2Int gridPos)
    {
        if (BuildingManager.Instance == null) return null;

        foreach (Building b in BuildingManager.Instance.AllBuildings)
        {
            if (b == null || b.IsDestroyed) continue;

            if (gridPos.x >= b.gridPosition.x && gridPos.x < b.gridPosition.x + b.width &&
                gridPos.y >= b.gridPosition.y && gridPos.y < b.gridPosition.y + b.height)
            {
                return b;
            }
        }
        return null;
    }

    private void CalculateIntegrationField(FlowFieldData field)
    {
        // Reset
        for (int x = 0; x < flowFieldWidth; x++)
            for (int y = 0; y < flowFieldHeight; y++)
                field.integrationField[x, y] = IMPASSABLE_COST;

        Vector2Int localTarget = GridToLocal(flowTarget);
        if (!IsValidLocal(localTarget)) return;

        field.integrationField[localTarget.x, localTarget.y] = 0;

        var openSet = new SortedSet<FlowNode>(new FlowNodeComparer());
        openSet.Add(new FlowNode(localTarget, 0));
        var visited = new HashSet<Vector2Int>();

        while (openSet.Count > 0)
        {
            var current = openSet.Min;
            openSet.Remove(current);

            if (visited.Contains(current.pos)) continue;
            visited.Add(current.pos);

            foreach (Vector2Int dir in DIRECTIONS)
            {
                Vector2Int neighbor = current.pos + dir;
                if (!IsValidLocal(neighbor) || visited.Contains(neighbor)) continue;

                float cost = field.costField[neighbor.x, neighbor.y];
                if (cost >= IMPASSABLE_COST) continue;

                bool diagonal = dir.x != 0 && dir.y != 0;
                float moveCost = diagonal ? DIAGONAL_COST * cost : cost;
                float newCost = current.cost + moveCost;

                if (newCost < field.integrationField[neighbor.x, neighbor.y])
                {
                    field.integrationField[neighbor.x, neighbor.y] = newCost;
                    openSet.Add(new FlowNode(neighbor, newCost));
                }
            }
        }
    }

    private void CalculateFlowDirections(FlowFieldData field)
    {
        for (int x = 0; x < flowFieldWidth; x++)
        {
            for (int y = 0; y < flowFieldHeight; y++)
            {
                if (field.integrationField[x, y] >= IMPASSABLE_COST)
                {
                    field.flowDirections[x, y] = Vector2.zero;
                    continue;
                }

                float bestCost = field.integrationField[x, y];
                Vector2 bestDir = Vector2.zero;

                foreach (Vector2Int dir in DIRECTIONS)
                {
                    Vector2Int neighbor = new Vector2Int(x + dir.x, y + dir.y);
                    if (!IsValidLocal(neighbor)) continue;

                    float neighborCost = field.integrationField[neighbor.x, neighbor.y];
                    if (neighborCost < bestCost)
                    {
                        bestCost = neighborCost;
                        bestDir = new Vector2(dir.x, dir.y).normalized;
                    }
                }

                field.flowDirections[x, y] = bestDir;
            }
        }
    }

    private Vector2Int GridToLocal(Vector2Int gridPos) => gridPos - gridOffset;
    private Vector2Int LocalToGrid(Vector2Int localPos) => localPos + gridOffset;
    private bool IsValidLocal(Vector2Int pos) => pos.x >= 0 && pos.x < flowFieldWidth && pos.y >= 0 && pos.y < flowFieldHeight;

    private void OnBuildingChanged(Building building)
    {
        Debug.Log($"[PathfindingManager] Building changed: {(building != null ? building.name : "null")}");
        RequestRecalculation();
    }

    #endregion

    #region Debug Visualization

    /// <summary>
    /// Debug info structure for visualization
    /// </summary>
    public struct FlowFieldDebugInfo
    {
        public Vector2Int targetPosition;
        public Vector2Int gridOffset;
        public int fieldWidth;
        public int fieldHeight;
        public int reachableCells;
        public int impassableCells;

        private float[,] costField;
        private float[,] integrationField;
        private Vector2[,] flowDirections;

        public FlowFieldDebugInfo(Vector2Int target, Vector2Int offset, int w, int h,
            float[,] cost, float[,] integration, Vector2[,] flow)
        {
            targetPosition = target;
            gridOffset = offset;
            fieldWidth = w;
            fieldHeight = h;
            costField = cost;
            integrationField = integration;
            flowDirections = flow;

            // Count reachable and impassable cells
            reachableCells = 0;
            impassableCells = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (integration[x, y] < float.MaxValue * 0.5f)
                        reachableCells++;
                    if (cost[x, y] >= float.MaxValue * 0.5f)
                        impassableCells++;
                }
            }
        }

        public float GetCost(int x, int y)
        {
            if (x < 0 || x >= fieldWidth || y < 0 || y >= fieldHeight)
                return float.MaxValue;
            return costField[x, y];
        }

        public float GetIntegration(int x, int y)
        {
            if (x < 0 || x >= fieldWidth || y < 0 || y >= fieldHeight)
                return float.MaxValue;
            return integrationField[x, y];
        }

        public Vector2 GetFlowDirection(int x, int y)
        {
            if (x < 0 || x >= fieldWidth || y < 0 || y >= fieldHeight)
                return Vector2.zero;
            return flowDirections[x, y];
        }
    }

    /// <summary>
    /// Get debug info for visualization (Editor only)
    /// </summary>
    public FlowFieldDebugInfo GetDebugInfo()
    {
        return new FlowFieldDebugInfo(
            flowTarget,
            gridOffset,
            flowFieldWidth,
            flowFieldHeight,
            groundFlowField.costField,
            groundFlowField.integrationField,
            groundFlowField.flowDirections
        );
    }

    #endregion

    #region Flow Field Data Structures

    private class FlowFieldData
    {
        public float[,] costField;
        public float[,] integrationField;
        public Vector2[,] flowDirections;

        public FlowFieldData(int w, int h)
        {
            costField = new float[w, h];
            integrationField = new float[w, h];
            flowDirections = new Vector2[w, h];
        }
    }

    private struct FlowNode
    {
        public Vector2Int pos;
        public float cost;
        public FlowNode(Vector2Int p, float c) { pos = p; cost = c; }
    }

    private class FlowNodeComparer : IComparer<FlowNode>
    {
        public int Compare(FlowNode a, FlowNode b)
        {
            int c = a.cost.CompareTo(b.cost);
            if (c != 0) return c;
            c = a.pos.x.CompareTo(b.pos.x);
            return c != 0 ? c : a.pos.y.CompareTo(b.pos.y);
        }
    }

    #endregion

    /// <summary>
    /// Find path from start to goal using A* algorithm
    /// Returns list of grid positions, or null if no path exists
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (GridManager.Instance == null)
        {
            Debug.LogWarning("[PathfindingManager] GridManager not found!");
            return null;
        }

        // Check if goal is walkable
        if (!GridManager.Instance.IsWalkable(goal))
        {
            Debug.LogWarning($"[PathfindingManager] Goal {goal} is not walkable!");
            return null;
        }

        // A* algorithm
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector2Int>();
        var allNodes = new Dictionary<Vector2Int, PathNode>();

        PathNode startNode = new PathNode(start, null, 0, Heuristic(start, goal));
        openSet.Add(startNode);
        allNodes[start] = startNode;

        int iterations = 0;
        int maxIterations = 10000; // Safety limit

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Get node with lowest F cost
            PathNode current = GetLowestFCost(openSet);

            // Check if reached goal
            if (current.position == goal)
            {
                List<Vector2Int> path = ReconstructPath(current);

                // Apply path smoothing if enabled
                if (smoothPaths && path.Count > 2)
                {
                    path = SmoothPath(path);
                }

                return path;
            }

            openSet.Remove(current);
            closedSet.Add(current.position);

            // Check neighbors (8-directional if enabled)
            foreach (var neighborData in GetNeighbors(current.position))
            {
                Vector2Int neighbor = neighborData.position;
                float moveCost = neighborData.cost;

                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Check if walkable
                if (!GridManager.Instance.IsWalkable(neighbor))
                {
                    continue;
                }

                // For diagonals, also check that we can actually move diagonally (no corner cutting)
                if (neighborData.isDiagonal && !CanMoveDiagonally(current.position, neighbor))
                {
                    continue;
                }

                // Calculate cost
                float terrainCost = GridManager.Instance.GetMovementCost(neighbor);
                float tentativeG = current.gCost + (moveCost * terrainCost);

                PathNode neighborNode;
                if (!allNodes.TryGetValue(neighbor, out neighborNode))
                {
                    // New node, add to open set
                    neighborNode = new PathNode(neighbor, current, tentativeG, Heuristic(neighbor, goal));
                    allNodes[neighbor] = neighborNode;
                    openSet.Add(neighborNode);
                }
                else if (tentativeG < neighborNode.gCost)
                {
                    // Found better path to this node
                    neighborNode.parent = current;
                    neighborNode.gCost = tentativeG;
                    neighborNode.fCost = tentativeG + neighborNode.hCost;
                }
            }
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning($"[PathfindingManager] Max iterations reached! Path from {start} to {goal} might be too complex.");
        }

        // No path found
        Debug.LogWarning($"[PathfindingManager] No path found from {start} to {goal}");
        return null;
    }

    /// <summary>
    /// Get neighboring grid positions with movement costs
    /// </summary>
    private List<NeighborData> GetNeighbors(Vector2Int pos)
    {
        var neighbors = new List<NeighborData>
        {
            // Cardinal directions (cost 1.0)
            new NeighborData(pos + Vector2Int.up, 1f, false),
            new NeighborData(pos + Vector2Int.down, 1f, false),
            new NeighborData(pos + Vector2Int.left, 1f, false),
            new NeighborData(pos + Vector2Int.right, 1f, false)
        };

        // Add diagonals if enabled
        if (allowDiagonals)
        {
            neighbors.Add(new NeighborData(pos + new Vector2Int(1, 1), DIAGONAL_COST, true));   // Up-Right
            neighbors.Add(new NeighborData(pos + new Vector2Int(-1, 1), DIAGONAL_COST, true));  // Up-Left
            neighbors.Add(new NeighborData(pos + new Vector2Int(1, -1), DIAGONAL_COST, true));  // Down-Right
            neighbors.Add(new NeighborData(pos + new Vector2Int(-1, -1), DIAGONAL_COST, true)); // Down-Left
        }

        return neighbors;
    }

    /// <summary>
    /// Check if diagonal movement is allowed (no corner cutting through obstacles)
    /// </summary>
    private bool CanMoveDiagonally(Vector2Int from, Vector2Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        // Check both adjacent cardinal tiles are walkable
        Vector2Int horizontalAdjacent = new Vector2Int(from.x + dx, from.y);
        Vector2Int verticalAdjacent = new Vector2Int(from.x, from.y + dy);

        return GridManager.Instance.IsWalkable(horizontalAdjacent) &&
               GridManager.Instance.IsWalkable(verticalAdjacent);
    }

    /// <summary>
    /// Smooth path by removing unnecessary waypoints using line-of-sight checks
    /// </summary>
    private List<Vector2Int> SmoothPath(List<Vector2Int> path)
    {
        if (path.Count <= 2) return path;

        List<Vector2Int> smoothed = new List<Vector2Int> { path[0] };
        int currentIndex = 0;

        while (currentIndex < path.Count - 1)
        {
            int furthestVisible = currentIndex + 1;

            // Find furthest point we can see directly
            for (int i = currentIndex + 2; i < path.Count; i++)
            {
                if (HasLineOfSight(path[currentIndex], path[i]))
                {
                    furthestVisible = i;
                }
            }

            smoothed.Add(path[furthestVisible]);
            currentIndex = furthestVisible;
        }

        return smoothed;
    }

    /// <summary>
    /// Check if there's a clear line of sight between two grid positions
    /// Uses Bresenham's line algorithm
    /// </summary>
    private bool HasLineOfSight(Vector2Int from, Vector2Int to)
    {
        int x0 = from.x, y0 = from.y;
        int x1 = to.x, y1 = to.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Skip start and end positions
            if (!(x0 == from.x && y0 == from.y) && !(x0 == to.x && y0 == to.y))
            {
                if (!GridManager.Instance.IsWalkable(new Vector2Int(x0, y0)))
                {
                    return false;
                }
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return true;
    }

    /// <summary>
    /// Calculate heuristic distance (Chebyshev for 8-dir, Manhattan for 4-dir)
    /// </summary>
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        if (allowDiagonals)
        {
            // Chebyshev distance (allows diagonal movement)
            return Mathf.Max(dx, dy) + (DIAGONAL_COST - 1) * Mathf.Min(dx, dy);
        }
        else
        {
            // Manhattan distance (4-directional only)
            return dx + dy;
        }
    }

    private struct NeighborData
    {
        public Vector2Int position;
        public float cost;
        public bool isDiagonal;

        public NeighborData(Vector2Int pos, float c, bool diag)
        {
            position = pos;
            cost = c;
            isDiagonal = diag;
        }
    }

    /// <summary>
    /// Get node with lowest F cost from open set
    /// </summary>
    private PathNode GetLowestFCost(List<PathNode> openSet)
    {
        PathNode lowest = openSet[0];
        for (int i = 1; i < openSet.Count; i++)
        {
            if (openSet[i].fCost < lowest.fCost ||
                (openSet[i].fCost == lowest.fCost && openSet[i].hCost < lowest.hCost))
            {
                lowest = openSet[i];
            }
        }
        return lowest;
    }

    /// <summary>
    /// Reconstruct path from goal to start
    /// </summary>
    private List<Vector2Int> ReconstructPath(PathNode goalNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode current = goalNode;

        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Simple pathfinding node for A*
    /// </summary>
    private class PathNode
    {
        public Vector2Int position;
        public PathNode parent;
        public float gCost; // Distance from start
        public float hCost; // Heuristic distance to goal
        public float fCost; // gCost + hCost

        public PathNode(Vector2Int pos, PathNode parent, float g, float h)
        {
            this.position = pos;
            this.parent = parent;
            this.gCost = g;
            this.hCost = h;
            this.fCost = g + h;
        }
    }
}
