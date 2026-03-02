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
        if (ResourceManager.Instance == null) return;

        if (specificResource != null)
        {
            ResourceManager.Instance.AddCapacity(specificResource, storageCapacity);
            Debug.Log($"[Storage] Added {storageCapacity} storage for {specificResource.ResourceName}");
        }
        else
        {
            foreach (var kvp in ResourceManager.Instance.GetAllResources())
            {
                ResourceManager.Instance.AddCapacity(kvp.Key, storageCapacity);
            }
            Debug.Log($"[Storage] Added {storageCapacity} general storage");
        }
    }

    public override void OnDestroyed(Building building)
    {
        if (ResourceManager.Instance == null) return;

        if (specificResource != null)
        {
            ResourceManager.Instance.RemoveCapacity(specificResource, storageCapacity);
            Debug.Log($"[Storage] Removed {storageCapacity} storage for {specificResource.ResourceName}");
        }
        else
        {
            foreach (var kvp in ResourceManager.Instance.GetAllResources())
            {
                ResourceManager.Instance.RemoveCapacity(kvp.Key, storageCapacity);
            }
            Debug.Log($"[Storage] Removed {storageCapacity} general storage");
        }
    }
}
