using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Tracking")]
    private List<Building> allBuildings = new List<Building>();
    public IReadOnlyList<Building> AllBuildings => allBuildings;

    // Factory tracking
    private List<WorkerFactoryComponent> allFactories = new List<WorkerFactoryComponent>();
    private Dictionary<WorkerData, List<WorkerFactoryComponent>> factoriesByWorkerType = new Dictionary<WorkerData, List<WorkerFactoryComponent>>();

    // Converter tracking
    private List<ResourceConverterComponent> allConverters = new List<ResourceConverterComponent>();
    private Dictionary<ResourceType, List<ResourceConverterComponent>> convertersByOutputType = new Dictionary<ResourceType, List<ResourceConverterComponent>>();

    // Events
    public event System.Action<Building> OnBuildingPlaced;
    public event System.Action<Building> OnBuildingDestroyedEvent;
    public event System.Action OnFactoriesChanged;
    public event System.Action OnConvertersChanged;

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

    // ============================================================================
    // FACTORY MANAGEMENT
    // ============================================================================

    /// <summary>
    /// Register a factory when it's built.
    /// </summary>
    public void RegisterFactory(WorkerFactoryComponent factory)
    {
        if (factory == null || allFactories.Contains(factory)) return;

        allFactories.Add(factory);

        WorkerData workerType = factory.WorkerType;
        if (workerType != null)
        {
            if (!factoriesByWorkerType.ContainsKey(workerType))
            {
                factoriesByWorkerType[workerType] = new List<WorkerFactoryComponent>();
            }
            factoriesByWorkerType[workerType].Add(factory);
        }

        OnFactoriesChanged?.Invoke();
    }

    /// <summary>
    /// Unregister a factory when it's destroyed.
    /// </summary>
    public void UnregisterFactory(WorkerFactoryComponent factory)
    {
        if (factory == null) return;

        allFactories.Remove(factory);

        WorkerData workerType = factory.WorkerType;
        if (workerType != null && factoriesByWorkerType.ContainsKey(workerType))
        {
            factoriesByWorkerType[workerType].Remove(factory);
            if (factoriesByWorkerType[workerType].Count == 0)
            {
                factoriesByWorkerType.Remove(workerType);
            }
        }

        OnFactoriesChanged?.Invoke();
    }

    /// <summary>
    /// Get all factories.
    /// </summary>
    public List<WorkerFactoryComponent> GetAllFactories()
    {
        return new List<WorkerFactoryComponent>(allFactories);
    }

    /// <summary>
    /// Get factories by worker type.
    /// </summary>
    public List<WorkerFactoryComponent> GetFactoriesForWorkerType(WorkerData workerType)
    {
        if (workerType != null && factoriesByWorkerType.ContainsKey(workerType))
        {
            return new List<WorkerFactoryComponent>(factoriesByWorkerType[workerType]);
        }
        return new List<WorkerFactoryComponent>();
    }

    /// <summary>
    /// Get all worker types that have factories.
    /// </summary>
    public List<WorkerData> GetAvailableWorkerTypes()
    {
        return new List<WorkerData>(factoriesByWorkerType.Keys);
    }

    /// <summary>
    /// Get total factory count for a worker type.
    /// </summary>
    public int GetFactoryCount(WorkerData workerType)
    {
        if (workerType != null && factoriesByWorkerType.ContainsKey(workerType))
        {
            return factoriesByWorkerType[workerType].Count;
        }
        return 0;
    }

    /// <summary>
    /// Get total queued workers across all factories for a type.
    /// </summary>
    public int GetTotalQueuedForType(WorkerData workerType)
    {
        int total = 0;
        if (workerType != null && factoriesByWorkerType.ContainsKey(workerType))
        {
            foreach (var factory in factoriesByWorkerType[workerType])
            {
                total += factory.QueueCount;
            }
        }
        return total;
    }

    /// <summary>
    /// Get total max queue size across all factories for a type.
    /// </summary>
    public int GetTotalMaxQueueForType(WorkerData workerType)
    {
        int total = 0;
        if (workerType != null && factoriesByWorkerType.ContainsKey(workerType))
        {
            foreach (var factory in factoriesByWorkerType[workerType])
            {
                total += factory.MaxQueueSize;
            }
        }
        return total;
    }

    /// <summary>
    /// Queue a worker at the first available factory of that type.
    /// </summary>
    public bool QueueWorker(WorkerData workerType)
    {
        if (workerType == null || !factoriesByWorkerType.ContainsKey(workerType))
            return false;

        foreach (var factory in factoriesByWorkerType[workerType])
        {
            if (factory.QueueCount < factory.MaxQueueSize)
            {
                return factory.QueueWorker();
            }
        }

        return false;
    }

    /// <summary>
    /// Cancel a worker from the last factory with queued workers of that type.
    /// </summary>
    public bool CancelWorker(WorkerData workerType)
    {
        if (workerType == null || !factoriesByWorkerType.ContainsKey(workerType))
            return false;

        for (int i = factoriesByWorkerType[workerType].Count - 1; i >= 0; i--)
        {
            var factory = factoriesByWorkerType[workerType][i];
            if (factory.QueueCount > 0)
            {
                return factory.CancelWorker();
            }
        }

        return false;
    }

    // ============================================================================
    // RESOURCE CONVERTER MANAGEMENT
    // ============================================================================

    /// <summary>
    /// Register a converter when it's built.
    /// </summary>
    public void RegisterConverter(ResourceConverterComponent converter)
    {
        if (converter == null || allConverters.Contains(converter)) return;

        allConverters.Add(converter);

        ResourceType outputType = converter.OutputResource;
        if (outputType != null)
        {
            if (!convertersByOutputType.ContainsKey(outputType))
            {
                convertersByOutputType[outputType] = new List<ResourceConverterComponent>();
            }
            convertersByOutputType[outputType].Add(converter);
        }

        OnConvertersChanged?.Invoke();
    }

    /// <summary>
    /// Unregister a converter when it's destroyed.
    /// </summary>
    public void UnregisterConverter(ResourceConverterComponent converter)
    {
        if (converter == null) return;

        allConverters.Remove(converter);

        ResourceType outputType = converter.OutputResource;
        if (outputType != null && convertersByOutputType.ContainsKey(outputType))
        {
            convertersByOutputType[outputType].Remove(converter);
            if (convertersByOutputType[outputType].Count == 0)
            {
                convertersByOutputType.Remove(outputType);
            }
        }

        OnConvertersChanged?.Invoke();
    }

    /// <summary>
    /// Get all converters.
    /// </summary>
    public List<ResourceConverterComponent> GetAllConverters()
    {
        return new List<ResourceConverterComponent>(allConverters);
    }

    /// <summary>
    /// Get converters by output resource type.
    /// </summary>
    public List<ResourceConverterComponent> GetConvertersForResourceType(ResourceType resourceType)
    {
        if (resourceType != null && convertersByOutputType.ContainsKey(resourceType))
        {
            return new List<ResourceConverterComponent>(convertersByOutputType[resourceType]);
        }
        return new List<ResourceConverterComponent>();
    }

    /// <summary>
    /// Get all resource types that have converters.
    /// </summary>
    public List<ResourceType> GetAvailableConversionTypes()
    {
        return new List<ResourceType>(convertersByOutputType.Keys);
    }

    /// <summary>
    /// Queue a conversion at the first available converter for that resource type.
    /// </summary>
    public bool QueueConversion(ResourceType resourceType)
    {
        if (resourceType == null || !convertersByOutputType.ContainsKey(resourceType))
            return false;

        foreach (var converter in convertersByOutputType[resourceType])
        {
            if (converter.QueueCount < converter.MaxQueueSize)
            {
                return converter.QueueConversion();
            }
        }

        return false;
    }

    /// <summary>
    /// Cancel a conversion from the last converter with queued conversions of that type.
    /// </summary>
    public bool CancelConversion(ResourceType resourceType)
    {
        if (resourceType == null || !convertersByOutputType.ContainsKey(resourceType))
            return false;

        for (int i = convertersByOutputType[resourceType].Count - 1; i >= 0; i--)
        {
            var converter = convertersByOutputType[resourceType][i];
            if (converter.QueueCount > 0)
            {
                return converter.CancelConversion();
            }
        }

        return false;
    }

    /// <summary>
    /// Get total queued conversions for a resource type.
    /// </summary>
    public int GetTotalQueuedConversions(ResourceType resourceType)
    {
        int total = 0;
        if (resourceType != null && convertersByOutputType.ContainsKey(resourceType))
        {
            foreach (var converter in convertersByOutputType[resourceType])
            {
                total += converter.QueueCount;
            }
        }
        return total;
    }

    /// <summary>
    /// Get total max queue size for conversions of a resource type.
    /// </summary>
    public int GetTotalMaxConversionQueue(ResourceType resourceType)
    {
        int total = 0;
        if (resourceType != null && convertersByOutputType.ContainsKey(resourceType))
        {
            foreach (var converter in convertersByOutputType[resourceType])
            {
                total += converter.MaxQueueSize;
            }
        }
        return total;
    }
}
