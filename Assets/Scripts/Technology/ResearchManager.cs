using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages technology research system
/// Handles tech unlocking, research progression, and tech effects
/// Singleton pattern for global access
/// </summary>
public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    [Header("Technology Configuration")]
    [Tooltip("Database containing all technologies")]
    [SerializeField] private TechnologyDatabase technologyDatabase;

    private List<TechnologyData> allTechnologies = new List<TechnologyData>();

    [Tooltip("Technologies that have been researched")]
    private HashSet<TechnologyData> researchedTechs = new HashSet<TechnologyData>();

    [Tooltip("Technologies currently available to research")]
    private HashSet<TechnologyData> availableTechs = new HashSet<TechnologyData>();

    [Header("Research Progress")]
    [Tooltip("Currently researching technology (null if none)")]
    private TechnologyData currentResearch = null;

    [Tooltip("Progress on current research (0-1)")]
    private float currentResearchProgress = 0f;

    [Tooltip("Time elapsed on current research")]
    private float researchTimeElapsed = 0f;

    [Header("Research Lab Requirements")]
    [Tooltip("Can only research one tech at a time (Research Lab limit)")]
    private bool isResearching = false;

    // Events for UI updates
    public delegate void TechResearchedEvent(TechnologyData tech);
    public event TechResearchedEvent OnTechResearched;

    public delegate void TechAvailableEvent(TechnologyData tech);
    public event TechAvailableEvent OnTechAvailable;

    public delegate void ResearchProgressEvent(TechnologyData tech, float progress);
    public event ResearchProgressEvent OnResearchProgress;

    // Public properties
    public TechnologyData CurrentResearch => currentResearch;
    public float CurrentResearchProgress => currentResearchProgress;
    public bool IsResearching => isResearching;
    public HashSet<TechnologyData> ResearchedTechs => researchedTechs;
    public HashSet<TechnologyData> AvailableTechs => availableTechs;

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

        LoadAllTechnologies();
    }

    private void Update()
    {
        if (isResearching && currentResearch != null)
        {
            UpdateResearch(Time.deltaTime);
        }
    }

    /// <summary>
    /// Load all technologies from database
    /// </summary>
    private void LoadAllTechnologies()
    {
        if (technologyDatabase != null)
        {
            allTechnologies = new List<TechnologyData>(technologyDatabase.technologies);
        }
        else
        {
            Debug.LogWarning("[ResearchManager] No TechnologyDatabase assigned!");
            allTechnologies = new List<TechnologyData>();
        }

        Debug.Log($"[ResearchManager] Loaded {allTechnologies.Count} technologies");

        // Initialize available techs (those with no prerequisites and Researchable unlock method)
        UpdateAvailableTechnologies();
    }

    /// <summary>
    /// Start researching a technology
    /// </summary>
    public bool StartResearch(TechnologyData tech)
    {
        // Validation checks
        if (tech == null)
        {
            Debug.LogWarning("[ResearchManager] Cannot start research: tech is null");
            return false;
        }

        if (isResearching)
        {
            Debug.LogWarning($"[ResearchManager] Already researching {currentResearch.techName}. Can only research one tech at a time.");
            return false;
        }

        if (researchedTechs.Contains(tech))
        {
            Debug.LogWarning($"[ResearchManager] {tech.techName} is already researched");
            return false;
        }

        if (!availableTechs.Contains(tech))
        {
            Debug.LogWarning($"[ResearchManager] {tech.techName} is not available for research (prerequisites not met or locked)");
            return false;
        }

        // Check resource costs
        if (!CanAffordResearch(tech))
        {
            Debug.LogWarning($"[ResearchManager] Not enough resources to research {tech.techName}");
            return false;
        }

        // Consume resources
        if (!ConsumeResearchCost(tech))
        {
            Debug.LogWarning($"[ResearchManager] Failed to consume resources for {tech.techName}");
            return false;
        }

        // Start research
        currentResearch = tech;
        currentResearchProgress = 0f;
        researchTimeElapsed = 0f;
        isResearching = true;

        Debug.Log($"[ResearchManager] Started researching {tech.techName} ({tech.GetTimeString()})");
        return true;
    }

    /// <summary>
    /// Cancel current research (does NOT refund resources)
    /// </summary>
    public void CancelResearch()
    {
        if (!isResearching)
        {
            Debug.LogWarning("[ResearchManager] No research to cancel");
            return;
        }

        Debug.Log($"[ResearchManager] Canceled research: {currentResearch.techName}");

        currentResearch = null;
        currentResearchProgress = 0f;
        researchTimeElapsed = 0f;
        isResearching = false;
    }

    /// <summary>
    /// Update research progress
    /// </summary>
    private void UpdateResearch(float deltaTime)
    {
        if (currentResearch == null) return;

        researchTimeElapsed += deltaTime;
        currentResearchProgress = Mathf.Clamp01(researchTimeElapsed / currentResearch.researchTime);

        // Notify UI of progress update
        OnResearchProgress?.Invoke(currentResearch, currentResearchProgress);

        // Check if research is completea
        if (researchTimeElapsed >= currentResearch.researchTime)
        {
            CompleteResearch();
        }
    }

    /// <summary>
    /// Complete current research and apply effects
    /// </summary>
    private void CompleteResearch()
    {
        if (currentResearch == null) return;

        TechnologyData completedTech = currentResearch;

        // Mark as researched
        researchedTechs.Add(completedTech);
        completedTech.IsResearched = true;

        // Apply technology effects
        ApplyTechnologyEffects(completedTech);

        // Update available technologies (new techs may be unlocked)
        UpdateAvailableTechnologies();

        // Reset research state
        currentResearch = null;
        currentResearchProgress = 0f;
        researchTimeElapsed = 0f;
        isResearching = false;

        // Notify listeners
        OnTechResearched?.Invoke(completedTech);

        Debug.Log($"[ResearchManager] Completed research: {completedTech.techName}");
    }

    /// <summary>
    /// Apply effects of researched technology
    /// </summary>
    private void ApplyTechnologyEffects(TechnologyData tech)
    {
        foreach (var effect in tech.effects)
        {
            if (effect != null)
            {
                effect.OnResearched(tech);
            }
        }
    }

    /// <summary>
    /// Update which technologies are available for research
    /// </summary>
    private void UpdateAvailableTechnologies()
    {
        availableTechs.Clear();

        foreach (TechnologyData tech in allTechnologies)
        {
            // Skip already researched techs
            if (researchedTechs.Contains(tech)) continue;

            // Check if prerequisites are met
            if (tech.ArePrerequisitesMet(researchedTechs))
            {
                // Check unlock method
                if (tech.unlockMethod == TechUnlockMethod.Researchable)
                {
                    availableTechs.Add(tech);
                    tech.IsAvailable = true;

                    // Notify if newly available
                    OnTechAvailable?.Invoke(tech);
                }
                // MissionReward techs are made available by missions
            }
            else
            {
                tech.IsAvailable = false;
            }
        }

        Debug.Log($"[ResearchManager] {availableTechs.Count} technologies available for research");
    }

    /// <summary>
    /// Unlock a technology as a mission reward (still needs to be researched)
    /// </summary>
    public void UnlockTechnologyForResearch(TechnologyData tech)
    {
        if (tech == null) return;

        if (researchedTechs.Contains(tech))
        {
            Debug.LogWarning($"[ResearchManager] {tech.techName} is already researched");
            return;
        }

        if (!tech.ArePrerequisitesMet(researchedTechs))
        {
            Debug.LogWarning($"[ResearchManager] Cannot unlock {tech.techName}: prerequisites not met");
            return;
        }

        availableTechs.Add(tech);
        tech.IsAvailable = true;

        OnTechAvailable?.Invoke(tech);

        Debug.Log($"[ResearchManager] Unlocked {tech.techName} for research (mission reward)");
    }

    /// <summary>
    /// Check if player has enough resources to research a technology
    /// </summary>
    private bool CanAffordResearch(TechnologyData tech)
    {
        if (ResourceManager.Instance == null) return false;

        foreach (ResourceCost cost in tech.researchCost)
        {
            if (cost.resourceType == null) continue;

            int currentAmount = ResourceManager.Instance.GetResourceAmount(cost.resourceType);
            if (currentAmount < cost.amount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Consume resources for research
    /// </summary>
    private bool ConsumeResearchCost(TechnologyData tech)
    {
        if (ResourceManager.Instance == null) return false;

        foreach (ResourceCost cost in tech.researchCost)
        {
            if (cost.resourceType == null) continue;

            if (!ResourceManager.Instance.RemoveResource(cost.resourceType, cost.amount))
            {
                Debug.LogError($"[ResearchManager] Failed to consume {cost.amount} {cost.resourceType.ResourceName}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a specific technology has been researched
    /// </summary>
    public bool IsTechResearched(TechnologyData tech)
    {
        return researchedTechs.Contains(tech);
    }

    /// <summary>
    /// Check if a specific technology is available for research
    /// </summary>
    public bool IsTechAvailable(TechnologyData tech)
    {
        return availableTechs.Contains(tech);
    }

    /// <summary>
    /// Get all researched technologies
    /// </summary>
    public List<TechnologyData> GetResearchedTechnologies()
    {
        return researchedTechs.ToList();
    }

    /// <summary>
    /// Get all available technologies
    /// </summary>
    public List<TechnologyData> GetAvailableTechnologies()
    {
        return availableTechs.ToList();
    }

    /// <summary>
    /// Get technologies by tier
    /// </summary>
    public List<TechnologyData> GetTechnologiesByTier(int tier)
    {
        return allTechnologies.Where(t => t.tier == tier).ToList();
    }

    /// <summary>
    /// Get technologies by category
    /// </summary>
    public List<TechnologyData> GetTechnologiesByCategory(TechCategory category)
    {
        return allTechnologies.Where(t => t.category == category).ToList();
    }

    /// <summary>
    /// Reset all research progress (for new game)
    /// </summary>
    public void ResetAllResearch()
    {
        researchedTechs.Clear();
        availableTechs.Clear();

        currentResearch = null;
        currentResearchProgress = 0f;
        researchTimeElapsed = 0f;
        isResearching = false;

        // Reset all tech status
        foreach (TechnologyData tech in allTechnologies)
        {
            tech.ResetResearchStatus();
        }

        UpdateAvailableTechnologies();

        Debug.Log("[ResearchManager] Reset all research progress");
    }

    /// <summary>
    /// Get resource production multiplier from researched techs
    /// </summary>
    public float GetResourceProductionMultiplier(ResourceType resourceType)
    {
        float multiplier = 1.0f;

        foreach (TechnologyData tech in researchedTechs)
        {
            foreach (var effect in tech.effects)
            {
                if (effect != null)
                {
                    string modifierKey = $"ResourceProduction_{resourceType.ResourceName}";
                    if (effect.ProvidesModifier(modifierKey))
                    {
                        multiplier += effect.GetModifier(modifierKey);
                    }
                }
            }
        }

        return multiplier;
    }

    /// <summary>
    /// Get generic modifier value from researched techs
    /// Format: "ResourceProduction_Iron", "WorkerEfficiency_Builder", etc.
    /// </summary>
    public float GetModifier(string modifierType)
    {
        float total = 0f;

        foreach (TechnologyData tech in researchedTechs)
        {
            foreach (var effect in tech.effects)
            {
                if (effect != null && effect.ProvidesModifier(modifierType))
                {
                    total += effect.GetModifier(modifierType);
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Get total multiplier (base 1.0 + all bonuses) for a modifier type
    /// </summary>
    public float GetTotalMultiplier(string modifierType)
    {
        return 1.0f + GetModifier(modifierType);
    }
}
