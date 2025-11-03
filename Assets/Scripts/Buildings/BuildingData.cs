using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Planetfall/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;
    [TextArea] public string description;
    public Sprite icon;

        [Header("Building Prefab") ]

        public GameObject prefab;

    

        [Header("Building Stats")]

        public float maxHealth = 100f;

    

        [Header("Construction Cost")]

        public List<ResourceCost> resourceCost = new List<ResourceCost>();
    public WorkerData builderType;
    public int buildersConsumed;

    [Header("Worker Capacity")]
    public List<WorkerData> allowedWorkerTypes = new List<WorkerData>();
    public int workerCapacity; // Total number of workers this building can hold

    [Header("Resource Generation")]
    public ResourceType generatedResourceType;
    public float generationAmount = 1f; // Amount of resource generated per interval per worker
    public float generationInterval = 1f; // Time in seconds between each generation cycle

    [Header("Grid Properties")]
    public int width = 1;
    public int height = 1;
}
