using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility for resolving ScriptableObject references by name string.
/// Used during save/load to convert between serialized name strings and SO references.
/// Caches are lazy-loaded from Resources folders.
/// </summary>
public static class ScriptableObjectResolver
{
    private static Dictionary<string, ResourceType> resourceCache;
    private static Dictionary<string, WorkerData> workerCache;
    private static Dictionary<string, BuildingData> buildingCache;
    private static Dictionary<string, EnemyData> enemyCache;
    private static Dictionary<string, TechnologyData> technologyCache;

    /// <summary>
    /// Resolve a ResourceType by its ResourceName.
    /// </summary>
    public static ResourceType ResolveResource(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (resourceCache == null)
        {
            resourceCache = new Dictionary<string, ResourceType>();
            var loaded = Resources.LoadAll<ResourceType>("Data/Resources");
            foreach (var rt in loaded)
            {
                if (!resourceCache.ContainsKey(rt.ResourceName))
                    resourceCache[rt.ResourceName] = rt;
            }
            Debug.Log($"[ScriptableObjectResolver] Cached {resourceCache.Count} ResourceTypes");
        }

        if (resourceCache.TryGetValue(name, out ResourceType result))
            return result;

        Debug.LogWarning($"[ScriptableObjectResolver] ResourceType not found: {name}");
        return null;
    }

    /// <summary>
    /// Resolve a WorkerData by its workerName.
    /// </summary>
    public static WorkerData ResolveWorker(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (workerCache == null)
        {
            workerCache = new Dictionary<string, WorkerData>();
            var loaded = Resources.LoadAll<WorkerData>("Data/Workers");
            foreach (var wd in loaded)
            {
                if (!workerCache.ContainsKey(wd.workerName))
                    workerCache[wd.workerName] = wd;
            }
            Debug.Log($"[ScriptableObjectResolver] Cached {workerCache.Count} WorkerData");
        }

        if (workerCache.TryGetValue(name, out WorkerData result))
            return result;

        Debug.LogWarning($"[ScriptableObjectResolver] WorkerData not found: {name}");
        return null;
    }

    /// <summary>
    /// Resolve a BuildingData by its buildingName.
    /// </summary>
    public static BuildingData ResolveBuilding(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (buildingCache == null)
        {
            buildingCache = new Dictionary<string, BuildingData>();
            var loaded = Resources.LoadAll<BuildingData>("Data/Buildings");
            foreach (var bd in loaded)
            {
                if (!buildingCache.ContainsKey(bd.buildingName))
                    buildingCache[bd.buildingName] = bd;
            }
            Debug.Log($"[ScriptableObjectResolver] Cached {buildingCache.Count} BuildingData");
        }

        if (buildingCache.TryGetValue(name, out BuildingData result))
            return result;

        Debug.LogWarning($"[ScriptableObjectResolver] BuildingData not found: {name}");
        return null;
    }

    /// <summary>
    /// Resolve an EnemyData by its enemyName.
    /// </summary>
    public static EnemyData ResolveEnemy(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (enemyCache == null)
        {
            enemyCache = new Dictionary<string, EnemyData>();
            var loaded = Resources.LoadAll<EnemyData>("Data/Enemies");
            foreach (var ed in loaded)
            {
                if (!enemyCache.ContainsKey(ed.enemyName))
                    enemyCache[ed.enemyName] = ed;
            }
            Debug.Log($"[ScriptableObjectResolver] Cached {enemyCache.Count} EnemyData");
        }

        if (enemyCache.TryGetValue(name, out EnemyData result))
            return result;

        Debug.LogWarning($"[ScriptableObjectResolver] EnemyData not found: {name}");
        return null;
    }

    /// <summary>
    /// Resolve a TechnologyData by its techName.
    /// </summary>
    public static TechnologyData ResolveTechnology(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (technologyCache == null)
        {
            technologyCache = new Dictionary<string, TechnologyData>();
            var loaded = Resources.LoadAll<TechnologyData>("Data/Research");
            foreach (var td in loaded)
            {
                if (!technologyCache.ContainsKey(td.techName))
                    technologyCache[td.techName] = td;
            }
            Debug.Log($"[ScriptableObjectResolver] Cached {technologyCache.Count} TechnologyData");
        }

        if (technologyCache.TryGetValue(name, out TechnologyData result))
            return result;

        Debug.LogWarning($"[ScriptableObjectResolver] TechnologyData not found: {name}");
        return null;
    }

    /// <summary>
    /// Clear all caches. Call on scene unload to prevent stale references.
    /// </summary>
    public static void ClearCaches()
    {
        resourceCache = null;
        workerCache = null;
        buildingCache = null;
        enemyCache = null;
        technologyCache = null;
    }
}
