using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// UI component for displaying dialogue.
/// Handles portrait, name, typewriter text effect, and voice playback.
/// Click anywhere on panel to advance dialogue.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Background Blur")]
    [SerializeField] private bool blurBackground = true;
    [SerializeField] private Canvas blurCanvas;

    [Header("Advance Input")]
    [SerializeField] private RectTransform[] ignoredAdvanceAreas;

    [Header("Typewriter Settings")]
    [Tooltip("Delay between each word appearing")]
    [SerializeField] private float wordDelay = 0.05f;

    // State
    private Coroutine typewriterCoroutine;
    private bool isTyping;
    private string fullText;
    private UIBackgroundBlur backgroundBlur;
    private bool isSubscribed;

    private void Start()
    {
        EnsureSubscribed();

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            OnDialogueStarted(DialogueManager.Instance.CurrentDialogue);
            OnEntryDisplayed(DialogueManager.Instance.CurrentEntry, DialogueManager.Instance.CurrentEntryIndex);
        }
        else if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (blurCanvas == null)
            blurCanvas = GetComponentInParent<Canvas>();
    }

    public void EnsureSubscribed()
    {
        if (isSubscribed || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.OnDialogueStarted += OnDialogueStarted;
        DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
        DialogueManager.Instance.OnEntryDisplayed += OnEntryDisplayed;
        isSubscribed = true;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (isSubscribed && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= OnDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
            DialogueManager.Instance.OnEntryDisplayed -= OnEntryDisplayed;
        }
    }

    private void Update()
    {
        // Check for click/input to advance dialogue
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            // New Input System
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 pointerPosition = Mouse.current.position.ReadValue();
                if (!IsPointerOverIgnoredAdvanceArea(pointerPosition))
                    OnClickAdvance();
            }
            else if (Keyboard.current != null &&
                    (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                OnClickAdvance();
            }
        }
    }

    /// <summary>
    /// Called when player clicks to advance
    /// </summary>
    public void OnClickAdvance()
    {
        if (isTyping)
        {
            // Complete typewriter immediately
            CompleteTypewriter();
        }
        else
        {
            // Advance to next entry
            DialogueManager.Instance?.AdvanceDialogue();
        }
    }

    private bool IsPointerOverIgnoredAdvanceArea(Vector2 screenPosition)
    {
        if (ignoredAdvanceAreas == null || ignoredAdvanceAreas.Length == 0)
            return false;

        Camera eventCamera = GetEventCamera();
        foreach (RectTransform ignoredArea in ignoredAdvanceAreas)
        {
            if (ignoredArea != null && ignoredArea.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(ignoredArea, screenPosition, eventCamera))
            {
                return true;
            }
        }

        return false;
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = blurCanvas != null ? blurCanvas : GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    /// <summary>
    /// Called when dialogue starts
    /// </summary>
    private void OnDialogueStarted(DialogueData dialogue)
    {
        if (blurBackground)
        {
            if (blurCanvas == null)
                blurCanvas = GetComponentInParent<Canvas>();

            backgroundBlur = UIBackgroundBlur.Ensure(blurCanvas);
            backgroundBlur?.ShowBlur();
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }

    /// <summary>
    /// Called when dialogue ends
    /// </summary>
    private void OnDialogueEnded(DialogueData dialogue)
    {
        StopTypewriter();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (backgroundBlur != null)
            backgroundBlur.HideBlur();

        // Stop any playing voice via AudioManager
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopVoice();
    }

    /// <summary>
    /// Called when a new entry should be displayed
    /// </summary>
    private void OnEntryDisplayed(DialogueEntry entry, int index)
    {
        if (entry == null) return;

        // Set portrait
        if (portraitImage != null)
        {
            Sprite displayPortrait = entry.GetDisplayPortrait();
            if (displayPortrait != null)
            {
                portraitImage.sprite = displayPortrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        // Set name
        if (nameText != null)
        {
            nameText.text = entry.speakerName;
        }

        // Start typewriter for dialogue text
        StartTypewriter(entry.dialogueText);

        // Play voice clip
        PlayVoiceClip(entry.voiceClip);
    }

    /// <summary>
    /// Start the typewriter effect
    /// </summary>
    private void StartTypewriter(string text)
    {
        StopTypewriter();
        fullText = text;
        typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
    }

    /// <summary>
    /// Stop the typewriter effect
    /// </summary>
    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        isTyping = false;
    }

    /// <summary>
    /// Complete typewriter immediately, showing full text
    /// </summary>
    private void CompleteTypewriter()
    {
        StopTypewriter();
        if (dialogueText != null)
            dialogueText.text = fullText;
    }

    /// <summary>
    /// Typewriter coroutine - displays text word by word
    /// </summary>
    private IEnumerator TypewriterCoroutine(string text)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        string[] words = text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            if (dialogueText != null)
            {
                if (i > 0)
                    dialogueText.text += " ";
                dialogueText.text += words[i];
            }

            // Use unscaled time so typewriter works when game is paused
            yield return new WaitForSecondsRealtime(wordDelay);
        }

        isTyping = false;
        typewriterCoroutine = null;
    }

    /// <summary>
    /// Play voice clip for current entry via AudioManager
    /// </summary>
    private void PlayVoiceClip(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayVoice(clip);
    }
}
