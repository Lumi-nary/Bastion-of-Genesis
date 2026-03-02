using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a boss health bar at the top of the screen.
/// Subscribes to BossEnemy static events to show/hide automatically.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bossHealthPanel;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Image fillImage;

    [Header("Phase Colors")]
    [SerializeField] private Color phase1Color = Color.green;
    [SerializeField] private Color phase2Color = Color.yellow;
    [SerializeField] private Color phase3Color = Color.red;

    private BossEnemy currentBoss;

    private void OnEnable()
    {
        BossEnemy.OnBossSpawned += HandleBossSpawned;
        BossEnemy.OnBossDefeated += HandleBossDefeated;
        BossEnemy.OnBossHealthChanged += HandleBossHealthChanged;
    }

    private void OnDisable()
    {
        BossEnemy.OnBossSpawned -= HandleBossSpawned;
        BossEnemy.OnBossDefeated -= HandleBossDefeated;
        BossEnemy.OnBossHealthChanged -= HandleBossHealthChanged;

        if (currentBoss != null)
        {
            currentBoss.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void Start()
    {
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(false);
        }
    }

    private void HandleBossSpawned(BossEnemy boss)
    {
        currentBoss = boss;
        boss.OnPhaseChanged += HandlePhaseChanged;

        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(true);
        }

        if (bossNameText != null)
        {
            bossNameText.text = boss.BossName;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = boss.MaxHealth;
            healthSlider.value = boss.CurrentHealth;
        }

        UpdatePhaseDisplay(1);
    }

    private void HandleBossDefeated(BossEnemy boss)
    {
        if (boss == currentBoss)
        {
            boss.OnPhaseChanged -= HandlePhaseChanged;
            currentBoss = null;

            if (bossHealthPanel != null)
            {
                bossHealthPanel.SetActive(false);
            }
        }
    }

    private void HandleBossHealthChanged(BossEnemy boss, float current, float max)
    {
        if (boss != currentBoss) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
    }

    private void HandlePhaseChanged(int phase)
    {
        UpdatePhaseDisplay(phase);
    }

    private void UpdatePhaseDisplay(int phase)
    {
        if (phaseText != null)
        {
            phaseText.text = $"Phase {phase}";
        }

        if (fillImage != null)
        {
            fillImage.color = phase switch
            {
                2 => phase2Color,
                3 => phase3Color,
                _ => phase1Color
            };
        }
    }
}
