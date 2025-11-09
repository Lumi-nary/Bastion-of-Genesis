using UnityEngine;
using System;

public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Pollution Settings")]
    [SerializeField] private float maxPollution = 1000f;
    [SerializeField] private float pollutionDecayRate = 0.5f; // Pollution naturally decreases over time
    [SerializeField] private bool enableNaturalDecay = true;

    [Header("Hostility Thresholds")]
    [Tooltip("Pollution level at which each race becomes hostile")]
    [SerializeField] private float humanHostilityThreshold = 100f;
    [SerializeField] private float elfHostilityThreshold = 250f;
    [SerializeField] private float dwarfHostilityThreshold = 500f;
    [SerializeField] private float demonHostilityThreshold = 750f;

    private float currentPollution = 0f;

    // Events
    public event Action<float, float> OnPollutionChanged; // current, max
    public event Action<RaceType> OnRaceHostilityChanged;

    // Hostility tracking
    private bool humansHostile = false;
    private bool elvesHostile = false;
    private bool dwarvesHostile = false;
    private bool demonsHostile = false;

    public float CurrentPollution => currentPollution;
    public float MaxPollution => maxPollution;
    public float PollutionPercentage => (currentPollution / maxPollution) * 100f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        // Natural pollution decay over time
        if (enableNaturalDecay && currentPollution > 0)
        {
            RemovePollution(pollutionDecayRate * Time.deltaTime);
        }
    }

    public void AddPollution(float amount)
    {
        if (amount <= 0) return;

        currentPollution = Mathf.Clamp(currentPollution + amount, 0, maxPollution);
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);

        CheckHostilityLevels();
    }

    public void RemovePollution(float amount)
    {
        if (amount <= 0) return;

        currentPollution = Mathf.Max(0, currentPollution - amount);
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);

        CheckHostilityLevels();
    }

    public void SetPollution(float amount)
    {
        currentPollution = Mathf.Clamp(amount, 0, maxPollution);
        OnPollutionChanged?.Invoke(currentPollution, maxPollution);

        CheckHostilityLevels();
    }

    private void CheckHostilityLevels()
    {
        // Check Humans
        if (!humansHostile && currentPollution >= humanHostilityThreshold)
        {
            humansHostile = true;
            OnRaceHostilityChanged?.Invoke(RaceType.Human);
            Debug.Log("Humans have become hostile due to pollution!");
        }

        // Check Elves
        if (!elvesHostile && currentPollution >= elfHostilityThreshold)
        {
            elvesHostile = true;
            OnRaceHostilityChanged?.Invoke(RaceType.Elf);
            Debug.Log("Elves have become hostile due to pollution!");
        }

        // Check Dwarves
        if (!dwarvesHostile && currentPollution >= dwarfHostilityThreshold)
        {
            dwarvesHostile = true;
            OnRaceHostilityChanged?.Invoke(RaceType.Dwarf);
            Debug.Log("Dwarves have become hostile due to pollution!");
        }

        // Check Demons
        if (!demonsHostile && currentPollution >= demonHostilityThreshold)
        {
            demonsHostile = true;
            OnRaceHostilityChanged?.Invoke(RaceType.Demon);
            Debug.Log("Demons have become hostile due to pollution!");
        }
    }

    public bool IsRaceHostile(RaceType race)
    {
        return race switch
        {
            RaceType.Human => humansHostile,
            RaceType.Elf => elvesHostile,
            RaceType.Dwarf => dwarvesHostile,
            RaceType.Demon => demonsHostile,
            _ => false
        };
    }

    public float GetHostilityThreshold(RaceType race)
    {
        return race switch
        {
            RaceType.Human => humanHostilityThreshold,
            RaceType.Elf => elfHostilityThreshold,
            RaceType.Dwarf => dwarfHostilityThreshold,
            RaceType.Demon => demonHostilityThreshold,
            _ => 0f
        };
    }

    /// <summary>
    /// Resets pollution level to 0, but KEEPS hostility state (it persists across chapters)
    /// </summary>
    public void ResetPollution()
    {
        currentPollution = 0f;
        // NOTE: Hostility flags are NOT reset - they persist across chapters
        // humansHostile, elvesHostile, dwarvesHostile, demonsHostile remain unchanged

        OnPollutionChanged?.Invoke(currentPollution, maxPollution);
    }

    /// <summary>
    /// Restore hostility state from saved data (used by ProgressionManager)
    /// </summary>
    public void RestoreHostilityState(bool humans, bool elves, bool dwarves, bool demons)
    {
        humansHostile = humans;
        elvesHostile = elves;
        dwarvesHostile = dwarves;
        demonsHostile = demons;

        // Trigger events for any hostile races
        if (humansHostile) OnRaceHostilityChanged?.Invoke(RaceType.Human);
        if (elvesHostile) OnRaceHostilityChanged?.Invoke(RaceType.Elf);
        if (dwarvesHostile) OnRaceHostilityChanged?.Invoke(RaceType.Dwarf);
        if (demonsHostile) OnRaceHostilityChanged?.Invoke(RaceType.Demon);

        Debug.Log($"Hostility state restored - Humans: {humans}, Elves: {elves}, Dwarves: {dwarves}, Demons: {demons}");
    }
}

public enum RaceType
{
    Human,
    Elf,
    Dwarf,
    Demon
}
