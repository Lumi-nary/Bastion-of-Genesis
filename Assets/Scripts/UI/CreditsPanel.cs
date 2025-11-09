using UnityEngine;
using TMPro;

public class CreditsPanel : MonoBehaviour
{
    [Header("Credits Text")]
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Scroll Settings")]
    [SerializeField] private bool enableAutoScroll = true;
    [SerializeField] private float scrollSpeed = 20f;
    [SerializeField] private RectTransform scrollContent;

    private float scrollPosition = 0f;

    private void OnEnable()
    {
        // Reset scroll position when panel is shown
        scrollPosition = 0f;
        if (scrollContent != null)
        {
            scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, scrollPosition);
        }

        // Set credits text if not already set in inspector
        if (creditsText != null && string.IsNullOrEmpty(creditsText.text))
        {
            SetDefaultCreditsText();
        }
    }

    private void Update()
    {
        if (enableAutoScroll && scrollContent != null)
        {
            // Auto-scroll credits upward
            scrollPosition += scrollSpeed * Time.deltaTime;
            scrollContent.anchoredPosition = new Vector2(scrollContent.anchoredPosition.x, scrollPosition);

            // Reset scroll if it goes too far (loop)
            if (scrollPosition > scrollContent.rect.height)
            {
                scrollPosition = -Screen.height;
            }
        }
    }

    private void SetDefaultCreditsText()
    {
        creditsText.text = @"<size=48><b>PLANETFALL: BASTION OF GENESIS</b></size>

<size=32><b>A BSEMC Capstone Project</b></size>

<size=24><b>GAME DESIGN & DEVELOPMENT</b></size>
EMC 4B Development Team

<size=24><b>PROGRAMMING</b></size>
Core Systems
Mission & Chapter System
Pollution & Hostility Mechanics
UI/UX Implementation

<size=24><b>GAME DESIGN</b></size>
Campaign Design (5 Chapters, 50 Missions)
Race Mechanics (Human, Elf, Dwarf, Demon)
Resource & Worker Systems
Technology Tree Design

<size=24><b>SPECIAL THANKS</b></size>
Unity Technologies
TextMesh Pro
All playtesters and supporters

<size=24><b>TOOLS & TECHNOLOGIES</b></size>
Unity 2D
C# Programming
TextMesh Pro
Unity New Input System

<size=32><b>VERSION 1.0</b></size>
2025

<size=20>
Technology vs Magic
Pollution vs Nature
Survival vs Hostility

Can you build your bastion and survive?
</size>

<size=18>Thank you for playing!</size>";
    }

    /// <summary>
    /// Set custom credits text (can be called from inspector or code)
    /// </summary>
    public void SetCreditsText(string text)
    {
        if (creditsText != null)
        {
            creditsText.text = text;
        }
    }
}
