using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class BuildingHoverPopupUITests
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
    public void BuildRequirementSummary_WithResourcesBuilderAndWorkers_IncludesAllRequirements()
    {
        ResourceType iron = CreateResource("Iron");
        WorkerData builder = CreateWorker("Builder");
        WorkerData engineer = CreateWorker("Engineer");
        BuildingData data = CreateBuildingData("Extractor");
        data.builderType = builder;
        data.buildersConsumed = 2;
        data.resourceCost.Add(new ResourceCost { resourceType = iron, amount = 50 });
        data.workerRequirements.Add(new WorkerRequirement
        {
            workerType = engineer,
            requiredCount = 1,
            capacity = 3
        });

        string summary = BuildingHoverPopupUI.BuildRequirementSummary(data);

        StringAssert.Contains("Build: 2 Builder", summary);
        StringAssert.Contains("50 Iron", summary);
        StringAssert.Contains("Operate: 1 Engineer required, 3 capacity", summary);
    }

    [Test]
    public void BuildBuiltBuildingStatusSummary_OperationalState_UsesExpectedLabels()
    {
        Assert.AreEqual("Workers: 2 / 3\nOperational", BuildingHoverPopupUI.BuildBuiltBuildingStatusSummary(2, 3, true));
        Assert.AreEqual("Workers: 0 / 3\nNot Operational", BuildingHoverPopupUI.BuildBuiltBuildingStatusSummary(0, 3, false));
    }

    [Test]
    public void ShouldShowOperationalStatus_NoWorkerRequirements_ReturnsFalse()
    {
        BuildingData data = CreateBuildingData("Iron Wall");

        Assert.IsFalse(BuildingHoverPopupUI.ShouldShowOperationalStatus(data));
    }

    [Test]
    public void ShouldShowOperationalStatus_WithWorkerRequirements_ReturnsTrue()
    {
        BuildingData data = CreateBuildingData("Extractor");
        data.workerRequirements.Add(new WorkerRequirement
        {
            workerType = CreateWorker("Engineer"),
            requiredCount = 1,
            capacity = 2
        });

        Assert.IsTrue(BuildingHoverPopupUI.ShouldShowOperationalStatus(data));
    }

    [Test]
    public void BuildBuiltBuildingHealthSummary_ClampsAndOmitsOperationalText()
    {
        string summary = BuildingHoverPopupUI.BuildBuiltBuildingHealthSummary(75f, 100f);

        Assert.AreEqual("Health: 75 / 100 HP", summary);
        Assert.IsFalse(summary.Contains("Operational"));
    }

    private BuildingData CreateBuildingData(string name)
    {
        BuildingData data = ScriptableObject.CreateInstance<BuildingData>();
        createdObjects.Add(data);
        data.buildingName = name;
        return data;
    }

    private WorkerData CreateWorker(string name)
    {
        WorkerData data = ScriptableObject.CreateInstance<WorkerData>();
        createdObjects.Add(data);
        data.workerName = name;
        return data;
    }

    private ResourceType CreateResource(string name)
    {
        ResourceType data = ScriptableObject.CreateInstance<ResourceType>();
        createdObjects.Add(data);
        SerializedObjectUtility.SetPrivateField(data, "resourceName", name);
        return data;
    }

    private static class SerializedObjectUtility
    {
        public static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = typeof(T).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
