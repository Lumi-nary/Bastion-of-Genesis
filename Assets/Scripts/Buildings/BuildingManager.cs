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

    public void PlaceBuilding(BuildingData buildingData, Vector3 position)
    {
        if (buildingData == null)
        {
            return; // No building selected
        }

        if (HasEnoughResources(buildingData.resourceCost) && HasEnoughBuilders(buildingData))
        {
            SpendResources(buildingData.resourceCost);
            ConsumeBuilders(buildingData);
            
            GameObject newBuildingGO = Instantiate(buildingData.prefab, position, Quaternion.identity);
            Building newBuilding = newBuildingGO.GetComponent<Building>();

            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(position);
            newBuilding.gridPosition = gridPos;
            newBuilding.width = buildingData.width;
            newBuilding.height = buildingData.height;

            GridManager.Instance.PlaceBuilding(newBuilding, gridPos, buildingData.width, buildingData.height);
        }
        else
        {
            Debug.Log("Not enough resources or builders to build " + buildingData.buildingName);
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
