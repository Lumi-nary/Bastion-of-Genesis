
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour
{
    public bool showDebugMenu = false;

    private ResourceType[] resourceTypes;
    private WorkerData[] workerTypes;

    private Dictionary<string, string> resourceInputAmounts = new Dictionary<string, string>();
    private Dictionary<string, string> workerInputAmounts = new Dictionary<string, string>();

    void Start()
    {
        resourceTypes = Resources.LoadAll<ResourceType>("GameData/Resources");
        workerTypes = Resources.LoadAll<WorkerData>("GameData/Workers");

        foreach (var resource in resourceTypes)
        {
            resourceInputAmounts[resource.name] = "100";
        }

        foreach (var worker in workerTypes)
        {
            workerInputAmounts[worker.name] = "1";
        }
    }


    void OnGUI()
    {
        if (!showDebugMenu)
        {
            return;
        }

        // Basic background
        GUI.Box(new Rect(10, 10, 350, 500), "Debug Menu");

        int yPos = 40;

        // Resources
        GUI.Label(new Rect(20, yPos, 200, 20), "Resources");
        yPos += 25;

        foreach (var resource in resourceTypes)
        {
            GUI.Label(new Rect(20, yPos, 100, 20), resource.ResourceName);
            GUI.Label(new Rect(130, yPos, 50, 20), ResourceManager.Instance.GetResourceAmount(resource).ToString());

            resourceInputAmounts[resource.name] = GUI.TextField(new Rect(190, yPos, 50, 20), resourceInputAmounts[resource.name]);

            if (GUI.Button(new Rect(250, yPos, 40, 20), "Add"))
            {
                if (int.TryParse(resourceInputAmounts[resource.name], out int amount))
                {
                    ResourceManager.Instance.AddResource(resource, amount);
                }
            }
            if (GUI.Button(new Rect(300, yPos, 60, 20), "Remove"))
            {
                if (int.TryParse(resourceInputAmounts[resource.name], out int amount))
                {
                    ResourceManager.Instance.RemoveResource(resource, amount);
                }
            }
            yPos += 25;
        }

        yPos += 10;

        // Workers
        GUI.Label(new Rect(20, yPos, 200, 20), "Workers");
        yPos += 25;

        foreach (var worker in workerTypes)
        {
            GUI.Label(new Rect(20, yPos, 100, 20), worker.workerName);
            GUI.Label(new Rect(130, yPos, 50, 20), WorkerManager.Instance.GetAvailableWorkerCount(worker).ToString());

            workerInputAmounts[worker.name] = GUI.TextField(new Rect(190, yPos, 50, 20), workerInputAmounts[worker.name]);

            if (GUI.Button(new Rect(250, yPos, 40, 20), "Add"))
            {
                if (int.TryParse(workerInputAmounts[worker.name], out int amount))
                {
                    for(int i = 0; i < amount; i++)
                    {
                        // Using ReturnWorker as it's the public method to add to the pool
                        WorkerManager.Instance.ReturnWorker(worker);
                    }
                }
            }
            if (GUI.Button(new Rect(300, yPos, 60, 20), "Remove"))
            {
                if (int.TryParse(workerInputAmounts[worker.name], out int amount))
                {
                    for(int i = 0; i < amount; i++)
                    {
                        // Using AssignWorker to remove from the pool
                        WorkerManager.Instance.AssignWorker(worker);
                    }
                }
            }
            yPos += 25;
        }
    }
}
