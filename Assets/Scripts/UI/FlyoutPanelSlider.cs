using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Slides a panel in from an offset while fading its CanvasGroup alpha.
/// Attach to the panel that should animate; call PlayIn() / PlayOut().
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FlyoutPanelSlider : MonoBehaviour
{
    [Tooltip("Duration of the slide animation in seconds.")]
    [SerializeField] private float duration = 0.18f;

    [Tooltip("Horizontal offset the panel slides in from (px). Negative = comes in from the left.")]
    [SerializeField] private float hiddenOffsetX = -40f;

    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rt;
    private CanvasGroup cg;
    private Coroutine routine;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void PlayIn()
    {
        Run(true, null);
    }

    public void PlayOut(Action onDone)
    {
        Run(false, onDone);
    }

    private void Run(bool show, Action onDone)
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (cg == null) cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RunRoutine(show, onDone));
    }

    private IEnumerator RunRoutine(bool show, Action onDone)
    {
        Vector2 target = rt.anchoredPosition;
        Vector2 hidden = target + new Vector2(hiddenOffsetX, 0f);
        Vector2 from = show ? hidden : target;
        Vector2 to = show ? target : hidden;

        rt.anchoredPosition = from;
        cg.alpha = show ? 0f : 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = curve.Evaluate(k);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            cg.alpha = show ? eased : 1f - eased;
            yield return null;
        }

        rt.anchoredPosition = to;
        cg.alpha = show ? 1f : 0f;
        routine = null;
        onDone?.Invoke();
    }
}
