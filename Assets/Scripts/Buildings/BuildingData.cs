using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Planetfall/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Building Prefab")]
    public GameObject prefab;

    [Header("Construction Cost")]
    public List<ResourceCost> resourceCost = new List<ResourceCost>();
    public WorkerData builderType;
    public int buildersConsumed;

    [Header("Resource Generation")]
    public ResourceType generatedResourceType;
    public float generationRate; // per worker per second
}
