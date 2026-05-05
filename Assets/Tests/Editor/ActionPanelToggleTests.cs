using NUnit.Framework;
using UnityEngine;

public class ActionPanelToggleTests
{
    private GameObject toggleObject;

    [TearDown]
    public void TearDown()
    {
        if (toggleObject != null)
        {
            Object.DestroyImmediate(toggleObject);
        }
    }

    [Test]
    public void OnPointerEnter_WhenHiddenAndNotManuallyHidden_RaisesShown()
    {
        ActionPanelToggle toggle = CreateToggle();
        int shownCount = 0;
        toggle.OnShown += () => shownCount++;

        toggle.HideInstant();
        toggle.OnPointerEnter(null);

        Assert.AreEqual(1, shownCount);
    }

    [Test]
    public void Hide_WhenShown_RaisesHidden()
    {
        ActionPanelToggle toggle = CreateToggle();
        int hiddenCount = 0;

        toggle.HideInstant();
        toggle.OnHidden += () => hiddenCount++;
        toggle.Show();
        toggle.Hide();

        Assert.AreEqual(1, hiddenCount);
    }

    private ActionPanelToggle CreateToggle()
    {
        toggleObject = new GameObject("ActionPanelToggle Test", typeof(RectTransform));
        RectTransform rectTransform = toggleObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200f, 100f);

        return toggleObject.AddComponent<ActionPanelToggle>();
    }
}
