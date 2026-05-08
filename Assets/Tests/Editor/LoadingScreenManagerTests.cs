using NUnit.Framework;

public class LoadingScreenManagerTests
{
    [Test]
    public void NormalizeAsyncProgress_MapsUnityAsyncRangeToUnitRange()
    {
        Assert.AreEqual(0f, LoadingScreenManager.NormalizeAsyncProgress(0f));
        Assert.AreEqual(0.5f, LoadingScreenManager.NormalizeAsyncProgress(0.45f), 0.0001f);
        Assert.AreEqual(1f, LoadingScreenManager.NormalizeAsyncProgress(0.9f), 0.0001f);
    }

    [Test]
    public void NormalizeAsyncProgress_ClampsOutOfRangeValues()
    {
        Assert.AreEqual(0f, LoadingScreenManager.NormalizeAsyncProgress(-0.2f));
        Assert.AreEqual(1f, LoadingScreenManager.NormalizeAsyncProgress(1f));
    }
}
