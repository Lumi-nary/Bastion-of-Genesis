using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class TutorialPulseHighlight : MonoBehaviour
{
    [SerializeField] private Color pulseColor = new Color(0.35f, 0.95f, 1f, 1f);
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseStrength = 0.45f;

    private Graphic graphic;
    private Color baseColor;
    private bool active;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseColor = graphic.color;
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Update()
    {
        if (!active || graphic == null)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        graphic.color = Color.Lerp(baseColor, pulseColor, t * pulseStrength);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (graphic == null)
            graphic = GetComponent<Graphic>();

        if (highlighted && !active)
            baseColor = graphic.color;

        active = highlighted;

        if (!highlighted)
            Restore();
    }

    private void Restore()
    {
        if (graphic != null)
            graphic.color = baseColor;
    }
}
