using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildingHoverHealthUI : MonoBehaviour
{
    [SerializeField] private float statusPopupDuration = 2f;

    private Canvas canvas;
    private Camera mainCamera;
    private Building hoveredBuilding;

    public void Initialize(Canvas targetCanvas)
    {
        canvas = targetCanvas;
        mainCamera = Camera.main;

        Hide();
    }

    private void Update()
    {
        if (canvas == null)
        {
            Initialize(FindFirstObjectByType<Canvas>());
        }

        UpdateHover();
    }

    public void Hide()
    {
        hoveredBuilding = null;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBuildingHoverPopup(this);
        }
    }

    private void UpdateHover()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (ShouldSuppressHover())
        {
            Hide();
            return;
        }

        Building building = GetBuildingUnderMouse();
        if (building == null)
        {
            Hide();
            return;
        }

        if (hoveredBuilding == building)
        {
            return;
        }

        hoveredBuilding = building;
        if (UIManager.Instance != null)
        {
            if (BuildingHoverPopupUI.ShouldShowOperationalStatus(building.BuildingData))
            {
                UIManager.Instance.ShowBuiltBuildingStatusPopup(building, statusPopupDuration, this);
            }
            else
            {
                UIManager.Instance.ShowBuiltBuildingHealthPopup(building, statusPopupDuration, this);
            }
        }
    }

    private bool ShouldSuppressHover()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPaused)
        {
            return true;
        }

        if (PlacementSystem.Instance != null && PlacementSystem.Instance.IsBuilding)
        {
            return true;
        }

        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private Building GetBuildingUnderMouse()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return null;
        }

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(mouseWorldPos.x, mouseWorldPos.y));

        foreach (Collider2D hit in hits)
        {
            Building building = hit.GetComponent<Building>();
            if (building != null && !building.IsDestroyed)
            {
                return building;
            }
        }

        return null;
    }
}
