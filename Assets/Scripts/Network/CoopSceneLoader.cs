using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// Handles FishNet scene management for co-op.
/// Registers the gameplay scene as a global scene when the server starts
/// so that connecting clients are automatically loaded into it.
/// </summary>
public class CoopSceneLoader : MonoBehaviour
{
    public static CoopSceneLoader Instance { get; private set; }

    public bool IsSceneRegistered { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Register the current gameplay scene as a FishNet global scene.
    /// Called when the host opens LAN from in-game.
    /// </summary>
    public void RegisterCurrentScene()
    {
        if (InstanceFinder.NetworkManager == null || !InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("[CoopSceneLoader] Cannot register scene - server not started");
            return;
        }

        string currentSceneName = UnitySceneManager.GetActiveScene().name;

        SceneLoadData sld = new SceneLoadData(currentSceneName);
        sld.ReplaceScenes = ReplaceOption.None;

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
        IsSceneRegistered = true;

        Debug.Log($"[CoopSceneLoader] Registered global scene: {currentSceneName}");
    }

    /// <summary>
    /// Unregister the global scene when closing LAN.
    /// </summary>
    public void UnregisterScene()
    {
        IsSceneRegistered = false;
        Debug.Log("[CoopSceneLoader] Scene unregistered");
    }

    /// <summary>
    /// Load a new scene for all connected players (chapter transitions).
    /// </summary>
    public void LoadNetworkedScene(string sceneName)
    {
        if (InstanceFinder.NetworkManager == null || !InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("[CoopSceneLoader] Cannot load networked scene - server not started");
            return;
        }

        SceneLoadData sld = new SceneLoadData(sceneName);
        sld.ReplaceScenes = ReplaceOption.All;

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);

        Debug.Log($"[CoopSceneLoader] Loading networked scene for all players: {sceneName}");
    }
}
