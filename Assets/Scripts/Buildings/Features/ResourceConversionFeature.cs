using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Building feature for manually converting resources into other resources.
/// Example: Command Center converts Energy + Metal into Robotic Swarm.
/// </summary>
[CreateAssetMenu(fileName = "Feature_ResourceConversion", menuName = "Planetfall/Building Features/Resource Conversion")]
public class ResourceConversionFeature : BuildingFeature
{
    [Header("Conversion Configuration")]
    [Tooltip("Resources consumed per conversion")]
    public List<ResourceCost> inputCost = new List<ResourceCost>();

    [Tooltip("Resource produced per conversion")]
    public ResourceType outputResource;

    [Tooltip("Amount of output resource produced")]
    public int outputAmount = 1;

    [Tooltip("Time in seconds to complete one conversion")]
    public float conversionTime = 5f;

    [Tooltip("Maximum conversions that can be queued")]
    public int maxQueueSize = 4;

    public List<ResourceCost> GetInputCost() => inputCost;
    public ResourceType GetOutputResource() => outputResource;
    public int GetOutputAmount() => outputAmount;
    public float GetConversionTime() => conversionTime;
    public int GetMaxQueueSize() => maxQueueSize;

    public override void OnBuilt(Building building)
    {
        // Add ResourceConverterComponent to the building
        ResourceConverterComponent converter = building.gameObject.AddComponent<ResourceConverterComponent>();
        converter.Initialize(building, this);
        Debug.Log($"[ResourceConversionFeature] Initialized converter for {outputResource?.ResourceName ?? "Unknown"} on {building.BuildingData.buildingName}");
    }
}

/// <summary>
/// Runtime component for resource conversion.
/// Handles queue and conversion timing.
/// </summary>
public class ResourceConverterComponent : MonoBehaviour
{
    private Building building;
    private ResourceConversionFeature conversionFeature;

    // Conversion state
    private int queueCount = 0;
    private float conversionProgress = 0f;
    private bool isConverting = false;

    // Events
    public event System.Action OnQueueChanged;
    public event System.Action<float> OnProgressChanged;

    public int QueueCount => queueCount;
    public int MaxQueueSize => conversionFeature?.GetMaxQueueSize() ?? 0;
    public float ConversionProgress => conversionProgress;
    public float ConversionTime => conversionFeature?.GetConversionTime() ?? 1f;
    public bool IsConverting => isConverting;
    public ResourceType OutputResource => conversionFeature?.GetOutputResource();
    public int OutputAmount => conversionFeature?.GetOutputAmount() ?? 1;
    public Building Building => building;
    public List<ResourceCost> InputCost => conversionFeature?.GetInputCost();

    public void Initialize(Building building, ResourceConversionFeature feature)
    {
        this.building = building;
        this.conversionFeature = feature;

        // Register with BuildingManager
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterConverter(this);
        }
    }

    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.UnregisterConverter(this);
        }
    }

    private void Update()
    {
        if (!isConverting || conversionFeature == null) return;

        conversionProgress += Time.deltaTime;
        OnProgressChanged?.Invoke(conversionProgress);

        if (conversionProgress >= conversionFeature.GetConversionTime())
        {
            CompleteConversion();
        }
    }

    /// <summary>
    /// Queue a resource conversion.
    /// </summary>
    public bool QueueConversion()
    {
        if (conversionFeature == null) return false;
        if (queueCount >= conversionFeature.GetMaxQueueSize()) return false;

        // Check input resources
        if (!HasEnoughResources(conversionFeature.GetInputCost())) return false;

        // Check output resource capacity (current + queued across all converters)
        ResourceType outputType = conversionFeature.GetOutputResource();
        int outputAmount = conversionFeature.GetOutputAmount();

        if (outputType != null && ResourceManager.Instance != null && BuildingManager.Instance != null)
        {
            int currentAmount = ResourceManager.Instance.GetResourceAmount(outputType);
            int totalQueued = BuildingManager.Instance.GetTotalQueuedConversions(outputType) * outputAmount;
            int capacity = ResourceManager.Instance.GetResourceCapacity(outputType);

            if (currentAmount + totalQueued + outputAmount > capacity)
            {
                Debug.Log($"[ResourceConverter] Cannot queue {outputType.ResourceName} - at max capacity ({capacity})");
                return false;
            }
        }

        // Spend resources
        SpendResources(conversionFeature.GetInputCost());

        queueCount++;
        OnQueueChanged?.Invoke();

        if (!isConverting)
        {
            StartConversion();
        }

        return true;
    }

    /// <summary>
    /// Cancel one conversion from queue.
    /// </summary>
    public bool CancelConversion()
    {
        if (queueCount <= 0) return false;

        // Refund resources
        RefundResources(conversionFeature.GetInputCost());

        queueCount--;
        OnQueueChanged?.Invoke();

        if (queueCount == 0)
        {
            StopConversion();
        }

        return true;
    }

    private void StartConversion()
    {
        isConverting = true;
        conversionProgress = 0f;
        OnProgressChanged?.Invoke(conversionProgress);
    }

    private void StopConversion()
    {
        isConverting = false;
        conversionProgress = 0f;
        OnProgressChanged?.Invoke(conversionProgress);
    }

    private void CompleteConversion()
    {
        // Add output resource
        ResourceType output = conversionFeature.GetOutputResource();
        int amount = conversionFeature.GetOutputAmount();

        if (output != null && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(output, amount);
            Debug.Log($"[ResourceConverter] Produced {amount} {output.ResourceName}");
        }

        queueCount--;
        OnQueueChanged?.Invoke();

        if (queueCount > 0)
        {
            StartConversion();
        }
        else
        {
            StopConversion();
        }
    }

    private bool HasEnoughResources(List<ResourceCost> cost)
    {
        if (ResourceManager.Instance == null) return false;

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
        if (ResourceManager.Instance == null) return;

        foreach (var resourceCost in cost)
        {
            ResourceManager.Instance.RemoveResource(resourceCost.resourceType, resourceCost.amount);
        }
    }

    private void RefundResources(List<ResourceCost> cost)
    {
        if (ResourceManager.Instance == null) return;

        foreach (var resourceCost in cost)
        {
            ResourceManager.Instance.AddResource(resourceCost.resourceType, resourceCost.amount);
        }
    }
}
