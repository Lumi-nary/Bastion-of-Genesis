using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    public bool IsVisible => panel != null && panel.activeSelf;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI resourceGenerationText;
    [SerializeField] private TextMeshProUGUI statusText; // To show operational status
    [SerializeField] private TextMeshProUGUI totalWorkerCountText; // To show total assigned workers / total capacity

    [Header("Worker Slot UI")]
    [SerializeField] private GameObject workerSlotPrefab; // A prefab for displaying one worker type
    [SerializeField] private Transform workerSlotsContainer; // The parent object for the worker slots

    private Building currentBuilding;
    private List<WorkerSlotUI> currentWorkerSlots = new List<WorkerSlotUI>();
    private Building lastBuiltWorkerSlotsFor;

    private RectTransform contentRoot;
    private Image iconImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI categoryText;
    private Slider healthSlider;
    private TextMeshProUGUI healthValueText;
    private TextMeshProUGUI descriptionText;
    private readonly Dictionary<string, TextMeshProUGUI> sectionTexts = new Dictionary<string, TextMeshProUGUI>();
    private readonly Dictionary<string, RectTransform> sectionRoots = new Dictionary<string, RectTransform>();

    private void Awake()
    {
        if (panel == null)
        {
            panel = gameObject;
        }

        BuildRuntimeLayout();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && currentBuilding != null)
        {
            RefreshDynamicValues();
        }
    }

    public void ShowPanel(Building building)
    {
        if (!ShouldShowFullInfoPanel(building))
        {
            HidePanel();
            return;
        }

        currentBuilding = building;
        panel.SetActive(true);
        PopulateStaticValues();
        RebuildWorkerSlotsIfNeeded();
        RefreshDynamicValues();
    }

    public void HidePanel()
    {
        currentBuilding = null;
        lastBuiltWorkerSlotsFor = null;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public static bool ShouldShowFullInfoPanel(Building building)
    {
        return building != null && !building.IsDestroyed && ShouldShowFullInfoPanel(building.BuildingData);
    }

    public static bool ShouldShowFullInfoPanel(BuildingData data)
    {
        if (data == null)
        {
            return false;
        }

        if (data.workerRequirements != null && data.workerRequirements.Count > 0)
        {
            return true;
        }

        bool hasWallFeature = false;
        bool hasAnyFeature = false;
        if (data.features == null)
        {
            return false;
        }

        foreach (BuildingFeature feature in data.features)
        {
            if (feature == null)
            {
                continue;
            }

            hasAnyFeature = true;

            if (feature is WallFeature)
            {
                hasWallFeature = true;
                continue;
            }

            if (IsInspectableFeature(feature))
            {
                return true;
            }
        }

        return !hasWallFeature && hasAnyFeature;
    }

    private static bool IsInspectableFeature(BuildingFeature feature)
    {
        return feature is ResourceExtractorFeature ||
               feature is ResourceProductionFeature ||
               feature is ResourceConversionFeature ||
               feature is StorageFeature ||
               feature is WorkerFactoryFeature ||
               feature is WorkerStorageFeature ||
               feature is TurretFeature ||
               feature is EnergyGeneratorFeature ||
               feature is EnergyConsumerFeature ||
               feature is PollutionFeature ||
               feature is UpgradeableFeature;
    }

    private void RebuildWorkerSlotsIfNeeded()
    {
        if (currentBuilding == null || currentBuilding == lastBuiltWorkerSlotsFor || workerSlotsContainer == null)
        {
            return;
        }

        foreach (Transform child in workerSlotsContainer)
        {
            Destroy(child.gameObject);
        }
        currentWorkerSlots.Clear();

        if (workerSlotPrefab != null && currentBuilding.BuildingData.workerRequirements != null)
        {
            foreach (var requirement in currentBuilding.BuildingData.workerRequirements)
            {
                GameObject slotGO = Instantiate(workerSlotPrefab, workerSlotsContainer);
                WorkerSlotUI slotUI = slotGO.GetComponent<WorkerSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(currentBuilding, requirement.workerType);
                    currentWorkerSlots.Add(slotUI);
                }
            }
        }

        lastBuiltWorkerSlotsFor = currentBuilding;
    }

    private void PopulateStaticValues()
    {
        if (currentBuilding == null || currentBuilding.BuildingData == null)
        {
            return;
        }

        BuildingData data = currentBuilding.BuildingData;
        string buildingName = string.IsNullOrWhiteSpace(data.buildingName) ? data.name : data.buildingName;

        SetText(buildingNameText, buildingName);
        SetText(titleText, buildingName);
        SetText(categoryText, data.category.ToString());
        SetText(descriptionText, data.description);

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        SetSection("Workers", BuildWorkersText(data), HasWorkers(data));
        SetSection("Resources", BuildResourceText(data), HasResourceSection(data));
        SetSection("Storage", BuildStorageText(data), data.HasFeature<StorageFeature>() || data.HasFeature<WorkerStorageFeature>());
        SetSection("Defense", BuildDefenseText(data), data.HasFeature<TurretFeature>());
        SetSection("Energy", BuildEnergyText(data), data.HasFeature<EnergyGeneratorFeature>() || data.HasFeature<EnergyConsumerFeature>() || data.HasFeature<TurretFeature>());
        SetSection("Upgrades", BuildUpgradeText(data), data.HasFeature<UpgradeableFeature>());
    }

    private void RefreshDynamicValues()
    {
        if (currentBuilding == null || currentBuilding.BuildingData == null)
        {
            return;
        }

        BuildingData data = currentBuilding.BuildingData;
        float maxHealth = Mathf.Max(1f, data.maxHealth);
        float currentHealth = Mathf.Clamp(currentBuilding.CurrentHealth, 0f, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        SetText(healthText, $"Health: {currentHealth:0} / {maxHealth:0}");
        SetText(healthValueText, $"{currentHealth:0} / {maxHealth:0} HP");

        bool hasWorkers = HasWorkers(data);
        if (hasWorkers)
        {
            SetText(totalWorkerCountText, $"Workers: {currentBuilding.GetTotalAssignedWorkerCount()} / {currentBuilding.GetTotalWorkerCapacity()}");
            SetText(statusText, currentBuilding.IsOperational ? "Operational" : "Needs Workers");
            SetText(categoryText, $"{data.category}  |  {(currentBuilding.IsOperational ? "Operational" : "Needs Workers")}");
            statusText.color = currentBuilding.IsOperational ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.45f, 0.4f);
        }
        else
        {
            SetText(totalWorkerCountText, string.Empty);
            SetText(statusText, "Operational");
            SetText(categoryText, $"{data.category}  |  Operational");
            statusText.color = new Color(0.55f, 0.95f, 0.55f);
        }

        foreach (var slot in currentWorkerSlots)
        {
            if (slot != null)
            {
                slot.UpdateUI();
            }
        }
    }

    private static bool HasWorkers(BuildingData data)
    {
        return data.workerRequirements != null && data.workerRequirements.Count > 0;
    }

    private static bool HasResourceSection(BuildingData data)
    {
        return data.HasFeature<ResourceExtractorFeature>() ||
               data.HasFeature<ResourceProductionFeature>() ||
               data.HasFeature<ResourceConversionFeature>() ||
               data.HasFeature<PollutionFeature>();
    }

    private static string BuildWorkersText(BuildingData data)
    {
        if (!HasWorkers(data))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(data.capacityType == WorkerCapacityType.Shared
            ? $"Shared capacity: {data.totalWorkerCapacity}"
            : "Per-type capacity");

        foreach (WorkerRequirement requirement in data.workerRequirements)
        {
            string workerName = requirement.workerType != null ? requirement.workerType.workerName : "Worker";
            builder.AppendLine($"{workerName}: {requirement.requiredCount} required, {requirement.capacity} capacity");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildResourceText(BuildingData data)
    {
        StringBuilder builder = new StringBuilder();

        ResourceExtractorFeature extractor = data.GetFeature<ResourceExtractorFeature>();
        if (extractor != null)
        {
            string resourceName = extractor.resourceType != null ? extractor.resourceType.ResourceName : "resource";
            builder.AppendLine($"Extracts {extractor.extractionAmount} {resourceName} every {extractor.productionCycle:0.#}s");
        }

        ResourceProductionFeature production = data.GetFeature<ResourceProductionFeature>();
        if (production != null)
        {
            string resourceName = production.outputResource != null ? production.outputResource.ResourceName : "resource";
            builder.AppendLine($"Produces {production.outputAmount} {resourceName} every {production.productionCycle:0.#}s");
        }

        ResourceConversionFeature conversion = data.GetFeature<ResourceConversionFeature>();
        if (conversion != null)
        {
            string resourceName = conversion.outputResource != null ? conversion.outputResource.ResourceName : "resource";
            builder.AppendLine($"Converts into {conversion.outputAmount} {resourceName} every {conversion.conversionTime:0.#}s");
            builder.AppendLine($"Queue capacity: {conversion.maxQueueSize}");
        }

        PollutionFeature pollution = data.GetFeature<PollutionFeature>();
        if (pollution != null)
        {
            builder.AppendLine($"Pollution: {pollution.pollutionRate:0.#}/s");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildStorageText(BuildingData data)
    {
        StringBuilder builder = new StringBuilder();

        StorageFeature storage = data.GetFeature<StorageFeature>();
        if (storage != null)
        {
            string resourceName = storage.specificResource != null ? storage.specificResource.ResourceName : "all resources";
            builder.AppendLine($"+{storage.storageCapacity} capacity for {resourceName}");
        }

        WorkerStorageFeature workerStorage = data.GetFeature<WorkerStorageFeature>();
        if (workerStorage != null)
        {
            builder.AppendLine($"+{workerStorage.robotCapacityIncrease} robot capacity");
            builder.AppendLine($"+{workerStorage.workerCapacityIncrease} worker capacity");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildDefenseText(BuildingData data)
    {
        TurretFeature turret = data.GetFeature<TurretFeature>();
        if (turret == null)
        {
            return string.Empty;
        }

        return $"Damage: {turret.damage:0.#}\nRange: {turret.attackRange:0.#}\nAttack speed: {turret.attackSpeed:0.#}s";
    }

    private static string BuildEnergyText(BuildingData data)
    {
        StringBuilder builder = new StringBuilder();

        EnergyGeneratorFeature generator = data.GetFeature<EnergyGeneratorFeature>();
        if (generator != null)
        {
            builder.AppendLine($"+{generator.energyOutput}/s generated");
        }

        EnergyConsumerFeature consumer = data.GetFeature<EnergyConsumerFeature>();
        if (consumer != null)
        {
            builder.AppendLine($"-{consumer.energyConsumption}/s consumed");
        }

        TurretFeature turret = data.GetFeature<TurretFeature>();
        if (turret != null)
        {
            builder.AppendLine($"Turret energy: {turret.automatedEnergyCost} automated, {turret.mannedEnergyCost} manned");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildUpgradeText(BuildingData data)
    {
        UpgradeableFeature upgrade = data.GetFeature<UpgradeableFeature>();
        if (upgrade == null)
        {
            return string.Empty;
        }

        string targetName = upgrade.upgradesTo != null ? upgrade.upgradesTo.buildingName : "next tier";
        return $"Upgrades to {targetName}";
    }

    private void SetSection(string title, string body, bool visible)
    {
        if (!sectionTexts.TryGetValue(title, out TextMeshProUGUI text) || text == null)
        {
            return;
        }

        Transform section = text.transform.parent;
        if (section != null)
        {
            section.gameObject.SetActive(visible && !string.IsNullOrWhiteSpace(body));
        }

        text.text = body;
    }

    private void BuildRuntimeLayout()
    {
        if (contentRoot != null || panel == null)
        {
            return;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.045f, 0.055f, 0.065f, 0.96f);
        }

        contentRoot = CreateUIObject<RectTransform>("RuntimeContent", panel.transform);
        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = new Vector2(16f, 16f);
        contentRoot.offsetMax = new Vector2(-16f, -16f);

        VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform header = CreateUIObject<RectTransform>("Header", contentRoot);
        HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8f;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        AddLayoutElement(header.gameObject, -1f, 44f);

        iconImage = CreateUIObject<Image>("Icon", header);
        iconImage.color = Color.white;
        AddLayoutElement(iconImage.gameObject, 40f, 40f);

        RectTransform titleGroup = CreateUIObject<RectTransform>("TitleGroup", header);
        VerticalLayoutGroup titleLayout = titleGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        titleLayout.spacing = 2f;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = true;
        AddLayoutElement(titleGroup.gameObject, 190f, 42f, true);

        titleText = CreateText("Name", titleGroup, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        categoryText = CreateText("CategoryChip", titleGroup, 12f, FontStyles.Bold, TextAlignmentOptions.Left);
        categoryText.color = new Color(0.7f, 0.88f, 1f);

        Button closeButton = CreateButton("CloseButton", header, "X");
        closeButton.onClick.AddListener(HidePanel);
        AddLayoutElement(closeButton.gameObject, 32f, 32f);

        RectTransform healthGroup = CreateUIObject<RectTransform>("Health", contentRoot);
        VerticalLayoutGroup healthLayout = healthGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        healthLayout.spacing = 4f;
        healthLayout.childControlWidth = true;
        healthLayout.childControlHeight = true;
        AddLayoutElement(healthGroup.gameObject, -1f, 44f);

        healthValueText = CreateText("HealthValue", healthGroup, 14f, FontStyles.Bold, TextAlignmentOptions.Left);
        healthSlider = CreateSlider("HealthBar", healthGroup);

        descriptionText = CreateText("Description", contentRoot, 13f, FontStyles.Normal, TextAlignmentOptions.Left);
        descriptionText.color = new Color(0.82f, 0.86f, 0.9f);

        CreateSection("Workers");
        CreateSection("Resources");
        CreateSection("Storage");
        CreateSection("Defense");
        CreateSection("Energy");
        CreateSection("Upgrades");
        ReparentWorkerSlotsContainer();
        HideLegacyTextReferences();
    }

    private void CreateSection(string title)
    {
        RectTransform section = CreateUIObject<RectTransform>(title + "Section", contentRoot);
        VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image background = section.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.095f, 0.11f, 0.9f);

        TextMeshProUGUI heading = CreateText(title + "Heading", section, 12f, FontStyles.Bold, TextAlignmentOptions.Left);
        heading.text = title.ToUpperInvariant();
        heading.color = new Color(0.7f, 0.88f, 1f);

        TextMeshProUGUI body = CreateText(title + "Body", section, 13f, FontStyles.Normal, TextAlignmentOptions.Left);
        sectionTexts[title] = body;
        sectionRoots[title] = section;
    }

    private void ReparentWorkerSlotsContainer()
    {
        if (workerSlotsContainer == null || !sectionRoots.TryGetValue("Workers", out RectTransform workersSection))
        {
            return;
        }

        workerSlotsContainer.SetParent(workersSection, false);
        workerSlotsContainer.gameObject.SetActive(true);

        if (workerSlotsContainer.GetComponent<LayoutElement>() == null)
        {
            LayoutElement element = workerSlotsContainer.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
        }
    }

    private void HideLegacyTextReferences()
    {
        SetLegacyInactive(buildingNameText);
        SetLegacyInactive(healthText);
        SetLegacyInactive(resourceGenerationText);
        SetLegacyInactive(statusText);
        SetLegacyInactive(totalWorkerCountText);
    }

    private static void SetLegacyInactive(TextMeshProUGUI text)
    {
        if (text != null)
        {
            text.gameObject.SetActive(false);
        }
    }

    private Slider CreateSlider(string name, Transform parent)
    {
        RectTransform root = CreateUIObject<RectTransform>(name, parent);
        AddLayoutElement(root.gameObject, -1f, 10f);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.18f, 0.2f, 1f);

        RectTransform fillArea = CreateUIObject<RectTransform>("Fill Area", root);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = Vector2.zero;
        fillArea.offsetMax = Vector2.zero;

        Image fill = CreateUIObject<Image>("Fill", fillArea);
        fill.color = new Color(0.42f, 0.85f, 0.45f, 1f);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.targetGraphic = background;
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        return slider;
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        Image image = CreateUIObject<Image>(name, parent);
        image.color = new Color(0.12f, 0.145f, 0.17f, 1f);

        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.2f, 0.24f, 0.28f, 1f);
        colors.pressedColor = new Color(0.08f, 0.1f, 0.12f, 1f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", image.transform, 16f, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = label;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateUIObject<TextMeshProUGUI>(name, parent);
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.96f, 0.98f, 1f);
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

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
