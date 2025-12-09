using UnityEngine;

/// <summary>
/// Resource nodes that occupy 3×3 grid space and are claimed when extractors are built on them
/// Uses sprite swapping: ore_mound → extractor_on_ore
/// </summary>
[CreateAssetMenu(fileName = "Property_ResourceNode", menuName = "Planetfall/Grid/Properties/Resource Node")]
public class ResourceNodeProperty : TileProperty
{
    [Header("Resource Node Configuration")]
    [Tooltip("Type of resource this node provides")]
    public ResourceType resourceType;

    [Tooltip("Total resources available (0 = infinite)")]
    public int totalAmount = 1000;

    [Tooltip("Sprite to display when claimed by a building")]
    public Sprite claimedSprite;

    [Header("Runtime State")]
    [Tooltip("Whether a building has claimed this node")]
    [SerializeField] private bool isClaimed = false;

    [Tooltip("Building that owns this node")]
    [SerializeField] private Building claimedBy = null;

    public bool IsClaimed => isClaimed;
    public Building ClaimedBy => claimedBy;

    public override bool IsBuildable()
    {
        // Ore mounds are buildable by extractors only
        // Actual validation happens in PlacementSystem
        // This just allows placement attempts (specific checks done elsewhere)
        return true;
    }

    /// <summary>
    /// Check if a building can be placed on this ore mound
    /// </summary>
    public bool CanPlaceBuilding(BuildingData buildingData)
    {
        // Must have ResourceExtractorFeature
        if (!buildingData.HasFeature<ResourceExtractorFeature>())
        {
            return false; // Only extractors can be placed on ore mounds
        }

        // Must match resource type
        ResourceExtractorFeature extractor = buildingData.GetFeature<ResourceExtractorFeature>();
        if (extractor == null || extractor.resourceType != this.resourceType)
        {
            return false; // Wrong extractor type
        }

        // Can't place if already claimed
        if (isClaimed)
        {
            return false; // Ore mound already has an extractor
        }

        return true;
    }

    public override bool IsWalkable()
    {
        // Enemies can walk through resource nodes
        return true;
    }

    public override void OnBuildingPlaced(Building building, Vector2Int tilePosition)
    {
        // Check if building can extract this resource type
        if (building.BuildingData.HasFeature<ResourceExtractorFeature>())
        {
            ResourceExtractorFeature extractor = building.BuildingData.GetFeature<ResourceExtractorFeature>();

            if (extractor.resourceType == this.resourceType)
            {
                // Claim node
                isClaimed = true;
                claimedBy = building;

                // Update tilemap sprite
                if (GridManager.Instance != null && claimedSprite != null)
                {
                    GridManager.Instance.SwapTileSprite(tilePosition, claimedSprite);
                }

                // TODO: Notify building it owns this resource node
                // This will be implemented when ResourceExtractor component is created

                Debug.Log($"[ResourceNodeProperty] {building.BuildingData.buildingName} claimed {resourceType.ResourceName} node at {tilePosition}!");
            }
        }
    }

    public override void OnBuildingRemoved(Building building, Vector2Int tilePosition)
    {
        if (claimedBy == building)
        {
            // Unclaim node
            isClaimed = false;
            claimedBy = null;

            // Restore tilemap sprite
            if (GridManager.Instance != null)
            {
                GridManager.Instance.RestoreTileSprite(tilePosition);
            }

            Debug.Log($"[ResourceNodeProperty] {resourceType.ResourceName} node at {tilePosition} is now unclaimed");
        }
    }

    public override string GetPropertyDescription()
    {
        if (isClaimed && claimedBy != null)
        {
            return $"{resourceType.ResourceName} node (claimed by {claimedBy.BuildingData.buildingName})";
        }
        else
        {
            return $"{resourceType.ResourceName} node ({totalAmount} resources available)";
        }
    }

    /// <summary>
    /// Reset property state (for new game or scene reload)
    /// </summary>
    public void ResetState()
    {
        isClaimed = false;
        claimedBy = null;
    }
}
