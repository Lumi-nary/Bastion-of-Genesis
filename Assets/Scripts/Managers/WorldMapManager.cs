using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// WorldMapManager displays chapter-specific map with kingdom lore and context.
/// Scene-specific singleton (Pattern 2) - destroyed when leaving WorldMapScene.
/// Reads SaveManager.pendingChapter to determine which map to display.
/// Provides missing asset fallback (text-only display if map prefab unavailable).
/// AC: 1, 2, 3, 7 - WorldMapScene loads, displays chapter map, handles missing assets.
/// </summary>
public class WorldMapManager : MonoBehaviour
{
    /// <summary>
    /// Scene-specific singleton instance (Pattern 2 - NO DontDestroyOnLoad).
    /// Destroyed when transitioning to GameWorld scene.
    /// </summary>
    public static WorldMapManager Instance { get; private set; }

    [Header("Chapter Map Prefabs")]
    [Tooltip("Array of chapter map prefabs (indexed by chapter number - 1). Each prefab contains map artwork, kingdom labels, crash site marker.")]
    [SerializeField] private GameObject[] chapterMapPrefabs;

    [Header("UI Components")]
    [Tooltip("TextMeshProUGUI displaying chapter title (e.g., 'Chapter 1: First Landing')")]
    [SerializeField] private TextMeshProUGUI chapterTitleText;

    [Tooltip("TextMeshProUGUI displaying chapter description with kingdom lore")]
    [SerializeField] private TextMeshProUGUI chapterDescriptionText;

    [Tooltip("Transform parent for instantiated map prefab (positioning and hierarchy)")]
    [SerializeField] private Transform mapDisplayParent;

    [Tooltip("TextMeshProUGUI showing fallback message when map prefab missing")]
    [SerializeField] private TextMeshProUGUI fallbackMessageText;

    /// <summary>
    /// Hardcoded chapter titles (MVP implementation).
    /// Future: Move to ScriptableObject (Epic 7+).
    /// </summary>
    private readonly string[] chapterTitles = {
        "Chapter 1: First Landing",
        "Chapter 2: Expansion",
        "Chapter 3: Resistance"
    };

    /// <summary>
    /// Hardcoded chapter descriptions with kingdom lore (MVP implementation).
    /// Future: Move to ScriptableObject (Epic 7+).
    /// </summary>
    private readonly string[] chapterDescriptions = {
        "Your ship has crashed in the wilderness between three warring kingdoms. Choose your path wisely.",
        "The kingdoms grow wary of your presence. Forge alliances or conquer them all.",
        "The planet fights back. Can you survive and complete your mission?"
    };

    /// <summary>
    /// Scene-specific singleton initialization (Pattern 2).
    /// NO DontDestroyOnLoad - manager destroyed when scene unloads.
    /// AC1: WorldMapManager.Instance initializes on scene load.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[WorldMapManager] Duplicate detected, destroying");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // NO DontDestroyOnLoad! (Pattern 2: Scene-specific singleton)

