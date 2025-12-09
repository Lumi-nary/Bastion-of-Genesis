using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages energy production, consumption, and distribution
/// Energy = 0 stops most buildings (except manned turrets and functionsWithoutEnergy buildings)
/// Singleton pattern for global access
/// </summary>
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance { get; private set; }

    [Header("Energy Configuration")]
    [Tooltip("Update interval for energy calculations (seconds)")]
    [SerializeField] private float updateInterval = 1f;

    private float updateTimer = 0f;

    [Header("Energy Stats")]
    private int totalProduction = 0;
    private int totalConsumption = 0;
    private int netEnergy = 0;
    private bool hasEnergy = true;

    [Header("Energy Resource")]
    [Tooltip("Energy resource type (assign in inspector)")]
    [SerializeField] private ResourceType energyResource;

    // Events
    public delegate void EnergyChangedEvent(int production, int consumption, int net);
    public event EnergyChangedEvent OnEnergyChanged;

    public delegate void PowerOutageEvent(bool hasPower);
    public event PowerOutageEvent OnPowerOutage;

    // Public properties
    public int TotalProduction => totalProduction;
    public int TotalConsumption => totalConsumption;
    public int NetEnergy => netEnergy;
    public bool HasEnergy => hasEnergy;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load energy resource if not assigned
        if (energyResource == null)
        {
            energyResource = Resources.Load<ResourceType>("Resources/Energy");
            if (energyResource == null)
            {
                Debug.LogError("[EnergyManager] Energy resource not found! Assign in inspector or create at Resources/Resources/Energy");
            }
        }
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer -= updateInterval;
            UpdateEnergySystem();
        }
    }

    /// <summary>
    /// Update energy production, consumption, and resource amount
    /// </summary>
    private void UpdateEnergySystem()
    {
        if (BuildingManager.Instance == null || ResourceManager.Instance == null) return;

        // Calculate total energy production
        totalProduction = CalculateTotalProduction();

        // Calculate total energy consumption
        totalConsumption = CalculateTotalConsumption();

        // Net energy (can be negative)
        netEnergy = totalProduction - totalConsumption;

        // Check if we have energy
        bool previousState = hasEnergy;
        hasEnergy = (netEnergy >= 0);

        // Notify if power state changed
        if (previousState != hasEnergy)
        {
            OnPowerOutage?.Invoke(hasEnergy);

            if (!hasEnergy)
            {
                Debug.LogWarning("[EnergyManager] POWER OUTAGE! Energy consumption exceeds production!");
            }
            else
            {
                Debug.Log("[EnergyManager] Power restored!");
            }
        }

        // Update energy resource based on net production
        if (energyResource != null)
        {
            if (netEnergy > 0)
            {
                // Add energy to storage
                ResourceManager.Instance.AddResource(energyResource, netEnergy);
            }
            else if (netEnergy < 0)
            {
                // Drain energy from storage
                int deficit = Mathf.Abs(netEnergy);
                ResourceManager.Instance.RemoveResource(energyResource, deficit);
            }
        }

        // Notify listeners
        OnEnergyChanged?.Invoke(totalProduction, totalConsumption, netEnergy);
    }

    /// <summary>
    /// Calculate total energy production from all generators
    /// Only operational generators with assigned Extractor workers produce energy
    /// </summary>
    private int CalculateTotalProduction()
    {
        int production = 0;

        if (BuildingManager.Instance == null) return production;

        // Get all energy buildings (generators)
        List<Building> generators = BuildingManager.Instance.GetBuildingsByCategory(BuildingCategory.Energy);

        foreach (Building generator in generators)
        {
            // Check if generator has EnergyGeneratorFeature
            EnergyGeneratorFeature energyFeature = generator.BuildingData.GetFeature<EnergyGeneratorFeature>();
            if (energyFeature != null)
            {
                // Get energy production from feature
                int generatorOutput = energyFeature.GetEnergyProduction(generator);
                if (generatorOutput > 0)
                {
                    production += generatorOutput;
                }
            }
        }

        return production;
    }

    /// <summary>
    /// Calculate total energy consumption from all buildings
    /// Buildings that functionsWithoutEnergy don't consume when Energy=0
    /// </summary>
    private int CalculateTotalConsumption()
    {
        int consumption = 0;

        if (BuildingManager.Instance == null) return consumption;

        // Get all buildings
        foreach (Building building in BuildingManager.Instance.AllBuildings)
        {
            int buildingConsumption = 0;

            // Check TurretFeature first (turrets have special energy logic)
            TurretFeature turretFeature = building.BuildingData.GetFeature<TurretFeature>();
            if (turretFeature != null)
            {
                // Skip if building can function without energy and we're in a brownout
                if (!hasEnergy && turretFeature.CanFunctionWithoutEnergy())
                {
                    continue;
                }

                buildingConsumption = turretFeature.GetEnergyConsumption(building);
            }
            else
            {
                // Check EnergyConsumerFeature
                EnergyConsumerFeature consumerFeature = building.BuildingData.GetFeature<EnergyConsumerFeature>();
                if (consumerFeature != null)
                {
                    // Skip if building can function without energy and we're in a brownout
                    if (!hasEnergy && consumerFeature.CanFunctionWithoutEnergy())
                    {
                        continue;
                    }

                    buildingConsumption = consumerFeature.GetEnergyConsumption(building);
                }
            }

            consumption += buildingConsumption;
        }

        return consumption;
    }

    /// <summary>
    /// Check if a specific building can function (has energy or functionsWithoutEnergy)
    /// </summary>
    public bool CanBuildingFunction(Building building)
    {
        if (building == null) return false;

        // Check TurretFeature first
        TurretFeature turretFeature = building.BuildingData.GetFeature<TurretFeature>();
        if (turretFeature != null && turretFeature.CanFunctionWithoutEnergy())
        {
            return true;
        }

        // Check EnergyConsumerFeature
        EnergyConsumerFeature consumerFeature = building.BuildingData.GetFeature<EnergyConsumerFeature>();
        if (consumerFeature != null && consumerFeature.CanFunctionWithoutEnergy())
        {
            return true;
        }

        // Otherwise, check if we have energy
        return hasEnergy || netEnergy >= 0;
    }

    /// <summary>
    /// Get energy production from a specific building type
    /// </summary>
    public int GetEnergyProduction(BuildingData buildingData)
    {
        EnergyGeneratorFeature feature = buildingData.GetFeature<EnergyGeneratorFeature>();
        return feature != null ? feature.energyOutput : 0;
    }

    /// <summary>
    /// Get energy consumption from a specific building type
    /// </summary>
    public int GetEnergyConsumption(BuildingData buildingData)
    {
        EnergyConsumerFeature feature = buildingData.GetFeature<EnergyConsumerFeature>();
        return feature != null ? feature.energyConsumption : 0;
    }

    /// <summary>
    /// Get energy stats as formatted string
    /// </summary>
    public string GetEnergyStatsString()
    {
        string status = hasEnergy ? "ONLINE" : "OUTAGE";
        return $"Energy: {netEnergy}/s ({totalProduction} prod - {totalConsumption} cons) [{status}]";
    }

    /// <summary>
    /// Force an immediate energy update (useful after building placement/destruction)
    /// </summary>
    public void ForceUpdate()
    {
        UpdateEnergySystem();
    }

    /// <summary>
    /// Drain energy from storage (used by Elven Healer enemies)
    /// </summary>
    public void DrainEnergy(int amount)
    {
        if (ResourceManager.Instance != null && energyResource != null && amount > 0)
        {
            ResourceManager.Instance.RemoveResource(energyResource, amount);
        }
    }
}
