using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime debug UI: slider to scrub pollution radius, plus a toggle to
/// animate it from 0 to max over a configurable duration ("simulate spread").
/// Self-builds its Canvas + controls on Start if none are wired.
/// </summary>
public class PollutionDebugSlider : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private float minRadius = 0f;
    [SerializeField] private float maxRadius = 15f;
    [SerializeField] private float initialRadius = 6f;

    [Header("Simulate Spread")]
    [SerializeField] private float spreadDurationSeconds = 8f;

    [Header("UI (auto-built if null)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider slider;
    [SerializeField] private Text valueLabel;
    [SerializeField] private Button simulateButton;
    [SerializeField] private Button resetButton;

    private bool isSimulating;
    private float simulateStartTime;

    private void Start()
    {
        if (canvas == null)
        {
            BuildUI();
        }

        slider.minValue = minRadius;
        slider.maxValue = maxRadius;
        slider.value = initialRadius;
        slider.onValueChanged.AddListener(OnSliderChanged);
        simulateButton.onClick.AddListener(OnSimulateClicked);
        resetButton.onClick.AddListener(OnResetClicked);

        ApplyRadius(initialRadius);
    }

    private void Update()
    {
        if (!isSimulating) return;

        float elapsed = Time.time - simulateStartTime;
        float t = Mathf.Clamp01(elapsed / spreadDurationSeconds);
        float r = Mathf.Lerp(minRadius, maxRadius, t);
        slider.value = r;

        if (t >= 1f)
        {
            isSimulating = false;
        }
    }

    private void OnSliderChanged(float value)
    {
        ApplyRadius(value);
        if (valueLabel != null) valueLabel.text = $"Pollution Radius: {value:F1}";
    }

    private void OnSimulateClicked()
    {
        isSimulating = true;
        simulateStartTime = Time.time;
        slider.value = minRadius;
    }

    private void OnResetClicked()
    {
        isSimulating = false;
        slider.value = initialRadius;
    }

    private void ApplyRadius(float r)
    {
        if (TileStateManager.Instance != null)
        {
            TileStateManager.Instance.SetPollutionRadius(r);
        }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("DebugCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var evGO = new GameObject("EventSystem");
            evGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                evGO.AddComponent(inputSystemModuleType);
            }
            else
            {
                evGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(12f, -12f);
        panelRT.sizeDelta = new Vector2(320f, 140f);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);

        valueLabel = CreateText(panelGO.transform, "Label", new Vector2(12f, -8f), new Vector2(-24f, 24f), "Pollution Radius: 0.0");
        valueLabel.alignment = TextAnchor.MiddleLeft;

        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(panelGO.transform, false);
        var sliderRT = sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 1f);
        sliderRT.anchorMax = new Vector2(1f, 1f);
        sliderRT.pivot = new Vector2(0f, 1f);
        sliderRT.anchoredPosition = new Vector2(12f, -40f);
        sliderRT.sizeDelta = new Vector2(-24f, 24f);
        slider = sliderGO.AddComponent<Slider>();

        var sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderGO.transform, false);
        var sliderBgRT = sliderBg.AddComponent<RectTransform>();
        sliderBgRT.anchorMin = Vector2.zero;
        sliderBgRT.anchorMax = Vector2.one;
        sliderBgRT.offsetMin = Vector2.zero;
        sliderBgRT.offsetMax = Vector2.zero;
        var sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(5f, 5f);
        fillAreaRT.offsetMax = new Vector2(-5f, -5f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.5f, 0.2f, 1f);
        slider.fillRect = fillRT;
        slider.targetGraphic = fillImg;

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10f, 0f);
        handleAreaRT.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(16f, 24f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRT;

        simulateButton = CreateButton(panelGO.transform, "SimulateBtn", new Vector2(12f, -78f), new Vector2(140f, 28f), "Simulate Spread");
        resetButton    = CreateButton(panelGO.transform, "ResetBtn",    new Vector2(164f, -78f), new Vector2(140f, 28f), "Reset");
    }

    private static Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, string content)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white;
        txt.fontSize = 14;
        txt.text = content;
        return txt;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        var labelTxt = labelGO.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.color = Color.white;
        labelTxt.fontSize = 13;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.text = label;

        return btn;
    }
}
