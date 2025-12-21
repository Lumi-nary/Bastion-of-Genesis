using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// NetworkedBuilding - Network-synced building component.
/// Attached to buildings spawned by NetworkedBuildingManager.
/// Syncs health, workers, and operational state.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedBuilding : NetworkBehaviour
{
    // Synced State using FishNet 4.x SyncVar<T>
    private readonly SyncVar<int> _buildingDataIndex = new SyncVar<int>(-1);
    private readonly SyncVar<int> _currentHealth = new SyncVar<int>();
    private readonly SyncVar<int> _maxHealth = new SyncVar<int>();
    private readonly SyncVar<bool> _isOperational = new SyncVar<bool>(true);
    private readonly SyncVar<Vector2Int> _gridPosition = new SyncVar<Vector2Int>();
    private readonly SyncVar<int> _assignedWorkerCount = new SyncVar<int>();

    // Cached data
    private BuildingData buildingData;

    // Properties
    public BuildingData BuildingData => buildingData;
    public int CurrentHealth => _currentHealth.Value;
    public int MaxHealth => _maxHealth.Value;
    public bool IsOperational => _isOperational.Value;
    public Vector2Int GridPosition => _gridPosition.Value;
    public int AssignedWorkerCount => _assignedWorkerCount.Value;
    public float HealthPercent => _maxHealth.Value > 0 ? (float)_currentHealth.Value / _maxHealth.Value : 0f;

    // Events
    public event System.Action<int, int> OnHealthUpdated;
    public event System.Action<bool> OnOperationalStateChanged;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Subscribe to SyncVar change events
        _buildingDataIndex.OnChange += OnBuildingIndexChanged;
        _currentHealth.OnChange += OnHealthChanged;
        _isOperational.OnChange += OnOperationalChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        // Unsubscribe from SyncVar change events
        _buildingDataIndex.OnChange -= OnBuildingIndexChanged;
        _currentHealth.OnChange -= OnHealthChanged;
        _isOperational.OnChange -= OnOperationalChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Load building data from index
        if (_buildingDataIndex.Value >= 0 && NetworkedBuildingManager.Instance != null)
        {
            buildingData = NetworkedBuildingManager.Instance.GetBuildingDataByIndex(_buildingDataIndex.Value);
            UpdateVisuals();

            // Register with legacy systems on remote clients
            if (!IsServer)
            {
                RegisterWithLegacySystems();
            }
        }
    }

    private void RegisterWithLegacySystems()
    {
        if (buildingData == null) return;

        Building legacyBuilding = GetComponent<Building>();
        if (legacyBuilding == null) return;

        // Sync properties to legacy component
        legacyBuilding.gridPosition = _gridPosition.Value;
        // width and height are handled by Building.buildingData usually, but force sync here for safety
        
        // Register with GridManager so client sees it blocked
        if (GridManager.Instance != null)
        {
            GridManager.Instance.PlaceBuilding(legacyBuilding, _gridPosition.Value, buildingData.width, buildingData.height);
        }

        // Register with BuildingManager
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterBuilding(legacyBuilding);
        }
    }

    // ============================================================================
    // SERVER INITIALIZATION
    // ============================================================================

    /// <summary>
    /// Initialize building on server after spawn
    /// </summary>
    [Server]
    public void ServerInitialize(int dataIndex, Vector2Int gridPos)
    {
        _buildingDataIndex.Value = dataIndex;
        _gridPosition.Value = gridPos;

        // Get building data
        if (NetworkedBuildingManager.Instance != null)
        {
            buildingData = NetworkedBuildingManager.Instance.GetBuildingDataByIndex(dataIndex);
            if (buildingData != null)
            {
                _maxHealth.Value = Mathf.RoundToInt(buildingData.maxHealth);
                _currentHealth.Value = _maxHealth.Value;
            }
        }

        Debug.Log($"[NetworkedBuilding] Initialized: {buildingData?.buildingName} at {gridPos}");
    }

    // ============================================================================
    // SERVER METHODS
    // ============================================================================

    /// <summary>
    /// Take damage (server only)
    /// </summary>
    [Server]
    public void ServerTakeDamage(int damage)
    {
        if (damage <= 0) return;

        _currentHealth.Value = Mathf.Max(0, _currentHealth.Value - damage);

        if (_currentHealth.Value <= 0)
        {
            ServerDestroy();
        }

        Debug.Log($"[NetworkedBuilding] Took {damage} damage, health: {_currentHealth.Value}/{_maxHealth.Value}");
    }

    /// <summary>
    /// Heal building (server only)
    /// </summary>
    [Server]
    public void ServerHeal(int amount)
    {
        if (amount <= 0) return;
        _currentHealth.Value = Mathf.Min(_maxHealth.Value, _currentHealth.Value + amount);
    }

    /// <summary>
    /// Set operational state (server only)
    /// </summary>
    [Server]
    public void ServerSetOperational(bool operational)
    {
        _isOperational.Value = operational;
    }

    /// <summary>
    /// Set assigned worker count (server only)
    /// </summary>
    [Server]
    public void ServerSetWorkerCount(int count)
    {
        _assignedWorkerCount.Value = count;
    }

    /// <summary>
    /// Destroy building (server only)
    /// </summary>
    [Server]
    public void ServerDestroy()
    {
        Debug.Log($"[NetworkedBuilding] Destroyed: {buildingData?.buildingName}");

        // Notify manager
        if (NetworkedBuildingManager.Instance != null)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                NetworkedBuildingManager.Instance.ServerRemoveBuilding(netObj.ObjectId, null);
            }
        }
    }

    // ============================================================================
    // SYNCVAR CALLBACKS (FishNet 4.x pattern)
    // ============================================================================

    private void OnBuildingIndexChanged(int prev, int next, bool asServer)
    {
        if (!asServer && NetworkedBuildingManager.Instance != null)
        {
            buildingData = NetworkedBuildingManager.Instance.GetBuildingDataByIndex(next);
            UpdateVisuals();
        }
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        OnHealthUpdated?.Invoke(next, _maxHealth.Value);
    }

    private void OnOperationalChanged(bool prev, bool next, bool asServer)
    {
        OnOperationalStateChanged?.Invoke(next);
    }

    // ============================================================================
    // VISUAL UPDATES
    // ============================================================================

    private void UpdateVisuals()
    {
        if (buildingData == null) return;

        // Update sprite if using SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && buildingData.icon != null)
        {
            sr.sprite = buildingData.icon;
        }

        // Update name
        gameObject.name = $"Building_{buildingData.buildingName}";
    }
}
