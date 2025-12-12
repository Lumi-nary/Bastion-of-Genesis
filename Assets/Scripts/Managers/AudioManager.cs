using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioManager - Handles all game audio including music, SFX, voice, and ambience.
/// Features:
/// - Music crossfade (normal ↔ battle)
/// - SFX with priority levels (zoom-based vs always audible)
/// - Voice/dialogue playback
/// - Ambience layers
/// - Integration with AudioMixer for volume control
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Music")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private float musicCrossfadeDuration = 1.5f;

    [Header("Voice")]
    [SerializeField] private AudioSource voiceSource;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 20;
    [SerializeField] private GameObject sfxSourcePrefab;

    [Header("Ambience")]
    [SerializeField] private AudioSource ambienceSource;

    [Header("Zoom Settings")]
    [Tooltip("Reference to main camera for zoom-based volume")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float minZoom = 5f;   // Closest zoom (full volume)
    [SerializeField] private float maxZoom = 20f;  // Farthest zoom (reduced volume)

    // Mixer parameter names (must match AudioMixer exposed parameters)
    private const string MASTER_VOLUME = "MasterVolume";
    private const string MUSIC_VOLUME = "MusicVolume";
    private const string SFX_VOLUME = "SFXVolume";
    private const string VOICE_VOLUME = "VoiceVolume";
    private const string AMBIENCE_VOLUME = "AmbienceVolume";

    // Music state
    private AudioSource activeMusicSource;
    private bool isMusicSourceA = true;
    private Coroutine crossfadeCoroutine;
    private AudioClip currentMusicClip;

    // SFX pool
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private Transform sfxContainer;

    // State
    private bool isInBattle = false;
    private AudioClip normalMusicClip;
    private AudioClip battleMusicClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        InitializeSFXPool();
    }

    private void Start()
    {
        // Find main camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Apply initial volumes from settings
        if (SettingsManager.Instance != null)
        {
            ApplySettingsVolumes();
        }
    }

    /// <summary>
    /// Initialize audio sources if not assigned
    /// </summary>
    private void InitializeAudioSources()
    {
        // Create music sources if not assigned
        if (musicSourceA == null)
        {
            GameObject musicObjA = new GameObject("MusicSource_A");
            musicObjA.transform.SetParent(transform);
            musicSourceA = musicObjA.AddComponent<AudioSource>();
            musicSourceA.loop = true;
            musicSourceA.playOnAwake = false;
            musicSourceA.spatialBlend = 0f; // 2D
        }

        if (musicSourceB == null)
        {
            GameObject musicObjB = new GameObject("MusicSource_B");
            musicObjB.transform.SetParent(transform);
            musicSourceB = musicObjB.AddComponent<AudioSource>();
            musicSourceB.loop = true;
            musicSourceB.playOnAwake = false;
            musicSourceB.spatialBlend = 0f; // 2D
        }

        // Create voice source if not assigned
        if (voiceSource == null)
        {
            GameObject voiceObj = new GameObject("VoiceSource");
            voiceObj.transform.SetParent(transform);
            voiceSource = voiceObj.AddComponent<AudioSource>();
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f; // 2D
        }

        // Create ambience source if not assigned
        if (ambienceSource == null)
        {
            GameObject ambienceObj = new GameObject("AmbienceSource");
            ambienceObj.transform.SetParent(transform);
            ambienceSource = ambienceObj.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f; // 2D
        }

        // Set mixer groups if mixer is assigned
        if (audioMixer != null)
        {
            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("Music");
            if (groups.Length > 0)
            {
                musicSourceA.outputAudioMixerGroup = groups[0];
                musicSourceB.outputAudioMixerGroup = groups[0];
            }

            groups = audioMixer.FindMatchingGroups("Voice");
            if (groups.Length > 0)
            {
                voiceSource.outputAudioMixerGroup = groups[0];
            }

            groups = audioMixer.FindMatchingGroups("Ambience");
            if (groups.Length > 0)
            {
                ambienceSource.outputAudioMixerGroup = groups[0];
            }
        }

        activeMusicSource = musicSourceA;
    }

    /// <summary>
    /// Initialize SFX object pool
    /// </summary>
    private void InitializeSFXPool()
    {
        sfxContainer = new GameObject("SFX_Pool").transform;
        sfxContainer.SetParent(transform);

        AudioMixerGroup sfxGroup = null;
        if (audioMixer != null)
        {
            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0)
            {
                sfxGroup = groups[0];
            }
        }

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxObj = new GameObject($"SFX_{i}");
            sfxObj.transform.SetParent(sfxContainer);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f; // 3D by default
            if (sfxGroup != null)
            {
                source.outputAudioMixerGroup = sfxGroup;
            }
            sfxPool.Add(source);
        }
    }

    // ============================================================================
    // MUSIC
    // ============================================================================

    /// <summary>
    /// Set the normal (non-battle) music track
    /// </summary>
    public void SetNormalMusic(AudioClip clip)
    {
        normalMusicClip = clip;
        if (!isInBattle && clip != null)
        {
            PlayMusic(clip);
        }
    }

    /// <summary>
    /// Set the battle music track
    /// </summary>
    public void SetBattleMusic(AudioClip clip)
    {
        battleMusicClip = clip;
    }

    /// <summary>
    /// Play music with optional crossfade from current track
    /// </summary>
    public void PlayMusic(AudioClip clip, bool crossfade = true)
    {
        if (clip == null) return;
        if (currentMusicClip == clip && activeMusicSource.isPlaying) return;

        currentMusicClip = clip;

        if (crossfade && activeMusicSource.isPlaying)
        {
            CrossfadeToMusic(clip);
        }
        else
        {
            activeMusicSource.clip = clip;
            activeMusicSource.volume = 1f;
            activeMusicSource.Play();
        }
    }

    /// <summary>
    /// Crossfade to new music track
    /// </summary>
    private void CrossfadeToMusic(AudioClip newClip)
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }
        crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(newClip));
    }

    private IEnumerator CrossfadeCoroutine(AudioClip newClip)
    {
        AudioSource fadeOutSource = activeMusicSource;
        AudioSource fadeInSource = isMusicSourceA ? musicSourceB : musicSourceA;

        fadeInSource.clip = newClip;
        fadeInSource.volume = 0f;
        fadeInSource.Play();

        float elapsed = 0f;
        float startVolume = fadeOutSource.volume;

        while (elapsed < musicCrossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / musicCrossfadeDuration;

            fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, t);
            fadeInSource.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        fadeOutSource.Stop();
        fadeOutSource.volume = 1f;

        activeMusicSource = fadeInSource;
        isMusicSourceA = !isMusicSourceA;
        crossfadeCoroutine = null;
    }

    /// <summary>
    /// Start battle music (crossfade from normal)
    /// </summary>
    public void StartBattleMusic()
    {
        if (isInBattle) return;
        isInBattle = true;

        if (battleMusicClip != null)
        {
            PlayMusic(battleMusicClip);
        }
        Debug.Log("[AudioManager] Battle music started");
    }

    /// <summary>
    /// End battle music (crossfade back to normal)
    /// </summary>
    public void EndBattleMusic()
    {
        if (!isInBattle) return;
        isInBattle = false;

        if (normalMusicClip != null)
        {
            PlayMusic(normalMusicClip);
        }
        Debug.Log("[AudioManager] Battle music ended");
    }

    /// <summary>
    /// Stop all music
    /// </summary>
    public void StopMusic()
    {
        musicSourceA.Stop();
        musicSourceB.Stop();
        currentMusicClip = null;
    }

    // ============================================================================
    // SFX
    // ============================================================================

    /// <summary>
    /// Play SFX at position (zoom-based volume)
    /// </summary>
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f; // 3D - affected by distance/zoom
        source.Play();
    }

    /// <summary>
    /// Play priority SFX (always audible regardless of zoom - explosions, alerts)
    /// </summary>
    public void PlayPrioritySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f; // 2D - not affected by distance
        source.Play();
    }

    /// <summary>
    /// Play 2D SFX (UI sounds, etc.)
    /// </summary>
    public void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f; // 2D
        source.Play();
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // All sources busy, steal oldest
        Debug.LogWarning("[AudioManager] SFX pool exhausted, consider increasing pool size");
        return sfxPool[0];
    }

    // ============================================================================
    // VOICE
    // ============================================================================

    /// <summary>
    /// Play voice/dialogue clip
    /// </summary>
    public void PlayVoice(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = volume;
        voiceSource.Play();
    }

    /// <summary>
    /// Stop current voice playback
    /// </summary>
    public void StopVoice()
    {
        voiceSource.Stop();
    }

    /// <summary>
    /// Check if voice is currently playing
    /// </summary>
    public bool IsVoicePlaying => voiceSource.isPlaying;

    // ============================================================================
    // AMBIENCE
    // ============================================================================

    /// <summary>
    /// Play ambience loop
    /// </summary>
    public void PlayAmbience(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        ambienceSource.clip = clip;
        ambienceSource.volume = volume;
        ambienceSource.Play();
    }

    /// <summary>
    /// Stop ambience
    /// </summary>
    public void StopAmbience()
    {
        ambienceSource.Stop();
    }

    // ============================================================================
    // VOLUME CONTROL (via AudioMixer)
    // ============================================================================

    /// <summary>
    /// Apply volume settings from SettingsManager
    /// </summary>
    public void ApplySettingsVolumes()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.CurrentSettings == null)
        {
            return;
        }

        var settings = SettingsManager.Instance.CurrentSettings;
        SetMasterVolume(settings.masterVolume);
        SetMusicVolume(settings.musicVolume);
        SetSFXVolume(settings.sfxVolume);
        SetVoiceVolume(settings.voiceVolume);
    }

    /// <summary>
    /// Set master volume (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        SetMixerVolume(MASTER_VOLUME, volume);
    }

    /// <summary>
    /// Set music volume (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        SetMixerVolume(MUSIC_VOLUME, volume);
    }

    /// <summary>
    /// Set SFX volume (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SetMixerVolume(SFX_VOLUME, volume);
    }

    /// <summary>
    /// Set voice volume (0-1)
    /// </summary>
    public void SetVoiceVolume(float volume)
    {
        SetMixerVolume(VOICE_VOLUME, volume);
    }

    /// <summary>
    /// Set ambience volume (0-1)
    /// </summary>
    public void SetAmbienceVolume(float volume)
    {
        SetMixerVolume(AMBIENCE_VOLUME, volume);
    }

    /// <summary>
    /// Convert linear volume (0-1) to decibels and set mixer parameter
    /// </summary>
    private void SetMixerVolume(string parameter, float linearVolume)
    {
        if (audioMixer == null) return;

        // Convert linear (0-1) to decibels (-80 to 0)
        float dB = linearVolume > 0.0001f ? Mathf.Log10(linearVolume) * 20f : -80f;
        audioMixer.SetFloat(parameter, dB);
    }

    // ============================================================================
    // ZOOM-BASED VOLUME (for 3D sources)
    // ============================================================================

    /// <summary>
    /// Get volume multiplier based on current camera zoom
    /// Used by 3D audio sources for distance-based falloff
    /// </summary>
    public float GetZoomVolumeMultiplier()
    {
        if (mainCamera == null) return 1f;

        float currentZoom = mainCamera.orthographicSize;
        float t = Mathf.InverseLerp(minZoom, maxZoom, currentZoom);
        return Mathf.Lerp(1f, 0.3f, t); // Full volume at min zoom, 30% at max zoom
    }

    /// <summary>
    /// Update camera reference (call when camera changes)
    /// </summary>
    public void SetCamera(Camera cam)
    {
        mainCamera = cam;
    }
}
