using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unified manager for tile-based visual environment objects (trees, rocks, etc.)
/// Spawns GameObjects based on TileProperty types and manages lifecycle
/// Note: OreMounds are manually placed in scenes and managed by OreMoundManager
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("Tree prefab (must have Tree component)")]
    [SerializeField] private GameObject treePrefab;

    [Tooltip("Large rock prefab (must have VisualObject component)")]
    [SerializeField] private GameObject largRockPrefab;

    [Header("Containers")]
    [Tooltip("Parent transform for trees")]
    [SerializeField] private Transform treeContainer;

    [Tooltip("Parent transform for environment objects (rocks, etc.)")]
    [SerializeField] private Transform environmentContainer;

    [Header("Default Sprites")]
    [Tooltip("Default tree sprites (fallback if TileProperty doesn't provide)")]
    [SerializeField] private Sprite defaultHealthyTreeSprite;
    [SerializeField] private Sprite defaultWitherTreeSprite;
    [SerializeField] private Sprite defaultDeadTreeSprite;

    // Track all spawned objects by grid position
    private Dictionary<Vector2Int, VisualObject> objectsByPosition = new Dictionary<Vector2Int, VisualObject>();

    // Track objects by type
    private Dictionary<Vector2Int, Tree> trees = new Dictionary<Vector2Int, Tree>();

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

        // Create containers if not assigned
        if (treeContainer == null)
        {
            treeContainer = new GameObject("Trees").transform;
            treeContainer.SetParent(transform);
        }

        if (environmentContainer == null)
        {
            environmentContainer = new GameObject("Environment").transform;
            environmentContainer.SetParent(transform);
        }
    }

    private void Start()
    {
        // Spawn objects after GridManager initializes
        SpawnAllEnvironmentObjects();
    }

    /// <summary>
    /// Scan tilemap for all environment property tiles and spawn visual GameObjects
    /// </summary>
    public void SpawnAllEnvironmentObjects()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("[EnvironmentManager] GridManager not found!");
            return;
        }

        Debug.Log("[EnvironmentManager] Scanning for environment objects...");

        // Spawn trees
        SpawnObjectsOfType<TreeProperty>(SpawnTree);

        // Add more types as needed
        // SpawnObjectsOfType<RockProperty>(SpawnRock);

        Debug.Log($"[EnvironmentManager] Spawned {objectsByPosition.Count} total objects.");
    }

    /// <summary>
    /// Generic method to spawn objects of a specific TileProperty type
    /// </summary>
    private void SpawnObjectsOfType<T>(System.Func<Vector2Int, VisualObject> spawnFunc) where T : TileProperty
    {
        List<Vector2Int> positions = GridManager.Instance.GetTilesWithProperty<T>();
        Debug.Log($"[EnvironmentManager] Found {positions.Count} tiles with {typeof(T).Name}");

        foreach (Vector2Int pos in positions)
        {
            spawnFunc?.Invoke(pos);
        }
    }

    /// <summary>
    /// Spawn a tree at grid position
    /// </summary>
    public Tree SpawnTree(Vector2Int gridPos)
    {
        // Check if object already exists
        if (objectsByPosition.ContainsKey(gridPos))
        {
            return objectsByPosition[gridPos] as Tree;
        }

        if (treePrefab == null)
        {
            Debug.LogError("[EnvironmentManager] Tree prefab not assigned!");
            return null;
        }

        // Get TileData and TreeProperty
        TileData tileData = GridManager.Instance.GetTileData(gridPos);
        if (tileData == null) return null;

        TreeProperty treeProperty = tileData.GetProperty<TreeProperty>();
        if (treeProperty == null) return null;

        // Spawn GameObject
        Vector3 worldPos = GridManager.Instance.GridToWorldPosition(gridPos);
        GameObject treeObj = Instantiate(treePrefab, worldPos, Quaternion.identity, treeContainer);
        treeObj.name = $"Tree_{gridPos.x}_{gridPos.y}";

        Tree tree = treeObj.GetComponent<Tree>();
        if (tree == null)
        {
            Debug.LogError("[EnvironmentManager] Tree prefab missing Tree component!");
            Destroy(treeObj);
            return null;
        }

        // Get sprites (from property or defaults)
        Sprite healthySprite = treeProperty.healthySprite != null ? treeProperty.healthySprite : defaultHealthyTreeSprite;
        Sprite witherSprite = treeProperty.witherSprite != null ? treeProperty.witherSprite : defaultWitherTreeSprite;
        Sprite deadSprite = treeProperty.deadSprite != null ? treeProperty.deadSprite : defaultDeadTreeSprite;

        // Initialize
        tree.Initialize(gridPos, treeProperty, healthySprite, witherSprite, deadSprite);

        // Register with property
        treeProperty.RegisterTreeGameObject(tree);

        // Track
        objectsByPosition[gridPos] = tree;
        trees[gridPos] = tree;

        return tree;
    }

    /// <summary>
    /// Remove object at grid position
    /// </summary>
    public void RemoveObject(Vector2Int gridPos)
    {
        if (objectsByPosition.TryGetValue(gridPos, out VisualObject obj))
        {
            objectsByPosition.Remove(gridPos);
            trees.Remove(gridPos);
            obj.DestroyObject();
        }
    }

    /// <summary>
    /// Get any object at grid position
    /// </summary>
    public VisualObject GetObject(Vector2Int gridPos)
    {
        objectsByPosition.TryGetValue(gridPos, out VisualObject obj);
        return obj;
    }

    /// <summary>
    /// Get tree at grid position
    /// </summary>
    public Tree GetTree(Vector2Int gridPos)
    {
        trees.TryGetValue(gridPos, out Tree tree);
        return tree;
    }

    /// <summary>
    /// Clear all environment objects
    /// </summary>
    public void ClearAll()
    {
        foreach (var obj in objectsByPosition.Values)
        {
            if (obj != null)
            {
                obj.DestroyObject();
            }
        }
        objectsByPosition.Clear();
        trees.Clear();
    }

    private void OnDestroy()
    {
        ClearAll();
    }
}
