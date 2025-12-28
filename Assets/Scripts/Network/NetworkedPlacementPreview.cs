using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NetworkedPlacementPreview - Displays other players' building placement previews.
/// Shows a ghost/preview of what building other players are about to place.
/// </summary>
public class NetworkedPlacementPreview : MonoBehaviour
{
    public static NetworkedPlacementPreview Instance { get; private set; }

    [Header("Preview Material")]
    [SerializeField] private Material otherPlayerPreviewMaterial;
    [SerializeField] private Color previewColor = new Color(0.3f, 0.5f, 1f, 0.5f); // Blue tint

    // Track preview objects for each player
    private Dictionary<NetworkPlayer, GameObject> playerPreviews = new Dictionary<NetworkPlayer, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to existing players
        foreach (var player in NetworkPlayer.AllPlayers)
        {
            SubscribeToPlayer(player);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from all players
        foreach (var player in NetworkPlayer.AllPlayers)
        {
            UnsubscribeFromPlayer(player);
        }

        // Clear all previews
        ClearAllPreviews();
    }

    private void Update()
    {
        // Check for new players that we haven't subscribed to yet
        foreach (var player in NetworkPlayer.AllPlayers)
        {
            if (!playerPreviews.ContainsKey(player) && player != NetworkPlayer.LocalPlayer)
            {
                SubscribeToPlayer(player);
            }
        }

        // Update preview positions for all players who are placing
        foreach (var kvp in playerPreviews)
        {
            NetworkPlayer player = kvp.Key;
            GameObject preview = kvp.Value;

            if (player != null && player.IsPlacingBuilding && preview != null)
            {
                preview.transform.position = player.PlacingPreviewPosition;
            }
        }
    }

    private void SubscribeToPlayer(NetworkPlayer player)
    {
        if (player == null || player == NetworkPlayer.LocalPlayer) return;

        player.OnPlacementPreviewChanged += OnPlayerPreviewChanged;
        
        // Initialize with null preview
        if (!playerPreviews.ContainsKey(player))
        {
            playerPreviews[player] = null;
        }

        // If player is already placing, create preview
        if (player.IsPlacingBuilding)
        {
            OnPlayerPreviewChanged(player);
        }
    }

    private void UnsubscribeFromPlayer(NetworkPlayer player)
    {
        if (player == null) return;

        player.OnPlacementPreviewChanged -= OnPlayerPreviewChanged;
    }

    private void OnPlayerPreviewChanged(NetworkPlayer player)
    {
        if (player == null || player == NetworkPlayer.LocalPlayer) return;

        if (player.IsPlacingBuilding)
        {
            // Player started or is placing - create/update preview
            CreateOrUpdatePreview(player);
        }
        else
        {
            // Player stopped placing - remove preview
            RemovePreview(player);
        }
    }

    private void CreateOrUpdatePreview(NetworkPlayer player)
    {
        int buildingIndex = player.PlacingBuildingIndex;
        if (buildingIndex < 0) return;

        // Get building data from NetworkedBuildingManager
        BuildingData buildingData = null;
        if (NetworkedBuildingManager.Instance != null)
        {
            buildingData = NetworkedBuildingManager.Instance.GetBuildingDataByIndex(buildingIndex);
        }

        if (buildingData == null || buildingData.prefab == null) return;

        // Check if we need to create or recreate the preview
        bool needsNewPreview = false;

        if (!playerPreviews.ContainsKey(player) || playerPreviews[player] == null)
        {
            needsNewPreview = true;
        }
        else
        {
            // Check if building type changed
            GameObject currentPreview = playerPreviews[player];
            if (currentPreview.name != $"OtherPlayerPreview_{player.PlayerId}_{buildingData.buildingName}")
            {
                // Different building, recreate
                Destroy(currentPreview);
                needsNewPreview = true;
            }
        }

        if (needsNewPreview)
        {
            // Create new preview from building prefab
            GameObject preview = Instantiate(buildingData.prefab);
            preview.name = $"OtherPlayerPreview_{player.PlayerId}_{buildingData.buildingName}";

            // Disable colliders
            foreach (Collider2D col in preview.GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }

            // Disable Building component
            Building previewBuilding = preview.GetComponent<Building>();
            if (previewBuilding != null)
            {
                previewBuilding.enabled = false;
            }

            // Apply preview material/color
            ApplyPreviewMaterial(preview);

            playerPreviews[player] = preview;
        }

        // Update position
        if (playerPreviews.TryGetValue(player, out GameObject previewObj) && previewObj != null)
        {
            previewObj.transform.position = player.PlacingPreviewPosition;
            previewObj.SetActive(true);
        }
    }

    private void ApplyPreviewMaterial(GameObject preview)
    {
        SpriteRenderer[] renderers = preview.GetComponentsInChildren<SpriteRenderer>();
        
        foreach (SpriteRenderer renderer in renderers)
        {
            if (otherPlayerPreviewMaterial != null)
            {
                renderer.material = otherPlayerPreviewMaterial;
            }
            else
            {
                // Create a semi-transparent blue tinted version
                renderer.color = previewColor;
            }
        }
    }

    private void RemovePreview(NetworkPlayer player)
    {
        if (playerPreviews.TryGetValue(player, out GameObject preview))
        {
            if (preview != null)
            {
                Destroy(preview);
            }
            playerPreviews[player] = null;
        }
    }

    private void ClearAllPreviews()
    {
        foreach (var kvp in playerPreviews)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        playerPreviews.Clear();
    }

    /// <summary>
    /// Called when a player disconnects to clean up their preview
    /// </summary>
    public void OnPlayerDisconnected(NetworkPlayer player)
    {
        UnsubscribeFromPlayer(player);
        RemovePreview(player);
        playerPreviews.Remove(player);
    }
}
