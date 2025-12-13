using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // Singleton instance
    public static ResourceManager Instance { get; private set; }

    [Header("Resource Configuration")]
    [SerializeField] private List<ResourceData> startingResources = new List<ResourceData>();

    // Dictionaries to store resource amounts and capacities
    private Dictionary<ResourceType, int> resourceAmounts = new Dictionary<ResourceType, int>();
    public IReadOnlyDictionary<ResourceType, int> ResourceAmounts => resourceAmounts;
    private Dictionary<ResourceType, int> resourceCapacities = new Dictionary<ResourceType, int>();

    // Event to notify when a resource amount changes
    public event System.Action<ResourceType, int> OnResourceChanged;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if the resource manager should persist across scenes
        }

        InitializeResources();
    }

    private void InitializeResources()
    {
        foreach (var resourceData in startingResources)
        {
            resourceAmounts[resourceData.resourceType] = resourceData.startingAmount;
            resourceCapacities[resourceData.resourceType] = resourceData.capacity;
            OnResourceChanged?.Invoke(resourceData.resourceType, resourceData.startingAmount);
        }
    }

    /// <summary>
    /// Adds a specified amount of a resource, respecting the capacity.
    /// </summary>
    /// <param name="resourceType">The type of resource to add.</param>
    /// <param name="amount">The amount to add.</param>
    public void AddResource(ResourceType resourceType, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot add a negative amount of resources. Use RemoveResource instead.");
            return;
        }

        if (!resourceAmounts.ContainsKey(resourceType))
        {
            Debug.LogWarning($"Resource type {resourceType.ResourceName} not initialized.");
            return;
        }

        int currentAmount = resourceAmounts[resourceType];
        int capacity = GetResourceCapacity(resourceType);
        int newAmount = Mathf.Min(currentAmount + amount, capacity);

        if (newAmount > currentAmount)
        {
            resourceAmounts[resourceType] = newAmount;
            OnResourceChanged?.Invoke(resourceType, newAmount);
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
        if (amount < 0)
        {
            Debug.LogWarning("Cannot remove a negative amount of resources. Use AddResource instead.");
            return false;
        }

        if (!resourceAmounts.ContainsKey(resourceType) || resourceAmounts[resourceType] < amount)
        {
            return false; // Not enough resources
        }

        resourceAmounts[resourceType] -= amount;
        OnResourceChanged?.Invoke(resourceType, resourceAmounts[resourceType]);
        return true;
    }

    /// <summary>
    /// Gets the current amount of a specified resource.
    /// </summary>
    /// <param name="resourceType">The type of resource to check.</param>
    /// <returns>The current amount of the resource.</returns>
    public int GetResourceAmount(ResourceType resourceType)
    {
        if (resourceAmounts.ContainsKey(resourceType))
        {
            return resourceAmounts[resourceType];
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
        if (resourceCapacities.ContainsKey(resourceType))
        {
            return resourceCapacities[resourceType];
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
}
