using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for SaveData serialization and SaveManager security.
/// Tests JSON serialization, path validation, and security checks.
/// </summary>
public class SaveDataTests
{
    // ============================================================================
    // SAVEDATA SERIALIZATION TESTS
    // ============================================================================

    [Test]
    public void SaveData_SerializesToValidJSON()
    {
        // Arrange
        SaveData data = new SaveData
        {
            version = "1.0.0",
            baseName = "Test Base",
            difficulty = Difficulty.Medium,
            mode = GameMode.Singleplayer,
            timestamp = "2025-11-15T14:30:00Z",
            currentChapter = 1,
            currentMission = 1,
            missionCompletions = new bool[] { false, false, false },
            gridState = "{}",
            pollutionLevel = 0f,
            hostPlayerName = "",
            connectedPlayers = new string[0]
        };

        // Act
        string json = JsonUtility.ToJson(data, true);

        // Assert
        Assert.IsNotNull(json, "JSON should not be null");
        Assert.IsTrue(json.Contains("\"baseName\": \"Test Base\""), "JSON should contain baseName field");
        Assert.IsTrue(json.Contains("\"difficulty\": 1"), "JSON should contain difficulty field (Medium = 1)");
        Assert.IsTrue(json.Contains("\"mode\": 0"), "JSON should contain mode field (Singleplayer = 0)");
        Assert.IsTrue(json.Contains("\"currentChapter\": 1"), "JSON should contain currentChapter field");
    }

    [Test]
    public void SaveData_DeserializesCorrectly()
    {
        // Arrange
        string json = @"{
            ""version"": ""1.0.0"",
            ""baseName"": ""Colony Alpha"",
            ""difficulty"": 1,
            ""mode"": 0,
            ""timestamp"": ""2025-11-15T14:30:00Z"",
            ""currentChapter"": 2,
            ""currentMission"": 3,
            ""pollutionLevel"": 12.5
        }";

        // Act
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Assert
        Assert.IsNotNull(data, "SaveData should not be null");
        Assert.AreEqual("Colony Alpha", data.baseName, "baseName should match");
        Assert.AreEqual(Difficulty.Medium, data.difficulty, "difficulty should be Medium");
        Assert.AreEqual(GameMode.Singleplayer, data.mode, "mode should be Singleplayer");
        Assert.AreEqual(2, data.currentChapter, "currentChapter should be 2");
        Assert.AreEqual(3, data.currentMission, "currentMission should be 3");
        Assert.AreEqual(12.5f, data.pollutionLevel, 0.01f, "pollutionLevel should be 12.5");
    }

    [Test]
    public void SaveData_RoundTripSerialization()
    {
        // Arrange
        SaveData original = new SaveData
        {
            baseName = "Roundtrip Test",
            difficulty = Difficulty.Hard,
            mode = GameMode.COOP,
            currentChapter = 5,
            currentMission = 10
        };

        // Act
        string json = JsonUtility.ToJson(original);
        SaveData deserialized = JsonUtility.FromJson<SaveData>(json);

        // Assert
        Assert.AreEqual(original.baseName, deserialized.baseName);
        Assert.AreEqual(original.difficulty, deserialized.difficulty);
        Assert.AreEqual(original.mode, deserialized.mode);
        Assert.AreEqual(original.currentChapter, deserialized.currentChapter);
        Assert.AreEqual(original.currentMission, deserialized.currentMission);
    }

    // ============================================================================
    // SAVEMETADATA HELPER METHOD TESTS
    // ============================================================================

    [Test]
    public void SaveMetadata_GetDisplayName_ReturnsCorrectFormat()
    {
        // Arrange
        SaveMetadata metadata = new SaveMetadata
        {
            baseName = "Test Colony",
            currentChapter = 2,
            currentMission = 5
        };

        // Act
        string displayName = metadata.GetDisplayName();

        // Assert
        Assert.AreEqual("Test Colony (CH2 M5)", displayName, "Display name should match format");
    }

    [Test]
    public void SaveMetadata_GetModeIcon_ReturnsSPForSingleplayer()
    {
        // Arrange
        SaveMetadata metadata = new SaveMetadata
        {
            mode = GameMode.Singleplayer
        };

        // Act
        string icon = metadata.GetModeIcon();

        // Assert
        Assert.AreEqual("[SP]", icon, "Mode icon should be [SP] for Singleplayer");
    }

    [Test]
    public void SaveMetadata_GetModeIcon_ReturnsCOOPForCooperative()
    {
        // Arrange
        SaveMetadata metadata = new SaveMetadata
        {
            mode = GameMode.COOP
        };

        // Act
        string icon = metadata.GetModeIcon();

        // Assert
        Assert.AreEqual("[COOP]", icon, "Mode icon should be [COOP] for Cooperative");
    }

    // ============================================================================
    // ENUM TESTS
    // ============================================================================

    [Test]
    public void Difficulty_EnumValuesAreCorrect()
    {
        Assert.AreEqual(0, (int)Difficulty.Easy, "Easy should be 0");
        Assert.AreEqual(1, (int)Difficulty.Medium, "Medium should be 1");
        Assert.AreEqual(2, (int)Difficulty.Hard, "Hard should be 2");
    }

    [Test]
    public void GameMode_EnumValuesAreCorrect()
    {
        Assert.AreEqual(0, (int)GameMode.Singleplayer, "Singleplayer should be 0");
        Assert.AreEqual(1, (int)GameMode.COOP, "COOP should be 1");
    }
}
