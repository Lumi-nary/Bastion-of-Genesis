using UnityEngine;

/// <summary>
/// Boss enemy subclass with phase system and enrage mechanics.
/// Phase 1 (100-50% HP): Normal behavior
/// Phase 2 (50-25% HP): Enraged — speed and damage boost
/// Phase 3 (below 25% HP): Desperate — further boosts
/// </summary>
public class BossEnemy : Enemy
{
    [Header("Boss Settings")]
    [SerializeField] private float phase2SpeedMultiplier = 1.3f;
    [SerializeField] private float phase2DamageMultiplier = 1.5f;
    [SerializeField] private float phase3SpeedMultiplier = 1.5f;
    [SerializeField] private float phase3DamageMultiplier = 2.0f;

    private int currentPhase = 1;
    private float baseDamage;
    private float baseSpeed;

    // Events
    public event System.Action<int> OnPhaseChanged;
    public static event System.Action<BossEnemy> OnBossSpawned;
    public static event System.Action<BossEnemy> OnBossDefeated;
    public static event System.Action<BossEnemy, float, float> OnBossHealthChanged; // boss, current, max

    public int CurrentPhase => currentPhase;
    public string BossName => enemyData != null ? enemyData.GetDisplayName() : "Boss";

    public override void Initialize(EnemyData data, float diffMult, float pollMult)
    {
        base.Initialize(data, diffMult, pollMult);

        baseDamage = effectiveDamage;
        baseSpeed = data.moveSpeed;
        currentPhase = 1;

        OnBossSpawned?.Invoke(this);
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (!isDead)
        {
            OnBossHealthChanged?.Invoke(this, CurrentHealth, MaxHealth);
            UpdatePhase();
        }
    }

    private void UpdatePhase()
    {
        float healthPercent = CurrentHealth / MaxHealth;
        int newPhase;

        if (healthPercent > 0.5f)
            newPhase = 1;
        else if (healthPercent > 0.25f)
            newPhase = 2;
        else
            newPhase = 3;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            ApplyPhaseModifiers();
            OnPhaseChanged?.Invoke(currentPhase);
            Debug.Log($"[BossEnemy] {BossName} entered Phase {currentPhase}!");
        }
    }

    private void ApplyPhaseModifiers()
    {
        switch (currentPhase)
        {
            case 2:
                effectiveDamage = baseDamage * phase2DamageMultiplier;
                break;
            case 3:
                effectiveDamage = baseDamage * phase3DamageMultiplier;
                break;
            default:
                effectiveDamage = baseDamage;
                break;
        }
    }

    /// <summary>
    /// Get the current speed multiplier based on phase
    /// </summary>
    public float GetPhaseSpeedMultiplier()
    {
        return currentPhase switch
        {
            2 => phase2SpeedMultiplier,
            3 => phase3SpeedMultiplier,
            _ => 1f
        };
    }

    protected override void Die()
    {
        OnBossDefeated?.Invoke(this);
        Debug.Log($"[BossEnemy] {BossName} has been defeated!");
        base.Die();
    }
}
