using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbushEvent : MonoBehaviour
{
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    [SerializeField] private bool hasStarted;
    [SerializeField] private bool hasFinished;

    [SerializeField] private float doorSpeed;
    [SerializeField] private Transform[] doors;
    [SerializeField] private Transform[] closePos;
    [SerializeField] private Transform[] openPos;

    private List<GameObject> enemies = new();

    private IEnumerator Ambush()
    {
        hasStarted = true;

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        yield return MoveDoor(closePos);

        yield return StartWave();

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null);
            yield return null;
        }

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        yield return MoveDoor(openPos);

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        hasFinished = true;
    }

    private IEnumerator MoveDoor(Transform[] targets)
    {
        if (targets == null || targets.Length != doors.Length) yield break;

        bool moving = true;

        while (moving)
        {
            moving = false;

            for (int i = 0; i < doors.Length; i++)
            {
                Transform door = doors[i];
                Transform target = targets[i];

                if (door == null || target == null) continue;

                door.position = Vector3.MoveTowards(door.position, target.position, doorSpeed * Time.deltaTime);

                if (Vector3.Distance(door.position, target.position) > 0.01f) moving = true;
            }

            yield return null;
        }
    }

    private IEnumerator StartWave()
    {
        enemies.Clear();

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            Spawn(i);
        }

        yield break;
    }

    public void Spawn(int index)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.enemyPrefab == null || entry.spawnPoint == null) return;

        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol);

        entry.hasSpawned = true;
        entry.spawnedEnemy = enemy;

        enemies.Add(enemyObj);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasFinished || hasStarted) return;

        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(Ambush());
        }
    }
}
