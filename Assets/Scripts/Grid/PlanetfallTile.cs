using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Extended Unity Tile that includes Planetfall TileData
/// Used in Unity Tilemap system for hand-designed levels
/// </summary>
[CreateAssetMenu(fileName = "PFTile_", menuName = "Planetfall/Grid/Planetfall Tile")]
public class PlanetfallTile : Tile
{
    [Header("Planetfall Data")]
    [Tooltip("Gameplay data for this tile")]
    public TileData tileData;

    /// <summary>
    /// Refresh tile sprite from TileData
    /// </summary>
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        if (tileData != null && tileData.tileSprite != null)
        {
            sprite = tileData.tileSprite;
        }
        base.RefreshTile(position, tilemap);
    }
}
