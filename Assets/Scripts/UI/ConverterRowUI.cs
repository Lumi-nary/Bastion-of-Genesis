using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI row for a resource converter.
/// Shows: [Icon] Resource Name - X/Y (queued/max) [Progress Bar] [Convert] [Cancel]
/// </summary>
public class ConverterRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceNameText;
    [SerializeField] private TextMeshProUGUI queueText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button convertButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Transform costContainer;

    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;

    private ResourceType resourceType;
    private List<ResourceConverterComponent> converters = new List<ResourceConverterComponent>();
    private readonly Color readyColor = new Color(0.55f, 0.95f, 1f, 1f);
    private readonly Color blockedColor = new Color(1f, 0.42f, 0.32f, 1f);
    private readonly Color mutedColor = new Color(0.55f, 0.7f, 0.82f, 1f);

    private void Awake()
    {
        EnsureHudElements();
        ApplyHudStyle();
    }

    public void Initialize(ResourceType resourceType)
    {
        this.resourceType = resourceType;
        EnsureHudElements();
        ApplyHudStyle();

        // Set icon
        if (resourceIcon != null && resourceType.Icon != null)
        {
            resourceIcon.sprite = resourceType.Icon;
        }

        // Set name
        if (resourceNameText != null)
        {
            resourceNameText.text = resourceType.ResourceName;
        }

        // Setup buttons
        if (convertButton != null)
        {
            convertButton.onClick.AddListener(OnConvertClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // Get converters and subscribe to events
        RefreshConverters();
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from converter events
        foreach (var converter in converters)
        {
            if (converter != null)
            {
                converter.OnQueueChanged -= UpdateDisplay;
                converter.OnProgressChanged -= UpdateProgress;
            }
        }
    }

    private void RefreshConverters()
    {
        // Unsubscribe from old converters
        foreach (var converter in converters)
        {
            if (converter != null)
            {
                converter.OnQueueChanged -= UpdateDisplay;
                converter.OnProgressChanged -= UpdateProgress;
            }
        }

        // Get current converters
        if (BuildingManager.Instance != null)
        {
            converters = BuildingManager.Instance.GetConvertersForResourceType(resourceType);
        }

        // Subscribe to new converters
        foreach (var converter in converters)
        {
            if (converter != null)
            {
                converter.OnQueueChanged += UpdateDisplay;
                converter.OnProgressChanged += UpdateProgress;
            }
        }

        // Set cost text from first converter
        if (costText != null && converters.Count > 0 && converters[0].InputCost != null)
        {
            costText.text = GetCostString(converters[0].InputCost);
        }
    }

    public void UpdateDisplay()
    {
        if (BuildingManager.Instance == null || resourceType == null) return;

        // Refresh converter list in case it changed
        RefreshConverters();

        int converterCount = converters.Count;
        int totalQueued = BuildingManager.Instance.GetTotalQueuedConversions(resourceType);
        int totalMax = BuildingManager.Instance.GetTotalMaxConversionQueue(resourceType);
        bool hasRoom = totalQueued < totalMax;
        bool canAfford = CanAfford();
        bool atCapacity = IsResourceAtCapacity();

        // Update queue text
        if (queueText != null)
        {
            queueText.text = $"{converterCount}x online  |  Queue {totalQueued}/{totalMax}";
        }

        // Update button states
        if (convertButton != null)
        {
            convertButton.interactable = hasRoom && canAfford && !atCapacity;
        }
        if (cancelButton != null)
        {
            cancelButton.interactable = totalQueued > 0;
        }

        UpdateStatus(hasRoom, canAfford, atCapacity);
        UpdateCostDisplay();

        // Update progress
        UpdateProgressFromConverters();
    }

    private void UpdateProgress(float progress)
    {
        UpdateProgressFromConverters();
    }

    private void Update()
    {
        // Continuously update progress display
        UpdateProgressFromConverters();
    }

    private void UpdateProgressFromConverters()
    {
        // Find a converter that is currently converting
        ResourceConverterComponent activeConverter = null;
        foreach (var converter in converters)
        {
            if (converter != null && converter.IsConverting)
            {
                activeConverter = converter;
                break;
            }
        }

        if (activeConverter != null)
        {
            float progress = activeConverter.ConversionProgress;
            float maxTime = activeConverter.ConversionTime;
            float percentage = (progress / maxTime) * 100f;

            if (progressBar != null)
            {
                progressBar.value = progress / maxTime;
            }

            if (progressText != null)
            {
                progressText.text = $"Converting {percentage:F0}%";
            }
        }
        else
        {
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }
            if (progressText != null)
            {
                progressText.text = "Idle";
            }
        }
    }

    private void OnConvertClicked()
    {
        if (BuildingManager.Instance != null && resourceType != null)
        {
            BuildingManager.Instance.QueueConversion(resourceType);
            UpdateDisplay();
        }
    }

    private void OnCancelClicked()
    {
        if (BuildingManager.Instance != null && resourceType != null)
        {
            BuildingManager.Instance.CancelConversion(resourceType);
            UpdateDisplay();
        }
    }

    private bool CanAfford()
    {
        if (converters.Count == 0) return false;

        var inputCost = converters[0].InputCost;
        if (inputCost == null) return true;
        if (ResourceManager.Instance == null) return false;

        foreach (var cost in inputCost)
        {
            if (ResourceManager.Instance.GetResourceAmount(cost.resourceType) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsResourceAtCapacity()
    {
        if (resourceType == null || ResourceManager.Instance == null || BuildingManager.Instance == null)
            return false;

        if (converters.Count == 0) return false;

        int outputAmount = converters[0].OutputAmount;
        int currentAmount = ResourceManager.Instance.GetResourceAmount(resourceType);
        int totalQueued = BuildingManager.Instance.GetTotalQueuedConversions(resourceType) * outputAmount;
        int capacity = ResourceManager.Instance.GetResourceCapacity(resourceType);

        return currentAmount + totalQueued + outputAmount > capacity;
    }

    private void UpdateStatus(bool hasRoom, bool canAfford, bool atCapacity)
    {
        if (statusText == null)
            return;

        if (!hasRoom)
        {
            statusText.text = "Queue full";
            statusText.color = blockedColor;
        }
        else if (atCapacity)
        {
            statusText.text = "Storage capacity full";
            statusText.color = blockedColor;
        }
        else if (!canAfford)
        {
            statusText.text = "Insufficient resources";
            statusText.color = blockedColor;
        }
        else
        {
            statusText.text = "Ready to convert";
            statusText.color = readyColor;
        }
    }

    private void UpdateCostDisplay()
    {
        List<ResourceCost> inputCost = converters.Count > 0 ? converters[0].InputCost : null;

        if (costText != null)
        {
            costText.text = GetCostString(inputCost);
            costText.color = CanAfford() ? mutedColor : blockedColor;
        }

        if (costContainer == null)
            return;

        ClearCostContainer();

        if (inputCost == null || inputCost.Count == 0)
        {
            CreateCostChip(null, "Free", true);
            return;
        }

        foreach (ResourceCost cost in inputCost)
        {
            bool affordable = ResourceManager.Instance == null ||
                ResourceManager.Instance.GetResourceAmount(cost.resourceType) >= cost.amount;
            string resourceName = cost.resourceType != null ? cost.resourceType.ResourceName : "Resource";
            CreateCostChip(cost.resourceType != null ? cost.resourceType.Icon : null, $"{cost.amount} {resourceName}", affordable);
        }
    }

    private void EnsureHudElements()
    {
        RectTransform rect = transform as RectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 96f);

        EnsureStructuredLayout();
    }

    private void ApplyHudStyle()
    {
        Image background = GetComponent<Image>();
        if (background != null)
            background.color = new Color(0.035f, 0.065f, 0.115f, 0.9f);

        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 96f;
        layoutElement.preferredHeight = 104f;

        if (resourceNameText != null)
        {
            resourceNameText.fontSize = 18f;
            resourceNameText.fontStyle = FontStyles.Bold;
            resourceNameText.color = new Color(0.9f, 0.98f, 1f, 1f);
        }

        if (queueText != null)
        {
            queueText.fontSize = 15f;
            queueText.color = mutedColor;
        }

        if (progressText != null)
        {
            progressText.fontSize = 15f;
            progressText.color = readyColor;
        }

        StyleButton(convertButton, new Color(0.05f, 0.35f, 0.58f, 1f));
        StyleButton(cancelButton, new Color(0.22f, 0.08f, 0.12f, 1f));
    }

    private void EnsureStructuredLayout()
    {
        Transform existingRoot = transform.Find("RowHudRoot");
        if (existingRoot != null)
        {
            AssignStructuredReferences(existingRoot);
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        RectTransform root = CreateLayoutObject("RowHudRoot", transform);
        Stretch(root);
        HorizontalLayoutGroup rootLayout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(4, 4, 2, 2);
        rootLayout.spacing = 12f;
        rootLayout.childAlignment = TextAnchor.MiddleLeft;
        rootLayout.childControlHeight = true;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childForceExpandWidth = false;
        LayoutElement rootElement = root.gameObject.AddComponent<LayoutElement>();
        rootElement.flexibleWidth = 1f;
        rootElement.preferredHeight = 86f;

        resourceIcon = CreateImage("Icon", root, 44f, 44f);

        RectTransform infoColumn = CreateLayoutObject("InfoColumn", root);
        VerticalLayoutGroup infoLayout = infoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 4f;
        infoLayout.childAlignment = TextAnchor.MiddleLeft;
        infoLayout.childControlHeight = true;
        infoLayout.childControlWidth = true;
        infoLayout.childForceExpandHeight = false;
        infoLayout.childForceExpandWidth = true;
        LayoutElement infoElement = infoColumn.gameObject.AddComponent<LayoutElement>();
        infoElement.flexibleWidth = 1f;
        infoElement.minWidth = 170f;

        resourceNameText = CreateText(infoColumn, "ResourceNameText", "Resource", 18f, new Color(0.9f, 0.98f, 1f, 1f));
        queueText = CreateText(infoColumn, "QueueText", "0x online  |  Queue 0/0", 15f, mutedColor);
        statusText = CreateText(infoColumn, "StatusText", "Ready to convert", 15f, readyColor);

        RectTransform progressColumn = CreateLayoutObject("ProgressColumn", root);
        VerticalLayoutGroup progressLayout = progressColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        progressLayout.spacing = 6f;
        progressLayout.childAlignment = TextAnchor.MiddleLeft;
        progressLayout.childControlHeight = true;
        progressLayout.childControlWidth = true;
        progressLayout.childForceExpandHeight = false;
        progressLayout.childForceExpandWidth = true;
        LayoutElement progressElement = progressColumn.gameObject.AddComponent<LayoutElement>();
        progressElement.preferredWidth = 120f;

        progressText = CreateText(progressColumn, "ProgressText", "Idle", 15f, readyColor);
        progressBar = CreateProgressBar(progressColumn);

        costContainer = CreateCostContainer(root);

        RectTransform actionColumn = CreateLayoutObject("Actions", root);
        VerticalLayoutGroup actionLayout = actionColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        actionLayout.spacing = 6f;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlHeight = true;
        actionLayout.childControlWidth = true;
        actionLayout.childForceExpandHeight = false;
        actionLayout.childForceExpandWidth = false;
        LayoutElement actionElement = actionColumn.gameObject.AddComponent<LayoutElement>();
        actionElement.preferredWidth = 96f;

        convertButton = CreateButton(actionColumn, "Convert", "Convert", new Color(0.05f, 0.35f, 0.58f, 1f));
        cancelButton = CreateButton(actionColumn, "Cancel", "Cancel", new Color(0.22f, 0.08f, 0.12f, 1f));
    }

    private void AssignStructuredReferences(Transform root)
    {
        resourceIcon = root.Find("Icon")?.GetComponent<Image>() ?? resourceIcon;
        resourceNameText = root.Find("InfoColumn/ResourceNameText")?.GetComponent<TextMeshProUGUI>() ?? resourceNameText;
        queueText = root.Find("InfoColumn/QueueText")?.GetComponent<TextMeshProUGUI>() ?? queueText;
        statusText = root.Find("InfoColumn/StatusText")?.GetComponent<TextMeshProUGUI>() ?? statusText;
        progressText = root.Find("ProgressColumn/ProgressText")?.GetComponent<TextMeshProUGUI>() ?? progressText;
        progressBar = root.Find("ProgressColumn/ProgressBar")?.GetComponent<Slider>() ?? progressBar;
        costContainer = root.Find("CostContainer") ?? costContainer;
        convertButton = root.Find("Actions/Convert")?.GetComponent<Button>() ?? convertButton;
        cancelButton = root.Find("Actions/Cancel")?.GetComponent<Button>() ?? cancelButton;
    }

    private RectTransform CreateLayoutObject(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.layer = gameObject.layer;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private Image CreateImage(string name, Transform parent, float width, float height)
    {
        RectTransform rect = CreateLayoutObject(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        return image;
    }

    private Button CreateButton(Transform parent, string name, string label, Color color)
    {
        RectTransform rect = CreateLayoutObject(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        TextMeshProUGUI text = CreateText(rect, "Label", label, 15f, Color.white);
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 88f;
        layout.preferredHeight = 32f;
        return button;
    }

    private Slider CreateProgressBar(Transform parent)
    {
        GameObject root = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
        root.layer = gameObject.layer;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.sizeDelta = new Vector2(108f, 14f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.layer = gameObject.layer;
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.SetParent(rootRect, false);
        Stretch(bgRect);
        background.GetComponent<Image>().color = new Color(0.02f, 0.035f, 0.06f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = gameObject.layer;
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.SetParent(rootRect, false);
        Stretch(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.layer = gameObject.layer;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        Stretch(fillRect);
        fill.GetComponent<Image>().color = new Color(0.15f, 0.78f, 1f, 1f);

        Slider slider = root.GetComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fillRect;
        slider.targetGraphic = fill.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = 108f;
        layout.preferredHeight = 14f;
        return slider;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        root.layer = gameObject.layer;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TextMeshProUGUI label = root.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = 145f;
        layout.preferredHeight = 28f;
        return label;
    }

    private Transform CreateCostContainer(Transform parent)
    {
        GameObject root = new GameObject("CostContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        root.layer = gameObject.layer;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        LayoutElement layoutElement = root.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 136f;
        layoutElement.preferredHeight = 28f;
        return root.transform;
    }

    private void CreateCostChip(Sprite icon, string text, bool affordable)
    {
        GameObject root = new GameObject("CostChip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        root.layer = gameObject.layer;
        root.transform.SetParent(costContainer, false);

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        if (icon != null)
        {
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.layer = gameObject.layer;
            iconGO.transform.SetParent(root.transform, false);
            Image image = iconGO.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            LayoutElement iconLayout = iconGO.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 18f;
            iconLayout.preferredHeight = 18f;
        }

        TextMeshProUGUI label = CreateChipText(root.transform, text, affordable ? mutedColor : blockedColor);
        label.fontSize = 14f;
    }

    private TextMeshProUGUI CreateChipText(Transform parent, string text, Color color)
    {
        GameObject root = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        root.layer = gameObject.layer;
        root.transform.SetParent(parent, false);
        TextMeshProUGUI label = root.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 14f;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }

    private void ClearCostContainer()
    {
        for (int i = costContainer.childCount - 1; i >= 0; i--)
            Destroy(costContainer.GetChild(i).gameObject);
    }

    private void StyleButton(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.fontSize = 15f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
        }
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private string GetCostString(List<ResourceCost> costs)
    {
        if (costs == null || costs.Count == 0) return "Free";

        List<string> parts = new List<string>();
        foreach (var cost in costs)
        {
            parts.Add($"{cost.amount} {cost.resourceType.ResourceName}");
        }
        return string.Join(", ", parts);
    }
}
