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

    [Header("Free Movement Texts")]
    [SerializeField] private string goUpText = "Subir";
    [SerializeField] private string goDownText = "Bajar";
    [SerializeField] private string activateText = "Activar elevador";

    private bool ambushCompleted;
    private bool eventRunning;
    private bool platformMoving;
    private bool isAtTop = true; // El elevador arranca arriba (ver Start())
    private Coroutine eventRoutine;

    private readonly List<GameObject> spawnedEnemies = new();

    public bool IsRunning => eventRunning;
    public bool AmbushCompleted => ambushCompleted;

    private void Start()
    {
        if (plattform != null && topDestination != null)
        {
            plattform.position = topDestination.position;
        }

        isAtTop = true;
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
        // Si el evento del elevador está corriendo (ambush o movimiento libre) cuando el player muere, se reinicia
        if (eventRunning)
        {
            Debug.Log("Player murió durante el evento del elevador, reiniciando");
            ReStart();
        }
    }

    public void Update()
    {

    }

    // Mantengo el método viejo como alias por compatibilidad con lo que ya lo llame (input de prueba, etc.)
    public void ActivateElevator()
    {
        ToggleElevator();
    }

    /// <summary>
    /// Punto de entrada único para el lever. Primera vez: dispara el evento completo de ambush.
    /// Después de completado: mueve el elevador libremente entre arriba y abajo sin enemigos.
    /// </summary>
    public void ToggleElevator()
    {
        if (eventRunning) return;

        if (!ambushCompleted)
        {
            eventRoutine = StartCoroutine(RunElevatorEvent());
        }
        else
        {
            eventRoutine = StartCoroutine(FreeMove());
        }
    }

    /// <summary>
    /// Texto a mostrar en el prompt de interacción, según el estado actual.
    /// </summary>
    public string GetNextActionText()
    {
        if (eventRunning) return null;

        if (!ambushCompleted) return activateText;

        return isAtTop ? goDownText : goUpText;
    }

    private IEnumerator RunElevatorEvent()
    {
        eventRunning = true;

        Debug.Log("Elevator event started");

        if (!ambushCompleted && stopDisAmbush != null)
        {
            Debug.Log("Moving to ambush stop");
            yield return MovePlatformTo(stopDisAmbush.position);
            isAtTop = false;

            Debug.Log("Starting ambush");
            yield return StartCoroutine(Ambush());

            Debug.Log("Ambush done, moving down");
            yield return MovePlatformTo(bottonDestination.position);
            isAtTop = false;

            ambushCompleted = true;
        }
        else
        {
            if (topDestination != null)
            {
                yield return MovePlatformTo(topDestination.position);
                isAtTop = true;
            }
        }

        eventRunning = false;
    }

    /// <summary>
    /// Movimiento libre (sin ambush) entre arriba y abajo, usado una vez ambushCompleted == true.
    /// </summary>
    private IEnumerator FreeMove()
    {
        eventRunning = true;

        Transform target = isAtTop ? bottonDestination : topDestination;

        if (target != null)
        {
            Debug.Log(isAtTop ? "Bajando elevador (libre)" : "Subiendo elevador (libre)");
            yield return MovePlatformTo(target.position);
            isAtTop = !isAtTop;
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
            if (moveToTopOnReStart && topDestination != null)
            {
                plattform.position = topDestination.position;
                isAtTop = true;
            }
            else if (bottonDestination != null)
            {
                plattform.position = bottonDestination.position;
                isAtTop = false;
            }
        }
    }
}