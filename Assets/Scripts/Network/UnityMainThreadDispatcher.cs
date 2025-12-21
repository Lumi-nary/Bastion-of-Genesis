using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UnityMainThreadDispatcher - Allows background threads to execute code on Unity's main thread.
/// Required for LANDiscovery to safely invoke Unity APIs from network threads.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    public static UnityMainThreadDispatcher Instance { get; private set; }

    private readonly Queue<Action> pendingActions = new Queue<Action>();
    private readonly object queueLock = new object();

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
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // Process all pending actions
        lock (queueLock)
        {
            while (pendingActions.Count > 0)
            {
                Action action = pendingActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Action failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Queue an action to be executed on the main thread.
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null) return;

        lock (queueLock)
        {
            pendingActions.Enqueue(action);
        }
    }
}
