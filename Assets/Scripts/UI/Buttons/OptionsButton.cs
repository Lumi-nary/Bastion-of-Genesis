using UnityEngine;

/// <summary>
/// OptionsButton navigates to the Options/Settings screen.
/// Follows Pattern 7 (UI Canvas Management) - calls MenuManager, never manipulates canvas directly.
/// </summary>
public class OptionsButton : MonoBehaviour
{
    /// <summary>
    /// OnClick event handler - navigates to Options canvas.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// </summary>
    public void OnClick()
    {
        MenuManager.Instance.ShowOptionsCanvas();
    }
}
