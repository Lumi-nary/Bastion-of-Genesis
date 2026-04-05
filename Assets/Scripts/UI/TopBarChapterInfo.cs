using UnityEngine;
using TMPro;

/// <summary>
/// Displays current chapter name and mission name in the top bar.
/// </summary>
public class TopBarChapterInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI chapterLabel;

    private bool initialized;

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!initialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        if (initialized) return;
        if (MissionChapterManager.Instance == null) return;
        if (MissionChapterManager.Instance.CurrentChapter == null) return;

        UpdateLabel();
        MissionChapterManager.Instance.OnMissionStarted += OnMissionStarted;
        initialized = true;
    }

    private void OnDestroy()
    {
        if (MissionChapterManager.Instance != null)
            MissionChapterManager.Instance.OnMissionStarted -= OnMissionStarted;
    }

    private void OnMissionStarted(MissionData mission)
    {
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (chapterLabel == null) return;

        var chapter = MissionChapterManager.Instance.CurrentChapter;
        var mission = MissionChapterManager.Instance.CurrentMission;

        string chapterName = chapter != null ? chapter.chapterName : "";
        string missionName = mission != null ? mission.missionName : "";

        if (!string.IsNullOrEmpty(missionName))
            chapterLabel.text = $"{chapterName}\n{missionName}";
        else
            chapterLabel.text = chapterName;
    }
}
