using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "Planetfall/Resource Type")]
public class ResourceType : ScriptableObject, ITooltipProvider
{
    [Header("Resource Properties")]
    [Tooltip("The name of the resource.")]
    [SerializeField] private string resourceName;

    [Tooltip("A description of the resource.")]
    [SerializeField] private string description;

    [Tooltip("The icon to represent the resource in the UI.")]
    [SerializeField] private Sprite icon;

    [Header("Capacity")]
    [Tooltip("Base maximum capacity before buildings/research bonuses")]
    [SerializeField] private int baseCapacity = 100;

    public string ResourceName => resourceName;
    public int BaseCapacity => baseCapacity;
    public string Description => description;
    public Sprite Icon => icon;

    // ITooltipProvider implementation
    public string GetTooltipHeader()
    {
        return resourceName;
    }

    public string GetTooltipDescription()
    {
        return description;
    }
}
