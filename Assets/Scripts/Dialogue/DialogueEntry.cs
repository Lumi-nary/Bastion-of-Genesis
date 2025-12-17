using UnityEngine;

/// <summary>
/// A single line of dialogue within a conversation.
/// Contains speaker info, text, portrait, and optional voice clip.
/// </summary>
[System.Serializable]
public class DialogueEntry
{
    [Header("Speaker")]
    [Tooltip("Name displayed above the dialogue text")]
    public string speakerName;

    [Tooltip("Portrait displayed on the left side")]
    public Sprite portrait;

    [Tooltip("Optional expression sprite - overrides portrait if set")]
    public Sprite expression;

    [Header("Content")]
    [TextArea(3, 6)]
    [Tooltip("The dialogue text to display")]
    public string dialogueText;

    [Header("Audio")]
    [Tooltip("Optional voice clip to play with this line")]
    public AudioClip voiceClip;

    /// <summary>
    /// Get the sprite to display (expression if set, otherwise portrait)
    /// </summary>
    public Sprite GetDisplayPortrait()
    {
        return expression != null ? expression : portrait;
    }
}
