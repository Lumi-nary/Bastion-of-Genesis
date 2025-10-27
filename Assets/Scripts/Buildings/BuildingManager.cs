using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    private BuildingData selectedBuilding;

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

    public void SelectBuilding(BuildingData building)
    {
        selectedBuilding = building;
    }

    public void PlaceBuilding(Vector3 position)
    {
        if (selectedBuilding == null)
        {
            return; // No building selected
        }

        if (HasEnoughResources(selectedBuilding.resourceCost) && HasEnoughBuilders(selectedBuilding))
        {
            SpendResources(selectedBuilding.resourceCost);
            ConsumeBuilders(selectedBuilding);
            Instantiate(selectedBuilding.prefab, position, Quaternion.identity);
            // Deselect building after placing
            selectedBuilding = null; 
        }
        else
        {
            Debug.Log("Not enough resources or builders to build " + selectedBuilding.buildingName);
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
