using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Programmatically fills a debug tilemap on Awake so we can reproduce the
/// TileStateManager clobber-on-startup bug and later verify fixes.
/// Runs in Awake (not Start) so tiles are placed before TileStateManager.Start.
/// </summary>
public class TilemapDebugPopulator : MonoBehaviour
{
    [Header("Tilemap References")]
    [Tooltip("The Ground (Tiles) tilemap - base terrain layer")]
    [SerializeField] private Tilemap groundTilemap;

    [Tooltip("The Terrain (Dynamic) tilemap - overlay objects layer")]
    [SerializeField] private Tilemap overlayTilemap;

    [Header("Tile Assets")]
    [SerializeField] private PlanetfallTile grassTile;
    [SerializeField] private PlanetfallTile sandTile;
    [SerializeField] private PlanetfallTile waterTile;
    [SerializeField] private PlanetfallTile treeTile;
    [SerializeField] private PlanetfallTile ironMoundTile;

    [Header("Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private bool populateOnAwake = true;

    private void Awake()
    {
        if (populateOnAwake)
        {
            FillGrid();
        }
    }

    [ContextMenu("Populate Now")]
    public void FillGrid()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("[TilemapDebugPopulator] groundTilemap not assigned");
            return;
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), null);
                if (overlayTilemap != null)
                {
                    overlayTilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                PlanetfallTile tile = grassTile;

                if (x < 3 && y < 3) tile = sandTile;
                if (x >= 8 && y >= 8) tile = waterTile;

                if (tile != null) groundTilemap.SetTile(pos, tile);
            }
        }

        if (overlayTilemap != null)
        {
            if (treeTile != null)
            {
                overlayTilemap.SetTile(new Vector3Int(2, 6, 0), treeTile);
                overlayTilemap.SetTile(new Vector3Int(3, 6, 0), treeTile);
                overlayTilemap.SetTile(new Vector3Int(6, 2, 0), treeTile);
            }
            if (ironMoundTile != null)
            {
                overlayTilemap.SetTile(new Vector3Int(7, 4, 0), ironMoundTile);
            }
        }

        Debug.Log($"[TilemapDebugPopulator] Populated {gridWidth}x{gridHeight} grid (ground + overlay)");
    }

    [ContextMenu("Clear")]
    public void ClearTiles()
    {
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (overlayTilemap != null) overlayTilemap.ClearAllTiles();
    }
}
