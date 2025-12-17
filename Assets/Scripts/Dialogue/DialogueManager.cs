using System;
using UnityEngine;

/// <summary>
/// Manages dialogue flow and state.
/// Singleton that controls starting, advancing, and ending dialogues.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    // Current state
    private DialogueData currentDialogue;
    private int currentEntryIndex;
    private bool isDialogueActive;
    private bool wasGamePaused;
    private float previousTimeScale;

    // Events
    public event Action<DialogueData> OnDialogueStarted;
    public event Action<DialogueData> OnDialogueEnded;
    public event Action<DialogueEntry, int> OnEntryDisplayed; // entry, index

    // Properties
    public bool IsDialogueActive => isDialogueActive;
    public DialogueData CurrentDialogue => currentDialogue;
    public int CurrentEntryIndex => currentEntryIndex;
    public DialogueEntry CurrentEntry => currentDialogue?.GetEntry(currentEntryIndex);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Start a dialogue sequence
    /// </summary>
    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.EntryCount == 0)
        {
            Debug.LogWarning("[DialogueManager] Cannot start null or empty dialogue");
            return;
        }

        if (isDialogueActive)
        {
            Debug.LogWarning("[DialogueManager] Dialogue already active, ending current first");
            EndDialogue();
        }

        currentDialogue = dialogue;
        currentEntryIndex = 0;
        isDialogueActive = true;

        // Handle pause
        if (dialogue.pauseGame)
        {
            previousTimeScale = Time.timeScale;
            wasGamePaused = Time.timeScale == 0f;
            Time.timeScale = 0f;
        }

        if (debugLog)
            Debug.Log($"[DialogueManager] Started dialogue: {dialogue.dialogueName}");

        OnDialogueStarted?.Invoke(dialogue);
        DisplayCurrentEntry();
    }

    /// <summary>
    /// Advance to next entry or end dialogue if at last entry.
    /// Called by DialogueUI when player clicks.
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!isDialogueActive || currentDialogue == null)
            return;

        currentEntryIndex++;

        if (currentDialogue.HasEntry(currentEntryIndex))
        {
            DisplayCurrentEntry();
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// End the current dialogue
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive)
            return;

        // Restore time scale if we paused
        if (currentDialogue != null && currentDialogue.pauseGame && !wasGamePaused)
        {
            Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;
        }

        if (debugLog)
            Debug.Log($"[DialogueManager] Ended dialogue: {currentDialogue?.dialogueName}");

        var endedDialogue = currentDialogue;
        currentDialogue = null;
        currentEntryIndex = 0;
        isDialogueActive = false;

        OnDialogueEnded?.Invoke(endedDialogue);
    }

    /// <summary>
    /// Skip to end of current dialogue immediately
    /// </summary>
    public void SkipDialogue()
    {
        if (isDialogueActive)
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// Display the current entry (fires event for UI)
    /// </summary>
    private void DisplayCurrentEntry()
    {
        var entry = CurrentEntry;
        if (entry == null)
            return;

        if (debugLog)
            Debug.Log($"[DialogueManager] Displaying entry {currentEntryIndex}: {entry.speakerName}");

        OnEntryDisplayed?.Invoke(entry, currentEntryIndex);
    }
}
