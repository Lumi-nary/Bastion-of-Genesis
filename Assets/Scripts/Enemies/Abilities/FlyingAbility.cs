using UnityEngine;

/// <summary>
/// Flying Demon ability - Bypasses all walls completely
/// Flying enemies ignore wall blocking and path directly to targets
/// </summary>
[CreateAssetMenu(fileName = "Ability_Flying", menuName = "Planetfall/Abilities/Flying")]
public class FlyingAbility : SpecialAbility
{
    [Header("Flying Configuration")]
    [Tooltip("Height offset for visual effect (optional)")]
    public float heightOffset = 0.5f;

    public override void OnInitialize(Enemy owner)
    {
        // Set visual height if desired
        if (heightOffset > 0)
        {
            Vector3 pos = owner.transform.position;
            pos.y += heightOffset;
            owner.transform.position = pos;
        }
    }

    public override Vector3 GetMovementDirection(Enemy owner, Vector3 defaultDirection)
    {
        // Flying enemies path directly to target (ignore walls)
        // Movement direction calculation is already direct in Enemy.cs
        // This ability just marks the enemy as flying (via MovementType in EnemyData)
        return defaultDirection;
    }
}
