using UnityEngine;

/// <summary>
/// DEPRECATED: TooltipManager has been merged into UIManager.
/// This class exists for backward compatibility only.
/// Use UIManager.Instance.ShowTooltip() and UIManager.Instance.HideTooltip() instead.
/// </summary>
[System.Obsolete("TooltipManager is deprecated. Use UIManager instead.")]
public class TooltipManager : MonoBehaviour
{
    // Legacy static instance that redirects to UIManager
    public static UIManager Instance => UIManager.Instance;

    private void Awake()
    {
        // This component is no longer needed - UIManager handles tooltips
        Debug.LogWarning("[TooltipManager] This component is deprecated. Tooltip functionality has been merged into UIManager. Please remove this GameObject and configure tooltips on UIManager instead.");
    }

    // Legacy methods that redirect to UIManager
    public void ShowTooltip(string header, string description)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTooltip(header, description);
        }
    }

    public void HideTooltip()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideTooltip();
        }
    }

    public void ShowTooltipFromProvider(ITooltipProvider provider)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTooltipFromProvider(provider);
        }
    }
}
