using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Color normalBg = Color.white;
    [SerializeField] private Color selectedBg = Color.white;
    [SerializeField] private Color normalText = Color.white;
    [SerializeField] private Color selectedText = new Color32(0x99, 0xA4, 0xFF, 0xFF);

    private bool subscribed;
    private bool tutorialSubscribed;
    private TutorialPulseHighlight pulseHighlight;

    private void Reset()
    {
        button = GetComponent<Button>();
        background = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (background == null)
            background = GetComponent<Image>();
        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
        if (background != null)
            pulseHighlight = background.GetComponent<TutorialPulseHighlight>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(OnClicked);

        TrySubscribe();
        TrySubscribeTutorial();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);

        if (subscribed && UIManager.Instance != null)
        {
            UIManager.Instance.OnActivePanelChanged -= OnActivePanelChanged;
            subscribed = false;
        }

        if (tutorialSubscribed && TutorialGuideManager.Instance != null)
        {
            TutorialGuideManager.Instance.OnTutorialObjectiveChanged -= OnTutorialObjectiveChanged;
            tutorialSubscribed = false;
        }

        SetTutorialHighlight(false);
    }

    private void Start()
    {
        TrySubscribe();
        TrySubscribeTutorial();
        ApplyTutorialGate();
    }

    private void TrySubscribe()
    {
        if (subscribed || UIManager.Instance == null)
            return;

        UIManager.Instance.OnActivePanelChanged += OnActivePanelChanged;
        subscribed = true;
        ApplyHighlight(UIManager.Instance.ActivePanel);
    }

    private void TrySubscribeTutorial()
    {
        if (tutorialSubscribed || TutorialGuideManager.Instance == null)
            return;

        TutorialGuideManager.Instance.OnTutorialObjectiveChanged += OnTutorialObjectiveChanged;
        tutorialSubscribed = true;
        ApplyTutorialGate();
    }

    private void OnClicked()
    {
        if (TutorialGuideManager.Instance != null && !TutorialGuideManager.Instance.CanOpenPanel(targetPanel))
            return;

        if (UIManager.Instance != null)
            UIManager.Instance.TogglePanel(targetPanel, transform as RectTransform);
    }

    private void OnActivePanelChanged(UIManager.PanelKind kind)
    {
        ApplyHighlight(kind);
    }

    private void OnTutorialObjectiveChanged(MissionObjective objective)
    {
        ApplyTutorialGate();
    }

    private void ApplyHighlight(UIManager.PanelKind active)
    {
        bool selected = active == targetPanel;
        Color bgColor = selected ? selectedBg : normalBg;

        if (background != null)
            background.color = bgColor;
        if (label != null)
            label.color = selected ? selectedText : normalText;

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = bgColor;
            colors.selectedColor = bgColor;
            button.colors = colors;
        }

        ApplyTutorialGate();
    }

    private void ApplyTutorialGate()
    {
        bool allowed = TutorialGuideManager.Instance == null || TutorialGuideManager.Instance.CanOpenPanel(targetPanel);
        bool highlighted = TutorialGuideManager.Instance != null && TutorialGuideManager.Instance.IsTargetPanel(targetPanel);

        if (button != null)
            button.interactable = allowed;

        SetTutorialHighlight(highlighted);
    }

    private void SetTutorialHighlight(bool highlighted)
    {
        if (background == null)
            return;

        if (pulseHighlight == null)
            pulseHighlight = background.gameObject.GetComponent<TutorialPulseHighlight>() ?? background.gameObject.AddComponent<TutorialPulseHighlight>();

        pulseHighlight.SetHighlighted(highlighted);
    }
}
