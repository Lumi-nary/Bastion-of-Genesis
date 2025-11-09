using UnityEngine;

public class ResumeButton : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    /// <summary>
    /// Assign this method to the resume button's OnClick() in inspector
    /// </summary>
    public void OnResumeClicked()
    {
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }
}
