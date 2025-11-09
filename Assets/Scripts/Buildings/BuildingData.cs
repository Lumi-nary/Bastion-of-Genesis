using System.Collections.Generic;
using UnityEngine;

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

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Planetfall/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Building Prefab")]
    public GameObject prefab;

    [Header("Building Stats")]
    public float maxHealth = 100f;

    [Header("Construction Cost")]
    public List<ResourceCost> resourceCost = new List<ResourceCost>();
    public WorkerData builderType;
    public int buildersConsumed;

    [Header("Worker Configuration")]
    public WorkerCapacityType capacityType = WorkerCapacityType.Shared;
    public int totalWorkerCapacity; // Used for Shared capacity
    public List<WorkerRequirement> workerRequirements = new List<WorkerRequirement>();

    [Header("Resource Generation")]
    public ResourceType generatedResourceType;
    public float generationAmount = 1f; // Amount of resource generated per interval per worker
    public float generationInterval = 1f; // Time in seconds between each generation cycle

    [Header("Pollution")]
    [Tooltip("Amount of pollution generated per interval per worker")]
    public float pollutionGeneration = 0f;
    [Tooltip("Time in seconds between each pollution generation cycle")]
    public float pollutionInterval = 1f;
    [Tooltip("Does this building generate pollution even without workers?")]
    public bool generatesIdlePollution = false;
    [Tooltip("Pollution generated per interval when idle (no workers)")]
    public float idlePollutionAmount = 0f;

    [Header("Grid Properties")]
    public int width = 1;
    public int height = 1;
}

