using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DEPRECATED: OreMoundManager has been merged into GridManager.
/// This class exists for backward compatibility only.
/// Use GridManager.Instance methods instead:
/// - RegisterOreMound(), UnregisterOreMound()
/// - GetMoundAtPosition(), GetMoundsByType()
/// - AllMounds, DiscoveredMounds, UndiscoveredMounds
/// </summary>
[System.Obsolete("OreMoundManager is deprecated. Use GridManager instead.")]
public class OreMoundManager : MonoBehaviour
{
    // Legacy static instance that redirects to GridManager
    public static GridManager Instance => GridManager.Instance;

    // Legacy event redirect - use GridManager's delegate type
    public event GridManager.MoundDiscoveredEvent OnMoundDiscovered
    {
        add { if (GridManager.Instance != null) GridManager.Instance.OnMoundDiscovered += value; }
        remove { if (GridManager.Instance != null) GridManager.Instance.OnMoundDiscovered -= value; }
    }

    // Legacy properties that redirect to GridManager
    public IReadOnlyList<OreMound> AllMounds => GridManager.Instance?.AllMounds;
    public IReadOnlyList<OreMound> DiscoveredMounds => GridManager.Instance?.DiscoveredMounds;
    public IReadOnlyList<OreMound> UndiscoveredMounds => GridManager.Instance?.UndiscoveredMounds;

    private void Awake()
    {
        Debug.LogWarning("[OreMoundManager] This component is deprecated. Ore mound functionality has been merged into GridManager. Please remove this GameObject.");
    }

    // Legacy methods that redirect to GridManager
    public void RegisterOreMound(OreMound mound)
    {
        GridManager.Instance?.RegisterOreMound(mound);
    }

    public void UnregisterOreMound(OreMound mound)
    {
        GridManager.Instance?.UnregisterOreMound(mound);
    }

    public List<OreMound> GetMoundsByType(OreMoundType type)
    {
        return GridManager.Instance?.GetMoundsByType(type) ?? new List<OreMound>();
    }

    public List<OreMound> GetDiscoveredMoundsByType(OreMoundType type)
    {
        return GridManager.Instance?.GetDiscoveredMoundsByType(type) ?? new List<OreMound>();
    }

    public OreMound GetNearestAvailableMound(OreMoundType type, Vector3 position)
    {
        return GridManager.Instance?.GetNearestAvailableMound(type, position);
    }

    public OreMound GetMoundAtPosition(Vector3 position, float tolerance = 0.5f)
    {
        return GridManager.Instance?.GetMoundAtPosition(position, tolerance);
    }

    public float GetDiscoveryProgress()
    {
        return GridManager.Instance?.GetMoundDiscoveryProgress() ?? 0f;
    }

    public string GetDiscoveryStatsString()
    {
        return GridManager.Instance?.GetMoundDiscoveryStatsString() ?? "No mounds";
    }

    public void DiscoverAllMounds()
    {
        GridManager.Instance?.DiscoverAllMounds();
    }

    public void ResetAllMounds()
    {
        GridManager.Instance?.ResetAllMounds();
    }
}
