using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Planetfall/Worker Data")]
public class WorkerData : ScriptableObject
{
    [Header("Worker Info")]
    public string workerName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Creation Cost")]
    public List<ResourceCost> cost = new List<ResourceCost>();
}
