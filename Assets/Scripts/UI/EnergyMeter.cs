using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays energy status as a meter with fill bar.
/// Shows current/max energy with color-coded text.
/// Blinks red "No Energy" when power is out.
/// </summary>
public class EnergyMeter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillBar;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private Image backgroundImage;

    [Header("Energy Resource")]
    [SerializeField] private ResourceType energyResource;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color fillNormalColor = new Color(0.2f, 0.8f, 1f); // Cyan/blue
    [SerializeField] private Color fillWarningColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color fillCriticalColor = Color.red;

    [Header("Thresholds")]
    [SerializeField] private float warningThreshold = 0.3f; // 30%
    [SerializeField] private float criticalThreshold = 0.1f; // 10%

    [Header("Blink Settings")]
    [SerializeField] private float blinkSpeed = 2f;

    // State
    private int currentEnergy = 0;
    private int maxEnergy = 500; // Default max, updated from storage
    private bool isBlinking = false;
    private float blinkTimer = 0f;
    private bool blinkState = false;

    private void Start()
    {
        // Load energy resource if not assigned
        if (energyResource == null)
        {
            energyResource = Resources.Load<ResourceType>("GameData/Resources/Energy");
        }

        // Subscribe to events
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += OnResourceChanged;

            // Initialize with current values
            if (energyResource != null)
            {
                currentEnergy = ResourceManager.Instance.GetResourceAmount(energyResource);
                maxEnergy = ResourceManager.Instance.GetResourceCapacity(energyResource);
                if (maxEnergy <= 0) maxEnergy = 500; // Fallback default
            }
        }

        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnPowerOutage += OnPowerOutage;
        }

        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
        }

        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.OnPowerOutage -= OnPowerOutage;
        }
    }

    private void Update()
    {
        if (isBlinking)
        {
            blinkTimer += Time.deltaTime * blinkSpeed;
            bool newBlinkState = Mathf.Sin(blinkTimer * Mathf.PI) > 0;

            if (newBlinkState != blinkState)
            {
                blinkState = newBlinkState;
                UpdateBlinkDisplay();
            }
        }
    }

    private void OnResourceChanged(ResourceType type, int amount)
    {
        if (energyResource != null && type == energyResource)
        {
            currentEnergy = amount;

            // Also update capacity in case it changed (storage buildings added/removed)
            if (ResourceManager.Instance != null)
            {
                int newCapacity = ResourceManager.Instance.GetResourceCapacity(energyResource);
                if (newCapacity > 0) maxEnergy = newCapacity;
            }

            UpdateDisplay();
        }
    }

    private void OnPowerOutage(bool hasPower)
    {
        isBlinking = !hasPower;
        if (hasPower)
        {
            blinkTimer = 0f;
            blinkState = false;
        }
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (maxEnergy <= 0) maxEnergy = 500;

        float fillPercent = (float)currentEnergy / maxEnergy;
        fillPercent = Mathf.Clamp01(fillPercent);

        // Update fill bar
        if (fillBar != null)
        {
            fillBar.fillAmount = fillPercent;

            // Color based on fill level
            if (fillPercent <= criticalThreshold)
            {
                fillBar.color = fillCriticalColor;
            }
            else if (fillPercent <= warningThreshold)
            {
                fillBar.color = fillWarningColor;
            }
            else
            {
                fillBar.color = fillNormalColor;
            }
        }

        // Update text
        if (energyText != null)
        {
            if (isBlinking && currentEnergy <= 0)
            {
                // Will be handled by UpdateBlinkDisplay
                UpdateBlinkDisplay();
            }
            else
            {
                energyText.text = $"{currentEnergy}/{maxEnergy}";

                // Text color based on fill level
                if (fillPercent <= criticalThreshold)
                {
                    energyText.color = criticalColor;
                }
                else if (fillPercent <= warningThreshold)
                {
                    energyText.color = warningColor;
                }
                else
                {
                    energyText.color = normalColor;
                }
            }
        }
    }

    private void UpdateBlinkDisplay()
    {
        if (energyText == null) return;

        if (currentEnergy <= 0 && isBlinking)
        {
            if (blinkState)
            {
                energyText.text = "No Energy";
                energyText.color = criticalColor;
            }
            else
            {
                energyText.text = $"0/{maxEnergy}";
                energyText.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, 0.3f);
            }
        }
    }

    /// <summary>
    /// Manually set the max energy (for testing or overrides)
    /// </summary>
    public void SetMaxEnergy(int max)
    {
        maxEnergy = Mathf.Max(1, max);
        UpdateDisplay();
    }
}
