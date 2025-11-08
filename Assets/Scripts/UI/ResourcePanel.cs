using System.Collections.Generic;
using UnityEngine;

public class ResourcePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject resourceSlotPrefab;
    [SerializeField] private Transform container;

    private Dictionary<ResourceType, ResourceSlotUI> resourceSlots = new Dictionary<ResourceType, ResourceSlotUI>();

    private void Start()
    {
        ResourceManager.Instance.OnResourceChanged += UpdateResourceDisplay;
        InitializePanel();
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= UpdateResourceDisplay;
        }
    }

    private void InitializePanel()
    {
        // Clear any existing slots in case of re-initialization
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        resourceSlots.Clear();

        // Create slots for all resources that already exist in the manager
        if (ResourceManager.Instance != null)
        {
            foreach (var resource in ResourceManager.Instance.ResourceAmounts)
            {
                UpdateResourceDisplay(resource.Key, resource.Value);
            }
        }
    }

    private void UpdateResourceDisplay(ResourceType type, int amount)
    {
        if (resourceSlots.ContainsKey(type))
        {
            // Slot exists
            if (amount > 0)
            {
                // Update existing slot
                resourceSlots[type].UpdateAmount(amount);
            }
            else
            {
                // Amount is zero, destroy the slot
                Destroy(resourceSlots[type].gameObject);
                resourceSlots.Remove(type);
            }
        }
        else
        {
            // Slot does not exist
            if (amount > 0)
            {
                // Create a new slot
                GameObject slotGO = Instantiate(resourceSlotPrefab, container);
                ResourceSlotUI slotUI = slotGO.GetComponent<ResourceSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(type, amount);
                    resourceSlots.Add(type, slotUI);
                }
            }
        }
    }
}
