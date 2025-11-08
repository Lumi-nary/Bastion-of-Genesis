using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TooltipUI tooltipUI;

    [Header("Settings")]
    [SerializeField] private float showDelay = 0.5f; // Delay before showing tooltip
    [SerializeField] private float hideDelay = 0.1f; // Small delay before hiding to prevent flicker

    private float hoverTimer;
    private float hideTimer;
    private bool isHovering;
    private bool isPendingHide;
    private string pendingHeader;
    private string pendingDescription;

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
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;
            isPendingHide = false;
            hideTimer = 0f;

            if (hoverTimer >= showDelay && !tooltipUI.gameObject.activeSelf)
            {
                ShowTooltipImmediate();
            }

            // Update tooltip position to follow mouse
            if (tooltipUI.gameObject.activeSelf && Mouse.current != null)
            {
                tooltipUI.UpdatePosition(Mouse.current.position.ReadValue());
            }
        }
        else if (isPendingHide)
        {
            hideTimer += Time.deltaTime;

            if (hideTimer >= hideDelay)
            {
                HideTooltipImmediate();
            }
        }
    }

    public void ShowTooltip(string header, string description)
    {
        pendingHeader = header;
        pendingDescription = description;
        isHovering = true;
        hoverTimer = 0f;
    }

    private void ShowTooltipImmediate()
    {
        if (tooltipUI != null && Mouse.current != null)
        {
            tooltipUI.Show(pendingHeader, pendingDescription);
            tooltipUI.UpdatePosition(Mouse.current.position.ReadValue());
        }
    }

    public void HideTooltip()
    {
        isHovering = false;
        hoverTimer = 0f;
        isPendingHide = true;
        hideTimer = 0f;
    }

    private void HideTooltipImmediate()
    {
        isPendingHide = false;
        hideTimer = 0f;

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
}
