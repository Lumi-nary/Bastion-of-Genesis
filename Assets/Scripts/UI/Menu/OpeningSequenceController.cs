using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays the boot/opening sequence in its own scene, then hands off to the main menu scene.
/// </summary>
public class OpeningSequenceController : MonoBehaviour
{
    [SerializeField] private Canvas openingSequenceCanvas;
    [SerializeField] private CanvasGroup openingLogoGroup;
    [SerializeField] private CanvasGroup openingWarningGroup;
    [SerializeField] private string nextSceneName = "MenuScene";
    [SerializeField] private float openingHoldDuration = 2.3f;
    [SerializeField] private float warningHoldDuration = 1.6f;
    [SerializeField] private float openingFadeDuration = 0.6f;

    private void Awake()
    {
        if (openingSequenceCanvas == null)
            openingSequenceCanvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        StartCoroutine(PlayOpeningSequence());
    }

    private IEnumerator PlayOpeningSequence()
    {
        CanvasGroup canvasGroup = openingSequenceCanvas != null
            ? openingSequenceCanvas.GetComponent<CanvasGroup>()
            : GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        SetCanvasGroupVisible(openingLogoGroup, true);
        SetCanvasGroupVisible(openingWarningGroup, false);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, openingHoldDuration));

        SetCanvasGroupVisible(openingLogoGroup, false);
        SetCanvasGroupVisible(openingWarningGroup, true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, warningHoldDuration));

        float duration = Mathf.Max(0.01f, openingFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void SetCanvasGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = false;
    }
}
