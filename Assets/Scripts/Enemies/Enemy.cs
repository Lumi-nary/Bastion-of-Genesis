using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet;

/// <summary>
/// Component attached to enemy GameObjects
/// Manages individual enemy behavior, health, combat, and special abilities
/// REFACTORED: Now uses modular SpecialAbility system - abilities are data-driven
/// Uses A* pathfinding via PathfindingManager for navigation
/// </summary>
public class Enemy : NetworkBehaviour
{
    [Header("Enemy Configuration")]
    [Tooltip("Enemy data defining this enemy's stats and behavior")]
    public EnemyData enemyData;

    [Header("Runtime Stats")]
    // Synced Health
    private readonly SyncVar<float> _syncedHealth = new SyncVar<float>();
    
    // Properties to maintain API compatibility
    public float currentHealth 
    { 
        get => _syncedHealth.Value; 
        private set => _syncedHealth.Value = value; 
    }

    private float maxHealth;
    private float effectiveDamage;
    private float lastAttackTime;
    private bool isDead = false;

    // Difficulty and pollution multipliers (set by spawner)
    private float difficultyMultiplier = 1f;
    private float pollutionMultiplier = 1f;

    [Header("Target")]
    private Building currentTarget;

    [Header("Movement")]
    private Vector2Int lastGridPosition;
    private Vector2Int currentCell;      // Current grid cell enemy is in
    private Vector2Int targetCell;       // Next cell to move toward
    private Vector3 targetWorldPos;      // World position of target cell center
    private bool hasTargetCell = false;  // Whether we have a valid target cell
    private const float CELL_ARRIVAL_THRESHOLD = 0.05f; // How close to cell center before snapping

    [Header("Separation")]
    [Tooltip("Radius for separation from other enemies")]
    [SerializeField] private float separationRadius = 0.5f;
    [Tooltip("Strength of separation force")]
    [SerializeField] private float separationStrength = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugMovement = false;
    private int stuckFrameCount = 0;
    private Vector3 lastPosition;

    // Public properties
    public EnemyData Data => enemyData;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public Building CurrentTarget => currentTarget;
    public EnemyRace Race => enemyData != null ? enemyData.race : EnemyRace.Human;

    // Events
    public delegate void EnemyDeathEvent(Enemy enemy);
    public event EnemyDeathEvent OnEnemyDeath;

