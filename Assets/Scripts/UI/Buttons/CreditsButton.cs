using UnityEngine;

/// <summary>
/// CreditsButton navigates to the Credits screen.
/// Follows Pattern 7 (UI Canvas Management) - calls MenuManager, never manipulates canvas directly.
/// </summary>
public class CreditsButton : MonoBehaviour
{
    /// <summary>
    /// OnClick event handler - navigates to Credits canvas.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// </summary>
    public void OnClick()
    {
        MenuManager.Instance.ShowCreditsCanvas();
    }
}
