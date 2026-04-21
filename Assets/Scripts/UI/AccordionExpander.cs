using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates a layout-driven panel's height (via LayoutElement.preferredHeight) from 0
/// to its natural content height, with a CanvasGroup alpha fade. Intended for accordion
/// sections whose parent uses a VerticalLayoutGroup.
///
/// Add a RectMask2D on the same GameObject so children clip to the animated height.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(LayoutElement))]
public class AccordionExpander : MonoBehaviour
{
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rt;
    private LayoutElement layoutElement;
    private CanvasGroup canvasGroup;
    private Coroutine routine;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Open()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(true, null));
    }

    public void Close(Action onDone)
    {
        if (!gameObject.activeSelf) { onDone?.Invoke(); return; }
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(false, onDone));
    }

    private IEnumerator Run(bool open, Action onDone)
    {
        if (open && !gameObject.activeSelf) gameObject.SetActive(true);

        // Let one layout pass happen so LayoutUtility reports a stable preferred height.
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        float target = Mathf.Max(0f, LayoutUtility.GetPreferredHeight(rt));

        float from = open ? 0f : (layoutElement.preferredHeight > 0f ? layoutElement.preferredHeight : target);
        float to   = open ? target : 0f;
        float aFrom = open ? 0f : 1f;
        float aTo   = open ? 1f : 0f;

        layoutElement.preferredHeight = from;
        layoutElement.minHeight = 0f;
        canvasGroup.alpha = aFrom;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = curve.Evaluate(k);
            layoutElement.preferredHeight = Mathf.LerpUnclamped(from, to, e);
            canvasGroup.alpha = Mathf.LerpUnclamped(aFrom, aTo, e);
            yield return null;
        }

        layoutElement.preferredHeight = to;
        canvasGroup.alpha = aTo;

        routine = null;
        onDone?.Invoke();
    }
}
