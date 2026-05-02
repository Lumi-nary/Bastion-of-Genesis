using NUnit.Framework;
using UnityEngine;

public class SettingsDataTests
{
    [Test]
    public void SettingsData_Clone_PreservesTutorialEnabledAndPlayerName()
    {
        SettingsData settings = new SettingsData();
        settings.tutorialEnabled = false;
        settings.playerName = "Kyra";

        SettingsData clone = settings.Clone();

        Assert.IsFalse(clone.tutorialEnabled);
        Assert.AreEqual("Kyra", clone.playerName);
    }

    [Test]
    public void SettingsManager_NormalizeSettings_OldJsonDefaultsTutorialAndPlayerName()
    {
        string oldJson = "{\"masterVolume\":0.8,\"musicVolume\":0.5,\"sfxVolume\":0.6,\"voiceVolume\":0.7,\"resolutionWidth\":1920,\"resolutionHeight\":1080,\"windowMode\":1,\"fullscreen\":false}";
        SettingsData settings = JsonUtility.FromJson<SettingsData>(oldJson);

        bool changed = SettingsManager.NormalizeSettings(settings, oldJson);

        Assert.IsTrue(changed);
        Assert.IsTrue(settings.tutorialEnabled);
        Assert.AreEqual(SettingsData.DefaultPlayerName, settings.playerName);
    }

    [Test]
    public void SettingsManager_NormalizeSettings_BlankPlayerNameDefaultsToPlayer()
    {
        string json = "{\"tutorialEnabled\":false,\"playerName\":\"   \"}";
        SettingsData settings = JsonUtility.FromJson<SettingsData>(json);

        bool changed = SettingsManager.NormalizeSettings(settings, json);

        Assert.IsTrue(changed);
        Assert.IsFalse(settings.tutorialEnabled);
        Assert.AreEqual(SettingsData.DefaultPlayerName, settings.playerName);
    }

    [Test]
    public void SettingsData_NormalizePlayerName_TrimsAndClamps()
    {
        string longName = "  CommanderPlayerNameLongerThanLimit  ";

        string normalized = SettingsData.NormalizePlayerName(longName);

        Assert.AreEqual(SettingsData.MaxPlayerNameLength, normalized.Length);
        Assert.AreEqual("CommanderPlayerNameLonge", normalized);
    }
}
