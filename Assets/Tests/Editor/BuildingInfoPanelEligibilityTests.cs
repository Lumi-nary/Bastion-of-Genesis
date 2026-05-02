using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BuildingInfoPanelEligibilityTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void ShouldShowFullInfoPanel_WallOnlyBuilding_ReturnsFalse()
    {
        BuildingData data = CreateBuildingData("Iron Wall", CreateFeature<WallFeature>());

        Assert.IsFalse(BuildingInfoPanel.ShouldShowFullInfoPanel(data));
    }

    [Test]
    public void ShouldShowFullInfoPanel_WorkerBuilding_ReturnsTrue()
    {
        BuildingData data = CreateBuildingData("Workshop");
        data.workerRequirements.Add(new WorkerRequirement
        {
            capacity = 2,
            requiredCount = 1
        });

        Assert.IsTrue(BuildingInfoPanel.ShouldShowFullInfoPanel(data));
    }

    [Test]
    public void ShouldShowFullInfoPanel_InspectableFeatureBuildings_ReturnTrue()
    {
        AssertInspectableFeature(CreateFeature<ResourceExtractorFeature>());
        AssertInspectableFeature(CreateFeature<ResourceProductionFeature>());
        AssertInspectableFeature(CreateFeature<ResourceConversionFeature>());
        AssertInspectableFeature(CreateFeature<StorageFeature>());
        AssertInspectableFeature(CreateFeature<TurretFeature>());
    }

    private void AssertInspectableFeature(BuildingFeature feature)
    {
        BuildingData data = CreateBuildingData(feature.GetType().Name, feature);

        Assert.IsTrue(BuildingInfoPanel.ShouldShowFullInfoPanel(data), $"{feature.GetType().Name} should open the full panel.");
    }

    private T CreateFeature<T>() where T : BuildingFeature
    {
        T feature = ScriptableObject.CreateInstance<T>();
        createdObjects.Add(feature);
        return feature;
    }

    private BuildingData CreateBuildingData(string name, BuildingFeature feature = null)
    {
        BuildingData buildingData = ScriptableObject.CreateInstance<BuildingData>();
        createdObjects.Add(buildingData);
        buildingData.buildingName = name;
        buildingData.maxHealth = 100f;

        if (feature != null)
        {
            buildingData.features.Add(feature);
        }

        return buildingData;
    }
}
