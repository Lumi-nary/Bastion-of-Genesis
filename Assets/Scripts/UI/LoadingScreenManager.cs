using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent fullscreen loading overlay for major scene and network transitions.
/// Uses uGUI/TMP and can run without a scene-authored prefab.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    private const string DefaultLogoPath = "Art/Sprites/UI/LOGO_CRAB_WHITE";
    private const float AsyncOperationMaxProgress = 0.9f;

    [Header("Optional Prefab")]
    [SerializeField] private GameObject loadingScreenPrefab;

    [Header("Runtime UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private Image spinnerImage;
    [SerializeField] private Image[] smearImages;
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Animation")]
    [SerializeField] private float spinnerDegreesPerSecond = -220f;
    [SerializeField] private float smearDelayDegrees = 18f;

    private Coroutine loadingRoutine;
    private bool visible;

    public bool IsVisible => visible;
    public float CurrentProgress { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static LoadingScreenManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LoadingScreenManager existing = FindAnyObjectByType<LoadingScreenManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject(nameof(LoadingScreenManager));
        return managerObject.AddComponent<LoadingScreenManager>();
    }

    public static float NormalizeAsyncProgress(float asyncProgress)
    {
        return Mathf.Clamp01(asyncProgress / AsyncOperationMaxProgress);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
        Hide();
    }

    private void Update()
    {
        if (!visible || spinnerImage == null)
        {
            return;
        }

        spinnerImage.rectTransform.Rotate(0f, 0f, spinnerDegreesPerSecond * Time.unscaledDeltaTime);

        if (smearImages == null)
        {
            return;
        }

        float spinnerAngle = spinnerImage.rectTransform.localEulerAngles.z;
        for (int i = 0; i < smearImages.Length; i++)
        {
            if (smearImages[i] == null)
            {
                continue;
            }

            float delayedAngle = spinnerAngle + smearDelayDegrees * (i + 1);
            smearImages[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, delayedAngle);
        }
    }

    public Coroutine LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[LoadingScreenManager] Cannot load an empty scene name.");
            return null;
        }

        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
        }

        loadingRoutine = StartCoroutine(LoadSceneRoutine(sceneName, mode));
        return loadingRoutine;
    }

    public void ShowIndeterminate(string label = null)
    {
        EnsureOverlay();
        SetLabel(label);
        SetProgress(0f);
        SetVisible(true);
    }

    public void SetProgress(float progress)
    {
        CurrentProgress = Mathf.Clamp01(progress);

        if (progressFill != null)
        {
            Vector3 scale = progressFill.localScale;
            scale.x = CurrentProgress;
            progressFill.localScale = scale;
        }
    }

    public void Hide()
    {
        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
            loadingRoutine = null;
        }

        SetVisible(false);
        SetProgress(0f);
    }

    private IEnumerator LoadSceneRoutine(string sceneName, LoadSceneMode mode)
    {
        ShowIndeterminate("Loading...");

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
        if (operation == null)
        {
            Debug.LogError($"[LoadingScreenManager] Scene load failed to start: {sceneName}");
            Hide();
            yield break;
        }

        while (!operation.isDone)
        {
            SetProgress(NormalizeAsyncProgress(operation.progress));
            yield return null;
        }

        SetProgress(1f);
        yield return null;
        Hide();
        loadingRoutine = null;
    }

    private void EnsureOverlay()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject prefab = loadingScreenPrefab != null
            ? loadingScreenPrefab
            : Resources.Load<GameObject>("Prefabs/UI/LoadingScreen");

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = "LoadingScreen";
            BindOverlay(instance);
            if (canvas != null)
            {
                return;
            }
        }

        CreateFallbackOverlay();
    }

    private void BindOverlay(GameObject root)
    {
        canvas = root.GetComponentInChildren<Canvas>(true);
        canvasGroup = root.GetComponentInChildren<CanvasGroup>(true);
        labelText = root.transform.Find("LoadingScreenCanvas/Panel/Label")?.GetComponent<TextMeshProUGUI>();
        progressFill = root.transform.Find("LoadingScreenCanvas/Panel/ProgressBar/Fill") as RectTransform;
        spinnerImage = root.transform.Find("LoadingScreenCanvas/Panel/Spinner/Logo")?.GetComponent<Image>();

        Transform smearRoot = root.transform.Find("LoadingScreenCanvas/Panel/Spinner/Smears");
        if (smearRoot != null)
        {
            smearImages = smearRoot.GetComponentsInChildren<Image>(true);
        }
    }

    private void CreateFallbackOverlay()
    {
        GameObject canvasObject = new GameObject("LoadingScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        Stretch(canvasObject.GetComponent<RectTransform>());

        GameObject panelObject = CreateUIObject("Panel", canvasObject.transform, typeof(Image));
        Stretch(panelObject.GetComponent<RectTransform>());
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.02f, 0.025f, 0.03f, 0.96f);

        CreateSpinner(panelObject.transform);
        CreateProgressBar(panelObject.transform);
        CreateLabel(panelObject.transform);
    }

    private void CreateSpinner(Transform parent)
    {
        GameObject spinnerRoot = CreateUIObject("Spinner", parent);
        RectTransform spinnerRect = spinnerRoot.GetComponent<RectTransform>();
        spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRect.pivot = new Vector2(0.5f, 0.5f);
        spinnerRect.anchoredPosition = new Vector2(0f, 70f);
        spinnerRect.sizeDelta = new Vector2(170f, 170f);

        Sprite logo = Resources.Load<Sprite>(DefaultLogoPath);
        if (logo == null)
        {
            logo = Resources.Load<Sprite>("LOGO_CRAB_WHITE");
        }

        GameObject smearRoot = CreateUIObject("Smears", spinnerRoot.transform);
        Stretch(smearRoot.GetComponent<RectTransform>());

        smearImages = new Image[3];
        for (int i = smearImages.Length - 1; i >= 0; i--)
        {
            Image smear = CreateLogoImage($"Smear{i + 1}", smearRoot.transform, logo);
            smear.color = new Color(1f, 1f, 1f, 0.07f + i * 0.03f);
            smear.rectTransform.localScale = Vector3.one * (1f + i * 0.035f);
            smearImages[i] = smear;
        }

        spinnerImage = CreateLogoImage("Logo", spinnerRoot.transform, logo);
        spinnerImage.color = Color.white;
    }

    private void CreateProgressBar(Transform parent)
    {
        GameObject barObject = CreateUIObject("ProgressBar", parent, typeof(Image));
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0.5f);
        barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(0f, -58f);
        barRect.sizeDelta = new Vector2(460f, 14f);

        Image barImage = barObject.GetComponent<Image>();
        barImage.color = new Color(1f, 1f, 1f, 0.16f);

        GameObject fillObject = CreateUIObject("Fill", barObject.transform, typeof(Image));
        progressFill = fillObject.GetComponent<RectTransform>();
        Stretch(progressFill);
        progressFill.pivot = new Vector2(0f, 0.5f);

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.55f, 0.93f, 1f, 0.95f);
    }

    private void CreateLabel(Transform parent)
    {
        GameObject labelObject = CreateUIObject("Label", parent, typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, -100f);
        labelRect.sizeDelta = new Vector2(520f, 42f);

        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = "Loading...";
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.83f, 0.95f, 1f, 0.95f);
        labelText.raycastTarget = false;
    }

    private Image CreateLogoImage(string name, Transform parent, Sprite sprite)
    {
        GameObject imageObject = CreateUIObject(name, parent, typeof(Image));
        Stretch(imageObject.GetComponent<RectTransform>());

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] extraComponents)
    {
        System.Type[] components = new System.Type[2 + extraComponents.Length];
        components[0] = typeof(RectTransform);
        components[1] = typeof(CanvasRenderer);
        for (int i = 0; i < extraComponents.Length; i++)
        {
            components[i + 2] = extraComponents[i];
        }

        GameObject gameObject = new GameObject(name, components);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void SetVisible(bool show)
    {
        visible = show;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = show ? 1f : 0f;
            canvasGroup.blocksRaycasts = show;
        }

        if (canvas != null)
        {
            canvas.enabled = show;
        }
    }

    private void SetLabel(string label)
    {
        if (labelText != null)
        {
            labelText.text = string.IsNullOrWhiteSpace(label) ? "Loading..." : label;
        }
    }
}
