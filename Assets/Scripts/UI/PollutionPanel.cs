using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PollutionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider pollutionSlider;
    [SerializeField] private TextMeshProUGUI pollutionAmountText;
    [SerializeField] private TextMeshProUGUI pollutionPercentageText;
    [SerializeField] private Image fillImage;

    [Header("Color Settings")]
    [SerializeField] private Color lowPollutionColor = Color.green;
    [SerializeField] private Color mediumPollutionColor = Color.yellow;
    [SerializeField] private Color highPollutionColor = Color.red;
    [SerializeField] private float mediumThreshold = 0.5f;
    [SerializeField] private float highThreshold = 0.75f;

    private void Start()
    {
        if (PollutionManager.Instance != null)
        {
            PollutionManager.Instance.OnPollutionChanged += UpdatePollutionDisplay;
            InitializeDisplay();
        }
    }

    private void OnDestroy()
    {
        if (PollutionManager.Instance != null)
        {
            PollutionManager.Instance.OnPollutionChanged -= UpdatePollutionDisplay;
        }
    }

    private void InitializeDisplay()
    {
        if (PollutionManager.Instance != null)
        {
            UpdatePollutionDisplay(PollutionManager.Instance.CurrentPollution, PollutionManager.Instance.MaxPollution);
        }
    }

    private void UpdatePollutionDisplay(float current, float max)
    {
        if (pollutionSlider != null)
        {
            pollutionSlider.maxValue = max;
            pollutionSlider.value = current;
        }

        if (pollutionAmountText != null)
        {
            pollutionAmountText.text = $"{current:F1} / {max:F0}";
        }

        if (pollutionPercentageText != null)
        {
            float percentage = (current / max) * 100f;
            pollutionPercentageText.text = $"{percentage:F1}%";
        }

        // Update color based on pollution level
        if (fillImage != null)
        {
            float ratio = current / max;
            Color targetColor;

            if (ratio < mediumThreshold)
            {
                targetColor = Color.Lerp(lowPollutionColor, mediumPollutionColor, ratio / mediumThreshold);
            }
            else if (ratio < highThreshold)
            {
                float t = (ratio - mediumThreshold) / (highThreshold - mediumThreshold);
                targetColor = Color.Lerp(mediumPollutionColor, highPollutionColor, t);
            }
            else
            {
                targetColor = highPollutionColor;
            }

            fillImage.color = targetColor;
        }
    }
}
