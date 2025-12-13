using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database of all technologies in the game.
/// Assign this to ResearchManager to populate available technologies.
/// </summary>
[CreateAssetMenu(fileName = "TechnologyDatabase", menuName = "Planetfall/Technology Database")]
public class TechnologyDatabase : ScriptableObject
{
    [Tooltip("All technologies available in the game")]
    public List<TechnologyData> technologies = new List<TechnologyData>();
}
