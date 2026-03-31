using System;

/// <summary>
/// Serializable data structs for each manager's save state.
/// Used by SaveData to store full game state.
/// All ScriptableObject references stored as name strings for serialization.
/// JsonUtility compatible: only primitives, arrays, and strings.
/// </summary>

// ============================================================================
// SHARED
// ============================================================================

[Serializable]
public class SerializableKeyValue
{
    public string key;
    public int value;

    public SerializableKeyValue() { }
    public SerializableKeyValue(string key, int value)
    {
        this.key = key;
        this.value = value;
    }
}

// ============================================================================
// RESOURCE STATE
// ============================================================================

[Serializable]
public class ResourceSaveData
{
    public SerializableKeyValue[] amounts = new SerializableKeyValue[0];
    public SerializableKeyValue[] capacities = new SerializableKeyValue[0];
}

// ============================================================================
// WORKER STATE
// ============================================================================

[Serializable]
public class WorkerSaveData
{
    public SerializableKeyValue[] available = new SerializableKeyValue[0];
    public SerializableKeyValue[] capacities = new SerializableKeyValue[0];
}

// ============================================================================
// BUILDING STATE
// ============================================================================

[Serializable]
public class BuildingSaveData
{
    public BuildingEntry[] buildings = new BuildingEntry[0];
    public string[] missionUnlockedBuildingNames = new string[0];
}

[Serializable]
public class BuildingEntry
{
    public string buildingDataName;
    public int gridPosX;
    public int gridPosY;
    public float currentHealth;
    public WorkerAssignment[] assignedWorkers = new WorkerAssignment[0];
}

[Serializable]
public class WorkerAssignment
{
    public string workerName;
    public int count;

    public WorkerAssignment() { }
    public WorkerAssignment(string workerName, int count)
    {
        this.workerName = workerName;
        this.count = count;
    }
}

// ============================================================================
// RESEARCH STATE
// ============================================================================

[Serializable]
public class ResearchSaveData
{
    public string[] researchedTechNames = new string[0];
    public string currentResearchName = "";
    public float currentResearchProgress;
    public float researchTimeElapsed;
    public float resourcesConsumed;
    public bool isResearching;
}

// ============================================================================
// POLLUTION STATE
// ============================================================================

[Serializable]
public class PollutionSaveData
{
    public float currentPollution;
    public int currentTier;
    public int menuDifficulty;
    public float peakIntegrationRadius;
}

// ============================================================================
// MISSION STATE
// ============================================================================

[Serializable]
public class MissionSaveData
{
    public int currentChapterIndex;
    public int currentMissionIndex;
    public float missionTimer;
    public bool missionActive;
    public ObjectiveSaveData[] objectives = new ObjectiveSaveData[0];
    public ScriptedWaveSaveData[] scriptedWaves = new ScriptedWaveSaveData[0];
}

[Serializable]
public class ObjectiveSaveData
{
    public bool isCompleted;
    public int currentAmount;
    public float currentTime;
}

[Serializable]
public class ScriptedWaveSaveData
{
    public bool isTriggered;
}

// ============================================================================
// ORE MOUND STATE
// ============================================================================

[Serializable]
public class OreMoundSaveData
{
    public OreMoundEntry[] mounds = new OreMoundEntry[0];
}

[Serializable]
public class OreMoundEntry
{
    public float posX;
    public float posY;
    public float posZ;
    public bool isDiscovered;
}

// ============================================================================
// ENEMY STATE
// ============================================================================

[Serializable]
public class EnemySaveData
{
    public int currentWave;
    public int enemiesKilled;
}
