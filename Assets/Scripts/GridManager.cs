using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private Grid sceneGrid;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    private float cellSize;

    // Dictionary to store placed objects
    private Dictionary<Vector2Int, Building> grid = new Dictionary<Vector2Int, Building>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (sceneGrid != null)
        {
            cellSize = sceneGrid.cellSize.x; // Assuming square cells
        }
        else
        {
            Debug.LogError("GridManager Error: Scene Grid is not assigned!");
            cellSize = 1f; // Fallback
        }
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize + cellSize / 2f, gridPosition.y * cellSize + cellSize / 2f, 0) + gridOrigin;
    }

    public bool IsCellOccupied(Vector2Int cellPosition)
    {
        return grid.ContainsKey(cellPosition);
    }

    public void PlaceBuilding(Building building, Vector2Int startCell, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int cell = new Vector2Int(startCell.x + x, startCell.y + y);
                grid[cell] = building;
            }
        }
    }

}