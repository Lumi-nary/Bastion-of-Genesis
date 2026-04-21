using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Left-nav button that opens one of the five HUD nav panels via UIManager.
/// Highlights itself when its target panel is active.
/// </summary>
[RequireComponent(typeof(Button))]
public class NavButton : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private UIManager.PanelKind targetPanel;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    [Header("Colors")]
    [SerializeField] private Color normalBg = Color.black;
    [SerializeField] private Color selectedBg = Color.white;
    [SerializeField] private Color normalText = Color.white;
    [SerializeField] private Color selectedText = Color.black;

    private void Reset()
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();
    }

    private bool subscribed;

    private void OnEnable()
    {
        if (button != null) button.onClick.AddListener(OnClicked);
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
        if (subscribed && UIManager.Instance != null)
        {
            UIManager.Instance.OnActivePanelChanged -= OnActivePanelChanged;
            subscribed = false;
        }
    }

    private void Start()
    {
        // Retry subscription in Start — UIManager.Instance may not have existed in OnEnable
        // due to Script Execution Order (Awake of all scripts before any OnEnable is typical,
        // but singleton assignment order can still leave Instance null at that point).
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (UIManager.Instance == null) return;
        UIManager.Instance.OnActivePanelChanged += OnActivePanelChanged;
        subscribed = true;
        ApplyHighlight(UIManager.Instance.ActivePanel);
    }

    private void OnClicked()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TogglePanel(targetPanel, transform as RectTransform);
    }

    private void OnActivePanelChanged(UIManager.PanelKind kind)
    {
        ApplyHighlight(kind);
    }

    private void ApplyHighlight(UIManager.PanelKind active)
    {
        bool selected = active == targetPanel;
        var bgColor = selected ? selectedBg : normalBg;

        if (background != null) background.color = bgColor;
        if (label != null) label.color = selected ? selectedText : normalText;

        // The Button's ColorBlock overrides Image.color on state changes, so sync its normalColor too.
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.selectedColor = bgColor;
            button.colors = colors;
        }
    }
}
