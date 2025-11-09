using UnityEngine;

public class PauseButton : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Assign this method to the pause button's OnClick() in inspector
    /// </summary>
    public void OnPauseClicked()
    {
        Pause();
    }

    private void TogglePause()
    {
        if (Time.timeScale == 0f)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void Resume()
    {
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }
}
