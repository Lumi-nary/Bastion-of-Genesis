using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Captures the frame behind a canvas, blurs it at low resolution, and draws it behind that canvas' UI.
/// Intended for modal canvases and dialogue overlays.
/// </summary>
public class UIBackgroundBlur : MonoBehaviour
{
    [Header("Capture")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private int captureWidth = 240;
    [SerializeField] private int blurIterations = 4;

    [Header("Overlay")]
    [SerializeField] private Color tint = new Color(0.02f, 0.06f, 0.11f, 0.35f);
    [SerializeField] private bool blockRaycasts = true;

    private RawImage overlayImage;
    private Texture2D blurredTexture;
    private Coroutine captureCoroutine;

    public static UIBackgroundBlur Ensure(Canvas canvas)
    {
        if (canvas == null)
            return null;

        UIBackgroundBlur blur = canvas.GetComponent<UIBackgroundBlur>();
        if (blur == null)
            blur = canvas.gameObject.AddComponent<UIBackgroundBlur>();

        blur.targetCanvas = canvas;
        blur.EnsureOverlay();
        return blur;
    }

    public void ShowBlur()
    {
        EnsureOverlay();

        if (captureCoroutine != null)
            StopCoroutine(captureCoroutine);

        captureCoroutine = StartCoroutine(CaptureBlurredBackground());
    }

    public void HideBlur()
    {
        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        if (overlayImage != null)
            overlayImage.gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();
    }

    private void OnDestroy()
    {
        if (blurredTexture != null)
            Destroy(blurredTexture);
    }

    private IEnumerator CaptureBlurredBackground()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        EnsureOverlay();
        overlayImage.gameObject.SetActive(false);

        if (targetCanvas != null)
            targetCanvas.enabled = false;

        yield return new WaitForEndOfFrame();

        Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();

        if (targetCanvas != null)
            targetCanvas.enabled = true;

        if (screenTexture == null)
        {
            captureCoroutine = null;
            yield break;
        }

        ReplaceBlurTexture(CreateBlurredTexture(screenTexture));
        Destroy(screenTexture);

        if (blurredTexture != null)
        {
            overlayImage.texture = blurredTexture;
            overlayImage.color = tint;
            overlayImage.raycastTarget = blockRaycasts;
            overlayImage.gameObject.SetActive(true);
            overlayImage.transform.SetAsFirstSibling();
        }

        captureCoroutine = null;
    }

    private void EnsureOverlay()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        if (overlayImage != null)
            return;

        GameObject overlayObject = new GameObject("BackgroundBlur", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        overlayObject.transform.SetParent(targetCanvas != null ? targetCanvas.transform : transform, false);
        overlayObject.transform.SetAsFirstSibling();

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayImage = overlayObject.GetComponent<RawImage>();
        overlayImage.color = tint;
        overlayImage.raycastTarget = blockRaycasts;
        overlayImage.gameObject.SetActive(false);
    }

    private Texture2D CreateBlurredTexture(Texture2D source)
    {
        int width = Mathf.Clamp(captureWidth, 32, Mathf.Max(32, source.width));
        int height = Mathf.Max(18, Mathf.RoundToInt(source.height * (width / (float)source.width)));

        Color32[] sourcePixels = source.GetPixels32();
        Color32[] scaledPixels = Downsample(sourcePixels, source.width, source.height, width, height);

        Color32[] blurredPixels = scaledPixels;
        int iterations = Mathf.Max(1, blurIterations);
        for (int i = 0; i < iterations; i++)
            blurredPixels = BoxBlur(blurredPixels, width, height);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.wrapMode = TextureWrapMode.Clamp;
        result.filterMode = FilterMode.Bilinear;
        result.SetPixels32(blurredPixels);
        result.Apply(false, false);
        return result;
    }

    private void ReplaceBlurTexture(Texture2D texture)
    {
        if (blurredTexture != null)
            Destroy(blurredTexture);

        blurredTexture = texture;
    }

    private static Color32[] Downsample(Color32[] source, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
    {
        Color32[] target = new Color32[targetWidth * targetHeight];

        for (int y = 0; y < targetHeight; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.RoundToInt(y * (sourceHeight - 1) / (float)Mathf.Max(1, targetHeight - 1)), 0, sourceHeight - 1);
            for (int x = 0; x < targetWidth; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.RoundToInt(x * (sourceWidth - 1) / (float)Mathf.Max(1, targetWidth - 1)), 0, sourceWidth - 1);
                target[y * targetWidth + x] = source[sourceY * sourceWidth + sourceX];
            }
        }

        return target;
    }

    private static Color32[] BoxBlur(Color32[] source, int width, int height)
    {
        Color32[] horizontal = new Color32[source.Length];
        Color32[] result = new Color32[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                horizontal[y * width + x] = Average(source, width, height, x - 1, y, x + 1, y);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[y * width + x] = Average(horizontal, width, height, x, y - 1, x, y + 1);
            }
        }

        return result;
    }

    private static Color32 Average(Color32[] pixels, int width, int height, int minX, int minY, int maxX, int maxY)
    {
        int r = 0;
        int g = 0;
        int b = 0;
        int a = 0;
        int count = 0;

        minX = Mathf.Clamp(minX, 0, width - 1);
        maxX = Mathf.Clamp(maxX, 0, width - 1);
        minY = Mathf.Clamp(minY, 0, height - 1);
        maxY = Mathf.Clamp(maxY, 0, height - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Color32 color = pixels[y * width + x];
                r += color.r;
                g += color.g;
                b += color.b;
                a += color.a;
                count++;
            }
        }

        return new Color32(
            (byte)(r / count),
            (byte)(g / count),
            (byte)(b / count),
            (byte)(a / count));
    }
}
