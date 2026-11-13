using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, ILevelSpawnSource
{
    [SerializeField] private GameObject enemyHolders;
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    public bool hasSpawnedAlready = false;

    private readonly List<EnemyBase> activeEnemies = new();

    // Objeto raiz de cada enemigo creado (el EnemyBase puede estar en un hijo)
    private readonly List<GameObject> spawnedRoots = new();

    public event Action<EnemyBase> OnEnemySpawned;

    private void OnEnable()
    {
        LevelObjectiveManager.OnKillsReverted += HandleKillsReverted;
    }

    private void OnDisable()
    {
        LevelObjectiveManager.OnKillsReverted -= HandleKillsReverted;
    }

    // Al morir el jugador (después de que el manager revierte los kills pendientes):
    // se destruyen los enemigos que queden y se recrean en su punto de spawn con la vida completa.
    // Los slots ya derrotados y fijados en un checkpoint no se recrean.
    // Solo spawners activados por SpawnerManager (no oleadas del boss, que tienen su propio reinicio).
    private void HandleKillsReverted()
    {
        if (!hasSpawnedAlready || CountLevelEnemies() == 0) return;

        List<int> toRespawn = new();
        for (int i = 0; i < spawnEntries.Count; i++)
            if (spawnEntries[i].hasSpawned) toRespawn.Add(i);

        DespawnAll();

        foreach (int i in toRespawn) SpawnSlot(i, true);
        hasSpawnedAlready = true;

        Debug.Log($"[EnemySpawner] {name}: jugador murió, enemigos recreados ({AliveCount}).", this);
    }

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
            if (spawnEntries[i].spawnOnStart) SpawnSlot(i, true);
        }

        hasSpawnedAlready = true;
    }

    public void Spawn(int index)
    {
        SpawnSlot(index, false);
    }

    // skipDefeated: solo para las salas normales (SpawnerManager y reinicio al morir).
    // Las oleadas del boss (WaveManager -> SpawnAll) siempre crean a sus enemigos.
    private void SpawnSlot(int index, bool skipDefeated)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.oneTimeSpawn && entry.hasSpawned) return;

        if (entry.enemyPrefab == null || entry.spawnPoint == null) return;

        // Slot ya derrotado (fijo o pendiente): no se crea. Se marca como usado para que,
        // si el jugador muere y el kill se revierte, el spawner lo vuelva a crear.
        if (skipDefeated && entry.countsForLevel &&
            LevelObjectiveManager.IsSlotDefeated(gameObject.scene, LevelObjectiveManager.PathOf(transform), index))
        {
            entry.hasSpawned = true;
            return;
        }

        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation, GetHolder());

        EnemyBase enemy = enemyObj.GetComponentInChildren<EnemyBase>();
        if (enemy == null)
        {
            Destroy(enemyObj);
            return;
        }

        // Cada entrada es un "slot" del nivel. Si reaparece (oneTimeSpawn = false), sigue ocupando
        // el mismo slot: LevelObjectiveManager cuenta una sola vez por slot.
        // resetsOnDeath = true: si el jugador muere antes de desbloquear la salida, estos kills
        // se pierden (los enemigos reaparecen y hay que volver a matarlos).
        LevelObjectiveManager.AssignObjective(enemy, gameObject.scene,
            LevelObjectiveManager.PathOf(transform), index,
            entry.countsForLevel, true);

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint);

        entry.hasSpawned = true;
        entry.spawnedEnemy = enemy;

        Track(enemy, enemyObj);
    }

    // Slots contables del spawner: una por entrada con prefab y countsForLevel.
    // No depende de oneTimeSpawn: las entradas que reaparecen también cuentan como un slot.
    public int CountLevelEnemies()
    {
        int n = 0;
        foreach (SpawnEntry e in spawnEntries)
            if (e.enemyPrefab != null && e.countsForLevel) n++;
        return n;
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

        // Invocaciones (boss): no cuentan para el nivel.
        LevelObjectiveManager.AssignObjective(enemy, gameObject.scene, null, 0, false, false);

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