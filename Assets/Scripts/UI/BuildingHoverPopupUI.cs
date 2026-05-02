using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildingHoverPopupUI : MonoBehaviour
{
    [SerializeField] private Vector2 screenOffset = new Vector2(18f, -12f);
    [SerializeField] private float maxWidth = 280f;
    [SerializeField] private float iconSize = 18f;
    [SerializeField] private float titleFontSize = 14f;
    [SerializeField] private float bodyFontSize = 12f;
    [SerializeField] private Color operationalColor = new Color(0.55f, 0.95f, 0.55f, 1f);
    [SerializeField] private Color notOperationalColor = new Color(1f, 0.45f, 0.4f, 1f);

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform panelRect;
    private Transform rowContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI statusText;
    private float hideAtUnscaledTime;
    private object activeSource;
    private Building activeBuilding;
    private bool activeBuildingShowsHealth;

    public void Initialize(Canvas targetCanvas)
    {
        if (targetCanvas == null)
        {
            return;
        }

        canvas = targetCanvas.rootCanvas != null ? targetCanvas.rootCanvas : targetCanvas;
        canvasRect = canvas.transform as RectTransform;

        if (panelRect == null)
        {
            BuildLayout();
        }

        HideImmediate();
    }

    private void Update()
    {
        if (panelRect == null || !panelRect.gameObject.activeSelf)
        {
            return;
        }

        if (activeBuilding != null)
        {
            RefreshBuiltBuildingStatus(activeBuilding);
        }

        PositionNearCursor();

        if (Time.unscaledTime >= hideAtUnscaledTime)
        {
            HideImmediate();
        }
    }

    public void ShowBuildingRequirements(BuildingData data, float durationSeconds, object source)
    {
        if (data == null)
        {
            Hide(source);
            return;
        }

        EnsureReady();
        if (panelRect == null)
        {
            return;
        }

        activeSource = source;
        activeBuilding = null;
        activeBuildingShowsHealth = false;
        hideAtUnscaledTime = Time.unscaledTime + Mathf.Max(0.01f, durationSeconds);

        SetTitle(GetBuildingName(data));
        SetStatus("Requirements", Color.white);
        RebuildRows(BuildRequirementRows(data));
        ShowPanel();
    }

    public void ShowBuiltBuildingStatus(Building building, float durationSeconds, object source)
    {
        if (building == null || building.IsDestroyed || building.BuildingData == null)
        {
            Hide(source);
            return;
        }

        EnsureReady();
        if (panelRect == null)
        {
            return;
        }

        activeSource = source;
        activeBuilding = building;
        activeBuildingShowsHealth = false;
        hideAtUnscaledTime = Time.unscaledTime + Mathf.Max(0.01f, durationSeconds);

        RefreshBuiltBuildingStatus(building);
        ShowPanel();
    }

    public void ShowBuiltBuildingHealth(Building building, float durationSeconds, object source)
    {
        if (building == null || building.IsDestroyed || building.BuildingData == null)
        {
            Hide(source);
            return;
        }

        EnsureReady();
        if (panelRect == null)
        {
            return;
        }

        activeSource = source;
        activeBuilding = building;
        activeBuildingShowsHealth = true;
        hideAtUnscaledTime = Time.unscaledTime + Mathf.Max(0.01f, durationSeconds);

        RefreshBuiltBuildingHealth(building);
        ShowPanel();
    }

    public void Hide(object source)
    {
        if (activeSource == null || activeSource == source)
        {
            HideImmediate();
        }
    }

    public static string BuildRequirementSummary(BuildingData data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        foreach (PopupRow row in BuildRequirementRows(data))
        {
            if (!string.IsNullOrWhiteSpace(row.Text))
            {
                builder.AppendLine(row.Text);
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildBuiltBuildingStatusSummary(Building building)
    {
        if (building == null || building.BuildingData == null)
        {
            return string.Empty;
        }

        return BuildBuiltBuildingStatusSummary(
            building.GetTotalAssignedWorkerCount(),
            building.GetTotalWorkerCapacity(),
            building.IsOperational);
    }

    public static string BuildBuiltBuildingStatusSummary(int assignedWorkers, int workerCapacity, bool isOperational)
    {
        return $"Workers: {assignedWorkers} / {workerCapacity}\n{(isOperational ? "Operational" : "Not Operational")}";
    }

    public static string BuildBuiltBuildingHealthSummary(float currentHealth, float maxHealth)
    {
        float safeMaxHealth = Mathf.Max(1f, maxHealth);
        float clampedHealth = Mathf.Clamp(currentHealth, 0f, safeMaxHealth);
        return $"Health: {clampedHealth:0} / {safeMaxHealth:0} HP";
    }

    public static bool ShouldShowOperationalStatus(BuildingData data)
    {
        return data != null && data.workerRequirements != null && data.workerRequirements.Count > 0;
    }

    private void RefreshBuiltBuildingStatus(Building building)
    {
        if (activeBuildingShowsHealth)
        {
            RefreshBuiltBuildingHealth(building);
            return;
        }

        BuildingData data = building.BuildingData;
        SetTitle(GetBuildingName(data));
        SetStatus(building.IsOperational ? "Operational" : "Not Operational", building.IsOperational ? operationalColor : notOperationalColor);

        List<PopupRow> rows = new List<PopupRow>
        {
            new PopupRow(null, $"Workers: {building.GetTotalAssignedWorkerCount()} / {building.GetTotalWorkerCapacity()}")
        };

        if (data.workerRequirements != null)
        {
            foreach (WorkerRequirement requirement in data.workerRequirements)
            {
                if (requirement == null)
                {
                    continue;
                }

                string workerName = requirement.workerType != null && !string.IsNullOrWhiteSpace(requirement.workerType.workerName)
                    ? requirement.workerType.workerName
                    : "Worker";
                int assigned = requirement.workerType != null ? building.GetAssignedWorkerCount(requirement.workerType) : 0;
                rows.Add(new PopupRow(requirement.workerType != null ? requirement.workerType.icon : null, $"{workerName}: {assigned} / {requirement.requiredCount} required"));
            }
        }

        RebuildRows(rows);
    }

    private void RefreshBuiltBuildingHealth(Building building)
    {
        BuildingData data = building.BuildingData;
        SetTitle(GetBuildingName(data));
        SetStatus("Health", Color.white);

        List<PopupRow> rows = new List<PopupRow>
        {
            new PopupRow(null, BuildBuiltBuildingHealthSummary(building.CurrentHealth, data.maxHealth))
        };

        RebuildRows(rows);
    }

    private static List<PopupRow> BuildRequirementRows(BuildingData data)
    {
        List<PopupRow> rows = new List<PopupRow>();

        if (data.builderType != null && data.buildersConsumed > 0)
        {
            string workerName = !string.IsNullOrWhiteSpace(data.builderType.workerName) ? data.builderType.workerName : "Worker";
            rows.Add(new PopupRow(data.builderType.icon, $"Build: {data.buildersConsumed} {workerName}"));
        }

        if (data.resourceCost != null)
        {
            foreach (ResourceCost cost in data.resourceCost)
            {
                if (cost == null || cost.resourceType == null || cost.amount <= 0)
                {
                    continue;
                }

                rows.Add(new PopupRow(cost.resourceType.Icon, $"{cost.amount} {cost.resourceType.ResourceName}"));
            }
        }

        if (data.workerRequirements != null)
        {
            foreach (WorkerRequirement requirement in data.workerRequirements)
            {
                if (requirement == null)
                {
                    continue;
                }

                string workerName = requirement.workerType != null && !string.IsNullOrWhiteSpace(requirement.workerType.workerName)
                    ? requirement.workerType.workerName
                    : "Worker";
                Sprite icon = requirement.workerType != null ? requirement.workerType.icon : null;
                rows.Add(new PopupRow(icon, $"Operate: {requirement.requiredCount} {workerName} required, {requirement.capacity} capacity"));
            }
        }

        if (rows.Count == 0)
        {
            rows.Add(new PopupRow(null, "No worker or resource requirements"));
        }

        return rows;
    }

    private void RebuildRows(List<PopupRow> rows)
    {
        if (rowContainer == null)
        {
            return;
        }

        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (PopupRow row in rows)
        {
            CreateRow(row);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    private void CreateRow(PopupRow row)
    {
        RectTransform root = CreateUIObject<RectTransform>("Row", rowContainer);

        HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image icon = CreateUIObject<Image>("Icon", root);
        icon.sprite = row.Icon;
        icon.enabled = row.Icon != null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        AddLayoutElement(icon.gameObject, iconSize, iconSize);

        TextMeshProUGUI text = CreateText("Text", root, bodyFontSize, FontStyles.Normal, TextAlignmentOptions.Left);
        text.text = row.Text;
        text.enableWordWrapping = true;
        AddLayoutElement(text.gameObject, maxWidth - iconSize - 34f, -1f, true);
    }

    private void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    private void SetStatus(string status, Color color)
    {
        if (statusText != null)
        {
            statusText.text = status;
            statusText.color = color;
        }
    }

    private void ShowPanel()
    {
        panelRect.gameObject.SetActive(true);
        panelRect.SetAsLastSibling();
        PositionNearCursor();
    }

    private void HideImmediate()
    {
        activeSource = null;
        activeBuilding = null;
        activeBuildingShowsHealth = false;

        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(false);
        }
    }

    private void EnsureReady()
    {
        if (canvas == null)
        {
            Initialize(FindFirstObjectByType<Canvas>());
        }
    }

    private void BuildLayout()
    {
        panelRect = CreateUIObject<RectTransform>("BuildingHoverPopup", canvas.transform);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(maxWidth, 120f);
        panelRect.SetAsLastSibling();

        Image background = panelRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.045f, 0.055f, 0.065f, 0.96f);
        background.raycastTarget = false;

        CanvasGroup canvasGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = panelRect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 9);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText("Title", panelRect, titleFontSize, FontStyles.Bold, TextAlignmentOptions.Left);
        titleText.color = new Color(0.94f, 0.96f, 0.98f, 1f);
        AddLayoutElement(titleText.gameObject, maxWidth - 20f, -1f);

        statusText = CreateText("Status", panelRect, bodyFontSize, FontStyles.Bold, TextAlignmentOptions.Left);
        statusText.color = new Color(0.7f, 0.88f, 1f, 1f);
        AddLayoutElement(statusText.gameObject, maxWidth - 20f, -1f);

        rowContainer = CreateUIObject<RectTransform>("Rows", panelRect);
        VerticalLayoutGroup rowLayout = rowContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 3f;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        AddLayoutElement(rowContainer.gameObject, maxWidth - 20f, -1f, true);
    }

    private void PositionNearCursor()
    {
        if (canvasRect == null || panelRect == null || Mouse.current == null)
        {
            return;
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPosition = Mouse.current.position.ReadValue() + screenOffset;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint);

        Vector2 size = panelRect.rect.size;
        Rect bounds = canvasRect.rect;

        float x = localPoint.x;
        float y = localPoint.y;

        if (x + size.x > bounds.xMax)
        {
            x = localPoint.x - size.x - Mathf.Abs(screenOffset.x);
        }

        if (y - size.y < bounds.yMin)
        {
            y = localPoint.y + size.y + Mathf.Abs(screenOffset.y);
        }

        x = Mathf.Clamp(x, bounds.xMin, bounds.xMax - size.x);
        y = Mathf.Clamp(y, bounds.yMin + size.y, bounds.yMax);
        panelRect.anchoredPosition = new Vector2(x, y);
    }

    private static string GetBuildingName(BuildingData data)
    {
        if (data == null)
        {
            return "Building";
        }

        return string.IsNullOrWhiteSpace(data.buildingName) ? data.name : data.buildingName;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateUIObject<TextMeshProUGUI>(name, parent);
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static T CreateUIObject<T>(string name, Transform parent) where T : Component
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        T existing = obj.GetComponent<T>();
        return existing != null ? existing : obj.AddComponent<T>();
    }

    private static void AddLayoutElement(GameObject obj, float preferredWidth, float preferredHeight, bool flexibleWidth = false)
    {
        LayoutElement element = obj.AddComponent<LayoutElement>();
        if (preferredWidth > 0f)
        {
            element.preferredWidth = preferredWidth;
        }
        if (preferredHeight > 0f)
        {
            element.preferredHeight = preferredHeight;
        }
        if (flexibleWidth)
        {
            element.flexibleWidth = 1f;
        }
    }

    private readonly struct PopupRow
    {
        public PopupRow(Sprite icon, string text)
        {
            Icon = icon;
            Text = text;
        }

        public Sprite Icon { get; }
        public string Text { get; }
    }
}
