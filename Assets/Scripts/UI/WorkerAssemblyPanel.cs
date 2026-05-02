using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Panel that displays all worker factories and resource converters.
/// Shows queue status, progress, and allows assembling/cancelling.
/// </summary>
public class WorkerAssemblyPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject factoryRowPrefab;
    [SerializeField] private GameObject converterRowPrefab;
    [SerializeField] private Transform factoryRowsContainer;
    [SerializeField] private Transform converterRowsContainer;
    [SerializeField] private Button assembleTabButton;
    [SerializeField] private Button convertTabButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Anchor to trigger button")]
    [Tooltip("Horizontal gap between the trigger button and the left edge of the panel.")]
    [SerializeField] private float anchorGapX = 8f;

    [Header("Slide Animation")]
    [SerializeField] private FlyoutPanelSlider slider;

    // Track spawned rows
    private Dictionary<WorkerData, FactoryRowUI> factoryRows = new Dictionary<WorkerData, FactoryRowUI>();
    private Dictionary<ResourceType, ConverterRowUI> converterRows = new Dictionary<ResourceType, ConverterRowUI>();
    private bool isVisible;
    private bool showingWorkers = true;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (slider == null && panel != null) slider = panel.GetComponent<FlyoutPanelSlider>();
        if (slider == null) slider = GetComponent<FlyoutPanelSlider>();
        EnsureHudLayout();
        ApplyActiveTab();
    }

    private void Start()
    {
        // Subscribe to changes
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnFactoriesChanged += RefreshPanel;
            BuildingManager.Instance.OnConvertersChanged += RefreshPanel;
        }

        HidePanel();
    }

    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnFactoriesChanged -= RefreshPanel;
            BuildingManager.Instance.OnConvertersChanged -= RefreshPanel;
        }
    }

    private void Update()
    {
        // Click outside to close
        if (isVisible && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsPointerOverPanel())
            {
                HidePanel();
            }
        }
    }

    private bool IsPointerOverPanel()
    {
        // Check if mouse is over any UI element (like the toggle button)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        if (panelRect == null) return false;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos);
    }

    /// <summary>
    /// Refresh the panel to show current factories and converters.
    /// </summary>
    public void RefreshPanel()
    {
        if (BuildingManager.Instance == null || rowContainer == null) return;

        RefreshFactoryRows();
        RefreshConverterRows();
        ApplyActiveTab();
    }

    private void RefreshFactoryRows()
    {
        if (factoryRowPrefab == null) return;

        // Get all worker types with factories
        List<WorkerData> workerTypes = BuildingManager.Instance.GetAvailableWorkerTypes();

        // Remove rows for worker types that no longer have factories
        List<WorkerData> toRemove = new List<WorkerData>();
        foreach (var kvp in factoryRows)
        {
            if (!workerTypes.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }
        foreach (var key in toRemove)
        {
            factoryRows.Remove(key);
        }

        // Add or update rows for each worker type
        foreach (WorkerData workerType in workerTypes)
        {
            if (!factoryRows.ContainsKey(workerType))
            {
                // Create new row
                GameObject rowGO = Instantiate(factoryRowPrefab, GetFactoryRowsParent());
                FactoryRowUI row = rowGO.GetComponent<FactoryRowUI>();
                if (row != null)
                {
                    row.Initialize(workerType);
                    factoryRows[workerType] = row;
                }
            }
            else
            {
                // Update existing row
                factoryRows[workerType].UpdateDisplay();
            }
        }
    }

    private void RefreshConverterRows()
    {
        if (converterRowPrefab == null) return;

        // Get all resource types with converters
        List<ResourceType> resourceTypes = BuildingManager.Instance.GetAvailableConversionTypes();

        // Remove rows for resource types that no longer have converters
        List<ResourceType> toRemove = new List<ResourceType>();
        foreach (var kvp in converterRows)
        {
            if (!resourceTypes.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }
        foreach (var key in toRemove)
        {
            converterRows.Remove(key);
        }

        // Add or update rows for each resource type
        foreach (ResourceType resourceType in resourceTypes)
        {
            if (!converterRows.ContainsKey(resourceType))
            {
                // Create new row
                GameObject rowGO = Instantiate(converterRowPrefab, GetConverterRowsParent());
                ConverterRowUI row = rowGO.GetComponent<ConverterRowUI>();
                if (row != null)
                {
                    row.Initialize(resourceType);
                    converterRows[resourceType] = row;
                }
            }
            else
            {
                // Update existing row
                converterRows[resourceType].UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// Show the assembly panel.
    /// </summary>
    public void ShowPanel()
    {
        ShowPanel(null);
    }

    /// <summary>
    /// Show the assembly panel anchored next to the button that triggered it.
    /// </summary>
    public void ShowPanel(RectTransform anchorButton)
    {
        if (panel == null) return;
        if (anchorButton != null && panelRect != null)
            PanelPositioner.PositionBeside(panelRect, anchorButton, anchorGapX);
        panel.SetActive(true);
        isVisible = true;
        RefreshPanel();
        if (slider != null) slider.PlayIn();
    }

    /// <summary>
    /// Hide the assembly panel.
    /// </summary>
    public void HidePanel()
    {
        if (panel == null) return;
        if (!isVisible)
        {
            panel.SetActive(false);
            return;
        }
        isVisible = false;
        if (slider != null && panel.activeSelf)
            slider.PlayOut(() => panel.SetActive(false));
        else
            panel.SetActive(false);
    }

    /// <summary>
    /// Toggle panel visibility.
    /// </summary>
    public void TogglePanel()
    {
        if (panel != null)
        {
            if (panel.activeSelf)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }
    }

    private Transform GetFactoryRowsParent()
    {
        return factoryRowsContainer != null ? factoryRowsContainer : rowContainer;
    }

    private Transform GetConverterRowsParent()
    {
        return converterRowsContainer != null ? converterRowsContainer : rowContainer;
    }

    private void ShowWorkersTab()
    {
        showingWorkers = true;
        ApplyActiveTab();
    }

    private void ShowConvertersTab()
    {
        showingWorkers = false;
        ApplyActiveTab();
    }

    private void ApplyActiveTab()
    {
        if (factoryRowsContainer != null)
            factoryRowsContainer.gameObject.SetActive(showingWorkers);

        if (converterRowsContainer != null)
            converterRowsContainer.gameObject.SetActive(!showingWorkers);

        StyleTabButton(assembleTabButton, showingWorkers);
        StyleTabButton(convertTabButton, !showingWorkers);

        if (emptyStateText != null)
        {
            int visibleRows = showingWorkers ? factoryRows.Count : converterRows.Count;
            emptyStateText.gameObject.SetActive(visibleRows == 0);
            emptyStateText.text = showingWorkers
                ? "No worker factories online"
                : "No resource converters online";
        }
    }

    private void EnsureHudLayout()
    {
        if (panel == null)
            return;

        if (panelRect == null)
            panelRect = panel.GetComponent<RectTransform>();

        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(720f, 520f);

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.025f, 0.045f, 0.085f, 0.94f);
            panelImage.raycastTarget = true;
        }

        RectTransform existingRoot = panel.transform.Find("AssemblyHudRoot") as RectTransform;
        if (existingRoot != null)
        {
            AssignGeneratedReferences(existingRoot);
            return;
        }

        if (rowContainer != null && rowContainer.parent == panel.transform)
            rowContainer.gameObject.SetActive(false);

        RectTransform root = CreateUIObject("AssemblyHudRoot", panel.transform);
        Stretch(root);

        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 14, 16);
        rootLayout.spacing = 12f;
        rootLayout.childControlHeight = true;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childForceExpandWidth = true;

        RectTransform header = CreateUIObject("Header", root);
        HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = false;
        LayoutElement headerLayoutElement = header.gameObject.AddComponent<LayoutElement>();
        headerLayoutElement.preferredHeight = 46f;

        TextMeshProUGUI title = CreateText("Title", header, "ASSEMBLY CONTROL", 21f, FontStyles.Bold, new Color(0.86f, 0.97f, 1f, 1f));
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.minWidth = 230f;
        titleLayout.flexibleWidth = 1f;

        assembleTabButton = CreateButton("AssembleTab", header, "Workers");
        convertTabButton = CreateButton("ConvertTab", header, "Converters");
        closeButton = CreateButton("CloseButton", header, "X");

        LayoutElement closeLayout = closeButton.GetComponent<LayoutElement>();
        closeLayout.preferredWidth = 42f;

        assembleTabButton.onClick.AddListener(ShowWorkersTab);
        convertTabButton.onClick.AddListener(ShowConvertersTab);
        closeButton.onClick.AddListener(HidePanel);

        RectTransform viewport = CreateUIObject("Viewport", root);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0.015f, 0.027f, 0.05f, 0.45f);
        viewport.gameObject.AddComponent<RectMask2D>();
        LayoutElement viewportLayout = viewport.gameObject.AddComponent<LayoutElement>();
        viewportLayout.flexibleHeight = 1f;

        ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = viewport;

        RectTransform content = CreateUIObject("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(10, 10, 10, 10);
        contentLayout.spacing = 8f;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = content;

        factoryRowsContainer = CreateRowsContainer("WorkerRows", content);
        converterRowsContainer = CreateRowsContainer("ConverterRows", content);

        emptyStateText = CreateText("EmptyState", content, "No worker factories online", 17f, FontStyles.Normal, new Color(0.55f, 0.76f, 0.9f, 1f));
        emptyStateText.alignment = TextAlignmentOptions.Center;
        LayoutElement emptyLayout = emptyStateText.gameObject.AddComponent<LayoutElement>();
        emptyLayout.preferredHeight = 70f;
    }

    private void AssignGeneratedReferences(RectTransform root)
    {
        Transform workerRows = root.Find("Viewport/Content/WorkerRows");
        Transform converterRows = root.Find("Viewport/Content/ConverterRows");
        Transform emptyState = root.Find("Viewport/Content/EmptyState");
        Transform assembleTab = root.Find("Header/AssembleTab");
        Transform convertTab = root.Find("Header/ConvertTab");
        Transform close = root.Find("Header/CloseButton");

        if (workerRows != null) factoryRowsContainer = workerRows;
        if (converterRows != null) converterRowsContainer = converterRows;
        if (emptyState != null) emptyStateText = emptyState.GetComponent<TextMeshProUGUI>();
        if (assembleTab != null) assembleTabButton = assembleTab.GetComponent<Button>();
        if (convertTab != null) convertTabButton = convertTab.GetComponent<Button>();
        if (close != null) closeButton = close.GetComponent<Button>();
    }

    private Transform CreateRowsContainer(string name, Transform parent)
    {
        RectTransform container = CreateUIObject(name, parent);
        VerticalLayoutGroup layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = container.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return container;
    }

    private RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = panel.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color)
    {
        RectTransform rect = CreateUIObject(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        RectTransform rect = CreateUIObject(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.055f, 0.105f, 0.17f, 0.92f);
        Button button = rect.gameObject.AddComponent<Button>();

        TextMeshProUGUI text = CreateText("Label", rect, label, 15f, FontStyles.Bold, new Color(0.84f, 0.96f, 1f, 1f));
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;

        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 118f;
        layout.preferredHeight = 38f;
        return button;
    }

    private void StyleTabButton(Button button, bool active)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = active
                ? new Color(0.05f, 0.35f, 0.58f, 0.95f)
                : new Color(0.045f, 0.08f, 0.13f, 0.82f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.color = active
                ? new Color(0.95f, 1f, 1f, 1f)
                : new Color(0.56f, 0.78f, 0.9f, 1f);
    }
}
