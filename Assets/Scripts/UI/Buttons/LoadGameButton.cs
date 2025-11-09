using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadGameButton : MonoBehaviour
{
    [SerializeField] private string defaultSceneName = "SampleScene";
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Disable button if no saved progress
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (button != null && ProgressionManager.Instance != null)
        {
            button.interactable = ProgressionManager.Instance.HasSavedProgress();
        }
    }

    /// <summary>
    /// Assign this method to the button's OnClick() in inspector
    /// </summary>
    public void OnLoadGameClicked()
    {
        Debug.Log("Loading saved game...");

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasSavedProgress())
        {
            ProgressionManager.Instance.LoadProgress();

            // Load the current chapter's scene
            string sceneName = defaultSceneName;
            if (ChapterManager.Instance != null && ChapterManager.Instance.CurrentChapter != null)
            {
                sceneName = ChapterManager.Instance.CurrentChapter.sceneName;
            }

            SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            Debug.LogWarning("No saved progress found!");
        }
    }
}
