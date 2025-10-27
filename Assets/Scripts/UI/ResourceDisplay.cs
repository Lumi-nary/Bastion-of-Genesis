using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResourceDisplay : MonoBehaviour
{
    [System.Serializable]
    public class ResourceDisplayItem
    {
        public ResourceType resourceType;
        public TextMeshProUGUI displayText;
    }

    [Header("UI References")]
    [SerializeField] private List<ResourceDisplayItem> resourceDisplayItems = new List<ResourceDisplayItem>();

    private Dictionary<ResourceType, TextMeshProUGUI> displayMapping = new Dictionary<ResourceType, TextMeshProUGUI>();

    private void Awake()
    {
        // Create a dictionary for fast lookups
        foreach (var item in resourceDisplayItems)
        {
            if (item.resourceType != null && item.displayText != null)
            {
                displayMapping[item.resourceType] = item.displayText;
            }
        }
    }

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
            InitializeDisplay();
        }
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
        }
    }

    private void InitializeDisplay()
    {
        foreach (var item in displayMapping)
        {
            int currentAmount = ResourceManager.Instance.GetResourceAmount(item.Key);
            item.Value.text = $"{item.Key.ResourceName}: {currentAmount} / {ResourceManager.Instance.GetResourceCapacity(item.Key)}";
        }
    }

    private void HandleResourceChanged(ResourceType resourceType, int newAmount)
    {
        if (displayMapping.ContainsKey(resourceType))
        {
            displayMapping[resourceType].text = $"{resourceType.ResourceName}: {newAmount} / {ResourceManager.Instance.GetResourceCapacity(resourceType)}";
        }
    }
}
