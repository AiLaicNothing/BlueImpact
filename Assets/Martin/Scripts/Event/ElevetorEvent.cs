using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevetorEvent : MonoBehaviour
{
    [Header("Platform")]
    [SerializeField] private Transform plattform;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float reachThreshold = 0.05f;

    [Header("Downward Distance From Starting Position")]
    [SerializeField] private float ambushPosY = 30f;
    [SerializeField] private float bottomPosY = 60f;

    [Header("Ambush")]
    [SerializeField] private GameObject[] wave1_enemies;
    [SerializeField] private GameObject[] wave2_enemies;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform safePoint;

    [Header("Free Movement Texts")]
    [SerializeField] private string goUpText = "Subir";
    [SerializeField] private string goDownText = "Bajar";
    [SerializeField] private string activateText = "Activar elevador";

    private Vector3 initialLocalPosition;

    private bool ambushCompleted;
    private bool eventRunning;
    private bool platformMoving;
    private bool isAtTop = true;

    private readonly List<GameObject> spawnedEnemies = new();

    public bool IsRunning => eventRunning;
    public bool IsMoving => platformMoving;
    public bool AmbushCompleted => ambushCompleted;

    private void Awake()
    {
        // The platform is already positioned at the top
        // in the scene. Save that original position.
        if (plattform != null)
        {
            initialLocalPosition = plattform.localPosition;
        }
    }

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        Debug.Log("Player died. Resetting elevator event.");
        ReStart();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ToggleElevator();
        }
    }

    public void ActivateElevator()
    {
        ToggleElevator();
    }

    public void ToggleElevator()
    {
        if (eventRunning || plattform == null)
            return;

        if (!ambushCompleted)
        {
            StartCoroutine(RunElevatorEvent());
        }
        else
        {
            StartCoroutine(FreeMove());
        }
    }

    public string GetNextActionText()
    {
        if (eventRunning)
            return null;

        if (!ambushCompleted)
            return activateText;

        return isAtTop ? goDownText : goUpText;
    }

    // =========================
    // MAIN EVENT
    // =========================

    private IEnumerator RunElevatorEvent()
    {
        eventRunning = true;

        Debug.Log("Elevator event started.");

        // Move downward from the original top position.
        yield return MovePlatformTo(ambushPosY);

        Debug.Log("Starting elevator ambush.");

        yield return Ambush();

        Debug.Log("Ambush completed. Moving down.");

        // Move to the final downward offset.
        yield return MovePlatformTo(bottomPosY);

        ambushCompleted = true;
        isAtTop = false;
        eventRunning = false;

        Debug.Log("Elevator event finished.");
    }

    // =========================
    // FREE MOVEMENT
    // =========================

    private IEnumerator FreeMove()
    {
        eventRunning = true;

        if (isAtTop)
        {
            Debug.Log("Elevator going down.");

            yield return MovePlatformTo(bottomPosY);
        }
        else
        {
            Debug.Log("Elevator going up.");

            yield return MovePlatformTo(0f);
        }

        isAtTop = !isAtTop;
        eventRunning = false;
    }

    // =========================
    // MOVEMENT
    // =========================

    private IEnumerator MovePlatformTo(float distanceDown)
    {
        if (plattform == null)
            yield break;

        platformMoving = true;

        // Calculate the destination from the original
        // local Y, not from the current platform position.
        float targetY = initialLocalPosition.y - distanceDown;

        while (Mathf.Abs(plattform.localPosition.y - targetY)
               > reachThreshold)
        {
            Vector3 currentPosition = plattform.localPosition;

            float newY = Mathf.MoveTowards(
                currentPosition.y,
                targetY,
                Mathf.Max(0.01f, moveSpeed) * Time.deltaTime
            );

            plattform.localPosition = new Vector3(
                currentPosition.x,
                newY,
                currentPosition.z
            );

            yield return null;
        }

        SetPlatformY(targetY);
        platformMoving = false;
    }

    private void SetPlatformY(float targetY)
    {
        if (plattform == null)
            return;

        Vector3 position = plattform.localPosition;
        position.y = targetY;

        plattform.localPosition = position;
    }

    // =========================
    // AMBUSH
    // =========================

    private IEnumerator Ambush()
    {
        yield return SpawnWaveAndWait(
            wave1_enemies,
            "Wave 1"
        );

        yield return new WaitForSeconds(1.5f);

        yield return SpawnWaveAndWait(
            wave2_enemies,
            "Wave 2"
        );
    }

    private IEnumerator SpawnWaveAndWait(
        GameObject[] wave,
        string waveName)
    {
        spawnedEnemies.Clear();

        SpawnWave(wave);

        Debug.Log(
            $"{waveName} spawned. Count = {spawnedEnemies.Count}"
        );

        yield return null;

        yield return new WaitUntil(
            () => AreAllSpawnedEnemiesCleared()
        );

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

            if (enemyPrefab == null)
                continue;

            Transform spawnPoint = GetSpawnPoint(i);

            Vector3 spawnPos = spawnPoint != null
                ? spawnPoint.position
                : transform.position;

            GameObject enemyObj = Instantiate(
                enemyPrefab,
                spawnPos,
                Quaternion.identity
            );

            EnemyBase enemy =
                enemyObj.GetComponent<EnemyBase>();

            if (enemy != null)
            {
                enemy.OnSpawn(
                    spawnPoints,
                    safePoint,
                    false,
                    spawnPoint
                );
            }

            spawnedEnemies.Add(enemyObj);
        }
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

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

    // =========================
    // RESET
    // =========================

    public void ReStart()
    {
        StopAllCoroutines();

        eventRunning = false;
        platformMoving = false;

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        spawnedEnemies.Clear();

        ambushCompleted = false;
        isAtTop = true;

        // Restore the original position from the scene.
        if (plattform != null)
        {
            plattform.localPosition = initialLocalPosition;
        }

        Debug.Log("Elevator event reset.");
    }
}