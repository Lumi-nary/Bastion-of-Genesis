using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBuildingTurretDebugSpawner : MonoBehaviour
{
    [SerializeField] private List<EnemyData> enemyTypes = new List<EnemyData>();
    [SerializeField] private List<Vector3> spawnPositions = new List<Vector3>();
    [SerializeField] private float spawnDelay = 0.25f;
    [SerializeField] private bool clearExistingEnemies = true;

    private IEnumerator Start()
    {
        if (!Application.isPlaying)
        {
            yield break;
        }

        float deadline = Time.time + 2f;
        while (EnemyManager.Instance == null && Time.time < deadline)
        {
            yield return null;
        }

        if (spawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnDelay);
        }

        if (EnemyManager.Instance == null)
        {
            Debug.LogWarning("[EnemyBuildingTurretDebugSpawner] EnemyManager is missing; no debug enemies spawned.");
            yield break;
        }

        if (enemyTypes.Count == 0 || spawnPositions.Count == 0)
        {
            Debug.LogWarning("[EnemyBuildingTurretDebugSpawner] Enemy types or spawn positions are empty.");
            yield break;
        }

        if (clearExistingEnemies)
        {
            EnemyManager.Instance.ClearAllEnemies();
        }

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            EnemyData enemyData = enemyTypes[i % enemyTypes.Count];
            if (enemyData == null)
            {
                continue;
            }

            EnemyManager.Instance.SpawnEnemy(enemyData, spawnPositions[i]);
        }
    }
}
