using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// <summary>
/// Top bar resource display. Spawns a slot for each registered resource (including energy).
/// Energy slot has color-coded fill and blink behavior on power outage.
/// Replaces the old ResourcePanel + EnergyMeter.
/// </summary>
public class TopBarResourceGroup : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject resourceSlotPrefab;

    [Header("Energy Settings")]
    [SerializeField] private ResourceType energyResource;
    [SerializeField] private Color energySlotBackground = new Color(0.85f, 0.75f, 0.1f, 0.9f);
    [SerializeField] private Color energyNormalColor = Color.white;
    [SerializeField] private Color energyWarningColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color energyCriticalColor = Color.red;
    [SerializeField] private float warningThreshold = 0.3f;
    [SerializeField] private float criticalThreshold = 0.1f;
    [SerializeField] private float blinkSpeed = 2f;

    private Dictionary<ResourceType, ResourceSlotUI> slots = new Dictionary<ResourceType, ResourceSlotUI>();

    // Energy blink state
    private ResourceSlotUI energySlot;
    private bool isBlinking;
    private float blinkTimer;
    private bool blinkState;
    private int currentEnergy;
    private int maxEnergy = 500;

    private bool initialized;

    private void Start()
    {
        if (energyResource == null)
            energyResource = Resources.Load<ResourceType>("GameData/Resources/Energy");

        TryInitialize();
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize();
            return;
        }

        // Energy blink
        if (isBlinking && energySlot != null)
        {
            blinkTimer += Time.deltaTime * blinkSpeed;
            bool newState = Mathf.Sin(blinkTimer * Mathf.PI) > 0;
            if (newState != blinkState)
            {
                blinkState = newState;
                UpdateEnergyBlink();
            }
        }
    }

    private void TryInitialize()
    {
        if (initialized) return;
        if (ResourceManager.Instance == null) return;

        ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
        InitializeSlots();

        if (EnergyManager.Instance != null)
            EnergyManager.Instance.OnPowerOutage += OnPowerOutage;

        initialized = true;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;

        if (EnergyManager.Instance != null)
            EnergyManager.Instance.OnPowerOutage -= OnPowerOutage;
    }

    private void InitializeSlots()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        slots.Clear();
        energySlot = null;

        foreach (var kvp in ResourceManager.Instance.ResourceAmounts)
            CreateOrUpdateSlot(kvp.Key, kvp.Value);
    }

    private void OnResourceChanged(ResourceType type, int amount)
    {
        if (type == null) return;
        CreateOrUpdateSlot(type, amount);

        // Track energy for blink
        if (energyResource != null && type.ResourceName == energyResource.ResourceName)
        {
            currentEnergy = amount;
            maxEnergy = Mathf.Max(1, ResourceManager.Instance.GetResourceCapacity(type));
            UpdateEnergyColor();
        }
    }

    private void CreateOrUpdateSlot(ResourceType type, int amount)
    {
        int capacity = ResourceManager.Instance.GetResourceCapacity(type);

        if (slots.TryGetValue(type, out ResourceSlotUI existing))
        {
            existing.UpdateAmount(amount, capacity);
        }
        else
        {
            GameObject go = Instantiate(resourceSlotPrefab, transform);
            ResourceSlotUI slot = go.GetComponent<ResourceSlotUI>();
            if (slot != null)
            {
                slot.Setup(type, amount, capacity);
                slots[type] = slot;

                if (energyResource != null && type.ResourceName == energyResource.ResourceName)
                {
                    energySlot = slot;
                    currentEnergy = amount;
                    maxEnergy = Mathf.Max(1, capacity);

                    // Yellow background for energy slot
                    var bg = go.GetComponent<Image>();
                    if (bg != null) bg.color = energySlotBackground;
                }
            }
        }
    }

    private void OnPowerOutage(bool hasPower)
    {
        isBlinking = !hasPower;
        if (hasPower)
        {
            blinkTimer = 0f;
            blinkState = false;
        }
        UpdateEnergyColor();
    }

    private void UpdateEnergyColor()
    {
        if (energySlot == null) return;

        var text = energySlot.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) return;

        if (isBlinking && currentEnergy <= 0)
        {
            UpdateEnergyBlink();
            return;
        }

        float fill = (float)currentEnergy / maxEnergy;
        if (fill <= criticalThreshold)
            text.color = energyCriticalColor;
        else if (fill <= warningThreshold)
            text.color = energyWarningColor;
        else
            text.color = energyNormalColor;
    }

    private void UpdateEnergyBlink()
    {
        if (energySlot == null) return;

        var text = energySlot.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) return;

        if (currentEnergy <= 0 && isBlinking)
        {
            if (blinkState)
            {
                text.text = "No Energy";
                text.color = energyCriticalColor;
            }
            else
            {
                text.text = $"0/{maxEnergy}";
                text.color = new Color(energyCriticalColor.r, energyCriticalColor.g, energyCriticalColor.b, 0.3f);
            }
        }
    }
}
