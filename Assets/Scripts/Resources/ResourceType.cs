using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "Planetfall/Resource Type")]
public class ResourceType : ScriptableObject
{
    [Header("Resource Properties")]
    [Tooltip("The name of the resource.")]
    [SerializeField] private string resourceName;

    [Tooltip("A description of the resource.")]
    [SerializeField] private string description;

    [Tooltip("The icon to represent the resource in the UI.")]
    [SerializeField] private Sprite icon;

    public string ResourceName => resourceName;
    public string Description => description;
    public Sprite Icon => icon;
}
