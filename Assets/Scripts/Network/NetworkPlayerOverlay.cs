using UnityEngine;
using TMPro;

/// <summary>
/// Shows a small overlay label identifying this player as HOST or PLAYER 2.
/// Attach to a Canvas in the gameplay scene.
/// </summary>
public class NetworkPlayerOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerLabel;

    private void Start()
    {
        UpdateLabel();
    }

    private void OnEnable()
    {
        // Retry every second in case network state isn't ready yet
        InvokeRepeating(nameof(UpdateLabel), 0.5f, 2f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(UpdateLabel));
    }

    private void UpdateLabel()
    {
        if (playerLabel == null) return;

        if (NetworkGameManager.Instance == null || !NetworkGameManager.Instance.IsOnline)
        {
            playerLabel.text = "SINGLEPLAYER";
            playerLabel.color = Color.white;
            return;
        }

        if (NetworkGameManager.Instance.IsHost)
        {
            playerLabel.text = "HOST";
            playerLabel.color = Color.cyan;
        }
        else if (NetworkGameManager.Instance.IsClient)
        {
            int playerId = NetworkPlayer.LocalPlayer != null ? NetworkPlayer.LocalPlayer.PlayerId : -1;
            playerLabel.text = $"PLAYER {playerId + 1}";
            playerLabel.color = Color.green;
        }
        else
        {
            playerLabel.text = "OFFLINE";
            playerLabel.color = Color.gray;
        }

        // Also show sync status
        bool hasResources = NetworkedResourceManager.Instance != null && NetworkedResourceManager.Instance.IsSpawned;
        bool hasBuildings = NetworkedBuildingManager.Instance != null && NetworkedBuildingManager.Instance.IsSpawned;
        bool hasWorkers = NetworkedWorkerManager.Instance != null && NetworkedWorkerManager.Instance.IsSpawned;

        string syncStatus = $"\nSync: R:{(hasResources ? "OK" : "X")} B:{(hasBuildings ? "OK" : "X")} W:{(hasWorkers ? "OK" : "X")}";
        playerLabel.text += syncStatus;
    }
}
