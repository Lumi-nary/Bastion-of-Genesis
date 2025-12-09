using UnityEngine;

/// <summary>
/// Improves worker capabilities
/// Example: "Advanced Training" increases Builder efficiency by 20%
/// </summary>
[CreateAssetMenu(fileName = "Effect_WorkerUpgrade", menuName = "Planetfall/Technology Effects/Worker Upgrade")]
public class WorkerUpgradeEffect : TechnologyEffect
{
    [Header("Worker Upgrade Configuration")]
    [Tooltip("Worker type affected (null = all workers)")]
    public WorkerData affectedWorker;

    [Tooltip("Percentage increase in worker efficiency (e.g., 20 for +20%)")]
    public float efficiencyBonus = 20f;

    [Tooltip("Production speed multiplier (e.g., 1.2 for 20% faster)")]
    public float speedMultiplier = 1.2f;

    public override void OnResearched(TechnologyData tech)
    {
        if (affectedWorker != null)
        {
            Debug.Log($"[WorkerUpgradeEffect] {affectedWorker.name} efficiency increased by {efficiencyBonus}%");
        }
        else
        {
            Debug.Log($"[WorkerUpgradeEffect] All worker efficiency increased by {efficiencyBonus}%");
        }
    }

    public override float GetModifier(string modifierType)
    {
        // Format: "WorkerEfficiency_WorkerName" or "WorkerSpeed_WorkerName"
        if (affectedWorker != null)
        {
            if (modifierType == $"WorkerEfficiency_{affectedWorker.name}")
            {
                return efficiencyBonus / 100f;
            }
            if (modifierType == $"WorkerSpeed_{affectedWorker.name}")
            {
                return speedMultiplier - 1f; // Return bonus amount
            }
        }
        else
        {
            if (modifierType.StartsWith("WorkerEfficiency_"))
            {
                return efficiencyBonus / 100f;
            }
            if (modifierType.StartsWith("WorkerSpeed_"))
            {
                return speedMultiplier - 1f;
            }
        }

        return 0f;
    }

    public override bool ProvidesModifier(string modifierType)
    {
        if (affectedWorker != null)
        {
            return modifierType == $"WorkerEfficiency_{affectedWorker.name}" ||
                   modifierType == $"WorkerSpeed_{affectedWorker.name}";
        }
        else
        {
            return modifierType.StartsWith("WorkerEfficiency_") ||
                   modifierType.StartsWith("WorkerSpeed_");
        }
    }

    public override string GetEffectDescription()
    {
        if (affectedWorker != null)
        {
            return $"+{efficiencyBonus}% {affectedWorker.name} efficiency";
        }
        else
        {
            return $"+{efficiencyBonus}% all worker efficiency";
        }
    }
}
