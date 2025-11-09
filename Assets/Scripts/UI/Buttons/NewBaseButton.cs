using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBaseButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    /// <summary>
    /// Assign this method to the button's OnClick() in inspector
    /// </summary>
    public void OnNewBaseClicked()
    {
        Debug.Log("Starting new game...");

        // Delete existing progress
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.DeleteProgress();
        }

        // Load game scene
        SceneManager.LoadSceneAsync(gameSceneName);
    }
}
