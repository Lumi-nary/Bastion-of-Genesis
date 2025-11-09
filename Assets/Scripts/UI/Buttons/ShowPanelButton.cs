using UnityEngine;

public class ShowPanelButton : MonoBehaviour
{
    [SerializeField] private GameObject panelToShow;
    [SerializeField] private GameObject panelToHide;

    /// <summary>
    /// Assign this method to the button's OnClick() in inspector
    /// Used for Credits, Options buttons that show/hide panels
    /// </summary>
    public void OnShowPanelClicked()
    {
        if (panelToShow != null)
            panelToShow.SetActive(true);

        if (panelToHide != null)
            panelToHide.SetActive(false);
    }
}
