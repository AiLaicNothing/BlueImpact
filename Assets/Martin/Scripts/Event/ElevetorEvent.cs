using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevetorEvent : MonoBehaviour
{
    [Header("Platform")]
    [SerializeField] private Transform plattform;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform topDestination;
    [SerializeField] private Transform stopDisAmbush;
    [SerializeField] private Transform bottonDestination;
    [SerializeField] private float reachThreshold = 0.05f;

    [Header("Ambush")]
    [SerializeField] private GameObject[] wave1_enemies;
    [SerializeField] private GameObject[] wave2_enemies;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform safePoint;

    [Header("Behavior")]
    [SerializeField] private bool triggerAmbushOnlyOnce = true;
    [SerializeField] private bool moveToTopOnReStart = false;

    private bool ambushCompleted;
    private bool eventRunning;
    private bool platformMoving;
    private Coroutine eventRoutine;

    private readonly List<GameObject> spawnedEnemies = new();

    private void Start()
    {
        if (plattform != null && topDestination != null)
        {
            plattform.position = topDestination.position;
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ActivateElevator();
        }
    }

    public void ActivateElevator()
    {
        if (eventRunning) return;

        eventRoutine = StartCoroutine(RunElevatorEvent());
    }

    private IEnumerator RunElevatorEvent()
    {
        eventRunning = true;

        Debug.Log("Elevator event started");

        if (!ambushCompleted && stopDisAmbush != null)
        {
            Debug.Log("Moving to ambush stop");
            yield return MovePlatformTo(stopDisAmbush.position);

            Debug.Log("Starting ambush");
            yield return StartCoroutine(Ambush());

            Debug.Log("Ambush done, moving down");
            yield return MovePlatformTo(bottonDestination.position);

            ambushCompleted = true;
        }
        else
        {
            if (topDestination != null) yield return MovePlatformTo(topDestination.position);
        }

        eventRunning = false;
    }

    private IEnumerator MovePlatformTo(Vector3 targetPosition)
    {
        if (plattform == null) yield break;

        platformMoving = true;

        while (Vector3.Distance(plattform.position, targetPosition) > reachThreshold)
        {
            plattform.position = Vector3.MoveTowards(plattform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        plattform.position = targetPosition;
        platformMoving = false;
    }

    private IEnumerator Ambush()
    {
        if (triggerAmbushOnlyOnce && ambushCompleted) yield break;

        yield return SpawnWaveAndWait(wave1_enemies, "Wave 1");

        yield return new WaitForSeconds(1.5f);

        yield return SpawnWaveAndWait(wave2_enemies, "Wave 2");
    }

    private IEnumerator SpawnWaveAndWait(GameObject[] wave, string waveName)
    {
        spawnedEnemies.Clear();
        SpawnWave(wave);

        Debug.Log($"{waveName} spawned. Count = {spawnedEnemies.Count}");

        yield return null; 

        yield return new WaitUntil(() => AreAllSpawnedEnemiesCleared());

        Debug.Log($"{waveName} cleared.");
    }

    private void SpawnWave(GameObject[] wave)
    {
        if (wave == null || wave.Length == 0)
        {
            Debug.LogWarning("Wave is empty.");
            return;
        }

        for (int i = 0; i < wave.Length; i++)
        {
            GameObject enemyPrefab = wave[i];

            if (enemyPrefab == null) continue;

            Transform spawnPoint = GetSpawnPoint(i);
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            var enemy = enemyObj.GetComponent<EnemyBase>();

            if (enemy != null)
            {
                enemy.OnSpawn(spawnPoints, safePoint, false, spawnPoint);
            }

            spawnedEnemies.Add(enemyObj);
        }
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        return spawnPoints[index % spawnPoints.Length];
    }

    private bool AreAllSpawnedEnemiesCleared()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = spawnedEnemies[i];

            if (enemy == null || !enemy.activeInHierarchy)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }

        return spawnedEnemies.Count == 0;
    }

    public void ReStart()
    {
        if (eventRoutine != null)
        {
            StopCoroutine(eventRoutine);
            eventRoutine = null;
        }

        eventRunning = false;
        platformMoving = false;

        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null) Destroy(spawnedEnemies[i]);
        }

        spawnedEnemies.Clear();

        ambushCompleted = false;

        if (plattform != null)
        {
            if (moveToTopOnReStart && topDestination != null) plattform.position = topDestination.position;

            else if (bottonDestination != null) plattform.position = bottonDestination.position;
        }
    }
}
