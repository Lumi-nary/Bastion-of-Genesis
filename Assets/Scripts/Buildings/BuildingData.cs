using System.Collections.Generic;
using UnityEngine;

// Enum for building categories
public enum BuildingCategory
{
    Command,     // Base
    Energy,      // Generators
    Extraction,  // Ore/Mana extractors
    Production,  // Factories
    Defense,     // Turrets, Walls
    Research     // Laboratory
}

// Enum to define how worker capacity is handled
public enum WorkerCapacityType { Shared, PerType }

// Class to define requirements for each worker type in a building
[System.Serializable]
public class WorkerRequirement
{
    public WorkerData workerType;
    public int capacity; // Used for PerType capacity
    public int requiredCount; // Minimum needed for the building to function
}

/// <summary>
/// ScriptableObject representing a building type
/// REFACTORED: Now uses modular BuildingFeature system for extensibility
/// </summary>
[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Planetfall/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Identity")]
    [Tooltip("Display name of the building")]
    public string buildingName;

    [Tooltip("Description of building purpose and behavior")]
    [TextArea]
    public string description;

    [Tooltip("Icon for UI display")]
    public Sprite icon;

    [Tooltip("Building category (Command, Energy, Extraction, etc.)")]
    public BuildingCategory category;

    [Tooltip("Building prefab (must have Building component)")]
    public GameObject prefab;

    [Tooltip("If false, building won't appear in build menu (e.g., Command Center)")]
    public bool isPlayerBuildable = true;

    [Header("Tech Requirements")]
    [Tooltip("Technology required to unlock this building (null if available from start)")]
    public TechnologyData requiredTech;

    [Header("Building Stats")]
    [Tooltip("Maximum health points")]
    public float maxHealth = 100f;

    [Header("Construction Cost")]
    [Tooltip("Resources required to construct")]
    public List<ResourceCost> resourceCost = new List<ResourceCost>();

    [Tooltip("Worker type that builds this (Builder/Engineer)")]
    public WorkerData builderType;

    [Tooltip("Number of builders consumed on construction")]
    public int buildersConsumed;

    [Header("Worker Configuration")]
    [Tooltip("How worker capacity is handled (Shared or Per-Type)")]
    public WorkerCapacityType capacityType = WorkerCapacityType.Shared;

    [Tooltip("Total worker capacity (used for Shared capacity type)")]
    public int totalWorkerCapacity;

    [Tooltip("Worker type requirements and capacities")]
    public List<WorkerRequirement> workerRequirements = new List<WorkerRequirement>();

    [Header("Building Features (Modular System)")]
    [Tooltip("List of features this building has (energy, combat, pollution, etc.)")]
    public List<BuildingFeature> features = new List<BuildingFeature>();

    [Header("Grid Properties")]
    [Tooltip("Width in grid tiles")]
    public int width = 1;

    [Tooltip("Height in grid tiles")]
    public int height = 1;

    [Tooltip("If true, this building spreads the integration (buildable) zone around it.")]
    public bool spreadsIntegration = true;

    /// <summary>
    /// Check if this building has a specific feature type
    /// </summary>
    public bool HasFeature<T>() where T : BuildingFeature
    {
        foreach (var feature in features)
        {
            if (feature is T) return true;
        }
        return false;
    }

    /// <summary>
    /// Get feature of specific type (returns null if not found)
    /// </summary>
    public T GetFeature<T>() where T : BuildingFeature
    {
        foreach (var feature in features)
        {
            if (feature is T) return feature as T;
        }
        return null;
    }

    /// <summary>
    /// Get all features of specific type
    /// </summary>
    public List<T> GetFeatures<T>() where T : BuildingFeature
    {
        List<T> result = new List<T>();
        foreach (var feature in features)
        {
            if (feature is T) result.Add(feature as T);
        }
        return result;
    }
}
