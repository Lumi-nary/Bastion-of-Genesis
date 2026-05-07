using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class MenuButtonClickSfx : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayIfInteractable();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayIfInteractable();
    }

    private void PlayIfInteractable()
    {
        if (button == null || !button.IsInteractable() || MenuManager.Instance == null)
            return;

        MenuManager.Instance.PlayButtonClickSfx();
    }
}
