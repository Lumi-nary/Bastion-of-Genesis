using UnityEngine;

/// <summary>
/// Storage feature - Increases resource storage capacity
/// Examples: Warehouse, Resource Vault
/// </summary>
[CreateAssetMenu(fileName = "Feature_Storage", menuName = "Planetfall/Building Features/Storage")]
public class StorageFeature : BuildingFeature
{
    [Header("Storage Configuration")]
    [Tooltip("Resource type to store (null = all resources)")]
    public ResourceType specificResource;

    [Tooltip("Storage capacity added")]
    public int storageCapacity = 500;

    public override void OnBuilt(Building building)
    {
        // Increase storage capacity when built
        if (ResourceManager.Instance != null)
        {
            if (specificResource != null)
            {
                // TODO: Add specific resource storage
                Debug.Log($"[Storage] Added {storageCapacity} storage for {specificResource.ResourceName}");
            }
            else
            {
                // TODO: Add general storage
                Debug.Log($"[Storage] Added {storageCapacity} general storage");
            }
        }
    }

    public override void OnDestroyed(Building building)
    {
        // Decrease storage capacity when destroyed
        if (ResourceManager.Instance != null)
        {
            if (specificResource != null)
            {
                // TODO: Remove specific resource storage
                Debug.Log($"[Storage] Removed {storageCapacity} storage for {specificResource.ResourceName}");
            }
            else
            {
                // TODO: Remove general storage
                Debug.Log($"[Storage] Removed {storageCapacity} general storage");
            }
        }
    }
}
