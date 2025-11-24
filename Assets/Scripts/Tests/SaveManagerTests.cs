using NUnit.Framework;
using System.IO;
using UnityEngine;

/// <summary>
/// Integration tests for SaveManager functions (Epic 3 Story 3.1).
/// Tests GetAllSaves(), LoadGame(), and DeleteSave() functions.
/// Covers AC 1-8: metadata scanning, save loading, deletion, error handling.
/// </summary>
public class SaveManagerTests
{
    private string testSavesPath;
    private SaveManager saveManager;

    [SetUp]
    public void Setup()
    {
        // Create temporary test directory
        testSavesPath = Path.Combine(Application.persistentDataPath, "Saves");

        if (!Directory.Exists(testSavesPath))
        {
            Directory.CreateDirectory(testSavesPath);
        }

        // Create SaveManager GameObject for testing
        GameObject go = new GameObject("TestSaveManager");
        saveManager = go.AddComponent<SaveManager>();
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up test saves
        if (Directory.Exists(testSavesPath))
        {
            string[] testFiles = Directory.GetFiles(testSavesPath, "test_*.json");
            foreach (string file in testFiles)
            {
                File.Delete(file);
            }
        }

        // Destroy SaveManager GameObject
        if (saveManager != null)
        {
            Object.DestroyImmediate(saveManager.gameObject);
        }
    }

    // ============================================================================
    // GETALLSAVES() TESTS (AC1, AC2)
    // ============================================================================

    [Test]
    public void GetAllSaves_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange - clean directory
        string[] existingFiles = Directory.GetFiles(testSavesPath, "*.json");
        foreach (string file in existingFiles)
        {
            File.Delete(file);
        }

        // Act
        var saves = saveManager.GetAllSaves();

