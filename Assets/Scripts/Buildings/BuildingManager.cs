using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Tracking")]
    private List<Building> allBuildings = new List<Building>();
    public IReadOnlyList<Building> AllBuildings => allBuildings;

    // Events
    public event System.Action<Building> OnBuildingPlaced;
    public event System.Action<Building> OnBuildingDestroyedEvent;

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
    }

    public void PlaceBuilding(BuildingData buildingData, Vector3 position)
    {
        if (buildingData == null)
        {
            return; // No building selected
        }

        if (HasEnoughResources(buildingData.resourceCost) && HasEnoughBuilders(buildingData))
        {
            SpendResources(buildingData.resourceCost);
            ConsumeBuilders(buildingData);

            GameObject newBuildingGO = Instantiate(buildingData.prefab, position, Quaternion.identity);
            Building newBuilding = newBuildingGO.GetComponent<Building>();

            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(position);

            // Position is the CENTER for all buildings, calculate bottom-left for grid tracking
            int centerOffsetX = buildingData.width / 2;
            int centerOffsetY = buildingData.height / 2;
            Vector2Int bottomLeftGridPos = new Vector2Int(gridPos.x - centerOffsetX, gridPos.y - centerOffsetY);

            newBuilding.gridPosition = bottomLeftGridPos;
            newBuilding.width = buildingData.width;
            newBuilding.height = buildingData.height;

            // Add BoxCollider2D for collision detection (blocks enemies, prevents overlapping buildings)
            BoxCollider2D collider = newBuildingGO.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = newBuildingGO.AddComponent<BoxCollider2D>();
            }
            // Set collider size to match building footprint (in world units, 1 grid cell = 1 unit)
            collider.size = new Vector2(buildingData.width, buildingData.height);
            collider.offset = Vector2.zero; // Center of sprite

            GridManager.Instance.PlaceBuilding(newBuilding, bottomLeftGridPos, buildingData.width, buildingData.height);

            // Check if building is an extractor on an ore mound
            if (buildingData.HasFeature<ResourceExtractorFeature>())
            {
                ResourceExtractorFeature extractor = buildingData.GetFeature<ResourceExtractorFeature>();
                if (extractor.requiresOreMound && OreMoundManager.Instance != null)
                {
                    // Position is already the center for all buildings
                    Vector3 centerWorldPos = position;
                    OreMound mound = OreMoundManager.Instance.GetMoundAtPosition(centerWorldPos, 0.5f);

                    if (mound != null && mound.CanPlaceExtractor())
                    {
                        mound.PlaceExtractor(newBuilding);
                        Debug.Log($"[BuildingManager] {buildingData.buildingName} placed on {mound.GetMoundTypeName()} at {centerWorldPos} - ore mound hidden");
                    }
                    else
                    {
                        Debug.LogWarning($"[BuildingManager] Could not find ore mound to claim at center {centerWorldPos}");
                    }
                }
            }

            // Track building
            allBuildings.Add(newBuilding);
            OnBuildingPlaced?.Invoke(newBuilding);
        }
        else
        {
            Debug.Log("Not enough resources or builders to build " + buildingData.buildingName);
        }
    }

    private bool HasEnoughResources(List<ResourceCost> cost)
    {
        foreach (var resourceCost in cost)
        {
            if (ResourceManager.Instance.GetResourceAmount(resourceCost.resourceType) < resourceCost.amount)
            {
                return false;
            }
        }
        return true;
    }

    private void SpendResources(List<ResourceCost> cost)
    {
        foreach (var resourceCost in cost)
        {
            ResourceManager.Instance.RemoveResource(resourceCost.resourceType, resourceCost.amount);
        }
    }

    private bool HasEnoughBuilders(BuildingData buildingData)
    {
        if (buildingData.builderType != null && buildingData.buildersConsumed > 0)
        {
            return WorkerManager.Instance.GetAvailableWorkerCount(buildingData.builderType) >= buildingData.buildersConsumed;
        }
        return true; // No builders required
    }

    private void ConsumeBuilders(BuildingData buildingData)
    {
        if (buildingData.builderType != null && buildingData.buildersConsumed > 0)
        {
            for (int i = 0; i < buildingData.buildersConsumed; i++)
            {
                WorkerManager.Instance.AssignWorker(buildingData.builderType);
            }
        }
    }

    /// <summary>
    /// Register a building (for scene-placed buildings that aren't spawned via PlaceBuilding)
    /// </summary>
    public void RegisterBuilding(Building building)
    {
        if (building != null && !allBuildings.Contains(building))
        {
            allBuildings.Add(building);
            Debug.Log($"[BuildingManager] Registered scene-placed building: {building.BuildingData?.buildingName ?? building.name}");
        }
    }

    /// <summary>
    /// Called when a building is destroyed (by enemies or player)
    /// </summary>
    public void OnBuildingDestroyed(Building building)
    {
        if (allBuildings.Contains(building))
        {
            allBuildings.Remove(building);
            OnBuildingDestroyedEvent?.Invoke(building);

            Debug.Log($"[BuildingManager] Building destroyed: {building.BuildingData.buildingName}");
        }
    }

    /// <summary>
    /// Get all buildings of a specific category
    /// </summary>
    public List<Building> GetBuildingsByCategory(BuildingCategory category)
    {
        List<Building> result = new List<Building>();
        foreach (Building building in allBuildings)
        {
            if (building.BuildingData.category == category)
            {
                result.Add(building);
            }
        }
        return result;
    }

    /// <summary>
    /// Get all buildings of a specific type
    /// </summary>
    public List<Building> GetBuildingsByType(BuildingData buildingData)
    {
        List<Building> result = new List<Building>();
        foreach (Building building in allBuildings)
        {
            if (building.BuildingData == buildingData)
            {
                result.Add(building);
            }
        }
        return result;
    }

    /// <summary>
    /// Get all buildings (used by enemy abilities for AOE, etc.)
    /// </summary>
    public List<Building> GetAllBuildings()
    {
        return new List<Building>(allBuildings);
    }
}
