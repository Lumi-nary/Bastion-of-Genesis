using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Test button to manually start a mission in SampleScene for UI testing
/// </summary>
public class TestMissionButton : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private ChapterData testChapter;
    [SerializeField] private int testMissionIndex = 0; // Which mission to test (0-9)

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnTestButtonClicked);
        }
    }

    public void OnTestButtonClicked()
    {
        if (MissionChapterManager.Instance == null)
        {
            Debug.LogError("MissionChapterManager not found in scene!");
            return;
        }

        if (testChapter == null)
        {
            Debug.LogError("No test chapter assigned!");
            return;
        }

        if (testMissionIndex < 0 || testMissionIndex >= testChapter.missions.Count)
        {
            Debug.LogError($"Invalid mission index {testMissionIndex}. Chapter has {testChapter.missions.Count} missions.");
            return;
        }

        Debug.Log($"Testing mission: {testChapter.missions[testMissionIndex].missionName}");

        // Manually set the chapter and start the mission
        MissionChapterManager.Instance.StartMission(testChapter.missions[testMissionIndex]);
    }
}