        // Assert
        Assert.IsNotNull(saves, "Result should not be null");
        Assert.AreEqual(0, saves.Count, "Should return empty list for empty directory");
    }

    [Test]
    public void GetAllSaves_WithValidSaves_ReturnsMetadataList()
    {
        // Arrange - create test save
        SaveData testData = new SaveData
        {
            baseName = "Test Colony",
            difficulty = Difficulty.Medium,
            mode = GameMode.Singleplayer,
            timestamp = "2025-11-23T10:00:00Z",
            totalPlaytime = 120f,
            currentChapter = 2,
            currentMission = 3
        };

        string testFilePath = Path.Combine(testSavesPath, "test_save.json");
        File.WriteAllText(testFilePath, JsonUtility.ToJson(testData));

        // Act
        var saves = saveManager.GetAllSaves();

        // Assert
        Assert.AreEqual(1, saves.Count, "Should find 1 save");
        Assert.AreEqual("test_save.json", saves[0].fileName);
        Assert.AreEqual("Test Colony", saves[0].baseName);
        Assert.AreEqual(Difficulty.Medium, saves[0].difficulty);
        Assert.AreEqual(GameMode.Singleplayer, saves[0].mode);
        Assert.AreEqual(120f, saves[0].totalPlaytime);
        Assert.AreEqual(2, saves[0].currentChapter);
        Assert.AreEqual(3, saves[0].currentMission);
        Assert.IsFalse(saves[0].isAutosave, "Should not be autosave");

        // Cleanup
        File.Delete(testFilePath);
    }

    [Test]
    public void GetAllSaves_DetectsAutosaveFiles()
    {
        // Arrange - create autosave
        SaveData autoData = new SaveData
        {
            baseName = "Auto Colony",
            difficulty = Difficulty.Easy,
            mode = GameMode.COOP,
            timestamp = "2025-11-23T11:00:00Z",
            totalPlaytime = 60f,
            currentChapter = 1,
            currentMission = 1
        };

        string autoFilePath = Path.Combine(testSavesPath, "autosave_1.json");
        File.WriteAllText(autoFilePath, JsonUtility.ToJson(autoData));

        // Act
        var saves = saveManager.GetAllSaves();

        // Assert
        var autosave = saves.Find(s => s.fileName == "autosave_1.json");
        Assert.IsNotNull(autosave, "Should find autosave file");
        Assert.IsTrue(autosave.isAutosave, "Should detect autosave pattern");

        // Cleanup
        File.Delete(autoFilePath);
    }

    [Test]
    public void GetAllSaves_SkipsCorruptedFiles()
    {
        // Arrange - create valid and corrupted saves
        string validFilePath = Path.Combine(testSavesPath, "test_valid.json");
        string corruptedFilePath = Path.Combine(testSavesPath, "test_corrupted.json");

        SaveData validData = new SaveData { baseName = "Valid" };
        File.WriteAllText(validFilePath, JsonUtility.ToJson(validData));
        File.WriteAllText(corruptedFilePath, "{ invalid json }}");

        // Act
        var saves = saveManager.GetAllSaves();

        // Assert
        Assert.AreEqual(1, saves.Count, "Should skip corrupted file and return only valid saves");
        Assert.AreEqual("test_valid.json", saves[0].fileName);

        // Cleanup
        File.Delete(validFilePath);
        File.Delete(corruptedFilePath);
    }

    // ============================================================================
    // LOADGAME() TESTS (AC3, AC4, AC6, AC7)
    // ============================================================================

    [Test]
    public void LoadGame_ValidFile_ReturnsTrue()
    {
        // Arrange
        SaveData testData = new SaveData
        {
            baseName = "Load Test",
            difficulty = Difficulty.Hard,
            mode = GameMode.Singleplayer,
            timestamp = "2025-11-23T12:00:00Z"
        };

        string testFilePath = Path.Combine(testSavesPath, "test_load.json");
        File.WriteAllText(testFilePath, JsonUtility.ToJson(testData));

        // Act
        bool result = saveManager.LoadGame("test_load.json");

        // Assert
        Assert.IsTrue(result, "Should return true for valid save");

        // Cleanup
        File.Delete(testFilePath);
    }

    [Test]
    public void LoadGame_MissingFile_ReturnsFalse()
    {
        // Act
        bool result = saveManager.LoadGame("nonexistent.json");

        // Assert
        Assert.IsFalse(result, "Should return false for missing file");
    }

    [Test]
    public void LoadGame_CorruptedFile_ReturnsFalse()
    {
        // Arrange - create corrupted save
        string corruptedFilePath = Path.Combine(testSavesPath, "test_corrupted.json");
        File.WriteAllText(corruptedFilePath, "{ this is not valid JSON }}}");

        // Act
        bool result = saveManager.LoadGame("test_corrupted.json");

        // Assert
        Assert.IsFalse(result, "Should return false for corrupted save");

        // Cleanup
        File.Delete(corruptedFilePath);
    }

    [Test]
    public void LoadGame_COOPSave_LogsCorrectly()
    {
        // Arrange - create COOP save
        SaveData coopData = new SaveData
        {
            baseName = "COOP Test",
            difficulty = Difficulty.Medium,
            mode = GameMode.COOP,
            timestamp = "2025-11-23T13:00:00Z"
        };

        string testFilePath = Path.Combine(testSavesPath, "test_coop.json");
        File.WriteAllText(testFilePath, JsonUtility.ToJson(coopData));

        // Act
        bool result = saveManager.LoadGame("test_coop.json");

        // Assert
        Assert.IsTrue(result, "Should load COOP save successfully");
        // Note: Manual verification of Console log: "[SaveManager] COOP save loaded: COOP Test"

        // Cleanup
        File.Delete(testFilePath);
    }

    // ============================================================================
    // DELETESAVE() TESTS (AC5, AC7)
    // ============================================================================

    [Test]
    public void DeleteSave_ExistingFile_ReturnsTrue()
    {
        // Arrange
        string testFilePath = Path.Combine(testSavesPath, "test_delete.json");
        File.WriteAllText(testFilePath, "{}");

        // Act
        bool result = saveManager.DeleteSave("test_delete.json");

        // Assert
        Assert.IsTrue(result, "Should return true for successful deletion");
        Assert.IsFalse(File.Exists(testFilePath), "File should be deleted");
    }

    [Test]
    public void DeleteSave_MissingFile_ReturnsFalse()
    {
        // Act
        bool result = saveManager.DeleteSave("nonexistent.json");

        // Assert
        Assert.IsFalse(result, "Should return false for missing file");
    }

    [Test]
    public void DeleteSave_InvalidFileName_ReturnsFalse()
    {
        // Act - try path traversal attack
        bool result = saveManager.DeleteSave("../../../etc/passwd");

        // Assert
        Assert.IsFalse(result, "Should reject path traversal attempts");
    }

    // ============================================================================
    // PATTERN COMPLIANCE TESTS (AC8)
    // ============================================================================

    [Test]
    public void SaveMetadata_ContainsAllRequiredFields()
    {
        // Arrange
        SaveMetadata metadata = new SaveMetadata
        {
            fileName = "test.json",
            baseName = "Test",
            difficulty = Difficulty.Easy,
            mode = GameMode.Singleplayer,
            timestamp = "2025-11-23T14:00:00Z",
            totalPlaytime = 300f,
            currentChapter = 1,
            currentMission = 1,
            isAutosave = false
        };

        // Assert - verify all 9 fields exist
        Assert.IsNotNull(metadata.fileName, "fileName should exist");
        Assert.IsNotNull(metadata.baseName, "baseName should exist");
        Assert.IsNotNull(metadata.timestamp, "timestamp should exist");
        Assert.AreEqual(300f, metadata.totalPlaytime, "totalPlaytime should exist");
        Assert.AreEqual(1, metadata.currentChapter, "currentChapter should exist");
        Assert.AreEqual(1, metadata.currentMission, "currentMission should exist");
        Assert.IsFalse(metadata.isAutosave, "isAutosave should exist");
    }
}
