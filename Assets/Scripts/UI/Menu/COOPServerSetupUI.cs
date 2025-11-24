using UnityEngine;
using TMPro;

/// <summary>
/// COOPServerSetupUI controls the COOP server configuration panel.
/// Displays local IP/port and provides server start button.
/// Epic 2: Placeholder implementation (no actual FishNet integration).
/// Epic 9: Full FishNet ServerManager integration.
/// </summary>
public class COOPServerSetupUI : MonoBehaviour
{
    // ============================================================================
    // SERIALIZED FIELDS (Assigned in Unity Inspector)
    // ============================================================================

    [Header("COOP Server UI")]
    [SerializeField] private TextMeshProUGUI ipPortText;
    [SerializeField] private TextMeshProUGUI statusText;

    // ============================================================================
    // PRIVATE FIELDS
    // ============================================================================

    private bool serverStarted = false;

    // ============================================================================
    // LIFECYCLE METHODS
    // ============================================================================

    private void OnEnable()
    {
        // Reset state when panel becomes visible
        serverStarted = false;

        // Display placeholder IP/port (Epic 9: Get from NetworkManager)
        if (ipPortText != null)
        {
            ipPortText.text = "192.168.1.100:7777";
        }

        // Reset status text
        if (statusText != null)
        {
            statusText.text = "Configure Server";
        }

        Debug.Log("[COOPServerSetupUI] COOP server panel enabled");
    }

    // ============================================================================
    // PUBLIC API (Called by UI Buttons)
    // ============================================================================

    /// <summary>
    /// Start COOP server (placeholder for Epic 9 FishNet integration).
    /// Called by "Start Server" button onClick event.
    /// Epic 9: Will call FishNet ServerManager.StartConnection().
    /// </summary>
    public void OnStartServerClicked()
    {
        if (serverStarted)
        {
            Debug.LogWarning("[COOPServerSetupUI] Server already started");
            return;
        }

        // Placeholder: Update status text (Epic 9: Start actual FishNet server)
        serverStarted = true;

        if (statusText != null)
        {
            statusText.text = "Server Ready (Placeholder - Epic 9)";
        }

        Debug.Log("[COOPServerSetupUI] Server start placeholder - full FishNet integration in Epic 9");

        // Epic 9 Implementation:
        // if (FishNet.InstanceFinder.ServerManager != null)
        // {
        //     FishNet.InstanceFinder.ServerManager.StartConnection();
        //     statusText.text = "Server Ready";
        //     Debug.Log($"[COOPServerSetupUI] FishNet server started on {GetLocalIP()}:7777");
        // }
    }

    /// <summary>
    /// Stop COOP server (called by Back button or mode toggle).
    /// Epic 9: Will call FishNet ServerManager.StopConnection().
    /// </summary>
    public void StopServer()
    {
        if (!serverStarted)
        {
            return;
        }

        serverStarted = false;

        if (statusText != null)
        {
            statusText.text = "Configure Server";
        }

        Debug.Log("[COOPServerSetupUI] Server stop placeholder - full FishNet integration in Epic 9");

        // Epic 9 Implementation:
        // if (FishNet.InstanceFinder.ServerManager != null)
        // {
        //     FishNet.InstanceFinder.ServerManager.StopConnection(true);
        //     Debug.Log("[COOPServerSetupUI] FishNet server stopped");
        // }
    }

    // ============================================================================
    // HELPER METHODS (Epic 9)
    // ============================================================================

    /// <summary>
    /// Get local IP address for display (Epic 9 implementation).
    /// Currently returns placeholder value.
    /// </summary>
    /// <returns>Local IP address string</returns>
    private string GetLocalIP()
    {
        // Epic 9 Implementation:
        // try
        // {
        //     var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        //     foreach (var ip in host.AddressList)
        //     {
        //         if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        //         {
        //             return ip.ToString();
        //         }
        //     }
        // }
        // catch (System.Exception ex)
        // {
        //     Debug.LogError($"[COOPServerSetupUI] Failed to get local IP: {ex.Message}");
        // }

        return "192.168.1.100"; // Placeholder
    }

    private void OnDisable()
    {
        // Stop server when panel is hidden (mode toggled back to SP)
        StopServer();
    }
}
