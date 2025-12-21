using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // Singleton instance
    public static ResourceManager Instance { get; private set; }

    // Dictionaries to store resource amounts and capacities
    private Dictionary<ResourceType, int> resourceAmounts = new Dictionary<ResourceType, int>();
    public IReadOnlyDictionary<ResourceType, int> ResourceAmounts => resourceAmounts;
    private Dictionary<ResourceType, int> resourceCapacities = new Dictionary<ResourceType, int>();

    // Track registered resource types for reset
    private HashSet<ResourceType> registeredTypes = new HashSet<ResourceType>();

    // Event to notify when a resource amount changes
    public event System.Action<ResourceType, int> OnResourceChanged;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ResourceManager persists across scenes but is RESET by MissionChapterManager
        // when starting each chapter. This allows:
        // 1. Resources to persist during gameplay within a chapter
        // 2. Clean slate for each new chapter via ResetAllResources()
        // 3. Chapter-specific starting resources via ChapterData
        DontDestroyOnLoad(gameObject);

        // No auto-initialization - ChapterData handles starting amounts
        // MissionChapterManager.InitializeChapterResources() will register types and set amounts
    }

    /// <summary>
    /// Helper to find the registered ResourceType instance that matches the given type by name.
    /// This prevents "split brain" issues where different instances of the same ScriptableObject
    /// (e.g. from Network vs Inspector) are treated as different keys.
    /// </summary>
    private ResourceType GetCanonicalType(ResourceType type)
    {
        if (type == null) return null;
        
        // Fast path: if the exact instance is already a key, return it
        if (resourceAmounts.ContainsKey(type)) return type;
        
        // Slow path: find by name
        foreach (var key in resourceAmounts.Keys)
        {
            if (key.ResourceName == type.ResourceName)
                return key;
        }
        
        // Not found, return the input type (it will likely be registered shortly)
        return type;
    }

    /// <summary>
    /// Register a resource type with its base capacity. Called by MissionChapterManager.
    /// </summary>
    public void RegisterResourceType(ResourceType resourceType, int startingAmount = 0)
    {
        if (resourceType == null) return;

        // Use canonical type if it exists (though Register implies it might not)
        ResourceType canonical = GetCanonicalType(resourceType);

        registeredTypes.Add(canonical);
        resourceCapacities[canonical] = canonical.BaseCapacity;
        resourceAmounts[canonical] = startingAmount;
        OnResourceChanged?.Invoke(canonical, startingAmount);

        Debug.Log($"[ResourceManager] Registered {canonical.ResourceName}: {startingAmount}/{canonical.BaseCapacity}");
    }

    /// <summary>
    /// Adds a specified amount of a resource, respecting the capacity.
    /// </summary>
    /// <param name="resourceType">The type of resource to add.</param>
    /// <param name="amount">The amount to add.</param>
    public void AddResource(ResourceType resourceType, int amount)
    {
        ResourceType canonical = GetCanonicalType(resourceType);
        if (amount < 0)
        {
            Debug.LogWarning("Cannot add a negative amount of resources. Use RemoveResource instead.");
            return;
        }

        if (!resourceAmounts.ContainsKey(canonical))
        {
            Debug.LogWarning($"Resource type {canonical.ResourceName} not initialized.");
            return;
        }

        int currentAmount = resourceAmounts[canonical];
        int capacity = GetResourceCapacity(canonical);
        int newAmount = Mathf.Min(currentAmount + amount, capacity);

        if (newAmount > currentAmount)
        {
            resourceAmounts[canonical] = newAmount;
            OnResourceChanged?.Invoke(canonical, newAmount);
        }
    }

    /// <summary>
    /// Removes a specified amount of a resource.
    /// </summary>
    /// <param name="resourceType">The type of resource to remove.</param>
    /// <param name="amount">The amount to remove.</param>
    /// <returns>True if the resources were successfully removed, false otherwise.</returns>
    public bool RemoveResource(ResourceType resourceType, int amount)
    {
        ResourceType canonical = GetCanonicalType(resourceType);
        if (amount < 0)
        {
            Debug.LogWarning("Cannot remove a negative amount of resources. Use AddResource instead.");
            return false;
        }

        if (!resourceAmounts.ContainsKey(canonical) || resourceAmounts[canonical] < amount)
        {
            return false; // Not enough resources
        }

        resourceAmounts[canonical] -= amount;
        OnResourceChanged?.Invoke(canonical, resourceAmounts[canonical]);
        return true;
    }

    /// <summary>
    /// Gets the current amount of a specified resource.
    /// </summary>
    /// <param name="resourceType">The type of resource to check.</param>
    /// <returns>The current amount of the resource.</returns>
    public int GetResourceAmount(ResourceType resourceType)
    {
        ResourceType canonical = GetCanonicalType(resourceType);
        if (canonical != null && resourceAmounts.ContainsKey(canonical))
        {
            return resourceAmounts[canonical];
        }

        return 0;
    }

    /// <summary>
    /// Gets the capacity of a specified resource.
    /// </summary>
    /// <param name="resourceType">The type of resource to check.</param>
    /// <returns>The capacity of the resource.</returns>
    public int GetResourceCapacity(ResourceType resourceType)
    {
        ResourceType canonical = GetCanonicalType(resourceType);
        if (canonical != null && resourceCapacities.ContainsKey(canonical))
        {
            return resourceCapacities[canonical];
        }

        return 0;
    }

    /// <summary>
    /// Gets a copy of all current resource amounts
    /// </summary>
    /// <returns>Dictionary of resource types and their current amounts</returns>
    public Dictionary<ResourceType, int> GetAllResources()
    {
        return new Dictionary<ResourceType, int>(resourceAmounts);
    }

    /// <summary>
    /// Check if there's capacity for more of a resource.
    /// </summary>
    public bool HasCapacityFor(ResourceType resourceType, int amount = 1)
    {
        int current = GetResourceAmount(resourceType);
        int capacity = GetResourceCapacity(resourceType);
        return current + amount <= capacity;
    }

    /// <summary>
    /// Get remaining capacity for a resource.
    /// </summary>
    public int GetRemainingCapacity(ResourceType resourceType)
    {
        int current = GetResourceAmount(resourceType);
        int capacity = GetResourceCapacity(resourceType);
        return Mathf.Max(0, capacity - current);
    }

    /// <summary>
    /// Check if resource is at max capacity.
    /// </summary>
    public bool IsAtCapacity(ResourceType resourceType)
    {
        return GetResourceAmount(resourceType) >= GetResourceCapacity(resourceType);
    }

    /// <summary>
    /// Set resource amount directly. Used for network sync.
    /// </summary>
    public void SetResourceAmount(ResourceType resourceType, int amount)
    {
        if (resourceType == null) return;
        ResourceType canonical = GetCanonicalType(resourceType);
        
        if (!resourceAmounts.ContainsKey(canonical)) 
        {
            RegisterResourceType(canonical, amount);
            return;
        }

        int capacity = GetResourceCapacity(canonical);
        resourceAmounts[canonical] = Mathf.Clamp(amount, 0, capacity);
        OnResourceChanged?.Invoke(canonical, resourceAmounts[canonical]);
    }

    /// <summary>
    /// Set capacity directly. Used for network sync.
    /// </summary>
    public void SetCapacity(ResourceType resourceType, int capacity)
    {
        if (resourceType == null) return;
        ResourceType canonical = GetCanonicalType(resourceType);
        
        if (!resourceCapacities.ContainsKey(canonical))
        {
            RegisterResourceType(canonical, 0);
        }

        resourceCapacities[canonical] = capacity;
        
        // Clamp current amount if it exceeds new capacity
        if (resourceAmounts.ContainsKey(canonical) && resourceAmounts[canonical] > capacity)
        {
            resourceAmounts[canonical] = capacity;
            OnResourceChanged?.Invoke(canonical, capacity);
        }
    }

    /// <summary>
    /// Reset all resources to zero and capacities to base values.
    /// Called by MissionChapterManager when starting a new chapter.
    /// </summary>
    public void ResetAllResources()
    {
        // Clear everything for fresh chapter start
        resourceAmounts.Clear();
        resourceCapacities.Clear();
        registeredTypes.Clear();

        Debug.Log("[ResourceManager] All resources reset (amounts, capacities, registrations cleared)");
    }

    /// <summary>
    /// Add to the capacity of a resource type. Used by buildings (StorageFeature) and research.
    /// </summary>
    public void AddCapacity(ResourceType resourceType, int amount)
    {
        ResourceType canonical = GetCanonicalType(resourceType);
        if (canonical == null || !resourceCapacities.ContainsKey(canonical)) return;

        resourceCapacities[canonical] += amount;
        Debug.Log($"[ResourceManager] {canonical.ResourceName} capacity increased by {amount} to {resourceCapacities[canonical]}");
    }

    /// <summary>
    /// Remove from the capacity of a resource type. Used when buildings are destroyed.
    /// </summary>
    public void RemoveCapacity(ResourceType resourceType, int amount)
    {
        ResourceType canonical = GetCanonicalType(resourceType);
        if (canonical == null || !resourceCapacities.ContainsKey(canonical)) return;

        resourceCapacities[canonical] = Mathf.Max(0, resourceCapacities[canonical] - amount);

        // Clamp current amount to new capacity
        if (resourceAmounts[canonical] > resourceCapacities[canonical])
        {
            resourceAmounts[canonical] = resourceCapacities[canonical];
            OnResourceChanged?.Invoke(canonical, resourceAmounts[canonical]);
        }

        Debug.Log($"[ResourceManager] {canonical.ResourceName} capacity decreased by {amount} to {resourceCapacities[canonical]}");
    }
}
