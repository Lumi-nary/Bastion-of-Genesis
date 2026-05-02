using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EnemyTargetingTests
{
    private readonly List<Object> createdObjects = new List<Object>();
    private BuildingManager buildingManager;
    private EnemyManager enemyManager;
    private GridManager gridManager;
    private Enemy enemy;

    [SetUp]
    public void SetUp()
    {
        ResetSingleton<BuildingManager>();
        ResetSingleton<EnemyManager>();
        ResetSingleton<GridManager>();

        GameObject gridManagerObject = new GameObject("GridManager");
        createdObjects.Add(gridManagerObject);
        gridManager = gridManagerObject.AddComponent<GridManager>();
        SetSingleton(gridManager);
        SetField(gridManager, "cellSize", 1f);

        GameObject buildingManagerObject = new GameObject("BuildingManager");
        createdObjects.Add(buildingManagerObject);
        buildingManager = buildingManagerObject.AddComponent<BuildingManager>();
        SetSingleton(buildingManager);

        GameObject enemyManagerObject = new GameObject("EnemyManager");
        createdObjects.Add(enemyManagerObject);
        enemyManager = enemyManagerObject.AddComponent<EnemyManager>();
        SetSingleton(enemyManager);
        SetField(enemyManager, "aggroRange", 10f);

        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
        createdObjects.Add(enemyData);
        enemyData.attackRange = 2f;

        GameObject enemyObject = new GameObject("Enemy");
        createdObjects.Add(enemyObject);
        enemyObject.transform.position = Vector3.zero;
        enemy = enemyObject.AddComponent<Enemy>();
        enemy.enemyData = enemyData;
    }

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
        ResetSingleton<GridManager>();
        ResetSingleton<BuildingManager>();
        ResetSingleton<EnemyManager>();
    }

    [Test]
    public void GetTargetForEnemy_TurretInRange_BeatsGeneratorExtractorAndFactory()
    {
        Building generator = CreateBuilding("Generator", BuildingCategory.Energy, new Vector3(1f, 0f, 0f));
        Building extractor = CreateBuilding("Extractor", BuildingCategory.Extraction, new Vector3(1f, 1f, 0f));
        Building factory = CreateBuilding("Factory", BuildingCategory.Production, new Vector3(0f, 1f, 0f));
        Building turret = CreateBuilding("Turret", BuildingCategory.Defense, new Vector3(1f, -1f, 0f), ScriptableObject.CreateInstance<TurretFeature>());

        Building target = enemyManager.GetTargetForEnemy(enemy);

        Assert.AreSame(turret, target);
        Assert.AreNotSame(generator, target);
        Assert.AreNotSame(extractor, target);
        Assert.AreNotSame(factory, target);
    }

    [Test]
    public void GetTargetForEnemy_WallInRange_IgnoresWallForRangedAggro()
    {
        Building commandCenter = CreateBuilding("Command Center", BuildingCategory.Command, new Vector3(8f, 0f, 0f));
        Building wall = CreateBuilding("Wall", BuildingCategory.Defense, new Vector3(1f, 0f, 0f), ScriptableObject.CreateInstance<WallFeature>());

        Building target = enemyManager.GetTargetForEnemy(enemy);

        Assert.AreSame(commandCenter, target);
        Assert.AreNotSame(wall, target);
    }

    [Test]
    public void GetTargetForEnemy_NoAggroTargetAttackable_ReturnsCommandCenter()
    {
        Building commandCenter = CreateBuilding("Command Center", BuildingCategory.Command, new Vector3(8f, 0f, 0f));
        Building generator = CreateBuilding("Generator", BuildingCategory.Energy, new Vector3(5f, 0f, 0f));

        Building target = enemyManager.GetTargetForEnemy(enemy);

        Assert.AreSame(commandCenter, target);
        Assert.AreNotSame(generator, target);
    }

    [Test]
    public void GetTargetForEnemy_DestroyedBuildingInRange_IgnoresDestroyedBuilding()
    {
        Building destroyedTurret = CreateBuilding("Destroyed Turret", BuildingCategory.Defense, new Vector3(1f, 0f, 0f), ScriptableObject.CreateInstance<TurretFeature>());
        Building generator = CreateBuilding("Generator", BuildingCategory.Energy, new Vector3(1f, 1f, 0f));
        SetField(destroyedTurret, "isDestroyed", true);

        Building target = enemyManager.GetTargetForEnemy(enemy);

        Assert.AreSame(generator, target);
        Assert.AreNotSame(destroyedTurret, target);
    }

    [Test]
    public void EnemyMovement_WallCellOccupied_IsNotWalkable()
    {
        Building wall = CreateBuilding("Wall", BuildingCategory.Defense, new Vector3(1.5f, 0.5f, 0f), ScriptableObject.CreateInstance<WallFeature>());
        wall.gridPosition = new Vector2Int(1, 0);
        gridManager.PlaceBuilding(wall, new Vector2Int(1, 0), 1, 1);

        MethodInfo method = typeof(Enemy).GetMethod("IsPositionWalkable", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Enemy.IsPositionWalkable should exist");

        bool isWalkable = (bool)method.Invoke(enemy, new object[] { new Vector2(1.5f, 0.5f) });

        Assert.IsFalse(isWalkable);
    }

    [Test]
    public void EnemyMovement_DiagonalBlockedByWall_TargetsWallInsteadOfCuttingCorner()
    {
        Building wall = CreateBuilding("Wall", BuildingCategory.Defense, new Vector3(1.5f, 0.5f, 0f), ScriptableObject.CreateInstance<WallFeature>());
        wall.gridPosition = new Vector2Int(1, 0);
        gridManager.PlaceBuilding(wall, new Vector2Int(1, 0), 1, 1);

        MethodInfo method = typeof(Enemy).GetMethod("TrySetBlockingTargetForDiagonal", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Enemy.TrySetBlockingTargetForDiagonal should exist");

        bool isBlocked = (bool)method.Invoke(enemy, new object[] { Vector2Int.zero, new Vector2Int(1, 1) });

        Assert.IsTrue(isBlocked);
        Assert.AreSame(wall, enemy.CurrentTarget);
    }

    private Building CreateBuilding(string name, BuildingCategory category, Vector3 position, BuildingFeature feature = null)
    {
        BuildingData data = ScriptableObject.CreateInstance<BuildingData>();
        createdObjects.Add(data);
        data.buildingName = name;
        data.category = category;
        data.maxHealth = 100f;
        data.width = 1;
        data.height = 1;

        if (feature != null)
        {
            createdObjects.Add(feature);
            data.features.Add(feature);
        }

        GameObject buildingObject = new GameObject(name);
        createdObjects.Add(buildingObject);
        buildingObject.transform.position = position;

        Building building = buildingObject.AddComponent<Building>();
        SetField(building, "buildingData", data);
        building.gridPosition = Vector2Int.RoundToInt(position);
        building.width = data.width;
        building.height = data.height;

        buildingManager.RegisterBuilding(building);
        return building;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} should exist on {target.GetType().Name}");
        field.SetValue(target, value);
    }

    private static void ResetSingleton<T>()
    {
        FieldInfo field = typeof(T).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Singleton backing field should exist on {typeof(T).Name}");
        field.SetValue(null, null);
    }

    private static void SetSingleton<T>(T instance)
    {
        FieldInfo field = typeof(T).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Singleton backing field should exist on {typeof(T).Name}");
        field.SetValue(null, instance);
    }
}
