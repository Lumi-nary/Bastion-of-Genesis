using UnityEngine;

/// <summary>
/// Trees that block building placement and wither based on pollution levels
/// Pollution-based clearing: healthy → withering → dead (buildable)
/// </summary>
[CreateAssetMenu(fileName = "Property_Tree", menuName = "Planetfall/Grid/Properties/Tree")]
public class TreeProperty : TileProperty
{
    [Header("Tree Configuration")]
    [Tooltip("Current health of tree (0-100)")]
    [Range(0, 100)]
    public float treeHealth = 100f;

    [Tooltip("Pollution level when withering starts")]
    [Range(0, 100)]
    public float witherThreshold = 30f;

    [Tooltip("Pollution level when tree dies")]
    [Range(0, 100)]
    public float deathThreshold = 60f;

    [Tooltip("How often to check pollution (seconds)")]
    public float pollutionCheckInterval = 5f;

    [Header("Visual States")]
    [Tooltip("Sprite for healthy tree")]
    public Sprite healthySprite;

    [Tooltip("Sprite for withered tree")]
    public Sprite witherSprite;

    [Tooltip("Sprite for dead tree (wasteland)")]
    public Sprite deadSprite;

    [Header("Runtime State")]
    private float timeSinceLastCheck = 0f;
    private TreeState currentState = TreeState.Healthy;
    private Tree visualTree; // Reference to the visual GameObject

    private enum TreeState
    {
        Healthy,
        Withering,
        Dead
    }

    public override bool IsBuildable()
    {
        // Trees are never buildable (until pollution kills them)
        // TODO: Implement per-tile state tracking for tree death
        return false;
    }

    public override bool IsWalkable()
    {
        // Enemies can always walk through trees (visual only)
        return true;
    }

    public override void OnUpdate(Vector2Int tilePosition)
    {
        // Don't update if already dead
        if (currentState == TreeState.Dead) return;

        // Check pollution periodically
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= pollutionCheckInterval)
        {
            timeSinceLastCheck = 0f;

            // Get pollution level at this tile
            float pollution = GetPollutionAtTile(tilePosition);

            // Update tree state based on pollution
            if (pollution >= deathThreshold)
            {
                // Tree dies, becomes buildable wasteland
                treeHealth = 0f;
                currentState = TreeState.Dead;
                UpdateVisual(tilePosition, deadSprite);
                ConvertToWasteland(tilePosition);
                Debug.Log($"[TreeProperty] Tree at {tilePosition} died from pollution ({pollution})");
            }
            else if (pollution >= witherThreshold)
            {
                // Tree withering, reduce health gradually
                float witherRange = deathThreshold - witherThreshold;
                float witherProgress = (pollution - witherThreshold) / witherRange;
                treeHealth = Mathf.Lerp(100f, 1f, witherProgress);

                if (currentState != TreeState.Withering)
                {
                    currentState = TreeState.Withering;
                    UpdateVisual(tilePosition, witherSprite);
                    Debug.Log($"[TreeProperty] Tree at {tilePosition} is withering (pollution: {pollution})");
                }
            }
            else
            {
                // Tree healthy, slowly recover if pollution drops
                treeHealth = Mathf.Min(100f, treeHealth + (1f * pollutionCheckInterval));

                if (currentState != TreeState.Healthy && treeHealth >= 50f)
                {
                    currentState = TreeState.Healthy;
                    UpdateVisual(tilePosition, healthySprite);
                    Debug.Log($"[TreeProperty] Tree at {tilePosition} recovered (pollution: {pollution})");
                }
            }
        }
    }

    /// <summary>
    /// Get pollution level at specific tile
    /// </summary>
    private float GetPollutionAtTile(Vector2Int tilePosition)
    {
        // TODO: Implement PollutionManager.GetPollutionAtTile()
        // For now, return 0 (no pollution)
        if (PollutionManager.Instance != null)
        {
            // Placeholder - needs PollutionManager implementation
            return 0f;
        }
        return 0f;
    }

    /// <summary>
    /// Register the visual GameObject for this tree
    /// </summary>
    public void RegisterTreeGameObject(Tree tree)
    {
        visualTree = tree;
    }

    /// <summary>
    /// Update visual sprite for this tile
    /// </summary>
    private void UpdateVisual(Vector2Int tilePosition, Sprite newSprite)
    {
        // Update GameObject if it exists
        if (visualTree != null)
        {
            Tree.TreeState newState = Tree.TreeState.Healthy;
            if (currentState == TreeState.Withering)
                newState = Tree.TreeState.Withering;
            else if (currentState == TreeState.Dead)
                newState = Tree.TreeState.Dead;

            visualTree.SetState(newState);
        }

        // Also update tilemap sprite (fallback)
        if (GridManager.Instance != null && newSprite != null)
        {
            GridManager.Instance.SwapTileSprite(tilePosition, newSprite);
        }
    }

    /// <summary>
    /// Convert tree tile to buildable wasteland
    /// </summary>
    private void ConvertToWasteland(Vector2Int tilePosition)
    {
        if (GridManager.Instance != null)
        {
            // TODO: Convert tile type to wasteland/dirt (buildable)
            // GridManager.Instance.ConvertTileType(tilePosition, wastelandTileData);
        }
    }

    public override string GetPropertyDescription()
    {
        switch (currentState)
        {
            case TreeState.Healthy:
                return $"Healthy tree ({treeHealth:F0}% health)";
            case TreeState.Withering:
                return $"Withering tree ({treeHealth:F0}% health) - high pollution";
            case TreeState.Dead:
                return "Dead tree - now buildable wasteland";
            default:
                return description;
        }
    }

    /// <summary>
    /// Reset property state (for new game or scene reload)
    /// </summary>
    public void ResetState()
    {
        treeHealth = 100f;
        currentState = TreeState.Healthy;
        timeSinceLastCheck = 0f;
    }
}
