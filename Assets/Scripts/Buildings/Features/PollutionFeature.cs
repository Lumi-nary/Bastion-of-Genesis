using UnityEngine;

/// <summary>
/// Pollution feature - Generates pollution over time
/// Pollution affects enemy difficulty, ore mound discovery, and mission objectives
/// </summary>
[CreateAssetMenu(fileName = "Feature_Pollution", menuName = "Planetfall/Building Features/Pollution")]
public class PollutionFeature : BuildingFeature
{
    [Header("Pollution Configuration")]
    [Tooltip("Pollution generated per second")]
    public float pollutionRate = 1f;

    [Tooltip("Only generate pollution when operational")]
    public bool requiresOperation = true;

    [Tooltip("Pollution multiplier when at full capacity")]
    public float fullCapacityMultiplier = 1.5f;

    private float pollutionTimer = 0f;
    private float pollutionInterval = 1f; // Generate pollution every second

    public override void OnUpdate(Building building)
    {
        // Check if should generate pollution
        if (requiresOperation && !building.IsOperational) return;

        // Check if we have energy
        if (!CanFunctionWithoutEnergy() && EnergyManager.Instance != null && !EnergyManager.Instance.HasEnergy)
        {
            return;
        }

        // Generate pollution over time
        pollutionTimer += Time.deltaTime;
        if (pollutionTimer >= pollutionInterval)
        {
            pollutionTimer -= pollutionInterval;

            if (PollutionManager.Instance != null)
            {
                float pollutionAmount = pollutionRate;

                // Increase pollution if at full worker capacity
                if (building.GetTotalAssignedWorkerCount() >= building.GetTotalWorkerCapacity())
                {
                    pollutionAmount *= fullCapacityMultiplier;
                }

                PollutionManager.Instance.AddPollution(pollutionAmount);
            }
        }
    }

    public override void OnBuilt(Building building)
    {
        pollutionTimer = 0f;
    }
}
