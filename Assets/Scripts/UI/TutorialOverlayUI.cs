using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlayUI : MonoBehaviour
{
    private const int OverlaySortingOrder = 900;
    private const float DisableButtonWidth = 180f;
    private const float DisableButtonHeight = 36f;

    [Header("Instruction")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Disable Tutorial")]
    [SerializeField] private Button disableTutorialButton;
    [SerializeField] private TextMeshProUGUI disableTutorialButtonText;

    [Header("World Highlights")]
    [SerializeField] private GameObject gridCellHighlightPrefab;
    [SerializeField] private Transform gridHighlightParent;

    private readonly List<GameObject> activeHighlights = new List<GameObject>();

    private void OnEnable()
    {
        if (TutorialGuideManager.CanShowTutorialOverlay())
            EnsureDisableTutorialButton();

        Subscribe();
        Refresh();
    }

    private void Start()
    {
        if (TutorialGuideManager.CanShowTutorialOverlay())
            EnsureDisableTutorialButton();

        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (TutorialGuideManager.Instance != null)
            TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;

        ClearHighlights();
    }

    private void Subscribe()
    {
        if (TutorialGuideManager.Instance == null)
            return;

        TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
        TutorialGuideManager.Instance.OnTutorialObjectiveChanged += OnTutorialObjectiveChanged;
    }

    private void OnTutorialObjectiveChanged(MissionObjective objective)
    {
        Refresh();
    }

    public void Refresh()
    {
        MissionObjective objective = TutorialGuideManager.Instance != null ? TutorialGuideManager.Instance.ActiveObjective : null;
        bool tutorialEnabled = TutorialGuideManager.CanShowTutorialOverlay();
        bool hasObjective = tutorialEnabled && objective != null;

        if (instructionPanel != null)
            instructionPanel.SetActive(hasObjective && !string.IsNullOrWhiteSpace(objective.tutorialInstruction));

        if (instructionText != null)
            instructionText.text = hasObjective ? objective.tutorialInstruction : string.Empty;

        if (disableTutorialButton != null)
            disableTutorialButton.gameObject.SetActive(tutorialEnabled);

        RebuildWorldHighlights(hasObjective ? objective : null);
    }

    private void EnsureDisableTutorialButton()
    {
        if (!TutorialGuideManager.CanShowTutorialOverlay())
            return;

        if (disableTutorialButton != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TutorialOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
        }

        GameObject buttonObject = new GameObject("DisableTutorialButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-24f, -88f);
        buttonRect.sizeDelta = new Vector2(DisableButtonWidth, DisableButtonHeight);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.05f, 0.16f, 0.22f, 0.88f);

        disableTutorialButton = buttonObject.GetComponent<Button>();
        ColorBlock colors = disableTutorialButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.46f, 0.82f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        disableTutorialButton.colors = colors;
        disableTutorialButton.onClick.AddListener(OnDisableTutorialClicked);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, 0f);

        disableTutorialButtonText = textObject.GetComponent<TextMeshProUGUI>();
        disableTutorialButtonText.text = "Disable Tutorial";
        disableTutorialButtonText.fontSize = 16f;
        disableTutorialButtonText.fontStyle = FontStyles.Bold;
        disableTutorialButtonText.alignment = TextAlignmentOptions.Center;
        disableTutorialButtonText.color = new Color(0.86f, 0.98f, 1f, 1f);
        disableTutorialButtonText.raycastTarget = false;
    }

    private void OnDisableTutorialClicked()
    {
        if (!TutorialGuideManager.IsTutorialEnabled)
            return;

        if (ModalDialog.Instance == null)
        {
            ConfirmDisableTutorial();
            return;
        }

        ModalDialog.Instance.Show(
            "Disable Tutorial",
            "Turn off tutorial guidance and hard-gated tutorial restrictions for this profile?",
            new[] { "Disable", "Cancel" },
            buttonIndex =>
            {
                if (buttonIndex == 0)
                    ConfirmDisableTutorial();
            });
    }

    private void ConfirmDisableTutorial()
    {
        if (TutorialGuideManager.Instance != null)
            TutorialGuideManager.Instance.DisableTutorial();

        Refresh();
    }

    private void RebuildWorldHighlights(MissionObjective objective)
    {
        ClearHighlights();

        if (objective == null || GridManager.Instance == null)
            return;

        if (objective.type == ObjectiveType.BuildStructures)
        {
            if (objective.allowedPlacementCells == null)
                return;

            foreach (Vector2Int cell in objective.allowedPlacementCells)
                CreateCellHighlight(cell);
        }
        else if (objective.type == ObjectiveType.AssignWorkers && objective.requiredAssignmentBuilding != null && BuildingManager.Instance != null)
        {
            foreach (Building building in BuildingManager.Instance.GetBuildingsByType(objective.requiredAssignmentBuilding))
            {
                if (building != null)
                    CreateCellHighlight(building.gridPosition);
            }
        }
    }

    private void CreateCellHighlight(Vector2Int cell)
    {
        if (gridCellHighlightPrefab == null)
            return;

        GameObject highlight = Instantiate(gridCellHighlightPrefab, gridHighlightParent);
        highlight.transform.position = GridManager.Instance.GridToWorldPosition(cell);
        activeHighlights.Add(highlight);
    }

    private void ClearHighlights()
    {
        foreach (GameObject highlight in activeHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }

        activeHighlights.Clear();
    }
}
