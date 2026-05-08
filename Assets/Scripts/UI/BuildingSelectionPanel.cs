using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Building selection with accordion categories in the ActionPanel
/// and a flyout building list panel that slides out from the selected
/// category button.
/// </summary>
public class BuildingSelectionPanel : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BuildingDatabase buildingDatabase;
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private GameObject categoryButtonPrefab;

    [Header("UI References")]
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private Transform buildingContainer;
    [SerializeField] private GameObject buildingListPanel;

    [Header("Category Settings")]
    [SerializeField] private Color selectedCategoryColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color normalCategoryColor = new Color(0.25f, 0.25f, 0.3f, 1f);

    [Header("Dynamic Sizing")]
    [Tooltip("Panel never shrinks below this height (px).")]
    [SerializeField] private float minPanelHeight = 160f;

    [Header("Auto-Hide Suppression")]
    [Tooltip("Optional: when the building list is open, this ActionPanelToggle's auto-hide is paused.")]
    [SerializeField] private ActionPanelToggle actionPanelToggle;

    [Header("Category Accordion Animation")]
    [SerializeField] private AccordionExpander categoryAccordion;

    [Header("Flyout Animation")]
    [Tooltip("Duration of the slide animation in seconds.")]
    [SerializeField] private float slideDuration = 0.18f;

    [Tooltip("Offset from hidden to shown (local X, in pixels). Negative = panel slides in from left.")]
    [SerializeField] private float slideHiddenOffsetX = -40f;

    [Tooltip("Gap between the category button's right edge and the panel's left edge.")]
    [SerializeField] private float panelGapX = 8f;

    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<GameObject> categoryButtons = new List<GameObject>();
    private List<GameObject> buildingButtons = new List<GameObject>();
    private Dictionary<BuildingCategory, Button> categoryButtonByCategory = new Dictionary<BuildingCategory, Button>();

    private BuildingCategory currentCategory;
    private Button selectedCategoryButton;
    private bool categoriesVisible;

    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingData>>();

    private RectTransform buildingListRect;
    private CanvasGroup buildingListCanvasGroup;
    private GridLayoutGroup buildingGrid;
    private Coroutine slideRoutine;
    private bool reopenAfterBuildModeEnds;
    private int suppressCloseInputFrame = -1;

    public bool IsVisible => categoriesVisible;

    private void Awake()
    {
        if (buildingDatabase == null)
        {
            Debug.LogError("[BuildingSelectionPanel] BuildingDatabase not assigned!");
            return;
        }

        CacheBuildingsByCategory();

        if (buildingListPanel != null)
        {
            buildingListRect = buildingListPanel.GetComponent<RectTransform>();
            buildingListCanvasGroup = buildingListPanel.GetComponent<CanvasGroup>();
            if (buildingListCanvasGroup == null)
                buildingListCanvasGroup = buildingListPanel.AddComponent<CanvasGroup>();
            buildingGrid = buildingListPanel.GetComponent<GridLayoutGroup>();
            if (buildingGrid == null && buildingContainer != null)
                buildingGrid = buildingContainer.GetComponent<GridLayoutGroup>();
        }
    }

    private void Start()
    {
        HidePanel();
        SubscribeTutorialGuide();
    }

    private void OnEnable()
    {
        SubscribeTutorialGuide();
    }

    private void OnDisable()
    {
        UnsubscribeFromBuildModeEnded();
        UnsubscribeTutorialGuide();
    }

    private void SubscribeTutorialGuide()
    {
        if (TutorialGuideManager.Instance == null)
            return;

        TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
        TutorialGuideManager.Instance.OnTutorialObjectiveChanged += OnTutorialObjectiveChanged;
    }

    private void UnsubscribeTutorialGuide()
    {
        if (TutorialGuideManager.Instance != null)
            TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
    }

    private void OnTutorialObjectiveChanged(MissionObjective objective)
    {
        ApplyCategoryTutorialHighlights();

        if (IsBuildingListVisible())
            ShowBuildingsForCategory(currentCategory);
    }

    private void Update()
    {
        if (!categoriesVisible || !IsBuildingListVisible())
            return;

        if (Time.frameCount == suppressCloseInputFrame)
            return;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HideBuildingListAnimated();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerInsideBuildingSelection())
        {
            HideBuildingListAnimated();
        }
    }

    private void CacheBuildingsByCategory()
    {
        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
            buildingsByCategory[category] = new List<BuildingData>();

        foreach (BuildingData building in buildingDatabase.availableBuildings)
        {
            if (building != null && building.isPlayerBuildable)
                buildingsByCategory[building.category].Add(building);
        }
    }

    public void TogglePanel()
    {
        if (categoriesVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void ShowPanel()
    {
        categoriesVisible = true;
        CreateCategoryButtons();

        if (categoryContainer != null)
        {
            categoryContainer.gameObject.SetActive(true);
            if (categoryAccordion != null)
                categoryAccordion.Open();
        }
    }

    public void HidePanel()
    {
        categoriesVisible = false;

        HideBuildingListImmediate();

        if (categoryContainer == null)
        {
            ClearCategoryButtons();
            return;
        }

        if (categoryAccordion != null && categoryContainer.gameObject.activeSelf)
        {
            categoryAccordion.Close(() =>
            {
                categoryContainer.gameObject.SetActive(false);
                ClearCategoryButtons();
            });
        }
        else
        {
            categoryContainer.gameObject.SetActive(false);
            ClearCategoryButtons();
        }
    }

    public void HidePanelImmediate()
    {
        categoriesVisible = false;
        HideBuildingListImmediate();

        if (categoryAccordion != null)
        {
            LayoutElement layoutElement = categoryAccordion.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight = 0f;
                layoutElement.minHeight = 0f;
            }

            CanvasGroup canvasGroup = categoryAccordion.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        if (categoryContainer != null)
            categoryContainer.gameObject.SetActive(false);

        ClearCategoryButtons();
    }

    private void CreateCategoryButtons()
    {
        ClearCategoryButtons();

        if (categoryContainer == null || categoryButtonPrefab == null) return;

        selectedCategoryButton = null;

        foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
        {
            if (CountUnlockedInCategory(category) == 0) continue;

            GameObject buttonGO = Instantiate(categoryButtonPrefab, categoryContainer);
            buttonGO.name = category.ToString() + "_Tab";
            categoryButtons.Add(buttonGO);

            Button button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                BuildingCategory captured = category;
                button.onClick.AddListener(() => SelectCategory(captured, button));
                categoryButtonByCategory[category] = button;

                TextMeshProUGUI text = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = GetCategoryDisplayName(category);

                SetButtonColor(button, normalCategoryColor);
            }
        }

        ApplyCategoryTutorialHighlights();
    }

    private void ClearCategoryButtons()
    {
        foreach (GameObject btn in categoryButtons)
            Destroy(btn);
        categoryButtons.Clear();
        categoryButtonByCategory.Clear();
    }

    private void SelectCategory(BuildingCategory category, Button button)
    {
        // Toggle off if clicking the already-selected category.
        if (selectedCategoryButton == button && buildingListPanel != null && buildingListPanel.activeSelf)
        {
            SetButtonColor(selectedCategoryButton, normalCategoryColor);
            selectedCategoryButton = null;
            HideBuildingListAnimated();
            return;
        }

        if (selectedCategoryButton != null)
            SetButtonColor(selectedCategoryButton, normalCategoryColor);

        currentCategory = category;
        selectedCategoryButton = button;
        SetButtonColor(button, selectedCategoryColor);

        ShowBuildingsForCategory(category);
        PositionPanelBesideButton(button);
        StartSlideIn();
    }

    private void ShowBuildingsForCategory(BuildingCategory category)
    {
        if (buildingContainer == null || buildingButtonPrefab == null) return;

        foreach (GameObject btn in buildingButtons)
            Destroy(btn);
        buildingButtons.Clear();

        List<BuildingData> buildings = buildingsByCategory[category];
        foreach (BuildingData data in buildings)
        {
            if (!IsBuildingUnlocked(data)) continue;

            GameObject buttonGO = Instantiate(buildingButtonPrefab, buildingContainer);
            buttonGO.name = data.buildingName + "_Button";
            buildingButtons.Add(buttonGO);

            BuildingButton buildingButton = buttonGO.GetComponent<BuildingButton>();
            if (buildingButton != null)
            {
                buildingButton.Configure(data);
                Button btn = buildingButton.GetButton();
                if (btn != null)
                {
                    btn.interactable = IsBuildingSelectableForTutorial(data);
                    SetTutorialHighlight(btn, TutorialGuideManager.Instance != null &&
                        TutorialGuideManager.Instance.IsTargetAction(TutorialTargetAction.SelectBuilding) &&
                        TutorialGuideManager.Instance.CanSelectBuilding(data));
                    btn.onClick.AddListener(() => OnBuildingSelected(data));
                }
            }
        }

        ResizePanelToFit(buildingButtons.Count);

        if (buildingListPanel != null && !buildingListPanel.activeSelf)
        {
            buildingListPanel.SetActive(true);
            if (actionPanelToggle != null) actionPanelToggle.SuppressAutoHide = true;
        }
    }

    private void ResizePanelToFit(int buttonCount)
    {
        if (buildingListRect == null || buildingGrid == null) return;

        int cols = Mathf.Max(1, buildingGrid.constraintCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt(buttonCount / (float)cols));
        float contentH = rows * buildingGrid.cellSize.y
                       + Mathf.Max(0, rows - 1) * buildingGrid.spacing.y
                       + buildingGrid.padding.top
                       + buildingGrid.padding.bottom;
        float height = Mathf.Max(minPanelHeight, contentH);

        Vector2 size = buildingListRect.sizeDelta;
        buildingListRect.sizeDelta = new Vector2(size.x, height);
    }

    private void PositionPanelBesideButton(Button button)
    {
        if (buildingListRect == null || button == null) return;
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null) return;
        RectTransform parentRect = buildingListRect.parent as RectTransform;
        if (parentRect == null) return;

        // Pick the button's top-right corner and convert to the panel-parent's local space.
        Vector3[] corners = new Vector3[4];
        buttonRect.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
        Vector3 topRightWorld = corners[2];

        Canvas canvas = parentRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, topRightWorld);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out localPoint);

        // Panel pivot is (0, 1) — localPoint now maps top-left of panel to top-right of button (+ gap).
        buildingListRect.anchoredPosition = new Vector2(localPoint.x + panelGapX, localPoint.y);
    }

    private void StartSlideIn()
    {
        if (buildingListRect == null) return;
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideRoutine(true, null));
    }

    private void HideBuildingListAnimated()
    {
        if (buildingListPanel == null || !buildingListPanel.activeSelf) return;
        ClearSelectedCategory();
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        if (actionPanelToggle != null) actionPanelToggle.SuppressAutoHide = false;
        slideRoutine = StartCoroutine(SlideRoutine(false, () =>
        {
            foreach (GameObject btn in buildingButtons) Destroy(btn);
            buildingButtons.Clear();
            buildingListPanel.SetActive(false);
        }));
    }

    private void HideBuildingListImmediate()
    {
        if (slideRoutine != null) { StopCoroutine(slideRoutine); slideRoutine = null; }
        if (buildingListPanel != null) buildingListPanel.SetActive(false);
        ClearSelectedCategory();
        foreach (GameObject btn in buildingButtons) Destroy(btn);
        buildingButtons.Clear();
        if (actionPanelToggle != null) actionPanelToggle.SuppressAutoHide = false;
    }

    private void HideBuildingListForPlacement()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.HideBuildingHoverPopup();

        if (slideRoutine != null) { StopCoroutine(slideRoutine); slideRoutine = null; }
        if (buildingListPanel != null) buildingListPanel.SetActive(false);
        foreach (GameObject btn in buildingButtons) Destroy(btn);
        buildingButtons.Clear();
        if (actionPanelToggle != null) actionPanelToggle.SuppressAutoHide = false;
    }

    private bool IsBuildingListVisible()
    {
        return buildingListPanel != null && buildingListPanel.activeSelf;
    }

    private bool IsPointerInsideBuildingSelection()
    {
        if (Mouse.current == null)
            return false;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();

        if (IsPointerInsideRect(buildingListRect, pointerPosition))
            return true;

        return IsPointerInsideRect(categoryContainer as RectTransform, pointerPosition);
    }

    private bool IsPointerInsideRect(RectTransform rect, Vector2 pointerPosition)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPosition, cam);
    }

    private void ClearSelectedCategory()
    {
        if (selectedCategoryButton != null)
        {
            SetButtonColor(selectedCategoryButton, normalCategoryColor);
            selectedCategoryButton = null;
        }
    }

    private IEnumerator SlideRoutine(bool show, System.Action onDone)
    {
        Vector2 targetPos = buildingListRect.anchoredPosition;
        Vector2 hiddenPos = targetPos + new Vector2(slideHiddenOffsetX, 0);

        Vector2 from = show ? hiddenPos : targetPos;
        Vector2 to = show ? targetPos : hiddenPos;

        buildingListRect.anchoredPosition = from;
        if (buildingListCanvasGroup != null) buildingListCanvasGroup.alpha = show ? 0f : 1f;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / slideDuration);
            float eased = slideCurve.Evaluate(k);
            buildingListRect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            if (buildingListCanvasGroup != null)
                buildingListCanvasGroup.alpha = show ? eased : 1f - eased;
            yield return null;
        }

        buildingListRect.anchoredPosition = to;
        if (buildingListCanvasGroup != null) buildingListCanvasGroup.alpha = show ? 1f : 0f;
        slideRoutine = null;
        onDone?.Invoke();
    }

    private void OnBuildingSelected(BuildingData data)
    {
        if (!IsBuildingSelectableForTutorial(data))
            return;

        if (data != null && PlacementSystem.Instance != null)
        {
            bool buildModeStarted = PlacementSystem.Instance.EnterBuildMode(data);
            if (!buildModeStarted)
                return;

            reopenAfterBuildModeEnds = true;
            PlacementSystem.Instance.BuildingPlacementEnded -= HandleBuildingPlacementEnded;
            PlacementSystem.Instance.BuildingPlacementEnded += HandleBuildingPlacementEnded;
            HideBuildingListForPlacement();
        }
    }

    private void HandleBuildingPlacementEnded()
    {
        if (!reopenAfterBuildModeEnds)
            return;

        reopenAfterBuildModeEnds = false;
        UnsubscribeFromBuildModeEnded();

        if (categoriesVisible && selectedCategoryButton != null)
        {
            ShowBuildingsForCategory(currentCategory);
            PositionPanelBesideButton(selectedCategoryButton);
            suppressCloseInputFrame = Time.frameCount;
            StartSlideIn();
        }
    }

    private void UnsubscribeFromBuildModeEnded()
    {
        if (PlacementSystem.Instance != null)
            PlacementSystem.Instance.BuildingPlacementEnded -= HandleBuildingPlacementEnded;
    }

    private bool IsBuildingSelectableForTutorial(BuildingData data)
    {
        return TutorialGuideManager.Instance == null || TutorialGuideManager.Instance.CanSelectBuilding(data);
    }

    private bool IsBuildingUnlocked(BuildingData data)
    {
        if (data == null) return false;
        if (data.requiredTech == null) return true;

        bool byResearch = ResearchManager.Instance != null && ResearchManager.Instance.IsTechResearched(data.requiredTech);
        bool byMission = BuildingManager.Instance != null && BuildingManager.Instance.IsBuildingUnlockedByMission(data);
        return byResearch || byMission;
    }

    private int CountUnlockedInCategory(BuildingCategory category)
    {
        int count = 0;
        foreach (BuildingData b in buildingsByCategory[category])
            if (IsBuildingUnlocked(b)) count++;
        return count;
    }

    private void SetButtonColor(Button button, Color color)
    {
        Image img = button.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private void SetTutorialHighlight(Button button, bool highlighted)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            TutorialPulseHighlight pulse = image.GetComponent<TutorialPulseHighlight>();
            if (highlighted)
            {
                if (pulse == null)
                    pulse = image.gameObject.AddComponent<TutorialPulseHighlight>();

                pulse.SetHighlighted(true);
            }
            else
            {
                if (pulse != null)
                    pulse.SetHighlighted(false);

                image.color = Color.white;
            }
        }

        SetTutorialTextHighlight(button, highlighted);
    }

    private void SetTutorialTextHighlight(Button button, bool highlighted)
    {
        if (button == null)
            return;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
            return;

        TutorialPulseHighlight pulse = text.GetComponent<TutorialPulseHighlight>();
        if (highlighted)
        {
            if (pulse == null)
                pulse = text.gameObject.AddComponent<TutorialPulseHighlight>();

            pulse.SetHighlighted(true);
            return;
        }

        if (pulse != null)
            pulse.SetHighlighted(false);
    }

    private void ApplyCategoryTutorialHighlights()
    {
        foreach (KeyValuePair<BuildingCategory, Button> entry in categoryButtonByCategory)
        {
            bool selected = selectedCategoryButton == entry.Value;
            SetButtonColor(entry.Value, selected ? selectedCategoryColor : normalCategoryColor);
            SetTutorialTextHighlight(entry.Value, IsTutorialTargetCategory(entry.Key));
        }
    }

    private bool IsTutorialTargetCategory(BuildingCategory category)
    {
        if (TutorialGuideManager.Instance == null ||
            !TutorialGuideManager.Instance.IsTargetAction(TutorialTargetAction.SelectBuilding))
        {
            return false;
        }

        MissionObjective objective = TutorialGuideManager.Instance.ActiveObjective;
        return objective != null &&
            objective.type == ObjectiveType.BuildStructures &&
            objective.requiredBuilding != null &&
            objective.requiredBuilding.category == category;
    }

    private string GetCategoryDisplayName(BuildingCategory category)
    {
        switch (category)
        {
            case BuildingCategory.Command: return "Command";
            case BuildingCategory.Energy: return "Energy";
            case BuildingCategory.Extraction: return "Extraction";
            case BuildingCategory.Production: return "Production";
            case BuildingCategory.Defense: return "Defense";
            case BuildingCategory.Research: return "Research";
            default: return category.ToString();
        }
    }
}
