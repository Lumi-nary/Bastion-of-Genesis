using UnityEngine;

/// <summary>
/// GameSettings stores global game configuration data.
/// Addresses professor feedback on trademark issue - game title is easily configurable.
/// Implements singleton pattern via Resources.Load for easy access across codebase.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Planetfall/Game Settings", order = 1)]
public class GameSettings : ScriptableObject
{
    private static GameSettings _instance;

    /// <summary>
    /// Singleton accessor - loads GameSettings from Resources folder.
    /// Returns cached instance after first load for performance.
    /// </summary>
    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameSettings>("GameSettings");

                if (_instance == null)
                {
                    Debug.LogError("[GameSettings] GameSettings.asset not found in Resources folder. Please create it via: Assets → Create → Planetfall → Game Settings");
                }
            }
            return _instance;
        }
    }

    [Header("Game Information")]
    [Tooltip("Game title displayed on main menu. Configurable to address trademark concerns.")]
    public string gameTitle = "Planetfall";

    [Tooltip("Current game version (e.g., 0.1.0-alpha)")]
    public string gameVersion = "0.1.0-alpha";

    [Tooltip("Development team or company name")]
    public string companyName = "Your Team Name";

    [Header("Credits (Epic 6)")]
    [Tooltip("Team member names for credits screen. Size: 6 members.")]
    public string[] credits = new string[6]
    {
        "Team Member 1",
        "Team Member 2",
        "Team Member 3",
        "Team Member 4",
        "Team Member 5",
        "Team Member 6"
    };

    /// <summary>
    /// Get formatted credits text for display (Epic 6 Story 6.1).
    /// Joins all credits entries with newlines.
    /// </summary>
    public string GetCreditsText()
    {
        return string.Join("\n", credits);
    }

    /// <summary>
    /// Validate singleton is properly configured.
    /// Called in Unity Editor when asset is selected.
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(gameTitle))
        {
            Debug.LogWarning("[GameSettings] Game title is empty. Please set a title in the Inspector.");
        }
    }
}
