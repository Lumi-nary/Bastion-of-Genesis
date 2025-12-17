using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A complete dialogue conversation containing multiple entries.
/// Create via: Right-click > Create > Planetfall > Dialogue Data
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Planetfall/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Dialogue Info")]
    [Tooltip("Name for reference (not displayed in-game)")]
    public string dialogueName;

    [Header("Settings")]
    [Tooltip("Pause the game while this dialogue is active")]
    public bool pauseGame = false;

    [Header("Entries")]
    [Tooltip("The sequence of dialogue lines")]
    public List<DialogueEntry> entries = new List<DialogueEntry>();

    /// <summary>
    /// Get the number of entries in this dialogue
    /// </summary>
    public int EntryCount => entries.Count;

    /// <summary>
    /// Get a specific entry by index
    /// </summary>
    public DialogueEntry GetEntry(int index)
    {
        if (index < 0 || index >= entries.Count)
            return null;
        return entries[index];
    }

    /// <summary>
    /// Check if index is valid
    /// </summary>
    public bool HasEntry(int index)
    {
        return index >= 0 && index < entries.Count;
    }
}
