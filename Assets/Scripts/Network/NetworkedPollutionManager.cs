using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

/// <summary>
/// NetworkedPollutionManager - Syncs pollution state across network.
/// Server-authoritative: Only server modifies pollution.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedPollutionManager : NetworkBehaviour
{
    public static NetworkedPollutionManager Instance { get; private set; }

    [Header("Pollution Settings")]
    [SerializeField] private float defaultMaxPollution = 1000f;
    [SerializeField] private float defaultDecayRate = 0.5f;

    // Synced pollution state (FishNet 4.x SyncVar<T>)
    private readonly SyncVar<float> _currentPollution = new SyncVar<float>();
    private readonly SyncVar<float> _maxPollution = new SyncVar<float>();
    private readonly SyncVar<float> _pollutionDecayRate = new SyncVar<float>();
    private readonly SyncVar<int> _currentDifficultyTier = new SyncVar<int>(1);

    // Events
    public event Action<float, float> OnPollutionUpdated;
    public event Action<int> OnDifficultyTierUpdated;

    // Properties
    public float CurrentPollution => _currentPollution.Value;
    public float MaxPollution => _maxPollution.Value;
    public float PollutionPercent => _maxPollution.Value > 0 ? _currentPollution.Value / _maxPollution.Value : 0f;
    public int DifficultyTier => _currentDifficultyTier.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _currentPollution.OnChange += OnPollutionChanged;
        _currentDifficultyTier.OnChange += OnDifficultyTierChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _currentPollution.OnChange -= OnPollutionChanged;
        _currentDifficultyTier.OnChange -= OnDifficultyTierChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _maxPollution.Value = defaultMaxPollution;
        _pollutionDecayRate.Value = defaultDecayRate;
        _currentPollution.Value = 0f;
        _currentDifficultyTier.Value = 1;

        Debug.Log("[NetworkedPollutionManager] Server initialized");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Initial sync
        if (PollutionManager.Instance != null && !IsServer)
        {
            PollutionManager.Instance.SetPollution(_currentPollution.Value);
        }
    }

    private void Update()
    {
        // Only server handles pollution decay
        if (!IsServerStarted) return;

        // Apply natural decay
        if (_currentPollution.Value > 0 && _pollutionDecayRate.Value > 0)
        {
            ServerRemovePollution(_pollutionDecayRate.Value * Time.deltaTime);
        }
    }

    // ============================================================================
    // SYNC CALLBACKS (FishNet 4.x)
    // ============================================================================

    private void OnPollutionChanged(float prev, float next, bool asServer)
    {
        OnPollutionUpdated?.Invoke(next, _maxPollution.Value);

        // Update local PollutionManager for compatibility
        if (PollutionManager.Instance != null && !asServer)
        {
            PollutionManager.Instance.SetPollution(next);
        }
    }

    private void OnDifficultyTierChanged(int prev, int next, bool asServer)
    {
        OnDifficultyTierUpdated?.Invoke(next);
        Debug.Log($"[NetworkedPollutionManager] Difficulty tier changed: {prev} -> {next}");
    }

    // ============================================================================
    // SERVER METHODS
    // ============================================================================

    /// <summary>
    /// Add pollution (server only)
    /// </summary>
    [Server]
    public void ServerAddPollution(float amount)
    {
        if (amount <= 0) return;

        _currentPollution.Value = Mathf.Min(_currentPollution.Value + amount, _maxPollution.Value);

        // Check for tier changes
        UpdateDifficultyTier();
    }

    /// <summary>
    /// Remove pollution (server only)
    /// </summary>
    [Server]
    public void ServerRemovePollution(float amount)
    {
        if (amount <= 0) return;

        _currentPollution.Value = Mathf.Max(0, _currentPollution.Value - amount);

        // Check for tier changes
        UpdateDifficultyTier();
    }

    /// <summary>
    /// Set pollution directly (server only)
    /// </summary>
    [Server]
    public void ServerSetPollution(float amount)
    {
        _currentPollution.Value = Mathf.Clamp(amount, 0, _maxPollution.Value);
        UpdateDifficultyTier();
    }

    /// <summary>
    /// Configure pollution settings from chapter data (server only)
    /// </summary>
    [Server]
    public void ServerConfigureFromChapter(float chapterMaxPollution, float chapterDecayRate)
    {
        _maxPollution.Value = chapterMaxPollution;
        _pollutionDecayRate.Value = chapterDecayRate;
        Debug.Log($"[NetworkedPollutionManager] Configured: Max={_maxPollution.Value}, Decay={_pollutionDecayRate.Value}");
    }

    /// <summary>
    /// Reset pollution for new chapter (server only)
    /// </summary>
    [Server]
    public void ServerResetPollution()
    {
        _currentPollution.Value = 0f;
        _currentDifficultyTier.Value = 1;
        Debug.Log("[NetworkedPollutionManager] Pollution reset");
    }

    /// <summary>
    /// Update difficulty tier based on pollution level
    /// Tier 1: 0-25%
    /// Tier 2: 25-50%
    /// Tier 3: 50-75%
    /// Tier 4: 75-100%
    /// </summary>
    [Server]
    private void UpdateDifficultyTier()
    {
        float percent = PollutionPercent;
        int newTier;

        if (percent < 0.25f)
            newTier = 1;
        else if (percent < 0.50f)
            newTier = 2;
        else if (percent < 0.75f)
            newTier = 3;
        else
            newTier = 4;

        if (newTier != _currentDifficultyTier.Value)
        {
            _currentDifficultyTier.Value = newTier;
        }
    }

    // ============================================================================
    // DIFFICULTY TIER MODIFIERS
    // ============================================================================

    /// <summary>
    /// Get enemy spawn rate multiplier based on tier
    /// </summary>
    public float GetSpawnRateMultiplier()
    {
        return _currentDifficultyTier.Value switch
        {
            1 => 1.0f,
            2 => 1.25f,
            3 => 1.5f,
            4 => 2.0f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Get enemy stat multiplier based on tier
    /// </summary>
    public float GetEnemyStatMultiplier()
    {
        return _currentDifficultyTier.Value switch
        {
            1 => 1.0f,
            2 => 1.1f,
            3 => 1.2f,
            4 => 1.35f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Get wave size multiplier based on tier
    /// </summary>
    public float GetWaveSizeMultiplier()
    {
        return _currentDifficultyTier.Value switch
        {
            1 => 1.0f,
            2 => 1.5f,
            3 => 2.0f,
            4 => 3.0f,
            _ => 1.0f
        };
    }
}
