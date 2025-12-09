using UnityEngine;

/// <summary>
/// Air Mage ability - Chance to dodge incoming attacks
/// Enemy has a chance to completely avoid taking damage
/// </summary>
[CreateAssetMenu(fileName = "Ability_Dodge", menuName = "Planetfall/Abilities/Dodge")]
public class DodgeAbility : SpecialAbility
{
    [Header("Dodge Configuration")]
    [Tooltip("Chance to dodge attacks (0.2 = 20% chance)")]
    [Range(0f, 1f)]
    public float dodgeChance = 0.2f;

    public override bool OnTakeDamage(Enemy owner, ref float damage)
    {
        if (Random.value < dodgeChance)
        {
            Debug.Log($"[DodgeAbility] {owner.Data.enemyName} dodged the attack!");
            return true; // Block damage
        }

        return false; // Don't block damage
    }
}
