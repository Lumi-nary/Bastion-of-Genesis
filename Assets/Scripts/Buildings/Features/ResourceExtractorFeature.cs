using UnityEngine;

/// <summary>
/// Resource extractor feature - Extracts resources from ore mounds
/// Must be placed on ore mound, requires Extractor workers
/// Examples: Iron Extractor, Copper Extractor, Mana Extractor
/// </summary>
[CreateAssetMenu(fileName = "Feature_ResourceExtractor", menuName = "Planetfall/Building Features/Resource Extractor")]
public class ResourceExtractorFeature : BuildingFeature
{
    [Header("Extraction Configuration")]
    [Tooltip("Type of resource extracted")]
    public ResourceType resourceType;

    [Tooltip("Amount of resource extracted per production cycle")]
    public int extractionAmount = 10;

    [Tooltip("Production cycle time (seconds)")]
    public float productionCycle = 5f;

    [Tooltip("Must be placed on an ore mound")]
    public bool requiresOreMound = true;

    private float productionTimer = 0f;

    public override void OnOperate(Building building)
    {
        // Only extract if operational (has required Extractor workers)
        if (!building.IsOperational) return;

        // Check if we have energy (unless can function without)
        if (!CanFunctionWithoutEnergy() && EnergyManager.Instance != null && !EnergyManager.Instance.HasEnergy)
        {
            return;
        }

        // Production cycle
        productionTimer += Time.deltaTime;
        if (productionTimer >= productionCycle)
        {
            productionTimer -= productionCycle;

            // Extract resources
            if (ResourceManager.Instance != null && resourceType != null)
            {
                ResourceManager.Instance.AddResource(resourceType, extractionAmount);
                Debug.Log($"[ResourceExtractor] {building.BuildingData.buildingName} extracted {extractionAmount} {resourceType.ResourceName}");
            }
        }
    }

    public override void OnBuilt(Building building)
    {
        productionTimer = 0f;

        // TODO: Validate placement on ore mound if required
        if (requiresOreMound)
        {
            // Check if building is on an ore mound
        }
    }
}
