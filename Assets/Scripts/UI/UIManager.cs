using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private BuildingInfoPanel buildingInfoPanel;
    [SerializeField] private BuildingSelectionPanel buildingSelectionPanel;

    [Header("Tooltip Settings")]
    [SerializeField] private TooltipUI tooltipUI;
    [SerializeField] private float tooltipShowDelay = 0.5f;
    [SerializeField] private float tooltipHideDelay = 0.1f;

    // Tooltip state
    private float tooltipHoverTimer;
    private float tooltipHideTimer;
    private bool isTooltipHovering;
    private bool isTooltipPendingHide;
    private string pendingTooltipHeader;
    private string pendingTooltipDescription;

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
        UpdateTooltip();
    }

    #region Building Panels

    public void ToggleBuildingSelectionPanel()
    {
        if (buildingSelectionPanel != null)
        {
            if (buildingSelectionPanel.gameObject.activeSelf)
            {
                buildingSelectionPanel.HidePanel();
            }
            else
            {
                buildingSelectionPanel.ShowPanel();
            }
        }
    }

    public void ShowBuildingInfoPanel(Building building)
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.ShowPanel(building);
        }
    }

    public void HideBuildingInfoPanel()
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.HidePanel();
        }
    }

    #endregion

    #region Tooltip System

    private void UpdateTooltip()
    {
        if (tooltipUI == null) return;

        if (isTooltipHovering)
        {
            tooltipHoverTimer += Time.deltaTime;
            isTooltipPendingHide = false;
            tooltipHideTimer = 0f;

            if (tooltipHoverTimer >= tooltipShowDelay && !tooltipUI.gameObject.activeSelf)
            {
                ShowTooltipImmediate();
            }

            // Update tooltip position to follow mouse
            if (tooltipUI.gameObject.activeSelf && Mouse.current != null)
            {
                tooltipUI.UpdatePosition(Mouse.current.position.ReadValue());
            }
        }
        else if (isTooltipPendingHide)
        {
            tooltipHideTimer += Time.deltaTime;

            if (tooltipHideTimer >= tooltipHideDelay)
            {
                HideTooltipImmediate();
            }
        }
    }

    public void ShowTooltip(string header, string description)
    {
        pendingTooltipHeader = header;
        pendingTooltipDescription = description;
        isTooltipHovering = true;
        tooltipHoverTimer = 0f;
    }

    private void ShowTooltipImmediate()
    {
        if (tooltipUI != null && Mouse.current != null)
        {
            tooltipUI.Show(pendingTooltipHeader, pendingTooltipDescription);
            tooltipUI.UpdatePosition(Mouse.current.position.ReadValue());
        }
    }

    public void HideTooltip()
    {
        isTooltipHovering = false;
        tooltipHoverTimer = 0f;
        isTooltipPendingHide = true;
        tooltipHideTimer = 0f;
    }

    private void HideTooltipImmediate()
    {
        isTooltipPendingHide = false;
        tooltipHideTimer = 0f;

        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }
    }

    public void ShowTooltipFromProvider(ITooltipProvider provider)
    {
        if (provider != null)
        {
            ShowTooltip(provider.GetTooltipHeader(), provider.GetTooltipDescription());
        }
    }

    #endregion
}
