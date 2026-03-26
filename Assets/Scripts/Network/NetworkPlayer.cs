using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
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

    // Placement preview state (synced to all clients)
    private readonly SyncVar<bool> _isPlacingBuilding = new SyncVar<bool>();
    private readonly SyncVar<int> _placingBuildingIndex = new SyncVar<int>(-1);
    private readonly SyncVar<Vector3> _placingPreviewPosition = new SyncVar<Vector3>();

    // Local reference
    public static NetworkPlayer LocalPlayer { get; private set; }

    // Properties
    public string PlayerName => _playerName.Value;
    public int PlayerId => _playerId.Value;
    public bool IsReady => _isReady.Value;

    // Placement preview properties
    public bool IsPlacingBuilding => _isPlacingBuilding.Value;
    public int PlacingBuildingIndex => _placingBuildingIndex.Value;
    public Vector3 PlacingPreviewPosition => _placingPreviewPosition.Value;

    // All players tracking (for preview visualization)
    public static List<NetworkPlayer> AllPlayers { get; } = new List<NetworkPlayer>();

    // Events for placement preview changes
    public event System.Action<NetworkPlayer> OnPlacementPreviewChanged;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _playerName.OnChange += OnPlayerNameChanged;
        _isPlacingBuilding.OnChange += OnPlacementStateChanged;
        _placingBuildingIndex.OnChange += OnPlacementIndexChanged;
        _placingPreviewPosition.OnChange += OnPlacementPositionChanged;

        if (!AllPlayers.Contains(this))
            AllPlayers.Add(this);
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _playerName.OnChange -= OnPlayerNameChanged;
        _isPlacingBuilding.OnChange -= OnPlacementStateChanged;
        _placingBuildingIndex.OnChange -= OnPlacementIndexChanged;
        _placingPreviewPosition.OnChange -= OnPlacementPositionChanged;

        AllPlayers.Remove(this);
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

    private void OnPlacementStateChanged(bool prev, bool next, bool asServer)
    {
        OnPlacementPreviewChanged?.Invoke(this);
    }

    private void OnPlacementIndexChanged(int prev, int next, bool asServer)
    {
        OnPlacementPreviewChanged?.Invoke(this);
    }

    private void OnPlacementPositionChanged(Vector3 prev, Vector3 next, bool asServer)
    {
        OnPlacementPreviewChanged?.Invoke(this);
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

    [ServerRpc]
    public void CmdStartPlacementPreview(int buildingIndex)
    {
        _isPlacingBuilding.Value = true;
        _placingBuildingIndex.Value = buildingIndex;
    }

    [ServerRpc]
    public void CmdUpdatePlacementPreviewPosition(Vector3 position)
    {
        if (_isPlacingBuilding.Value)
        {
            _placingPreviewPosition.Value = position;
        }
    }

    [ServerRpc]
    public void CmdStopPlacementPreview()
    {
        _isPlacingBuilding.Value = false;
        _placingBuildingIndex.Value = -1;
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
    public void CmdRequestAssembleWorker(int workerTypeIndex)
    {
        Debug.Log($"[NetworkPlayer] Player {_playerId.Value} requesting to assemble worker type {workerTypeIndex}");

        if (NetworkedWorkerManager.Instance != null)
        {
            NetworkedWorkerManager.Instance.ServerAssembleWorker(workerTypeIndex, this);
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
