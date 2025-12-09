using UnityEngine;

/// <summary>
/// ScriptableObject holding a 13-sprite transition tileset.
/// Create via Assets > Create > Planetfall > Transition Tileset
/// </summary>
[CreateAssetMenu(fileName = "NewTransitionTileset", menuName = "Planetfall/Transition Tileset")]
public class TransitionTileset : ScriptableObject
{
    [Header("Full Tile Sprites (no transition)")]
    [Tooltip("Base sprite - the 'from' state full tile")]
    public Sprite baseFromSprite;

    [Tooltip("Target sprite - the 'to' state full tile")]
    public Sprite baseToSprite;

    [Header("Outer Corners (convex)")]
    public Sprite outerCornerTL;
    public Sprite outerCornerTR;
    public Sprite outerCornerBL;
    public Sprite outerCornerBR;

    [Header("Edges")]
    public Sprite edgeTop;
    public Sprite edgeBottom;
    public Sprite edgeLeft;
    public Sprite edgeRight;

    [Header("Center")]
    public Sprite center;

    [Header("Inner Corners (concave)")]
    public Sprite innerCornerTL;
    public Sprite innerCornerTR;
    public Sprite innerCornerBL;
    public Sprite innerCornerBR;

    /// <summary>
    /// Get sprite by index (0-12 layout)
    /// </summary>
    public Sprite GetSprite(int index)
    {
        return index switch
        {
            0 => outerCornerTL,
            1 => edgeTop,
            2 => outerCornerTR,
            3 => edgeLeft,
            4 => center,
            5 => edgeRight,
            6 => outerCornerBL,
            7 => edgeBottom,
            8 => outerCornerBR,
            9 => innerCornerTL,
            10 => innerCornerTR,
            11 => innerCornerBL,
            12 => innerCornerBR,
            _ => null
        };
    }

    /// <summary>
    /// Convert to array for compatibility
    /// </summary>
    public Sprite[] ToArray()
    {
        return new Sprite[]
        {
            outerCornerTL,    // 0
            edgeTop,          // 1
            outerCornerTR,    // 2
            edgeLeft,         // 3
            center,           // 4
            edgeRight,        // 5
            outerCornerBL,    // 6
            edgeBottom,       // 7
            outerCornerBR,    // 8
            innerCornerTL,    // 9
            innerCornerTR,    // 10
            innerCornerBL,    // 11
            innerCornerBR     // 12
        };
    }
}
