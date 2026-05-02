using NUnit.Framework;
using UnityEngine;

public class PollutionManagerLimitTests
{
    private GameObject pollutionObject;
    private PollutionManager pollutionManager;

    [SetUp]
    public void SetUp()
    {
        if (PollutionManager.Instance != null)
        {
            Object.DestroyImmediate(PollutionManager.Instance.gameObject);
        }

        pollutionObject = new GameObject("PollutionManagerTest");
        pollutionManager = pollutionObject.AddComponent<PollutionManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (pollutionObject != null)
        {
            Object.DestroyImmediate(pollutionObject);
        }
    }

    [Test]
    public void PollutionChanged_WithMissionLimit_ReportsChapterMaxForUiScale()
    {
        float reportedCurrent = -1f;
        float reportedMax = -1f;

        pollutionManager.ConfigureFromChapter(5000f, 0f, 10f);
        pollutionManager.SetMissionPollutionLimitPercent(10f);
        pollutionManager.OnPollutionChanged += (current, max) =>
        {
            reportedCurrent = current;
            reportedMax = max;
        };

        pollutionManager.AddPollution(1000f);

        Assert.AreEqual(500f, pollutionManager.CurrentPollution);
        Assert.AreEqual(500f, pollutionManager.CurrentPollutionLimit);
        Assert.AreEqual(500f, reportedCurrent);
        Assert.AreEqual(5000f, reportedMax);
        Assert.AreEqual(10f, pollutionManager.PollutionNormalized * 100f);
    }
}
