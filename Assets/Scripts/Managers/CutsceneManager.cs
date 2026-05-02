using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// CutsceneManager controls cutscene playback for chapter introductions.
/// Scene-specific singleton (NO DontDestroyOnLoad) - destroyed when leaving CutsceneScene.
/// Handles cutscene playback, skip functionality (ESC key + UI button), and scene transitions.
/// Pattern 2: Scene-specific Manager Pattern (unlike SaveManager which is DontDestroyOnLoad).
/// ADR-6: Scene Flow - MenuScene → CutsceneScene → WorldMapScene → GameWorld.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    // ============================================================================
    // SINGLETON PATTERN (Scene-specific, NO DontDestroyOnLoad)
    // ============================================================================

    public static CutsceneManager Instance { get; private set; }

    // ============================================================================
    // SERIALIZED FIELDS (Assigned in Unity Inspector)
    // ============================================================================

    [Header("Timeline Playback")]
    [SerializeField] private PlayableDirector director;

    [Header("Still Image Cutscenes")]
    [SerializeField] private StillImageCutscenePlayer stillImageCutscenePlayer;

    // ============================================================================
    // PUBLIC EVENTS
    // ============================================================================

    /// <summary>
    /// Event fired when cutscene completes naturally (Timeline finishes).
    /// Used for future analytics/achievements tracking.
    /// </summary>
    public event Action OnCutsceneCompleted;

    /// <summary>
    /// Event fired when user skips cutscene (ESC key or Skip button).
    /// Used for future analytics/achievements tracking.
    /// </summary>
    public event Action OnCutsceneSkipped;

    // ============================================================================
    // PRIVATE FIELDS
    // ============================================================================

    private InputAction escapeAction;
    private bool cutsceneActive = false;

    // ============================================================================
    // LIFECYCLE METHODS
    // ============================================================================

    /// <summary>
    /// Awake - Initialize singleton (Pattern 2: Scene-specific singleton).
    /// NO DontDestroyOnLoad - CutsceneManager only exists in CutsceneScene.
    /// </summary>
    private void Awake()
    {
        // Singleton self-destruct pattern for duplicate instances
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CutsceneManager] Duplicate CutsceneManager detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[CutsceneManager] CutsceneManager singleton initialized");
    }

    /// <summary>
    /// Start - Subscribe to Timeline events and begin cutscene playback.
    /// Reads SaveManager.pendingChapter to determine which cutscene to play.
    /// </summary>
    private void Start()
    {
        // Subscribe to PlayableDirector stopped event for completion callback
        if (director != null)
        {
            director.stopped += OnTimelineStopped;
        }
        else
        {
            Debug.LogError("[CutsceneManager] PlayableDirector reference not assigned!");
        }

        if (stillImageCutscenePlayer == null)
        {
            stillImageCutscenePlayer = GetComponentInChildren<StillImageCutscenePlayer>(true);
        }

        // Get chapter number from SaveManager pending data (ADR-7: Pending Data Handoff)
        int chapterNumber = 1; // Default to Chapter 1
        if (SaveManager.Instance != null)
        {
            chapterNumber = SaveManager.Instance.pendingChapter;
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] SaveManager.Instance not found, defaulting to Chapter 1");
        }

        Debug.Log($"[CutsceneManager] CutsceneScene loaded for Chapter {chapterNumber}");

        // Start cutscene playback
        PlayCutscene(chapterNumber);
    }

    /// <summary>
    /// OnEnable - Subscribe to ESC key input for skip functionality (AC4).
    /// Uses Unity Input System for keyboard input handling.
    /// </summary>
    private void OnEnable()
    {
        // Setup ESC key listener using Unity Input System
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
        escapeAction.performed += OnEscapePressed;
        escapeAction.Enable();
    }

    /// <summary>
    /// OnDisable - Unsubscribe from ESC key input and Timeline events.
    /// </summary>
    private void OnDisable()
    {
        if (escapeAction != null)
        {
            escapeAction.performed -= OnEscapePressed;
            escapeAction.Disable();
            escapeAction.Dispose();
        }

        if (director != null)
        {
            director.stopped -= OnTimelineStopped;
        }
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Play cutscene for the specified chapter number.
    /// Loads CutsceneData from Resources/Data/Cutscenes/Chapter{N}_Intro.asset.
    /// Falls back to Timeline asset from Resources/Data/Cutscenes/Chapter{N}_Intro.playable.
    /// AC2: Timeline Playback Starts.
    /// AC7: Missing Asset Fallback - logs warning and skips to WorldMapScene if asset not found.
    /// </summary>
    /// <param name="chapterNumber">Chapter number (1-based)</param>
    public void PlayCutscene(int chapterNumber)
    {
        string assetPath = $"Data/Cutscenes/Chapter{chapterNumber}_Intro";
        CutsceneData cutsceneData = Resources.Load<CutsceneData>(assetPath);

        if (cutsceneData != null && stillImageCutscenePlayer != null)
        {
            if (director != null && director.state == PlayState.Playing)
            {
                director.Stop();
            }

            cutsceneActive = true;
            stillImageCutscenePlayer.Play(cutsceneData, OnStillImageCutsceneComplete);
            Debug.Log($"[CutsceneManager] Playing data cutscene for Chapter {chapterNumber}: {cutsceneData.cutsceneName}");
            return;
        }

        if (director == null)
        {
            Debug.LogError("[CutsceneManager] Cannot play cutscene: PlayableDirector not assigned");
            LoadGameplayScene(); // Fallback to WorldMapScene
            return;
        }

        // Load Timeline asset from Resources folder (legacy fallback)
        PlayableAsset cutsceneAsset = Resources.Load<PlayableAsset>(assetPath);

        if (cutsceneAsset != null)
        {
            // Timeline asset found, assign and play
            director.playableAsset = cutsceneAsset;
            director.Play();
            cutsceneActive = true;

            Debug.Log($"[CutsceneManager] Playing cutscene for Chapter {chapterNumber}");
        }
        else
        {
            // AC7: Missing Asset Fallback
            Debug.LogWarning($"[CutsceneManager] Cutscene asset not found for Chapter {chapterNumber}, skipping to gameplay");
            LoadGameplayScene();
        }
    }

    /// <summary>
    /// Skip cutscene immediately (called by ESC key or Skip button).
    /// AC5: SkipCutscene() Implementation - immediate stop, no transition animation.
    /// Stops Timeline playback and loads WorldMapScene.
    /// </summary>
    public void SkipCutscene()
    {
        if (!cutsceneActive)
        {
            return; // Cutscene not active, ignore skip request
        }

        // CRITICAL: Set cutsceneActive to false BEFORE director.Stop()
        // because Stop() fires OnTimelineStopped synchronously, which would
        // call LoadGameplayScene() a second time if cutsceneActive is still true.
        cutsceneActive = false;

        // AC5: Immediate PlayableDirector stop (no fade-out)
        if (director != null && director.state == PlayState.Playing)
        {
            float skipTime = (float)director.time;
            director.Stop();
            Debug.Log($"[CutsceneManager] Cutscene skipped via skip button at time {skipTime:F2}s");
        }

        if (stillImageCutscenePlayer != null && stillImageCutscenePlayer.IsPlaying)
        {
            stillImageCutscenePlayer.Stop();
            Debug.Log("[CutsceneManager] Still image cutscene playback stopped");
        }

        // Fire OnCutsceneSkipped event (for future analytics)
        OnCutsceneSkipped?.Invoke();

        // AC5: Load WorldMapScene (no transition animation, <100ms response time)
        Debug.Log("[CutsceneManager] Cutscene skipped, loading gameplay scene");
        LoadGameplayScene();
    }

    // ============================================================================
    // PRIVATE METHODS
    // ============================================================================

    /// <summary>
    /// ESC key callback - Skip cutscene when ESC key pressed (AC4).
    /// Instant action (Factorio/Rimworld UX philosophy).
    /// </summary>
    /// <param name="context">Input action callback context</param>
    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (cutsceneActive)
        {
            float skipTime = director != null ? (float)director.time : 0f;
            Debug.Log($"[CutsceneManager] Cutscene skipped via ESC key at time {skipTime:F2}s");
            SkipCutscene();
        }
    }

    /// <summary>
    /// Timeline stopped callback - Called when PlayableDirector finishes playback.
    /// AC6: OnCutsceneComplete() Callback - loads WorldMapScene when Timeline finishes.
    /// </summary>
    /// <param name="director">PlayableDirector that stopped</param>
    private void OnTimelineStopped(PlayableDirector director)
    {
        if (!cutsceneActive)
        {
            return; // Already handled (skip or error), ignore duplicate event
        }

        cutsceneActive = false;

        // Fire OnCutsceneComplete event (for future analytics)
        OnCutsceneCompleted?.Invoke();

        // AC6: Load WorldMapScene when cutscene completes naturally
        Debug.Log("[CutsceneManager] Cutscene completed, loading gameplay scene");
        LoadGameplayScene();
    }

    private void OnStillImageCutsceneComplete()
    {
        if (!cutsceneActive)
        {
            return;
        }

        cutsceneActive = false;
        OnCutsceneCompleted?.Invoke();

        Debug.Log("[CutsceneManager] Still image cutscene completed, loading gameplay scene");
        LoadGameplayScene();
    }

    /// <summary>
    /// Load gameplay scene after cutscene.
    /// Scene Flow: CutsceneScene → Chapter1Map (skips WorldMapScene).
    /// </summary>
    private void LoadGameplayScene()
    {
        int chapterIndex = SaveManager.Instance != null ? SaveManager.Instance.pendingChapter - 1 : 0;

        Debug.Log($"[CutsceneManager] LoadGameplayScene - chapterIndex: {chapterIndex}");

        if (MissionChapterManager.Instance != null)
        {
            Debug.Log($"[CutsceneManager] Starting chapter {chapterIndex + 1} via MissionChapterManager");
            MissionChapterManager.Instance.StartChapter(chapterIndex);
        }
        else
        {
            string sceneName = $"Chapter{chapterIndex + 1}Map";
            Debug.LogWarning($"[CutsceneManager] MissionChapterManager not found, loading {sceneName} directly");
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
