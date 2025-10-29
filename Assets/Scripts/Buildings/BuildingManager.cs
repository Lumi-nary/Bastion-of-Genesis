using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

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

    public void PlaceBuilding(BuildingData building, Vector3 position)
    {
        if (building == null)
        {
            return; // No building selected
        }

        if (HasEnoughResources(building.resourceCost) && HasEnoughBuilders(building))
        {
            SpendResources(building.resourceCost);
            ConsumeBuilders(building);
            Instantiate(building.prefab, position, Quaternion.identity);
        }
        else
        {
            Debug.Log("Not enough resources or builders to build " + building.buildingName);
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
}
