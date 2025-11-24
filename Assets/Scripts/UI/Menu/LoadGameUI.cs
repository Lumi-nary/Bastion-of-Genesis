using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// LoadGameUI manages the Load Game canvas and save browser.
/// Handles save list display, load/delete operations, and empty state.
/// Epic 3 Story 3.2 - LoadGameUI and SaveListItemUI.
/// Pattern 2: Scene-specific (NO DontDestroyOnLoad).
/// Pattern 7: All canvas switching via MenuManager.
/// </summary>
public class LoadGameUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private ScrollRect saveListScrollView;
    [SerializeField] private Transform saveListContent;
    [SerializeField] private GameObject saveListItemPrefab;
    [SerializeField] private GameObject emptyStatePanel;

    /// <summary>
    /// Start - Initial refresh after all Awake() methods complete.
    /// Ensures SaveManager is initialized before first refresh.
    /// </summary>
    private void Start()
    {
        // Refresh on Start to ensure SaveManager.Awake() has completed
        if (SaveManager.Instance != null && gameObject.activeInHierarchy)
        {
            RefreshSaveList();
        }
    }

    /// <summary>
    /// OnEnable - Auto-refresh save list when canvas becomes active (AC2).
    /// Pattern 2: Initialize in OnEnable() for canvas that may be disabled on scene load.
    /// </summary>
    private void OnEnable()
    {
        // Safety: Only refresh if SaveManager is initialized
        // (prevents errors if canvas is enabled before SaveManager.Awake runs)
        if (SaveManager.Instance != null)
        {
            RefreshSaveList();
        }
    }

    /// <summary>
    /// Refresh save list from disk (AC2, AC8).
    /// Scans filesystem and populates scroll view with SaveListItemUI prefabs.
    /// Performance target: <500ms for up to 100 saves (NFR-1).
    /// </summary>
    public void RefreshSaveList()
    {
        // Validate SaveManager exists
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LoadGameUI] SaveManager.Instance is null - cannot refresh save list");
            ShowEmptyState();
            return;
        }

        // AC2.2: Call SaveManager.GetAllSaves() to scan filesystem
        List<SaveMetadata> saves = SaveManager.Instance.GetAllSaves();

        // Clear existing save list items (AC: Clear list before refresh)
        ClearSaveList();

        // AC8: Show/hide empty state based on list count
        if (saves == null || saves.Count == 0)
        {
            ShowEmptyState();
            Debug.Log("[LoadGameUI] No saves found, showing empty state");
            return;
        }
        else
        {
            HideEmptyState();
        }

        // AC2.3: Instantiate SaveListItemUI prefab for each save
        foreach (SaveMetadata metadata in saves)
        {
            if (saveListItemPrefab != null && saveListContent != null)
            {
                GameObject itemObject = Instantiate(saveListItemPrefab, saveListContent);
                SaveListItemUI itemUI = itemObject.GetComponent<SaveListItemUI>();

                if (itemUI != null)
                {
                    itemUI.Initialize(metadata, this);
                }
                else
                {
                    Debug.LogError("[LoadGameUI] SaveListItemUI component missing on prefab");
                }
            }
        }

        // AC2.4: Log refresh completion
        Debug.Log($"[LoadGameUI] Save list refreshed: {saves.Count} saves displayed");
    }

    /// <summary>
    /// Clear all save list items from scroll view.
    /// Pattern: Destroy children of saveListContent transform.
    /// </summary>
    private void ClearSaveList()
    {
        if (saveListContent == null)
            return;

        // Destroy all child GameObjects
        foreach (Transform child in saveListContent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Show empty state panel (AC8).
    /// Displays "No saves found" message and hint text.
    /// </summary>
    private void ShowEmptyState()
    {
        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(true);
        }

        if (saveListScrollView != null)
        {
            saveListScrollView.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hide empty state panel (AC8).
    /// Shows scroll view with save list.
    /// </summary>
    private void HideEmptyState()
    {
        if (emptyStatePanel != null)
        {
            emptyStatePanel.SetActive(false);
        }

        if (saveListScrollView != null)
        {
            saveListScrollView.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Handle save load button click (AC5, AC6).
    /// Called by SaveListItemUI when user clicks Load button.
    /// </summary>
    /// <param name="metadata">Save metadata to load</param>
    public void OnSaveClicked(SaveMetadata metadata)
    {
        if (metadata == null)
        {
            Debug.LogError("[LoadGameUI] OnSaveClicked called with null metadata");
            return;
        }

        Debug.Log($"[LoadGameUI] Loading save: {metadata.fileName}");

        // AC6: COOP auto-start integration
        if (metadata.mode == GameMode.COOP)
        {
            // Check if PlanetfallNetworkManager exists (brownfield - may not be implemented yet)
            // TODO: Epic 9 - Implement PlanetfallNetworkManager.StartHost() for COOP saves

            // For now, just log the COOP intent
            Debug.LogWarning("[LoadGameUI] COOP save detected, but PlanetfallNetworkManager not yet implemented (Epic 9)");
            Debug.Log($"[LoadGameUI] Starting COOP server for save: {metadata.baseName}");
        }

        // AC5.3: Call SaveManager.LoadGame()
        bool loadSuccess = SaveManager.Instance.LoadGame(metadata.fileName);

        if (loadSuccess)
        {
            // AC5.4: Load GameWorld scene asynchronously
            Debug.Log("[LoadGameUI] Save loaded successfully, transitioning to GameWorld scene");
            SceneManager.LoadSceneAsync("GameWorld");
        }
        else
        {
            // AC5.4: Show error modal (Story 3.3)
            Debug.LogError($"[LoadGameUI] Failed to load save: {metadata.fileName}");

            // Story 3.3: Show error modal for load failure
            if (ModalDialog.Instance != null)
            {
                ModalDialog.Instance.ShowError("Save file corrupted. Unable to load.");
            }
        }
    }

    /// <summary>
    /// Handle save delete button click (AC7).
    /// Called by SaveListItemUI when user clicks Delete button.
    /// Shows confirmation modal before deletion.
    /// </summary>
    /// <param name="metadata">Save metadata to delete</param>
    public void OnDeleteClicked(SaveMetadata metadata)
    {
        if (metadata == null)
        {
            Debug.LogError("[LoadGameUI] OnDeleteClicked called with null metadata");
            return;
        }

        Debug.Log($"[LoadGameUI] Delete requested for: {metadata.fileName}");

        // AC7.2: Show confirmation modal (Story 3.3)
        if (ModalDialog.Instance != null)
        {
            string confirmMessage = $"Delete save '{metadata.baseName}'? This cannot be undone.";
            ModalDialog.Instance.ShowConfirmation(
                message: confirmMessage,
                onConfirm: () => ConfirmDelete(metadata),
                onCancel: () => Debug.Log($"[LoadGameUI] Delete cancelled for: {metadata.fileName}")
            );
        }
        else
        {
            Debug.LogError("[LoadGameUI] ModalDialog.Instance is null - cannot show confirmation");
        }
    }

    /// <summary>
    /// Confirm delete operation after user confirmation.
    /// AC7.3: Calls SaveManager.DeleteSave() and refreshes list.
    /// </summary>
    /// <param name="metadata">Save metadata to delete</param>
    private void ConfirmDelete(SaveMetadata metadata)
    {
        // AC7.3: Call SaveManager.DeleteSave()
        bool deleteSuccess = SaveManager.Instance.DeleteSave(metadata.fileName);

        if (deleteSuccess)
        {
            Debug.Log($"[LoadGameUI] Save deleted successfully: {metadata.fileName}");

            // AC7.3: Refresh list after deletion
            RefreshSaveList();
        }
        else
        {
            Debug.LogError($"[LoadGameUI] Failed to delete save: {metadata.fileName}");

            // Story 3.3: Show error modal
            if (ModalDialog.Instance != null)
            {
                ModalDialog.Instance.ShowError($"Failed to delete save '{metadata.baseName}'. The file may be in use or locked.");
            }
        }
    }
}
