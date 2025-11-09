using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionObjectiveSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI objectiveDescriptionText;
    [SerializeField] private TextMeshProUGUI objectiveProgressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image checkmarkIcon;
    [SerializeField] private Image optionalIcon;

    [Header("Colors")]
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color incompleteColor = Color.white;
    [SerializeField] private Color optionalColor = Color.yellow;

    private MissionObjective objective;

    public void Setup(MissionObjective obj)
    {
        objective = obj;
        UpdateObjective(obj);
    }

    public void UpdateObjective(MissionObjective obj)
    {
        if (obj == null) return;

        // Update description text
        if (objectiveDescriptionText != null)
        {
            string description = obj.objectiveDescription;
            
            // Add optional tag if needed
            if (obj.isOptional)
            {
                description = $"<color=#{ColorUtility.ToHtmlStringRGB(optionalColor)}>[Optional]</color> {description}";
            }

            // Add strikethrough if completed
            if (obj.isCompleted)
            {
                description = $"<s>{description}</s>";
            }

            objectiveDescriptionText.text = description;
            objectiveDescriptionText.color = obj.isCompleted ? completedColor : incompleteColor;
        }

        // Update progress text
        if (objectiveProgressText != null)
        {
            objectiveProgressText.text = obj.GetProgressText();
            objectiveProgressText.color = obj.isCompleted ? completedColor : incompleteColor;
        }

        // Update progress bar
        if (progressBar != null)
        {
            progressBar.value = obj.GetProgress();
            
            // Change progress bar color
            var fillImage = progressBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = obj.isCompleted ? completedColor : incompleteColor;
            }
        }

        // Update checkmark icon
        if (checkmarkIcon != null)
        {
            checkmarkIcon.gameObject.SetActive(obj.isCompleted);
            checkmarkIcon.color = completedColor;
        }

        // Update optional icon
        if (optionalIcon != null)
        {
            optionalIcon.gameObject.SetActive(obj.isOptional && !obj.isCompleted);
            optionalIcon.color = optionalColor;
        }
    }

    private void Update()
    {
        // Update progress in real-time for time-based objectives
        if (objective != null && !objective.isCompleted)
        {
            switch (objective.type)
            {
                case ObjectiveType.SurviveTime:
                case ObjectiveType.MaintainPollution:
                    UpdateObjective(objective);
                    break;
            }
        }
    }
}
