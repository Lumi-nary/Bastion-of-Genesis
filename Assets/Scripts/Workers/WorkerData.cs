using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Planetfall/Worker Data")]
public class WorkerData : ScriptableObject, ITooltipProvider
{
    [Header("Worker Info")]
    public string workerName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Creation Cost")]
    public List<ResourceCost> cost = new List<ResourceCost>();

    // ITooltipProvider implementation
    public string GetTooltipHeader()
    {
        return workerName;
    }

    public string GetTooltipDescription()
    {
        return description;
    }
}
