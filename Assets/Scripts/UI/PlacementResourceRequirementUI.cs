using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlacementResourceRequirementUI : MonoBehaviour
{
    [SerializeField] private Vector2 worldOffset = new Vector2(0.45f, 0.25f);
    [SerializeField] private Vector2 screenOffset = new Vector2(4f, 0f);
    [SerializeField] private Color enoughColor = new Color(0.94f, 0.96f, 0.98f, 1f);
    [SerializeField] private Color insufficientColor = new Color(1f, 0.24f, 0.2f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.78f, 0.82f, 0.86f, 0.35f);
    [SerializeField] private int paddingLeft = 6;
    [SerializeField] private int paddingRight = 6;
    [SerializeField] private int paddingTop = 4;
    [SerializeField] private int paddingBottom = 4;
    [SerializeField] private float iconSize = 18f;
    [SerializeField] private float textWidth = 42f;
    [SerializeField] private float fontSize = 14f;

    private readonly List<ResourceCostRow> rows = new List<ResourceCostRow>();
    private ResourceCostRow workerRow;

    private Canvas canvas;
    private Camera mainCamera;
    private RectTransform panelRect;
    private BuildingData activeBuilding;
    private Vector3 anchorWorldPosition;
    private int placementCount = 1;
    private bool hasAnchor;
    private bool subscribedToResources;
    private bool subscribedToWorkers;

    public static PlacementResourceRequirementUI Ensure(Canvas targetCanvas)
    {
        PlacementResourceRequirementUI existing = FindFirstObjectByType<PlacementResourceRequirementUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (targetCanvas != null)
                existing.Initialize(targetCanvas);
            return existing;
        }

        Canvas canvas = targetCanvas != null ? targetCanvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
            canvas = CreateFallbackCanvas();

        GameObject go = new GameObject("PlacementResourceRequirementUI");
        go.transform.SetParent(canvas.transform, false);

        PlacementResourceRequirementUI ui = go.AddComponent<PlacementResourceRequirementUI>();
        ui.Initialize(canvas);
        return ui;
    }

    public void Initialize(Canvas targetCanvas)
    {
        if (targetCanvas == null)
            return;

        canvas = targetCanvas;
        mainCamera = Camera.main;
        if (panelRect == null)
            BuildLayout();
    }

    private void OnEnable()
    {
        TrySubscribeToRequirementChanges();
    }

    private void OnDisable()
    {
        if (subscribedToResources && ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
        subscribedToResources = false;

        if (subscribedToWorkers && WorkerManager.Instance != null)
            WorkerManager.Instance.OnWorkerCountChanged -= HandleWorkerCountChanged;
        subscribedToWorkers = false;
    }

    private void Update()
    {
        if (panelRect == null || activeBuilding == null)
            return;

        PositionNearPlacement();
    }

    public void Show(BuildingData building)
    {
        TrySubscribeToRequirementChanges();
        activeBuilding = building;
        placementCount = 1;

        if (canvas == null)
            Initialize(FindFirstObjectByType<Canvas>());
        if (panelRect == null || activeBuilding == null)
            return;

        RefreshRows();
        panelRect.gameObject.SetActive(HasCosts(activeBuilding));
        PositionNearPlacement();
    }

    public void SetPlacementAnchor(Vector3 previewCenterWorldPosition, int footprintWidth, int footprintHeight)
    {
        if (activeBuilding == null)
            return;

        float xOffset = Mathf.Max(0.5f, footprintWidth * 0.5f) + worldOffset.x;
        float yOffset = Mathf.Max(0.5f, footprintHeight * 0.5f) + worldOffset.y;
        anchorWorldPosition = previewCenterWorldPosition + new Vector3(xOffset, yOffset, 0f);
        hasAnchor = true;
        PositionNearPlacement();
    }

    public void SetPlacementCount(int count)
    {
        int clampedCount = Mathf.Max(1, count);
        if (placementCount == clampedCount)
            return;

        placementCount = clampedCount;
        RefreshRows();
    }

    public void Hide()
    {
        activeBuilding = null;
        placementCount = 1;
        hasAnchor = false;
        if (panelRect != null)
            panelRect.gameObject.SetActive(false);
    }

    private void HandleResourceChanged(ResourceType type, int amount)
    {
        if (activeBuilding != null)
            RefreshRows();
    }

    private void HandleWorkerCountChanged(WorkerData workerData, int amount)
    {
        if (activeBuilding != null)
            RefreshRows();
    }

    private void TrySubscribeToRequirementChanges()
    {
        if (!subscribedToResources && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
            subscribedToResources = true;
        }

        if (!subscribedToWorkers && WorkerManager.Instance != null)
        {
            WorkerManager.Instance.OnWorkerCountChanged += HandleWorkerCountChanged;
            subscribedToWorkers = true;
        }
    }

    private void RefreshRows()
    {
        if (panelRect == null || activeBuilding == null)
            return;

        TrySubscribeToRequirementChanges();

        List<ResourceCost> costs = activeBuilding.resourceCost;
        int visibleIndex = 0;
        bool showWorkerRequirement = HasWorkerRequirement(activeBuilding);

        if (showWorkerRequirement)
        {
            if (workerRow.Root == null)
                workerRow = CreateRow(panelRect, "WorkerRequirement");

            workerRow.Root.SetActive(true);
            workerRow.Root.transform.SetAsFirstSibling();
            workerRow.Icon.sprite = activeBuilding.builderType.icon;
            workerRow.Icon.enabled = activeBuilding.builderType.icon != null;

            int requiredWorkers = activeBuilding.buildersConsumed * placementCount;
            workerRow.AmountText.text = requiredWorkers.ToString();

            int currentWorkers = WorkerManager.Instance != null
                ? WorkerManager.Instance.GetAvailableWorkerCount(activeBuilding.builderType)
                : 0;
            workerRow.AmountText.color = currentWorkers >= requiredWorkers ? enoughColor : insufficientColor;
        }
        else if (workerRow.Root != null)
        {
            workerRow.Root.SetActive(false);
        }

        if (costs != null)
        {
            foreach (ResourceCost cost in costs)
            {
                if (cost == null || cost.resourceType == null || cost.amount <= 0)
                    continue;

                ResourceCostRow row = GetOrCreateRow(visibleIndex);
                row.Root.SetActive(true);
                row.Icon.sprite = cost.resourceType.Icon;
                row.Icon.enabled = cost.resourceType.Icon != null;
                int requiredAmount = cost.amount * placementCount;
                row.AmountText.text = requiredAmount.ToString();

                int currentAmount = ResourceManager.Instance != null
                    ? ResourceManager.Instance.GetResourceAmount(cost.resourceType)
                    : 0;
                row.AmountText.color = currentAmount >= requiredAmount ? enoughColor : insufficientColor;
                visibleIndex++;
            }
        }

        for (int i = visibleIndex; i < rows.Count; i++)
            rows[i].Root.SetActive(false);

        panelRect.gameObject.SetActive(showWorkerRequirement || visibleIndex > 0);
    }

    private ResourceCostRow GetOrCreateRow(int index)
    {
        while (rows.Count <= index)
            rows.Add(CreateRow(panelRect));

        return rows[index];
    }

    private ResourceCostRow CreateRow(Transform parent)
    {
        return CreateRow(parent, "ResourceCost");
    }

    private ResourceCostRow CreateRow(Transform parent, string rowName)
    {
        RectTransform root = CreateUIObject<RectTransform>(rowName, parent);
        float rowWidth = iconSize + textWidth + 3f;
        root.sizeDelta = new Vector2(rowWidth, iconSize);

        HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
        rootLayout.minWidth = rowWidth;
        rootLayout.minHeight = iconSize;
        rootLayout.preferredWidth = rowWidth;
        rootLayout.preferredHeight = iconSize;

        Image icon = CreateUIObject<Image>("Icon", root);
        icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.minWidth = iconSize;
        iconLayout.minHeight = iconSize;
        iconLayout.preferredWidth = iconSize;
        iconLayout.preferredHeight = iconSize;
        iconLayout.flexibleWidth = 0f;
        iconLayout.flexibleHeight = 0f;

        TextMeshProUGUI text = CreateUIObject<TextMeshProUGUI>("Amount", root);
        text.rectTransform.sizeDelta = new Vector2(textWidth, iconSize);
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.minWidth = textWidth;
        textLayout.minHeight = iconSize;
        textLayout.preferredWidth = textWidth;
        textLayout.preferredHeight = iconSize;
        textLayout.flexibleWidth = 0f;
        textLayout.flexibleHeight = 0f;

        return new ResourceCostRow(root.gameObject, icon, text);
    }

    private void BuildLayout()
    {
        panelRect = GetComponent<RectTransform>();
        if (panelRect == null)
            panelRect = gameObject.AddComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(48f, 80f);
        panelRect.SetAsLastSibling();

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        CanvasGroup canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Hide();
    }

    private void PositionNearPlacement()
    {
        if (!hasAnchor || canvas == null || panelRect == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(mainCamera, anchorWorldPosition) + screenOffset;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint);

        Vector2 size = panelRect.rect.size;
        Rect canvasBounds = canvasRect.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, canvasBounds.xMin, canvasBounds.xMax - size.x);
        localPoint.y = Mathf.Clamp(localPoint.y, canvasBounds.yMin + size.y, canvasBounds.yMax);
        panelRect.anchoredPosition = localPoint;
    }

    private static bool HasCosts(BuildingData building)
    {
        if (building == null)
            return false;

        if (HasWorkerRequirement(building))
            return true;

        if (building.resourceCost == null)
            return false;

        foreach (ResourceCost cost in building.resourceCost)
        {
            if (cost != null && cost.resourceType != null && cost.amount > 0)
                return true;
        }

        return false;
    }

    private static bool HasWorkerRequirement(BuildingData building)
    {
        return building != null && building.builderType != null && building.buildersConsumed > 0;
    }

    private static Canvas CreateFallbackCanvas()
    {
        GameObject go = new GameObject("RuntimeUICanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static T CreateUIObject<T>(string name, Transform parent) where T : Component
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        T existing = obj.GetComponent<T>();
        return existing != null ? existing : obj.AddComponent<T>();
    }

    private readonly struct ResourceCostRow
    {
        public ResourceCostRow(GameObject root, Image icon, TextMeshProUGUI amountText)
        {
            Root = root;
            Icon = icon;
            AmountText = amountText;
        }

        public GameObject Root { get; }
        public Image Icon { get; }
        public TextMeshProUGUI AmountText { get; }
    }
}
