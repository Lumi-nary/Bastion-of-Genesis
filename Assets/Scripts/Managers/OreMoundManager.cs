using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all ore mounds in the scene
/// Handles discovery based on pollution levels
/// Singleton pattern for global access
/// </summary>
public class OreMoundManager : MonoBehaviour
{
    public static OreMoundManager Instance { get; private set; }

    [Header("Ore Mound Tracking")]
    private List<OreMound> allMounds = new List<OreMound>();
    private List<OreMound> discoveredMounds = new List<OreMound>();
    private List<OreMound> undiscoveredMounds = new List<OreMound>();

    [Header("Discovery Settings")]
    [Tooltip("How often to check for mound discoveries (seconds)")]
    [SerializeField] private float discoveryCheckInterval = 5f;
    private float discoveryCheckTimer = 0f;

    // Events
    public delegate void MoundDiscoveredEvent(OreMound mound);
    public event MoundDiscoveredEvent OnMoundDiscovered;

    // Public properties
    public IReadOnlyList<OreMound> AllMounds => allMounds;
    public IReadOnlyList<OreMound> DiscoveredMounds => discoveredMounds;
    public IReadOnlyList<OreMound> UndiscoveredMounds => undiscoveredMounds;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Subscribe to mound events
        OreMound.OnMoundDiscovered += HandleMoundDiscovered;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        OreMound.OnMoundDiscovered -= HandleMoundDiscovered;
    }

    private void Update()
    {
        // Check for mound discoveries based on pollution
        discoveryCheckTimer += Time.deltaTime;
        if (discoveryCheckTimer >= discoveryCheckInterval)
        {
            discoveryCheckTimer = 0f;
            CheckForDiscoveries();
        }
    }

    /// <summary>
    /// Register an ore mound (called by OreMound.Start)
    /// </summary>
    public void RegisterOreMound(OreMound mound)
    {
        if (mound == null || allMounds.Contains(mound)) return;

        allMounds.Add(mound);

        if (mound.IsDiscovered)
        {
            discoveredMounds.Add(mound);
        }
        else
        {
            undiscoveredMounds.Add(mound);
        }

        Debug.Log($"[OreMoundManager] Registered {mound.GetMoundTypeName()} at {mound.Position}");
    }

    /// <summary>
    /// Unregister an ore mound (called by OreMound.OnDestroy)
    /// </summary>
    public void UnregisterOreMound(OreMound mound)
    {
        if (mound == null) return;

        allMounds.Remove(mound);
        discoveredMounds.Remove(mound);
        undiscoveredMounds.Remove(mound);
    }

    /// <summary>
    /// Check for mound discoveries based on current pollution level
    /// </summary>
    private void CheckForDiscoveries()
    {
        if (PollutionManager.Instance == null) return;

        float currentPollution = PollutionManager.Instance.CurrentPollution;

        // Check all undiscovered mound
        foreach (OreMound mound in undiscoveredMounds.ToList())
        {
            if (mound.CanDiscoverAtPollution(currentPollution))
            {
                mound.Discover();
            }
        }
    }

    /// <summary>
    /// Handle mound discovery event
    /// </summary>
    private void HandleMoundDiscovered(OreMound mound)
    {
        if (undiscoveredMounds.Contains(mound))
        {
            undiscoveredMounds.Remove(mound);
            discoveredMounds.Add(mound);

            OnMoundDiscovered?.Invoke(mound);

            Debug.Log($"[OreMoundManager] {mound.GetMoundTypeName()} discovered! ({discoveredMounds.Count}/{allMounds.Count})");
        }
    }

    /// <summary>
    /// Get all mounds of a specific type
    /// </summary>
    public List<OreMound> GetMoundsByType(OreMoundType type)
    {
        return allMounds.Where(m => m.moundType == type).ToList();
    }

    /// <summary>
    /// Get all discovered mounds of a specific type
    /// </summary>
    public List<OreMound> GetDiscoveredMoundsByType(OreMoundType type)
    {
        return discoveredMounds.Where(m => m.moundType == type).ToList();
    }

    /// <summary>
    /// Get nearest available mound of a specific type
    /// </summary>
    public OreMound GetNearestAvailableMound(OreMoundType type, Vector3 position)
    {
        OreMound nearestMound = null;
        float nearestDistance = float.MaxValue;

        foreach (OreMound mound in discoveredMounds)
        {
            if (mound.moundType != type) continue;
            if (mound.HasExtractor) continue; // Skip occupied mounds

            float distance = Vector3.Distance(position, mound.Position);
            if (distance < nearestDistance)
            {
                nearestMound = mound;
                nearestDistance = distance;
            }
        }

        return nearestMound;
    }

    /// <summary>
    /// Check if a position is on an ore mound
    /// </summary>
    public OreMound GetMoundAtPosition(Vector3 position, float tolerance = 0.5f)
    {
        foreach (OreMound mound in allMounds)
        {
            float distance = Vector3.Distance(position, mound.Position);
            if (distance <= tolerance)
            {
                return mound;
            }
        }

        return null;
    }

    /// <summary>
    /// Get mound discovery progress as percentage
    /// </summary>
    public float GetDiscoveryProgress()
    {
        if (allMounds.Count == 0) return 0f;

        return (float)discoveredMounds.Count / allMounds.Count;
    }

    /// <summary>
    /// Get mound discovery stats string
    /// </summary>
    public string GetDiscoveryStatsString()
    {
        return $"Ore Mounds: {discoveredMounds.Count}/{allMounds.Count} discovered";
    }

    /// <summary>
    /// Force discover all mounds (debug/cheat)
    /// </summary>
    public void DiscoverAllMounds()
    {
        foreach (OreMound mound in undiscoveredMounds.ToList())
        {
            mound.Discover();
        }

        Debug.Log("[OreMoundManager] All mounds discovered (cheat)");
    }

    /// <summary>
    /// Reset all mound discoveries (for new mission)
    /// </summary>
    public void ResetAllMounds()
    {
        discoveredMounds.Clear();
        undiscoveredMounds.Clear();
        allMounds.Clear();

        Debug.Log("[OreMoundManager] Reset all mound data");
    }
}
