using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    public bool hasSpawnedAlready = false;

    private void Start()
    {
    }

    public void SpawnEnemies()
    {
        if (hasSpawnedAlready) return;

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            if (spawnEntries[i].spawnOnStart) Spawn(i);
        }

        hasSpawnedAlready = true;
    }

    public void Spawn(int index)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.oneTimeSpawn && entry.hasSpawned) return;

        if (entry.enemyPrefab == null || entry.spawnPoint == null) return;

        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Destroy(enemyObj);
            return;
        }

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint);

        entry.hasSpawned = true;
        entry.spawnedEnemy = enemy;
    }

    public void Respawn(int index)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.oneTimeSpawn) return;

        Spawn(index);
    }

    public void SpawnAll()
    {
        for (int i = 0; i < spawnEntries.Count; i++) Spawn(i);
    }
}