    public delegate void EnemyDamagedEvent(Enemy enemy, float damage, float remainingHealth);
    public event EnemyDamagedEvent OnEnemyDamaged;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterNetworkedEnemy(this);
        }
        
        // Listen to health changes for local UI/Events?
        _syncedHealth.OnChange += OnHealthSyncChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterNetworkedEnemy(this);
        }
        _syncedHealth.OnChange -= OnHealthSyncChanged;
    }

    private void OnHealthSyncChanged(float prev, float next, bool asServer)
    {
        // Fire damaged event on clients for UI
        if (!asServer && prev > next)
        {
            OnEnemyDamaged?.Invoke(this, prev - next, next);
        }
    }

    private void Awake()
    {
        // Don't init here if networked, wait for ServerInitialize
        // But if local testing (no network), allow Awake?
        // We'll rely on Initialize call.
    }

    /// <summary>
    /// Initialize enemy with data and multipliers (called by spawner)
    /// </summary>
    public void Initialize(EnemyData data, float diffMult, float pollMult)
    {
        enemyData = data;
        difficultyMultiplier = diffMult;
        pollutionMultiplier = pollMult;

        // Calculate effective stats based on difficulty and pollution
        maxHealth = data.GetEffectiveHP(difficultyMultiplier, pollutionMultiplier);
        
        if (IsServerStarted || IsServer) // Set SyncVar only on server
        {
            currentHealth = maxHealth;
        }

        effectiveDamage = data.GetEffectiveDamage(difficultyMultiplier, pollutionMultiplier);

        isDead = false;
        lastAttackTime = 0f;

        // Track initial grid position and snap to cell center
        if (GridManager.Instance != null)
        {
            currentCell = GridManager.Instance.WorldToGridPosition(transform.position);
            lastGridPosition = currentCell;

            // Snap to cell center (X.5, Y.5)
            // Only force position if Server, otherwise let NetworkTransform handle it
            if (IsServerStarted || IsServer)
            {
                Vector3 cellCenter = GridManager.Instance.GridToWorldPosition(currentCell);
                transform.position = cellCenter;
            }

            hasTargetCell = false; // Will be set on first movement update
        }

        // Initialize all abilities
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                ability.OnInitialize(this);
            }
        }
    }

    private void Update()
    {
        if (isDead) return;
        
        // If Networked, Client does NOT run AI/Movement
        if (IsClientStarted && !IsServerStarted) return;
        
        // If not networked (local testing), allow it.
        // How to detect? InstanceFinder.NetworkManager == null?
        // IsServerStarted is false if not connected.
        // But we want to support offline mode?
        // If inherited NetworkBehaviour, and not initialized, IsServerStarted is false.
        // We assume offline mode is just "Server Started" (Host).
        // If totally offline (no FishNet), Update runs? 
        // FishNet: NetworkBehaviour Update might run if not spawned? No.
        
        // For now: Only Server runs AI.
        // If this is a Client, RETURN.
        if (IsClient && !IsServer) return;

        // Detect if enemy was manually moved (e.g., in editor) and re-sync position
        SyncCellPosition();

        // Update all abilities (passive effects, healing, etc.)
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                ability.OnUpdate(this);
            }
        }
        
        // ... rest of update
        
        TrackGridPosition();

        // Update behavior based on current state
        if (currentTarget != null && !currentTarget.IsDestroyed)
        {
            if (IsInAttackRange())
            {
                AttackTarget();
            }
            else
            {
                MoveUsingFlowField();
            }
        }
        else
        {
            // Find new target
            FindTarget();
            if (currentTarget != null)
            {
                MoveUsingFlowField();
            }
        }
    }

    /// <summary>
    /// Detect if enemy was moved externally (e.g., manually in editor) and re-sync cell tracking
    /// This ensures the pathfinding system stays in sync with actual position
    /// Only triggers when enemy is far from expected position (not during normal movement)
    /// </summary>
    private void SyncCellPosition()
    {
        if (GridManager.Instance == null) return;

        Vector3 currentCellCenter = GridManager.Instance.GridToWorldPosition(currentCell);
        float distFromCurrentCell = Vector3.Distance(transform.position, currentCellCenter);

        // If we're moving toward a target cell, check distance to both cells
        if (hasTargetCell)
        {
            float distFromTargetCell = Vector3.Distance(transform.position, targetWorldPos);

            // During normal movement, enemy should be within ~1.5 cells of either current or target
            // If far from both, enemy was moved externally
            float maxExpectedDist = GridManager.Instance.GetCellSize() * 1.5f;

            if (distFromCurrentCell > maxExpectedDist && distFromTargetCell > maxExpectedDist)
            {
                // Enemy was moved externally - re-sync
                ResyncToCurrentPosition();
            }
        }
        else
        {
            // Not moving - should be at cell center
            // Only re-sync if significantly far from current cell (more than half a cell)
            float halfCell = GridManager.Instance.GetCellSize() * 0.5f;

            if (distFromCurrentCell > halfCell)
            {
                // Enemy was moved externally - re-sync
                ResyncToCurrentPosition();
            }
        }
    }

    /// <summary>
    /// Re-sync enemy position to current cell after external movement
    /// </summary>
    private void ResyncToCurrentPosition()
    {
        Vector2Int actualCell = GridManager.Instance.WorldToGridPosition(transform.position);

        if (debugMovement)
        {
            Debug.Log($"[Enemy] {name} position sync: was tracking cell {currentCell}, " +
                      $"actually at cell {actualCell}. Re-syncing.");
        }

        // Snap to the center of the actual cell we're in
        currentCell = actualCell;
        Vector3 cellCenter = GridManager.Instance.GridToWorldPosition(currentCell);
        transform.position = cellCenter;

        // Clear target cell to get fresh direction from flow field
        hasTargetCell = false;
        stuckFrameCount = 0;
    }

    /// <summary>
    /// Track when enemy moves to a new grid cell (for tile effects)
    /// </summary>
    private void TrackGridPosition()
    {
        if (GridManager.Instance == null) return;

        Vector2Int currentGridPos = GridManager.Instance.WorldToGridPosition(transform.position);

        if (currentGridPos != lastGridPosition)
        {
            // Notify old tile we left
            GridManager.Instance.NotifyEnemyExit(this, lastGridPosition);

            // Notify new tile we entered
            GridManager.Instance.NotifyEnemyEnter(this, currentGridPos);

            lastGridPosition = currentGridPos;
        }
    }

    /// <summary>
    /// Find the next target based on targeting priority
    /// Priority: Base → Generators → Extractors → Defenses → Walls
    /// Abilities can override targeting (e.g., Elven Healer prioritizes production buildings)
    /// </summary>
    private void FindTarget()
    {
        Building defaultTarget = null;

        // Get default target from EnemyManager
        if (EnemyManager.Instance != null)
        {
            defaultTarget = EnemyManager.Instance.GetTargetForEnemy(this);
        }

        // Allow abilities to override target selection
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                Building preferredTarget = ability.GetPreferredTarget(this, defaultTarget);
                if (preferredTarget != null)
                {
                    currentTarget = preferredTarget;
                    return;
                }
            }
        }

        // Use default target
        currentTarget = defaultTarget;
    }

    /// <summary>
    /// Move using flow field pathfinding - cell-to-cell movement
    /// Enemy moves from cell center to cell center following flow field directions
    /// </summary>
    private void MoveUsingFlowField()
    {
        if (currentTarget == null || enemyData == null || GridManager.Instance == null) return;

        // Don't move if already in attack range of current target
        if (IsInAttackRange())
        {
            if (debugMovement) Debug.Log($"[Enemy] {name} in attack range of {currentTarget.name}, attacking");
            return;
        }

        // If we don't have a target cell, determine next move
        if (!hasTargetCell)
        {
            DetermineNextMove();
            if (!hasTargetCell)
            {
                // Couldn't determine a move - stay put
                return;
            }
        }

        // Move toward target cell center
        Vector3 toTarget = targetWorldPos - transform.position;
        float distanceToTarget = toTarget.magnitude;

        if (debugMovement)
        {
            Debug.Log($"[Enemy] {name} at ({transform.position.x:F2}, {transform.position.y:F2}) " +
                      $"cell {currentCell} -> target cell {targetCell} dist {distanceToTarget:F3}");
        }

        // Check if we've arrived at target cell center
        if (distanceToTarget <= CELL_ARRIVAL_THRESHOLD)
        {
            // Snap to exact cell center
            transform.position = targetWorldPos;
            currentCell = targetCell;
            hasTargetCell = false; // Get new target next frame

            if (debugMovement) Debug.Log($"[Enemy] {name} arrived at cell {currentCell}");
            return;
        }

        // Move toward target cell center
        Vector3 direction = toTarget.normalized;

        // Allow abilities to modify movement direction
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                direction = ability.GetMovementDirection(this, direction);
            }
        }

        // Calculate movement this frame
        float speed = enemyData.moveSpeed;
        float moveDistance = speed * Time.deltaTime;

        // Don't overshoot the target
        if (moveDistance > distanceToTarget)
        {
            moveDistance = distanceToTarget;
        }

        Vector3 newPosition = transform.position + direction * moveDistance;

        // Check if new position is walkable (for non-flying enemies)
        if (enemyData.movementType != MovementType.Flying)
        {
            if (!IsPositionWalkable(newPosition))
            {
                if (debugMovement) Debug.Log($"[Enemy] {name} blocked by non-walkable tile at {newPosition}");
                hasTargetCell = false; // Re-evaluate path
                return;
            }
        }

        // Track if enemy is stuck
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved < 0.001f)
        {
            stuckFrameCount++;
            if (stuckFrameCount > 60 && debugMovement)
            {
                Debug.LogWarning($"[Enemy] {name} STUCK at cell {currentCell} for {stuckFrameCount} frames");
            }
        }
        else
        {
            stuckFrameCount = 0;
        }
        lastPosition = transform.position;

        // Apply separation to avoid stacking on other enemies
        Vector3 separationOffset = GetSeparationOffset();
        newPosition += separationOffset * Time.deltaTime;

        // Apply movement
        transform.position = newPosition;
    }

    /// <summary>
    /// Calculate separation offset to prevent enemies from stacking on each other.
    /// Uses simple repulsion from nearby enemies.
    /// </summary>
    private Vector3 GetSeparationOffset()
    {
        if (EnemyManager.Instance == null) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        foreach (Enemy other in EnemyManager.Instance.ActiveEnemies)
        {
            if (other == null || other == this || other.isDead) continue;

            Vector3 toThis = transform.position - other.transform.position;
            float distance = toThis.magnitude;

            if (distance < separationRadius && distance > 0.01f)
            {
                // Repel away from nearby enemy, stronger when closer
                float strength = (separationRadius - distance) / separationRadius;
                separation += toThis.normalized * strength;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            separation /= neighborCount;
            separation *= separationStrength;
        }

        return separation;
    }

    /// <summary>
    /// Determine the next cell to move to
    /// Flow field now routes through walls - enemy attacks walls in its path
    /// </summary>
    private void DetermineNextMove()
    {
        if (PathfindingManager.Instance == null || GridManager.Instance == null)
            return;

        // Get flow direction from current cell
        Vector3 cellCenter = GridManager.Instance.GridToWorldPosition(currentCell);
        Vector3 flowDir = PathfindingManager.Instance.GetFlowDirection(cellCenter, enemyData.movementType);

        if (flowDir == Vector3.zero)
        {
            // No flow direction at all - truly stuck (shouldn't happen with wall pathing)
            if (debugMovement) Debug.LogWarning($"[Enemy] {name} no flow direction at cell {currentCell}");
            return;
        }

        // Calculate next cell based on flow direction
        int dx = Mathf.RoundToInt(flowDir.x);
        int dy = Mathf.RoundToInt(flowDir.y);

        // Handle diagonal directions
        if (dx != 0 && dy != 0)
        {
            dx = flowDir.x > 0 ? 1 : -1;
            dy = flowDir.y > 0 ? 1 : -1;
        }

        // Ensure we have some direction
        if (dx == 0 && dy == 0)
        {
            dx = flowDir.x > 0.1f ? 1 : (flowDir.x < -0.1f ? -1 : 0);
            dy = flowDir.y > 0.1f ? 1 : (flowDir.y < -0.1f ? -1 : 0);
        }

        if (dx == 0 && dy == 0)
        {
            if (debugMovement) Debug.Log($"[Enemy] {name} at destination or stuck");
            return;
        }

        Vector2Int nextCell = new Vector2Int(currentCell.x + dx, currentCell.y + dy);

        // Check if next cell contains any building - if so, attack it
        // All buildings are destroyable and block movement
        if (GridManager.Instance.IsCellOccupied(nextCell))
        {
            Building buildingInPath = GetBuildingAtCell(nextCell);
            if (buildingInPath != null)
            {
                // Building is in the adjacent cell - attack it
                currentTarget = buildingInPath;
                hasTargetCell = false; // Stop movement, focus on attacking

                if (debugMovement) Debug.Log($"[Enemy] {name} targeting building {buildingInPath.name} at {nextCell}");
                return;
            }
        }

        // Next cell is walkable - move to it
        if (IsPositionWalkable(GridManager.Instance.GridToWorldPosition(nextCell)))
        {
            targetCell = nextCell;
            targetWorldPos = GridManager.Instance.GridToWorldPosition(targetCell);
            hasTargetCell = true;

            if (debugMovement)
            {
                Debug.Log($"[Enemy] {name} flow dir ({flowDir.x:F2}, {flowDir.y:F2}) -> cell {targetCell}");
            }
        }
        else
        {
            // Cell not walkable (water, etc.) - shouldn't happen with proper flow field
            if (debugMovement) Debug.LogWarning($"[Enemy] {name} flow points to non-walkable cell {nextCell}");
        }
    }

    /// <summary>
    /// Get wall building at a specific cell
    /// </summary>
    private Building GetWallAtCell(Vector2Int cell)
    {
        if (BuildingManager.Instance == null) return null;

        foreach (Building building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDestroyed) continue;
            if (building.BuildingData == null) continue;

            // Check if building occupies this cell
            if (cell.x >= building.gridPosition.x && cell.x < building.gridPosition.x + building.width &&
                cell.y >= building.gridPosition.y && cell.y < building.gridPosition.y + building.height)
            {
                // Check if it's a wall
                if (building.BuildingData.HasFeature<WallFeature>())
                {
                    return building;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get any building at a specific cell
    /// </summary>
    private Building GetBuildingAtCell(Vector2Int cell)
    {
        if (BuildingManager.Instance == null) return null;

        foreach (Building building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDestroyed) continue;

            // Check if building occupies this cell
            if (cell.x >= building.gridPosition.x && cell.x < building.gridPosition.x + building.width &&
                cell.y >= building.gridPosition.y && cell.y < building.gridPosition.y + building.height)
            {
                return building;
            }
        }
        return null;
    }

    /// <summary>
    /// Set target cell to move toward a building
    /// </summary>
    private void MoveTowardBuilding(Building building)
    {
        if (building == null || GridManager.Instance == null) return;

        // Get the nearest cell adjacent to the building
        Vector2Int buildingCell = building.gridPosition;
        int buildingWidth = building.width;
        int buildingHeight = building.height;

        // Find the closest edge cell of the building to move toward
        Vector2Int bestCell = currentCell;
        float bestDist = float.MaxValue;

        // Check all cells adjacent to the building
        for (int x = -1; x <= buildingWidth; x++)
        {
            for (int y = -1; y <= buildingHeight; y++)
            {
                // Skip interior cells
                if (x >= 0 && x < buildingWidth && y >= 0 && y < buildingHeight)
                    continue;

                Vector2Int adjacentCell = new Vector2Int(buildingCell.x + x, buildingCell.y + y);
                float dist = Vector2Int.Distance(currentCell, adjacentCell);

                if (dist < bestDist)
                {
                    // Check if this cell is walkable
                    Vector3 cellPos = GridManager.Instance.GridToWorldPosition(adjacentCell);
                    if (IsPositionWalkable(cellPos))
                    {
                        bestDist = dist;
                        bestCell = adjacentCell;
                    }
                }
            }
        }

        if (bestCell != currentCell)
        {
            // Move one cell toward the best adjacent cell
            int dx = Mathf.Clamp(bestCell.x - currentCell.x, -1, 1);
            int dy = Mathf.Clamp(bestCell.y - currentCell.y, -1, 1);

            Vector2Int nextCell = new Vector2Int(currentCell.x + dx, currentCell.y + dy);
            Vector3 nextPos = GridManager.Instance.GridToWorldPosition(nextCell);

            // Check if next cell is walkable or occupied (we stop at occupied)
            if (IsPositionWalkable(nextPos))
            {
                targetCell = nextCell;
                targetWorldPos = nextPos;
                hasTargetCell = true;
                if (debugMovement) Debug.Log($"[Enemy] {name} moving toward building at cell {nextCell}");
            }
            else if (GridManager.Instance.IsCellOccupied(nextCell))
            {
                // We're adjacent to an occupied cell - we should be in attack range
                if (debugMovement) Debug.Log($"[Enemy] {name} adjacent to occupied cell {nextCell}");
            }
        }
    }

    /// <summary>
    /// Find the nearest building blocking our path
    /// Prioritizes walls, then any building in our direction
    /// </summary>
    private Building FindNearestBlockingBuilding()
    {
        if (BuildingManager.Instance == null || currentTarget == null) return null;

        Vector3 targetPos = currentTarget.transform.position;
        Vector3 dirToTarget = (targetPos - transform.position).normalized;

        Building nearestWall = null;
        float nearestWallDist = float.MaxValue;
        Building nearestOther = null;
        float nearestOtherDist = float.MaxValue;

        foreach (Building building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDestroyed) continue;

            float distance = GetDistanceToBuilding(building);

            // Skip if too far (more than 20 cells away)
            if (distance > 20f) continue;

            // Check if building is in our general direction (or nearby)
            Vector3 dirToBuilding = (building.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(dirToTarget, dirToBuilding);

            // Consider buildings in our path (dot > -0.5 means not completely behind us)
            if (dot < -0.5f) continue;

            bool isWall = building.BuildingData != null && building.BuildingData.HasFeature<WallFeature>();

            if (isWall)
            {
                if (distance < nearestWallDist)
                {
                    nearestWallDist = distance;
                    nearestWall = building;
                }
            }
            else if (building != currentTarget) // Don't select command center as blocking
            {
                if (distance < nearestOtherDist)
                {
                    nearestOtherDist = distance;
                    nearestOther = building;
                }
            }
        }

        // Prioritize walls
        return nearestWall ?? nearestOther;
    }

    /// <summary>
    /// Get distance to the nearest edge of a building
    /// </summary>
    private float GetDistanceToBuilding(Building building)
    {
        if (building == null) return float.MaxValue;

        Vector3 enemyPos = transform.position;
        Vector3 buildingCenter = building.transform.position;

        float halfWidth = building.width * 0.5f;
        float halfHeight = building.height * 0.5f;

        float nearestX = Mathf.Clamp(enemyPos.x, buildingCenter.x - halfWidth, buildingCenter.x + halfWidth);
        float nearestY = Mathf.Clamp(enemyPos.y, buildingCenter.y - halfHeight, buildingCenter.y + halfHeight);

        return Vector2.Distance(enemyPos, new Vector2(nearestX, nearestY));
    }

    /// <summary>
    /// Check if enemy is in attack range of a specific building
    /// </summary>
    private bool IsInRangeOf(Building building)
    {
        if (building == null || enemyData == null) return false;
        return GetDistanceToBuilding(building) <= enemyData.attackRange;
    }

    /// <summary>
    /// Check if a position is walkable (simple 1x1 cell check)
    /// Only checks the center cell - enemy occupies single grid cell
    /// </summary>
    private bool IsPositionWalkable(Vector2 position)
    {
        if (GridManager.Instance == null) return true;

        // Simple single cell check (enemy is 1x1)
        Vector2Int cell = GridManager.Instance.WorldToGridPosition(position);

        // Check if terrain is walkable (water, etc.)
        // IsCellOccupied and IsObstacle are buildings/obstacles - flow field handles those
        if (!GridManager.Instance.IsWalkable(cell) &&
            !GridManager.Instance.IsCellOccupied(cell) &&
            !GridManager.Instance.IsObstacle(cell))
        {
            return false; // Non-walkable terrain (water)
        }

        return true;
    }

    /// <summary>
    /// Check if enemy is in range to attack target
    /// Uses distance to nearest edge of building, not center
    /// All buildings: always in range when adjacent (cell-based movement limitation)
    /// </summary>
    private bool IsInAttackRange()
    {
        if (currentTarget == null || enemyData == null) return false;

        // If we're adjacent to the building, always consider in range
        // This is because cell-based movement means adjacent cell is the closest we can get
        if (IsAdjacentToBuilding(currentTarget))
        {
            return true;
        }

        // Calculate distance to nearest edge of building (not center)
        float distanceToEdge = GetDistanceToTargetEdge();
        return distanceToEdge <= enemyData.attackRange;
    }

    /// <summary>
    /// Check if enemy is in a cell adjacent to the building
    /// </summary>
    private bool IsAdjacentToBuilding(Building building)
    {
        if (building == null || GridManager.Instance == null) return false;

        // Check if enemy's cell is adjacent to any cell the building occupies
        for (int bx = 0; bx < building.width; bx++)
        {
            for (int by = 0; by < building.height; by++)
            {
                Vector2Int buildingCell = new Vector2Int(building.gridPosition.x + bx, building.gridPosition.y + by);

                // Check all 8 adjacent cells
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        Vector2Int adjacentCell = new Vector2Int(buildingCell.x + dx, buildingCell.y + dy);
                        if (currentCell == adjacentCell)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Get distance to the nearest edge of the target building
    /// </summary>
    private float GetDistanceToTargetEdge()
    {
        if (currentTarget == null) return float.MaxValue;

        Vector3 enemyPos = transform.position;
        Vector3 buildingCenter = currentTarget.transform.position;

        // Get building half-size (assuming buildings are centered on their transform)
        float halfWidth = currentTarget.width * 0.5f;
        float halfHeight = currentTarget.height * 0.5f;

        // Clamp enemy position to building bounds to find nearest point on building
        float nearestX = Mathf.Clamp(enemyPos.x, buildingCenter.x - halfWidth, buildingCenter.x + halfWidth);
        float nearestY = Mathf.Clamp(enemyPos.y, buildingCenter.y - halfHeight, buildingCenter.y + halfHeight);

        Vector3 nearestPoint = new Vector3(nearestX, nearestY, 0);

        return Vector3.Distance(enemyPos, nearestPoint);
    }

    /// <summary>
    /// Attack the current target
    /// All special abilities are executed on attack
    /// </summary>
    private void AttackTarget()
    {
        if (currentTarget == null || enemyData == null) return;

        // Check attack cooldown (attackSpeed is attacks per second, so cooldown is 1/attackSpeed)
        if (Time.time - lastAttackTime < (1f / enemyData.attackSpeed)) return;

        // Check if any ability replaces normal damage (e.g., EnergyDrainAbility)
        bool damageReplaced = false;
        EnergyDrainAbility energyDrain = enemyData.GetAbility<EnergyDrainAbility>();
        if (energyDrain != null && energyDrain.replaceDamage)
        {
            damageReplaced = true;
        }

        // Deal damage to target (unless replaced by ability)
        if (!damageReplaced)
        {
            currentTarget.TakeDamage(effectiveDamage);
        }

        // Execute all special abilities on attack
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                ability.OnAttack(this, currentTarget, effectiveDamage);
            }
        }

        lastAttackTime = Time.time;

        if (!damageReplaced)
        {
            Debug.Log($"[Enemy] {enemyData.GetDisplayName()} attacked {currentTarget.name} for {effectiveDamage} damage");
        }
    }

    /// <summary>
    /// Take damage from turrets or other sources
    /// Abilities can block/modify damage (e.g., DodgeAbility, shield abilities)
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Client should not calculate damage logic, just visuals.
        // But if this is called on Client (e.g. by a projectile), we need to ensure it only runs on Server.
        // However, most projectiles should have Server-side logic.
        if (!IsServerStarted && !IsServer) return;

        // Give abilities a chance to block/modify damage
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                if (ability.OnTakeDamage(this, ref damage))
                {
                    // Damage was blocked (e.g., dodged)
                    return;
                }
            }
        }

        // Apply armor reduction
        float finalDamage = Mathf.Max(damage - enemyData.armor, 0f);

        currentHealth -= finalDamage;

        // Event fired via SyncVar callback on clients
        OnEnemyDamaged?.Invoke(this, finalDamage, currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal this enemy (called by HealingAbility or other sources)
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        if (!IsServerStarted && !IsServer) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    /// <summary>
    /// Enemy death logic
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;

        // Execute ability death effects
        foreach (var ability in enemyData.specialAbilities)
        {
            if (ability != null)
            {
                ability.OnDeath(this);
            }
        }

        // Notify listeners
        OnEnemyDeath?.Invoke(this);

        // Notify EnemyManager
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled(this);
        }
        
        // Network Despawn
        if (IsServerStarted)
        {
             // Delay destroy slightly for death animation? 
             // Logic handles instantly for now.
             InstanceFinder.ServerManager.Despawn(gameObject);
        }
        else
        {
             // Fallback for local
             Destroy(gameObject);
        }
    }

    /// <summary>
    /// Force target change (e.g., when target is destroyed)
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
    }

    /// <summary>
    /// Set a specific target (used by EnemyManager)
    /// </summary>
    public void SetTarget(Building target)
    {
        currentTarget = target;
    }
}
