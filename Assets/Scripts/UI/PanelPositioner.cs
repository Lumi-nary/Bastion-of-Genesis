using UnityEngine;

/// <summary>
/// Helpers for anchoring a UI panel next to a trigger button.
/// </summary>
public static class PanelPositioner
{
    /// <summary>
    /// Sets <paramref name="panel"/>.anchoredPosition so its top-left edge sits at the
    /// top-right edge of <paramref name="button"/> (+ <paramref name="gapX"/>).
    /// Requires panel pivot (0, 1) and panel anchor to match its parent's pivot
    /// (both at (0.5, 0.5) is the standard Canvas setup).
    /// </summary>
    public static void PositionBeside(RectTransform panel, RectTransform button, float gapX = 8f)
    {
        if (panel == null || button == null) return;
        RectTransform parentRect = panel.parent as RectTransform;
        if (parentRect == null) return;

        Vector3[] corners = new Vector3[4];
        button.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
        Vector3 topRightWorld = corners[2];

        Canvas canvas = parentRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, topRightWorld);
        Vector2 localPoint; // origin = parent's pivot
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out localPoint);

        // Account for the panel's anchor offset so this works for any simple (non-stretch) anchor setup.
        Vector2 anchorOffset = Vector2.zero;
        if (panel.anchorMin == panel.anchorMax)
        {
            Rect pr = parentRect.rect; // local-space rect, origin = parent's pivot
            anchorOffset = new Vector2(
                pr.xMin + pr.width * panel.anchorMin.x,
                pr.yMin + pr.height * panel.anchorMin.y);
        }

        panel.anchoredPosition = new Vector2(localPoint.x - anchorOffset.x + gapX, localPoint.y - anchorOffset.y);
    }
}