        Debug.Log("[WorldMapManager] WorldMapManager initialized");
    }

    /// <summary>
    /// Automatically display chapter map on scene load.
    /// AC2: DisplayChapterMap() called automatically in Start().
    /// </summary>
    private void Start()
    {
        DisplayChapterMap();
    }

    /// <summary>
    /// Display chapter-specific map based on SaveManager.pendingChapter.
    /// Reads pending chapter number, instantiates map prefab, sets title/description text.
    /// Handles missing asset gracefully with fallback text-only display.
    /// AC2: Reads SaveManager.pendingChapter, loads chapter map prefab, instantiates in scene.
    /// AC3: Chapter title and description display with TextMeshPro styling.
    /// AC7: Missing asset fallback - log warning, show fallback text, continue flow.
    /// </summary>
    public void DisplayChapterMap()
    {
        // AC2: Read SaveManager.pendingChapter to get chapter number
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[WorldMapManager] SaveManager.Instance not found! Cannot display chapter map.");
            ShowFallback(1); // Default to Chapter 1
            return;
        }

        int chapterNumber = SaveManager.Instance.pendingChapter;

        // Pattern 4 Logging: [ManagerName] Operation: details
        Debug.Log($"[WorldMapManager] WorldMapScene loaded for Chapter {chapterNumber}");
        Debug.Log($"[WorldMapManager] Displaying map for Chapter {chapterNumber}");

        // Set chapter title and description text (AC3)
        SetChapterText(chapterNumber);

        // AC2: Load and instantiate chapter map prefab
        // Check if chapterMapPrefabs array is valid and has entry for this chapter
        if (chapterMapPrefabs == null || chapterMapPrefabs.Length == 0)
        {
            // AC7: Missing asset fallback
            Debug.LogWarning($"[WorldMapManager] chapterMapPrefabs array is null or empty, using default display");
            ShowFallback(chapterNumber);
            return;
        }

        int prefabIndex = chapterNumber - 1; // Convert 1-based chapter to 0-based index

        // Check if index is within array bounds
        if (prefabIndex < 0 || prefabIndex >= chapterMapPrefabs.Length)
        {
            // AC7: Missing asset fallback
            Debug.LogWarning($"[WorldMapManager] Chapter {chapterNumber} map prefab index out of bounds (index: {prefabIndex}, array length: {chapterMapPrefabs.Length}), using default display");
            ShowFallback(chapterNumber);
            return;
        }

        GameObject mapPrefab = chapterMapPrefabs[prefabIndex];

        // Check if prefab is assigned (not null)
        if (mapPrefab == null)
        {
            // AC7: Missing asset fallback
            Debug.LogWarning($"[WorldMapManager] Chapter {chapterNumber} map prefab not assigned, using default display");
            ShowFallback(chapterNumber);
            return;
        }

        // Instantiate map prefab at mapDisplayParent (AC2)
        if (mapDisplayParent != null)
        {
            GameObject instantiatedMap = Instantiate(mapPrefab, mapDisplayParent);
            Debug.Log($"[WorldMapManager] Chapter {chapterNumber} map instantiated successfully");

            // Hide fallback message if it was visible
            if (fallbackMessageText != null)
            {
                fallbackMessageText.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("[WorldMapManager] mapDisplayParent is null! Cannot instantiate map prefab.");
            ShowFallback(chapterNumber);
        }
    }

    /// <summary>
    /// Set chapter title and description text based on chapter number.
    /// Uses hardcoded arrays (MVP implementation).
    /// AC3: Chapter title and description display with proper TextMeshPro styling.
    /// </summary>
    /// <param name="chapterNumber">1-based chapter number</param>
    private void SetChapterText(int chapterNumber)
    {
        int textIndex = chapterNumber - 1; // Convert to 0-based index

        // Set chapter title (AC3)
        if (chapterTitleText != null)
        {
            if (textIndex >= 0 && textIndex < chapterTitles.Length)
            {
                chapterTitleText.text = chapterTitles[textIndex];
                Debug.Log($"[WorldMapManager] Chapter title: {chapterTitles[textIndex]}");
            }
            else
            {
                chapterTitleText.text = $"Chapter {chapterNumber}";
                Debug.LogWarning($"[WorldMapManager] Chapter {chapterNumber} title not found in array, using default");
            }
        }
        else
        {
            Debug.LogError("[WorldMapManager] chapterTitleText is null!");
        }

        // Set chapter description (AC3)
        if (chapterDescriptionText != null)
        {
            if (textIndex >= 0 && textIndex < chapterDescriptions.Length)
            {
                chapterDescriptionText.text = chapterDescriptions[textIndex];
            }
            else
            {
                chapterDescriptionText.text = "Lore unavailable.";
                Debug.LogWarning($"[WorldMapManager] Chapter {chapterNumber} description not found in array, using default");
            }
        }
        else
        {
            Debug.LogError("[WorldMapManager] chapterDescriptionText is null!");
        }
    }

    /// <summary>
    /// Show fallback text-only display when map prefab is missing.
    /// AC7: Display placeholder text, chapter title/description still visible, Wake Up button functional.
    /// </summary>
    /// <param name="chapterNumber">1-based chapter number</param>
    private void ShowFallback(int chapterNumber)
    {
        // Hide map display area (AC7)
        if (mapDisplayParent != null)
        {
            // Clear any existing children (in case of re-display)
            foreach (Transform child in mapDisplayParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Show fallback message (AC7)
        if (fallbackMessageText != null)
        {
            fallbackMessageText.text = $"Map artwork unavailable - Chapter {chapterNumber}";
            fallbackMessageText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[WorldMapManager] fallbackMessageText is null! Cannot display fallback message.");
        }

        // Chapter title and description are still set by SetChapterText() (AC7)
        // Wake Up button remains functional (no code changes needed) (AC7)
    }
}
