using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

/// <summary>
/// NetworkedEnemy - Network-synced enemy component.
/// Server controls movement and attacks, clients see synced state.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkedEnemy : NetworkBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseHealth = 100;
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;

    // Synced state (FishNet 4.x SyncVar<T>)
    private readonly SyncVar<int> _currentHealth = new SyncVar<int>();
    private readonly SyncVar<int> _maxHealth = new SyncVar<int>();
    private readonly SyncVar<int> _enemyTypeIndex = new SyncVar<int>();
    private readonly SyncVar<Vector3> _targetPosition = new SyncVar<Vector3>();

    // Server-only state
    private float attackTimer;
    private Transform currentTarget;
    private bool isDead;

    // Events
    public event Action OnDeath;
    public event Action<int, int> OnHealthUpdated;

    // Properties
    public int CurrentHealth => _currentHealth.Value;
    public int MaxHealth => _maxHealth.Value;
    public bool IsDead => isDead;
    public float HealthPercent => _maxHealth.Value > 0 ? (float)_currentHealth.Value / _maxHealth.Value : 0f;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _currentHealth.OnChange += OnHealthChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _currentHealth.OnChange -= OnHealthChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[NetworkedEnemy] Spawned on client, type: {_enemyTypeIndex.Value}");
    }

    private void Update()
    {
        // Only server handles AI
        if (!IsServerStarted) return;
        if (isDead) return;

        ServerUpdateAI();
    }

    // ============================================================================
    // SERVER INITIALIZATION
    // ============================================================================

    [Server]
    public void ServerInitialize(int typeIndex, float statMultiplier)
    {
        _enemyTypeIndex.Value = typeIndex;
        _maxHealth.Value = Mathf.RoundToInt(baseHealth * statMultiplier);
        _currentHealth.Value = _maxHealth.Value;
        isDead = false;

        Debug.Log($"[NetworkedEnemy] Initialized type {typeIndex} with {_maxHealth.Value} HP (x{statMultiplier})");
    }

    // ============================================================================
    // SERVER AI
    // ============================================================================

    [Server]
    private void ServerUpdateAI()
    {
        if (currentTarget == null)
        {
            FindTarget();
        }

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance <= attackRange)
            {
                ServerAttack();
            }
            else
            {
                ServerMoveTowards(currentTarget.position);
            }
        }
        else
        {
            ServerMoveTowards(Vector3.zero);
        }
    }

    [Server]
    private void FindTarget()
    {
        currentTarget = null;
    }

    [Server]
    private void ServerMoveTowards(Vector3 target)
    {
        _targetPosition.Value = target;
        Vector3 direction = (target - transform.position).normalized;
        transform.position += direction * baseMoveSpeed * Time.deltaTime;
    }

    [Server]
    private void ServerAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0) return;

        attackTimer = attackCooldown;

        if (currentTarget != null)
        {
            NetworkedBuilding building = currentTarget.GetComponent<NetworkedBuilding>();
            if (building != null)
            {
                building.ServerTakeDamage(baseDamage);
                Debug.Log($"[NetworkedEnemy] Attacked building for {baseDamage} damage");
            }
        }

        RpcPlayAttackAnimation();
    }

    // ============================================================================
    // SERVER DAMAGE
    // ============================================================================

    [Server]
    public void ServerTakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        _currentHealth.Value = Mathf.Max(0, _currentHealth.Value - damage);

        if (_currentHealth.Value <= 0)
        {
            ServerDie();
        }
    }

    [Server]
    private void ServerDie()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[NetworkedEnemy] Died");

        OnDeath?.Invoke();
        RpcPlayDeathAnimation();
        Invoke(nameof(DespawnEnemy), 1f);
    }

    [Server]
    private void DespawnEnemy()
    {
        FishNet.InstanceFinder.ServerManager.Despawn(gameObject);
    }

    // ============================================================================
    // SYNC CALLBACKS (FishNet 4.x)
    // ============================================================================

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        OnHealthUpdated?.Invoke(next, _maxHealth.Value);
    }

    // ============================================================================
    // CLIENT RPCS
    // ============================================================================

    [ObserversRpc]
    private void RpcPlayAttackAnimation()
    {
        // TODO: Implement animation system
    }

    [ObserversRpc]
    private void RpcPlayDeathAnimation()
    {
        // TODO: Implement animation system
    }
}
