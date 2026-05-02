using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Planetfall/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    [Header("Cutscene Info")]
    public string cutsceneName;

    [Header("Default Timing")]
    [Min(0.1f)] public float defaultBeatDuration = 3.35f;
    [Min(0f)] public float finalHoldSeconds = 1.25f;

    [Header("Default Typewriter")]
    [Min(0.1f)] public float defaultTypewriterWordsPerSecond = 6f;

    [Header("Beats")]
    public List<CutsceneBeat> beats = new List<CutsceneBeat>();

    public int BeatCount => beats.Count;
}

[System.Serializable]
public class CutsceneBeat
{
    [Header("Timing")]
    [Tooltip("Seconds to hold this beat. Set to 0 to use the cutscene default, or the voiceline length if one is assigned.")]
    [Min(0f)] public float duration;

    [Header("Dialogue")]
    public bool showDialogue = true;
    public string speaker;

    [TextArea(3, 6)]
    public string dialogue;

    [Header("Typewriter")]
    public bool useTypewriter = true;
    public bool overrideTypewriterSpeed;
    [Min(0.1f)] public float typewriterWordsPerSecond = 6f;

    [Header("Audio")]
    public AudioClip voiceline;
    public AudioClip soundEffect;
    public AudioClip backgroundMusic;

    [Header("Visual")]
    public Sprite image;
    public bool clearImage;
    public Color imageColor = Color.white;
    public CutsceneVisualEffect visualEffect = CutsceneVisualEffect.None;
    public GameObject visualEffectPrefab;
    [Min(0f)] public float screenShakeMagnitude = 12f;
    [Min(0f)] public float screenShakeSeconds = 0.45f;
    public bool loopScreenShake;
}

public enum CutsceneVisualEffect
{
    None,
    BlackScreen,
    ScreenShake,
    SpawnPrefab
}
