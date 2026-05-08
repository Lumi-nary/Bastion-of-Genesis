using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PollutionManagerImportTests
{
    private GameObject tileStateObject;
    private GameObject pollutionObject;
    private GameObject commandCenterObject;
    private GameObject gridObject;

    [TearDown]
    public void TearDown()
    {
        SetSingleton<TileStateManager>(null);
        SetSingleton<PollutionManager>(null);

        if (pollutionObject != null)
            Object.DestroyImmediate(pollutionObject);
        if (tileStateObject != null)
            Object.DestroyImmediate(tileStateObject);
        if (commandCenterObject != null)
            Object.DestroyImmediate(commandCenterObject);
        if (gridObject != null)
            Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void ImportState_WithSavedPeakRadius_RebuildsTileSpreadImmediately()
    {
        SetSingleton<TileStateManager>(null);
        SetSingleton<PollutionManager>(null);

        TileStateManager tileStateManager = CreateTileStateManager();

        tileStateManager.ConfigureFromChapter(1f, 0f);
        Assert.AreEqual(GroundState.Alive, tileStateManager.GetGroundState(new Vector2Int(5, 0)));

        pollutionObject = new GameObject("PollutionManagerTest");
        PollutionManager pollutionManager = pollutionObject.AddComponent<PollutionManager>();
        pollutionManager.ConfigureFromChapter(1000f, 0f, 1f);

        pollutionManager.ImportState(new PollutionSaveData
        {
            currentPollution = 0f,
            currentTier = (int)DifficultyTier.Tier1,
            menuDifficulty = (int)Difficulty.Medium,
            peakIntegrationRadius = 6f
        });

        Assert.AreEqual(6f, tileStateManager.GetPollutionRadius());
        Assert.AreEqual(GroundState.Polluted, tileStateManager.GetGroundState(new Vector2Int(5, 0)));
    }

    private TileStateManager CreateTileStateManager()
    {
        gridObject = new GameObject("Grid");
        gridObject.AddComponent<Grid>();
        Tilemap tilemap = gridObject.AddComponent<Tilemap>();

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        for (int x = -8; x <= 8; x++)
        {
            for (int y = -8; y <= 8; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }

        commandCenterObject = new GameObject("CommandCenter");
        commandCenterObject.transform.position = Vector3.zero;

        tileStateObject = new GameObject("TileStateManagerTest");
        TileStateManager manager = tileStateObject.AddComponent<TileStateManager>();
        SetSingleton(manager);

        SetPrivateField(manager, "terrainTilemap", tilemap);
        SetPrivateField(manager, "pollutionCenter", commandCenterObject.transform);
        InvokePrivateMethod(manager, "Start");

        return manager;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist.");
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist.");
        method.Invoke(target, null);
    }

    private static void SetSingleton<T>(T value)
    {
        FieldInfo field = typeof(T).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Singleton backing field for '{typeof(T).Name}' should exist.");
        field.SetValue(null, value);
    }
}
