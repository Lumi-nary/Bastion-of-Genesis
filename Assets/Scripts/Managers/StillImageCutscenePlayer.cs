using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays data-driven still-image cutscenes with editable dialogue, audio, and visual effects.
/// </summary>
public class StillImageCutscenePlayer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private RectTransform shakeRoot;

    [Header("Audio")]
    [SerializeField] private AudioSource voicelineSource;
    [SerializeField] private AudioSource soundEffectSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float cutsceneMusicBaselineDb = -9f;

    public bool IsPlaying { get; private set; }

    private Coroutine playRoutine;
    private Coroutine screenShakeRoutine;
    private Action onComplete;
    private CutsceneData currentCutscene;
    private Vector2 shakeRootStartPosition;
    private AudioClip currentBeatMusic;

    private void Awake()
    {
        if (shakeRoot == null)
        {
            shakeRoot = cutsceneImage != null ? cutsceneImage.rectTransform : transform as RectTransform;
        }

        if (shakeRoot != null)
        {
            shakeRootStartPosition = shakeRoot.anchoredPosition;
        }

        if (voicelineSource == null)
        {
            voicelineSource = gameObject.AddComponent<AudioSource>();
            voicelineSource.playOnAwake = false;
        }

        if (soundEffectSource == null)
        {
            soundEffectSource = gameObject.AddComponent<AudioSource>();
            soundEffectSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }
    }

    public void Play(CutsceneData cutsceneData, Action completed)
    {
        if (cutsceneData == null || cutsceneData.BeatCount == 0)
        {
            Debug.LogWarning("[StillImageCutscenePlayer] Cannot play null or empty CutsceneData.");
            completed?.Invoke();
            return;
        }

        Stop();
        currentCutscene = cutsceneData;
        onComplete = completed;
        IsPlaying = true;
        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (screenShakeRoutine != null)
        {
            StopCoroutine(screenShakeRoutine);
            screenShakeRoutine = null;
        }

        if (shakeRoot != null)
        {
            shakeRoot.anchoredPosition = shakeRootStartPosition;
        }

        if (voicelineSource != null)
        {
            voicelineSource.Stop();
        }

        if (soundEffectSource != null)
        {
            soundEffectSource.Stop();
        }

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        currentBeatMusic = null;
        IsPlaying = false;
    }

    public void PreviewBeat(CutsceneData cutsceneData, int beatIndex, string previewDialogueOverride = null)
    {
        if (cutsceneData == null || beatIndex < 0 || beatIndex >= cutsceneData.BeatCount)
        {
            return;
        }

        Stop();
        currentCutscene = cutsceneData;

        CutsceneBeat beat = cutsceneData.beats[beatIndex];
        ApplyVisuals(beat);
        ApplyDialogue(beat, previewDialogueOverride);
    }

    public float GetPreviewBeatDuration(CutsceneData cutsceneData, int beatIndex)
    {
        if (cutsceneData == null || beatIndex < 0 || beatIndex >= cutsceneData.BeatCount)
        {
            return 0f;
        }

        currentCutscene = cutsceneData;
        return GetBeatDuration(cutsceneData.beats[beatIndex]);
    }

    public string GetPreviewTypewriterText(CutsceneData cutsceneData, int beatIndex, float elapsedSeconds)
    {
        if (cutsceneData == null || beatIndex < 0 || beatIndex >= cutsceneData.BeatCount)
        {
            return string.Empty;
        }

        currentCutscene = cutsceneData;
        CutsceneBeat beat = cutsceneData.beats[beatIndex];

        if (!beat.showDialogue || !beat.useTypewriter || string.IsNullOrWhiteSpace(beat.dialogue))
        {
            return beat.dialogue;
        }

        List<string> words = GetWords(beat.dialogue);
        float beatDuration = GetBeatDuration(beat);
        float revealDuration = GetTypewriterRevealDuration(beat, beatDuration, words.Count);

        if (words.Count == 0 || revealDuration <= 0f)
        {
            return beat.dialogue;
        }

        int visibleWordCount = Mathf.Clamp(Mathf.CeilToInt((elapsedSeconds / revealDuration) * words.Count), 0, words.Count);
        return string.Join(" ", words.GetRange(0, visibleWordCount));
    }

    public void PreviewVisualEffect(CutsceneData cutsceneData, int beatIndex, float elapsedSeconds)
    {
        if (cutsceneData == null || beatIndex < 0 || beatIndex >= cutsceneData.BeatCount || shakeRoot == null)
        {
            return;
        }

        CutsceneBeat beat = cutsceneData.beats[beatIndex];

        if (beat.visualEffect != CutsceneVisualEffect.ScreenShake || (!beat.loopScreenShake && elapsedSeconds >= beat.screenShakeSeconds))
        {
            shakeRoot.anchoredPosition = shakeRootStartPosition;
            return;
        }

        shakeRoot.anchoredPosition = shakeRootStartPosition + UnityEngine.Random.insideUnitCircle * beat.screenShakeMagnitude;
    }

    private IEnumerator PlayRoutine()
    {
        for (int i = 0; i < currentCutscene.beats.Count; i++)
        {
            CutsceneBeat beat = currentCutscene.beats[i];
            ApplyVisuals(beat);
            ApplyDialogue(beat);
            PlayAudio(beat);
            PlayVisualEffect(beat);

            yield return PlayBeatDialogue(beat, GetBeatDuration(beat));
        }

        SetPanelVisible(false);

        if (currentCutscene.finalHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(currentCutscene.finalHoldSeconds);
        }

        IsPlaying = false;
        playRoutine = null;
        onComplete?.Invoke();
    }

    private void ApplyVisuals(CutsceneBeat beat)
    {
        if (cutsceneImage == null)
        {
            return;
        }

        if (beat.visualEffect == CutsceneVisualEffect.BlackScreen)
        {
            cutsceneImage.sprite = null;
            cutsceneImage.color = Color.black;
            return;
        }

        if (beat.clearImage)
        {
            cutsceneImage.sprite = null;
        }
        else if (beat.image != null)
        {
            cutsceneImage.sprite = beat.image;
        }

        cutsceneImage.preserveAspect = true;
        cutsceneImage.color = beat.imageColor;
    }

    private void ApplyDialogue(CutsceneBeat beat, string dialogueOverride = null)
    {
        bool hasDialogue = beat.showDialogue && (!string.IsNullOrWhiteSpace(beat.speaker) || !string.IsNullOrWhiteSpace(beat.dialogue));
        SetPanelVisible(hasDialogue);

        if (speakerText != null)
        {
            speakerText.text = hasDialogue ? beat.speaker : string.Empty;
        }

        if (dialogueText != null)
        {
            if (!hasDialogue)
            {
                dialogueText.text = string.Empty;
            }
            else if (dialogueOverride != null)
            {
                dialogueText.text = dialogueOverride;
            }
            else
            {
                dialogueText.text = !beat.useTypewriter ? beat.dialogue : string.Empty;
            }
        }
    }

    private IEnumerator PlayBeatDialogue(CutsceneBeat beat, float beatDuration)
    {
        bool shouldType = beat.showDialogue && beat.useTypewriter && dialogueText != null && !string.IsNullOrWhiteSpace(beat.dialogue);

        if (!shouldType)
        {
            yield return new WaitForSeconds(beatDuration);
            yield break;
        }

        List<string> words = GetWords(beat.dialogue);

        if (words.Count == 0)
        {
            dialogueText.text = beat.dialogue;
            yield return new WaitForSeconds(beatDuration);
            yield break;
        }

        float revealDuration = GetTypewriterRevealDuration(beat, beatDuration, words.Count);
        float delay = revealDuration > 0f ? revealDuration / words.Count : 0f;
        float elapsed = 0f;

        dialogueText.text = string.Empty;

        for (int i = 0; i < words.Count; i++)
        {
            dialogueText.text = i == 0 ? words[i] : $"{dialogueText.text} {words[i]}";

            if (delay > 0f)
            {
                elapsed += delay;
                yield return new WaitForSeconds(delay);
            }
        }

        float remaining = beatDuration - elapsed;

        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }
    }

    private void PlayAudio(CutsceneBeat beat)
    {
        if (voicelineSource != null)
        {
            voicelineSource.Stop();

            if (beat.voiceline != null)
            {
                voicelineSource.clip = beat.voiceline;
                voicelineSource.Play();
            }
        }

        if (soundEffectSource != null && beat.soundEffect != null)
        {
            soundEffectSource.PlayOneShot(beat.soundEffect);
        }

        PlayBeatMusic(beat.backgroundMusic);
    }

    private void PlayBeatMusic(AudioClip musicClip)
    {
        if (currentBeatMusic == musicClip)
        {
            return;
        }

        currentBeatMusic = musicClip;

        if (musicSource == null)
        {
            return;
        }

        if (musicClip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.volume = DecibelsToLinear(cutsceneMusicBaselineDb);
        musicSource.Play();
    }

    private static float DecibelsToLinear(float decibels)
    {
        return Mathf.Pow(10f, decibels / 20f);
    }

    private void PlayVisualEffect(CutsceneBeat beat)
    {
        if (beat.visualEffect == CutsceneVisualEffect.ScreenShake && shakeRoot != null)
        {
            if (screenShakeRoutine != null)
            {
                StopCoroutine(screenShakeRoutine);
            }

            screenShakeRoutine = StartCoroutine(ScreenShakeRoutine(beat, GetBeatDuration(beat)));
        }

        if (beat.visualEffect == CutsceneVisualEffect.SpawnPrefab && beat.visualEffectPrefab != null)
        {
            Instantiate(beat.visualEffectPrefab, transform);
        }
    }

    private float GetBeatDuration(CutsceneBeat beat)
    {
        if (beat.duration > 0f)
        {
            return beat.duration;
        }

        if (beat.voiceline != null)
        {
            return beat.voiceline.length;
        }

        return Mathf.Max(0.1f, currentCutscene.defaultBeatDuration);
    }

    private float GetTypewriterRevealDuration(CutsceneBeat beat, float beatDuration, int wordCount)
    {
        if (wordCount <= 0)
        {
            return 0f;
        }

        if (!beat.overrideTypewriterSpeed && beat.voiceline != null)
        {
            return Mathf.Max(0f, beatDuration);
        }

        float wordsPerSecond = beat.overrideTypewriterSpeed
            ? beat.typewriterWordsPerSecond
            : currentCutscene.defaultTypewriterWordsPerSecond;

        return Mathf.Min(beatDuration, wordCount / Mathf.Max(0.1f, wordsPerSecond));
    }

    private static List<string> GetWords(string text)
    {
        List<string> words = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return words;
        }

        string[] splitWords = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        words.AddRange(splitWords);
        return words;
    }

    private IEnumerator ScreenShakeRoutine(CutsceneBeat beat, float beatDuration)
    {
        float elapsed = 0f;
        float shakeSeconds = beat.loopScreenShake ? beatDuration : beat.screenShakeSeconds;

        while (elapsed < shakeSeconds)
        {
            elapsed += Time.deltaTime;
            shakeRoot.anchoredPosition = shakeRootStartPosition + UnityEngine.Random.insideUnitCircle * beat.screenShakeMagnitude;
            yield return null;
        }

        shakeRoot.anchoredPosition = shakeRootStartPosition;
        screenShakeRoutine = null;
    }

    private void SetPanelVisible(bool visible)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(visible);
        }
    }
}
