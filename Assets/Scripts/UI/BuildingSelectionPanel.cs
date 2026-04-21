using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    private BuildingCategory currentCategory;
    private Button selectedCategoryButton;
    private bool categoriesVisible;

    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingData>>();

    private RectTransform buildingListRect;
    private CanvasGroup buildingListCanvasGroup;
    private GridLayoutGroup buildingGrid;
    private Coroutine slideRoutine;

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

                TextMeshProUGUI text = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = GetCategoryDisplayName(category);

                SetButtonColor(button, normalCategoryColor);
            }
        }
    }

    private void ClearCategoryButtons()
    {
        foreach (GameObject btn in categoryButtons)
            Destroy(btn);
        categoryButtons.Clear();
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
                    btn.onClick.AddListener(() => OnBuildingSelected(data));
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
        foreach (GameObject btn in buildingButtons) Destroy(btn);
        buildingButtons.Clear();
        if (actionPanelToggle != null) actionPanelToggle.SuppressAutoHide = false;
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
        if (data != null && PlacementSystem.Instance != null)
            PlacementSystem.Instance.EnterBuildMode(data);
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
