using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// NetworkPlayer - Represents a connected player in the network.
/// Spawned for each connected client, handles player-specific networking.
/// FishNet 4.x compatible.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    // Synced state (FishNet 4.x SyncVar<T>)
    private readonly SyncVar<string> _playerName = new SyncVar<string>("Player");
    private readonly SyncVar<int> _playerId = new SyncVar<int>();
    private readonly SyncVar<bool> _isReady = new SyncVar<bool>();

    // Local reference
    public static NetworkPlayer LocalPlayer { get; private set; }

    // Properties
    public string PlayerName => _playerName.Value;
    public int PlayerId => _playerId.Value;
    public bool IsReady => _isReady.Value;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _playerName.OnChange += OnPlayerNameChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _playerName.OnChange -= OnPlayerNameChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[NetworkPlayer] Spawning on Client! Owner: {OwnerId}");

        if (IsOwner)
        {
            LocalPlayer = this;
            Debug.Log($"[NetworkPlayer] Local player initialized: {_playerName.Value}");

            string name = SaveManager.Instance?.pendingBaseName ?? $"Player {OwnerId}";
            CmdSetPlayerName(name);

            // Start Sync Debug Loop
            StartCoroutine(DebugSyncLoop());
        }
    }

    private System.Collections.IEnumerator DebugSyncLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            Debug.Log($"[NetworkPlayer] Client Sync Active. I am Owner: {IsOwner}, PlayerId: {PlayerId}, Name: {PlayerName}");
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner && LocalPlayer == this)
        {
            LocalPlayer = null;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _playerId.Value = OwnerId;
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} spawned on server");
    }

    private void OnPlayerNameChanged(string prev, string next, bool asServer)
    {
        Debug.Log($"[NetworkPlayer] Player name changed: {prev} -> {next}");
    }

    // ============================================================================
    // SERVER RPCS (Client -> Server)
    // ============================================================================

    [ServerRpc]
    public void CmdSetPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = $"Player {_playerId.Value}";

        if (name.Length > 20)
            name = name.Substring(0, 20);

        _playerName.Value = name;
        Debug.Log($"[NetworkPlayer] Server set player {_playerId.Value} name to: {name}");
    }

    [ServerRpc]
    public void CmdSetReady(bool ready)
    {
        _isReady.Value = ready;
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} ready: {ready}");
    }

    // ============================================================================
    // GAME ACTION RPCS
    // ============================================================================

    [ServerRpc]
    public void CmdRequestBuildingPlacement(int buildingIndex, Vector3 position)
    {
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} requesting building {buildingIndex} at {position}");

        if (NetworkedBuildingManager.Instance != null)
        {
            NetworkedBuildingManager.Instance.ServerPlaceBuilding(buildingIndex, position, this);
        }
    }

    [ServerRpc]
    public void CmdRequestBuildingRemoval(int buildingNetId)
    {
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} requesting removal of building {buildingNetId}");

        if (NetworkedBuildingManager.Instance != null)
        {
            NetworkedBuildingManager.Instance.ServerRemoveBuilding(buildingNetId, this);
        }
    }

    [ServerRpc]
    public void CmdRequestTrainWorker(int workerTypeIndex)
    {
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} requesting to train worker type {workerTypeIndex}");

        if (NetworkedWorkerManager.Instance != null)
        {
            NetworkedWorkerManager.Instance.ServerTrainWorker(workerTypeIndex, this);
        }
    }

    [ServerRpc]
    public void CmdRequestAssignWorkers(Vector2Int gridPos, int workerTypeIndex, int count)
    {
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} requesting to assign {count} workers to building at {gridPos}");

        if (NetworkedBuildingManager.Instance != null)
        {
            // For now only assign 1 at a time as per NetworkedBuildingManager logic
            for(int i=0; i<count; i++)
            {
                NetworkedBuildingManager.Instance.ServerAssignWorker(gridPos, workerTypeIndex, this);
            }
        }
    }

    [ServerRpc]
    public void CmdRequestRemoveWorkers(Vector2Int gridPos, int workerTypeIndex, int count)
    {
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} requesting to remove {count} workers from building at {gridPos}");

        if (NetworkedBuildingManager.Instance != null)
        {
            for(int i=0; i<count; i++)
            {
                NetworkedBuildingManager.Instance.ServerRemoveWorker(gridPos, workerTypeIndex, this);
            }
        }
    }

    // ============================================================================
    // TARGET RPCS (Server -> Specific Client)
    // ============================================================================

    [TargetRpc]
    public void TargetSendError(NetworkConnection conn, string message)
    {
        Debug.LogWarning($"[NetworkPlayer] Server error: {message}");

        if (ModalDialog.Instance != null)
        {
            ModalDialog.Instance.ShowError(message);
        }
    }

    [TargetRpc]
    public void TargetSendNotification(NetworkConnection conn, string title, string message)
    {
        Debug.Log($"[NetworkPlayer] Server notification: {title} - {message}");

        if (ModalDialog.Instance != null)
        {
            ModalDialog.Instance.ShowInfo(title, message);
        }
    }
}
