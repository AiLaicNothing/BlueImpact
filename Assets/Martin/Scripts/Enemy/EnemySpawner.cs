using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyHolders;
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    public bool hasSpawnedAlready = false;

    private readonly List<EnemyBase> activeEnemies = new();

    // Objeto raiz de cada enemigo creado (el EnemyBase puede estar en un hijo)
    private readonly List<GameObject> spawnedRoots = new();

    public event Action<EnemyBase> OnEnemySpawned;

    // Enemigos de este spawner que siguen existiendo (los muertos se destruyen)
    public int AliveCount
    {
        get
        {
            activeEnemies.RemoveAll(e => e == null);
            return activeEnemies.Count;
        }
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

        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation, GetHolder());

        EnemyBase enemy = enemyObj.GetComponentInChildren<EnemyBase>();
        if (enemy == null)
        {
            Destroy(enemyObj);
            return;
        }

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint);

        entry.hasSpawned = true;
        entry.spawnedEnemy = enemy;

        Track(enemy, enemyObj);
    }

    // Para invocaciones (por ejemplo, los golems del boss): no depende de SpawnEntry
    public EnemyBase SpawnAt(GameObject prefab, Transform spawnPoint, Transform safePoint)
    {
        if (prefab == null || spawnPoint == null) return null;

        GameObject obj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, GetHolder());

        EnemyBase enemy = obj.GetComponentInChildren<EnemyBase>();
        if (enemy == null)
        {
            Destroy(obj);
            return null;
        }

        enemy.OnSpawn(null, safePoint != null ? safePoint : spawnPoint, false, spawnPoint);

        Track(enemy, obj);
        return enemy;
    }

    // Destruye a todos los enemigos que creo este spawner y que siguen vivos.
    // Lo usa el boss al reiniciarse: los invocados no deben quedarse en la sala.
    public void DespawnAll()
    {
        foreach (GameObject root in spawnedRoots)
        {
            if (root != null) Destroy(root);
        }

        spawnedRoots.Clear();
        activeEnemies.Clear();

        ResetSpawnState();
    }

    // Permite volver a crear las entradas aunque sean oneTimeSpawn.
    // Necesario para invocaciones que se repiten (Climax mas de una vez, o tras morir el jugador).
    public void ResetSpawnState()
    {
        hasSpawnedAlready = false;

        foreach (SpawnEntry entry in spawnEntries)
        {
            entry.hasSpawned = false;
            entry.spawnedEnemy = null;
        }
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

    private Transform GetHolder()
    {
        return enemyHolders != null ? enemyHolders.transform : transform;
    }

    private void Track(EnemyBase enemy, GameObject root)
    {
        activeEnemies.Add(enemy);

        spawnedRoots.RemoveAll(r => r == null);
        spawnedRoots.Add(root);

        OnEnemySpawned?.Invoke(enemy);
    }
}