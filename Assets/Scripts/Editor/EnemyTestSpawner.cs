using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool for testing enemy spawning
/// </summary>
public class EnemyTestSpawner : EditorWindow
{
    private EnemyData selectedEnemy;
    private Vector2 spawnPosition = Vector2.zero;

    [MenuItem("Tools/Enemy Test Spawner")]
    public static void ShowWindow()
    {
        GetWindow<EnemyTestSpawner>("Enemy Spawner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enemy Test Spawner", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to spawn enemies!", MessageType.Warning);
            return;
        }

        selectedEnemy = (EnemyData)EditorGUILayout.ObjectField("Enemy Data", selectedEnemy, typeof(EnemyData), false);
        spawnPosition = EditorGUILayout.Vector2Field("Spawn Position", spawnPosition);

        GUILayout.Space(10);

        if (GUILayout.Button("Spawn Enemy"))
        {
            SpawnEnemy();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Spawn at Random Edge"))
        {
            SpawnAtRandomEdge();
        }

        GUILayout.Space(10);
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Spawn Wave (5 enemies)"))
        {
            SpawnWave(5);
        }

        if (GUILayout.Button("Clear All Enemies"))
        {
            ClearAllEnemies();
        }

        GUILayout.Space(10);
        GUILayout.Label("Debug Info", EditorStyles.boldLabel);

        if (EnemyManager.Instance != null)
        {
            EditorGUILayout.LabelField("Active Enemies:", EnemyManager.Instance.ActiveEnemyCount.ToString());
            EditorGUILayout.LabelField("Current Wave:", EnemyManager.Instance.CurrentWave.ToString());
            EditorGUILayout.LabelField("Enemies Killed:", EnemyManager.Instance.EnemiesKilled.ToString());
        }
        else
        {
            EditorGUILayout.HelpBox("EnemyManager not found!", MessageType.Error);
        }

        // Auto-repaint while playing
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void SpawnEnemy()
    {
        if (selectedEnemy == null)
        {
            Debug.LogError("[EnemyTestSpawner] No enemy data selected!");
            return;
        }

        if (EnemyManager.Instance == null)
        {
            Debug.LogError("[EnemyTestSpawner] EnemyManager not found!");
            return;
        }

        Vector3 pos = new Vector3(spawnPosition.x, spawnPosition.y, 0);
        Enemy enemy = EnemyManager.Instance.SpawnEnemy(selectedEnemy, pos);

        if (enemy != null)
        {
            Debug.Log($"[EnemyTestSpawner] Spawned {selectedEnemy.enemyName} at {pos}");
        }
    }

    private void SpawnAtRandomEdge()
    {
        if (selectedEnemy == null)
        {
            Debug.LogError("[EnemyTestSpawner] No enemy data selected!");
            return;
        }

        // Spawn at random edge of camera view
        Camera cam = Camera.main;
        if (cam == null) return;

        float edge = Random.Range(0, 4);
        Vector3 pos;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        switch ((int)edge)
        {
            case 0: // Top
                pos = new Vector3(Random.Range(camPos.x - halfWidth, camPos.x + halfWidth), camPos.y + halfHeight + 2, 0);
                break;
            case 1: // Bottom
                pos = new Vector3(Random.Range(camPos.x - halfWidth, camPos.x + halfWidth), camPos.y - halfHeight - 2, 0);
                break;
            case 2: // Left
                pos = new Vector3(camPos.x - halfWidth - 2, Random.Range(camPos.y - halfHeight, camPos.y + halfHeight), 0);
                break;
            default: // Right
                pos = new Vector3(camPos.x + halfWidth + 2, Random.Range(camPos.y - halfHeight, camPos.y + halfHeight), 0);
                break;
        }

        spawnPosition = new Vector2(pos.x, pos.y);
        SpawnEnemy();
    }

    private void SpawnWave(int count)
    {
        if (selectedEnemy == null)
        {
            Debug.LogError("[EnemyTestSpawner] No enemy data selected!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnAtRandomEdge();
        }
    }

    private void ClearAllEnemies()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.ClearAllEnemies();
            Debug.Log("[EnemyTestSpawner] Cleared all enemies");
        }
    }
}
