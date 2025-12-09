using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resource production feature - Produces resources over time
/// Requires input resources and workers
/// Examples: Worker Factory, Ammo Factory
/// </summary>
[CreateAssetMenu(fileName = "Feature_ResourceProduction", menuName = "Planetfall/Building Features/Resource Production")]
public class ResourceProductionFeature : BuildingFeature
{
    [Header("Production Configuration")]
    [Tooltip("Resources consumed per production cycle")]
    public List<ResourceCost> inputResources = new List<ResourceCost>();

    [Tooltip("Resource produced")]
    public ResourceType outputResource;

    [Tooltip("Amount produced per cycle")]
    public int outputAmount = 1;

    [Tooltip("Production cycle time (seconds)")]
    public float productionCycle = 10f;

    private float productionTimer = 0f;

    public override void OnOperate(Building building)
    {
        // Only produce if operational (has required workers)
        if (!building.IsOperational) return;

        // Check if we have energy
        if (!CanFunctionWithoutEnergy() && EnergyManager.Instance != null && !EnergyManager.Instance.HasEnergy)
        {
            return;
        }

        // Production cycle
        productionTimer += Time.deltaTime;
        if (productionTimer >= productionCycle)
        {
            productionTimer -= productionCycle;

            // Check if we have input resources
            if (ResourceManager.Instance != null && HasInputResources())
            {
                // Consume input resources
                ConsumeInputResources();

                // Produce output resource
                ResourceManager.Instance.AddResource(outputResource, outputAmount);
                Debug.Log($"[ResourceProduction] {building.BuildingData.buildingName} produced {outputAmount} {outputResource.ResourceName}");
            }
        }
    }

    private bool HasInputResources()
    {
        if (ResourceManager.Instance == null) return false;

        foreach (var cost in inputResources)
        {
            if (ResourceManager.Instance.GetResourceAmount(cost.resourceType) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }

    private void ConsumeInputResources()
    {
        if (ResourceManager.Instance == null) return;

        foreach (var cost in inputResources)
        {
            ResourceManager.Instance.RemoveResource(cost.resourceType, cost.amount);
        }
    }

    public override void OnBuilt(Building building)
    {
        productionTimer = 0f;
    }
}
