using UnityEngine;

/// <summary>
/// JoinGameButton navigates to the Join Game (co-op) screen.
/// Follows Pattern 7 (UI Canvas Management) - calls MenuManager, never manipulates canvas directly.
/// </summary>
public class JoinGameButton : MonoBehaviour
{
    /// <summary>
    /// OnClick event handler - navigates to Join Game canvas.
    /// Called by Unity Button.onClick event (wired in Inspector).
    /// </summary>
    public void OnClick()
    {
        MenuManager.Instance.ShowJoinGameCanvas();
    }
}
