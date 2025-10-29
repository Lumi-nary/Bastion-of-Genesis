using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingDatabase", menuName = "Planetfall/Building Database")]
public class BuildingDatabase : ScriptableObject
{
    public List<BuildingData> availableBuildings = new List<BuildingData>();
}
