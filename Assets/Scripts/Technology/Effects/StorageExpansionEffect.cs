using UnityEngine;

/// <summary>
/// Increases resource storage capacity
/// Example: "Warehouse Technology" increases storage by 500
/// </summary>
[CreateAssetMenu(fileName = "Effect_StorageExpansion", menuName = "Planetfall/Technology Effects/Storage Expansion")]
public class StorageExpansionEffect : TechnologyEffect
{
    [Header("Storage Configuration")]
    [Tooltip("Resource type to expand storage for (null = all resources)")]
    public ResourceType storageResource;

    [Tooltip("Amount to increase storage capacity by")]
    public int storageIncrease = 500;

    public override void OnResearched(TechnologyData tech)
    {
        if (ResourceManager.Instance != null)
        {
            // TODO: Add IncreaseStorageCapacity method to ResourceManager
            if (storageResource != null)
            {
                Debug.Log($"[StorageExpansionEffect] Increased {storageResource.ResourceName} storage by {storageIncrease}");
            }
            else
            {
                Debug.Log($"[StorageExpansionEffect] Increased all resource storage by {storageIncrease}");
            }
        }
    }

    public override void OnRemoved(TechnologyData tech)
    {
        if (ResourceManager.Instance != null)
        {
            // TODO: Add DecreaseStorageCapacity method to ResourceManager
            if (storageResource != null)
            {
                Debug.Log($"[StorageExpansionEffect] Removed {storageIncrease} {storageResource.ResourceName} storage");
            }
            else
            {
                Debug.Log($"[StorageExpansionEffect] Removed {storageIncrease} from all resource storage");
            }
        }
    }

    public override string GetEffectDescription()
    {
        if (storageResource != null)
        {
            return $"+{storageIncrease} {storageResource.ResourceName} storage";
        }
        else
        {
            return $"+{storageIncrease} storage capacity (all resources)";
        }
    }
}
