using UnityEngine;

public class QuitButton : MonoBehaviour
{
    /// <summary>
    /// Assign this method to the button's OnClick() in inspector
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("Quitting game...");

        // Save progress before quitting
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.SaveProgress();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
