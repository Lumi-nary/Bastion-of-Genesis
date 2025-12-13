using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Building Data")]
    [SerializeField] private BuildingData buildingData;
    public BuildingData BuildingData => buildingData;

    private List<WorkerData> assignedWorkers = new List<WorkerData>();

    [Header("Grid Information")]
    public Vector2Int gridPosition;
    public int width;
    public int height;

    public float CurrentHealth { get; private set; }
    private bool isDestroyed = false;
    public bool IsDestroyed => isDestroyed;

    // Events
    public event Action<Building, float> OnBuildingDamaged;
    public event Action<Building> OnBuildingDestroyed;

    private void Awake()
    {
        // Ensure BoxCollider2D exists for physics-based click detection
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Set collider size based on BuildingData dimensions
        if (buildingData != null)
        {
            collider.size = new Vector2(buildingData.width, buildingData.height);
            collider.offset = Vector2.zero;

            // Also set width/height from BuildingData if not already set
            if (width == 0) width = buildingData.width;
            if (height == 0) height = buildingData.height;
        }
        else
        {
            // Fallback for scene-placed buildings without data yet
            // Use existing or default size
            if (collider.size == Vector2.zero)
            {
                collider.size = new Vector2(width > 0 ? width : 1, height > 0 ? height : 1);
            }
        }

        // Ensure collider is NOT a trigger (needed for raycast detection)
        collider.isTrigger = false;
    }

    // Property to check if the building has the minimum required workers to function
    public bool IsOperational
    {
        get
        {
            if (buildingData.workerRequirements.Count == 0) return true; // No workers required, so always operational

            foreach (var req in buildingData.workerRequirements)
            {
                if (GetAssignedWorkerCount(req.workerType) < req.requiredCount)
                {
                    return false; // Missing required workers
                }
            }
            return true;
        }
    }

    private void Start()
    {
        CurrentHealth = buildingData.maxHealth;

        // Register with GridManager if placed in scene editor (gridPosition not set by BuildingManager)
        if (GridManager.Instance != null && gridPosition == Vector2Int.zero)
        {
            // Calculate grid position from world position
            Vector2Int worldGridPos = GridManager.Instance.WorldToGridPosition(transform.position);

            // Calculate bottom-left grid position based on building size
            int centerOffsetX = width / 2;
            int centerOffsetY = height / 2;
            gridPosition = new Vector2Int(worldGridPos.x - centerOffsetX, worldGridPos.y - centerOffsetY);

            // Register with grid
            GridManager.Instance.PlaceBuilding(this, gridPosition, width, height);
        }

        // Register with BuildingManager if not already tracked (scene-placed buildings)
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterBuilding(this);
        }

        // Initialize all features
        foreach (var feature in buildingData.features)
        {
            if (feature != null)
            {
                feature.OnBuilt(this);
            }
        }
    }

    private void Update()
    {
        // Execute all features (modular system)
        foreach (var feature in buildingData.features)
        {
            if (feature != null)
            {
                feature.OnUpdate(this);

                // Execute operational features if building is operational
                if (IsOperational)
                {
                    feature.OnOperate(this);
                }
            }
        }
    }

    public int GetTotalAssignedWorkerCount()
    {
        return assignedWorkers.Count;
    }

    public int GetAssignedWorkerCount(WorkerData workerData)
    {
        return assignedWorkers.Count(w => w == workerData);
    }

    public int GetTotalWorkerCapacity()
    {
        if (buildingData.capacityType == WorkerCapacityType.Shared)
        {
            return buildingData.totalWorkerCapacity;
        }
        else // PerType
        {
            // Sum of all individual capacities
            return buildingData.workerRequirements.Sum(req => req.capacity);
        }
    }

    public int GetCapacityForWorker(WorkerData workerData)
    {
        if (buildingData.capacityType == WorkerCapacityType.Shared)
        {
            return buildingData.totalWorkerCapacity;
        }
        else // PerType
        {
            var requirement = buildingData.workerRequirements.FirstOrDefault(r => r.workerType == workerData);
            return requirement != null ? requirement.capacity : 0;
        }
    }

    public bool AssignWorker(WorkerData workerData)
    {
        var requirement = buildingData.workerRequirements.FirstOrDefault(r => r.workerType == workerData);
        if (requirement == null) 
        {
            Debug.LogWarning($"Worker type {workerData.name} is not allowed in {buildingData.name}.");
            return false; // This worker type is not allowed
        }

        // Check capacity
        if (buildingData.capacityType == WorkerCapacityType.Shared)
        {
            if (GetTotalAssignedWorkerCount() >= GetTotalWorkerCapacity()) return false; // Shared capacity is full
        }
        else // PerType
        {
            if (GetAssignedWorkerCount(workerData) >= requirement.capacity) return false; // This type's capacity is full
        }

        // Try to assign the worker from the manager
        if (WorkerManager.Instance.AssignWorker(workerData))
        {
            assignedWorkers.Add(workerData);
            return true;
        }

        return false;
    }

    public void RemoveWorker(WorkerData workerData)
    {
        // Find the specific worker to remove. Using reference equality.
        WorkerData workerToRemove = assignedWorkers.FirstOrDefault(w => w == workerData);
        if (workerToRemove != null)
        {
            assignedWorkers.Remove(workerToRemove);
            WorkerManager.Instance.ReturnWorker(workerData);
        }
    }

    /// <summary>
    /// Take damage from enemies
    /// Features can react to damage
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        CurrentHealth -= damage;

        OnBuildingDamaged?.Invoke(this, damage);

        // Notify features about damage
        foreach (var feature in buildingData.features)
        {
            if (feature != null)
            {
                feature.OnDamaged(this, damage);
            }
        }

        if (CurrentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    /// <summary>
    /// Destroy this building
    /// Features execute cleanup logic
    /// </summary>
    private void DestroyBuilding()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Notify features about destruction
        foreach (var feature in buildingData.features)
        {
            if (feature != null)
            {
                feature.OnDestroyed(this);
            }
        }

        // Kill all assigned workers (as per design doc)
        foreach (WorkerData worker in assignedWorkers.ToList())
        {
            // Worker is killed, don't return to pool
            Debug.Log($"[Building] Worker killed in {buildingData.buildingName} destruction");
        }
        assignedWorkers.Clear();

        // Unregister from GridManager (free up cells for pathfinding)
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RemoveBuilding(this, gridPosition, width, height);
            Debug.Log($"[Building] Unregistered from GridManager at {gridPosition}");
        }

        // Notify listeners
        OnBuildingDestroyed?.Invoke(this);

        // Notify BuildingManager
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingDestroyed(this);
        }

        Debug.Log($"[Building] {buildingData.buildingName} destroyed!");

        // Destroy GameObject
        Destroy(gameObject);
    }

    /// <summary>
    /// Repair building to full health
    /// </summary>
    public void Repair()
    {
        CurrentHealth = buildingData.maxHealth;
    }

    /// <summary>
    /// Apply curse effect from Demon enemies
    /// Reduces max HP temporarily
    /// </summary>
    public void ApplyCurse(float maxHPReduction, float duration)
    {
        // TODO: Implement curse debuff system
        // For now, just log the effect
        Debug.Log($"[Building] {buildingData.buildingName} cursed! Max HP reduced by {maxHPReduction * 100}% for {duration}s");
    }

    /// <summary>
    /// Repair building by specific amount
    /// </summary>
    public void Repair(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, buildingData.maxHealth);
    }
}
