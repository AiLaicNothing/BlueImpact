using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevetorEvent : MonoBehaviour
{
    [Header("Persistence")]
    [SerializeField] private WorldEvent_Data eventData;

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

    private bool requestedTop;

    private Coroutine eventRoutine;
    private Coroutine freeMoveRoutine;

    private readonly List<GameObject> spawnedEnemies = new();

    public bool IsRunning => eventRunning;
    public bool IsMoving => platformMoving;
    public bool AmbushCompleted => ambushCompleted;
    public bool IsAtTop => isAtTop;

    // =========================
    // INITIALIZATION
    // =========================

    private void Awake()
    {
        // The elevator already starts at the TOP
        // position in the Unity Editor.
        if (plattform != null)
        {
            initialLocalPosition = plattform.localPosition;
        }
    }

    private void Start()
    {
        RestoreElevatorState();
    }

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= HandlePlayerDied;

        if (eventRunning)
        {
            StopAllCoroutines();

            ClearSpawnedEnemies();

            eventRoutine = null;
            freeMoveRoutine = null;

            eventRunning = false;
            platformMoving = false;
        }
    }

    private void RestoreElevatorState()
    {
        if (plattform == null) return;

        // Default state for the first visit.
        ambushCompleted = false;
        isAtTop = true;

        if (Event_Manager.Instance != null &&
            eventData != null &&
            Event_Manager.Instance.TryGetState(
                eventData,
                out WorldEvent_State saved))
        {
            ambushCompleted = saved.completed;

            // 0 = Top
            // 1 = Bottom
            if (ambushCompleted)
            {
                isAtTop = saved.state == 0;
            }
        }

        // An unfinished ambush always restarts at the top.
        // A completed ambush restores its last saved floor.
        float targetDistance = isAtTop ? 0f : bottomPosY;

        SetPlatformY(initialLocalPosition.y - targetDistance);

        eventRunning = false;
        platformMoving = false;

        Debug.Log(
            $"[ElevetorEvent] Restored. " +
            $"Ambush completed: {ambushCompleted}, " +
            $"At top: {isAtTop}", this);
    }

    private void SaveElevatorState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning( "[ElevetorEvent] Missing Event_Manager or WorldEvent_Data.", this);
            return;
        }

        // completed = whether the ambush was defeated
        // state 0 = top, 1 = bottom
        Event_Manager.Instance.SaveState(eventData, ambushCompleted, isAtTop ? 0 : 1);
    }

    private void HandlePlayerDied()
    {
        if (ambushCompleted) return;

        // Reset only an unfinished, running encounter.
        if (eventRunning)
        {
            Debug.Log("[ElevetorEvent] Player died. Restarting ambush.");

            ReStart();
        }
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

        // First activation starts the scripted encounter.
        if (!ambushCompleted)
        {
            eventRunning = true;

            eventRoutine = StartCoroutine(RunElevatorEvent());

            return;
        }

        // After completion, the elevator works normally.
        if (isAtTop)
        {
            GoDown();
        }
        else
        {
            GoUp();
        }
    }

    public string GetNextActionText()
    {
        if (eventRunning) return null;

        if (!ambushCompleted) return activateText;

        return isAtTop ? goDownText : goUpText;
    }

    // =========================
    // PUBLIC MOVEMENT COMMANDS
    // =========================

    public void GoUp()
    {
        StartFreeMovement(true);
    }

    public void GoDown()
    {
        StartFreeMovement(false);
    }

    private void StartFreeMovement(bool goToTop)
    {
        if (plattform == null) return;

        if (!ambushCompleted || eventRoutine != null) return;

        float distanceDown = goToTop ? 0f : bottomPosY;
        float targetY = initialLocalPosition.y - distanceDown;

        // If already moving toward the same destination,
        // ignore the repeated command.
        if (freeMoveRoutine != null)
        {
            if (requestedTop == goToTop)
                return;

            // Allow another lever to reverse the elevator
            // during normal travel after the ambush.
            StopCoroutine(freeMoveRoutine);
            freeMoveRoutine = null;
        }
        else if (Mathf.Abs(plattform.localPosition.y - targetY) <= reachThreshold)
        {
            return;
        }

        requestedTop = goToTop;
        eventRunning = true;

        freeMoveRoutine = StartCoroutine(FreeMove(goToTop));
    }

    private IEnumerator RunElevatorEvent()
    {
        Debug.Log("[ElevetorEvent] Event started.");

        // Descend to the ambush area.
        yield return MovePlatformTo(ambushPosY);

        Debug.Log("[ElevetorEvent] Starting ambush.");

        // Wait until all enemy waves are cleared.
        yield return Ambush();

        Debug.Log("[ElevetorEvent] Ambush cleared. Moving to bottom.");

        //Continue to the bottom.
        yield return MovePlatformTo(bottomPosY);

        //Permanently complete the encounter.
        ambushCompleted = true;
        isAtTop = false;

        // Save BOTH completion and final elevator position.
        SaveElevatorState();

        eventRunning = false;
        eventRoutine = null;

        Debug.Log("[ElevetorEvent] Event completed.");
    }


    private IEnumerator FreeMove(bool goToTop)
    {
        float distanceDown = goToTop ? 0f : bottomPosY;

        Debug.Log(goToTop? "[ElevetorEvent] Going up.": "[ElevetorEvent] Going down.");

        yield return MovePlatformTo(distanceDown);

        // Update state only after reaching the destination.
        isAtTop = goToTop;

        // Save the floor where the elevator was left.
        SaveElevatorState();

        eventRunning = false;
        freeMoveRoutine = null;
    }

    // =========================
    // MOVEMENT
    // =========================

    private IEnumerator MovePlatformTo(float distanceDown)
    {
        if (plattform == null) yield break;

        platformMoving = true;

        // All destinations use the original TOP
        // position as their reference.
        float targetY = initialLocalPosition.y - distanceDown;

        while (Mathf.Abs(plattform.localPosition.y - targetY) > reachThreshold)
        {
            Vector3 currentPosition = plattform.localPosition;

            float newY = Mathf.MoveTowards(currentPosition.y, targetY, Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);

            plattform.localPosition = new Vector3(currentPosition.x, newY, currentPosition.z);

            yield return null;
        }

        SetPlatformY(targetY);

        platformMoving = false;
    }

    private void SetPlatformY(float targetY)
    {
        if (plattform == null) return;

        Vector3 position = plattform.localPosition;

        position.y = targetY;

        plattform.localPosition = position;
    }

    private IEnumerator Ambush()
    {
        yield return SpawnWaveAndWait(wave1_enemies, "Wave 1");

        yield return new WaitForSeconds(1.5f);

        yield return SpawnWaveAndWait(wave2_enemies, "Wave 2");
    }

    private IEnumerator SpawnWaveAndWait(GameObject[] wave, string waveName)
    {
        spawnedEnemies.Clear();

        SpawnWave(wave);

        Debug.Log( $"[ElevetorEvent] {waveName} spawned. " +
            $"Count: {spawnedEnemies.Count}");

        yield return null;

        yield return new WaitUntil(() => AreAllSpawnedEnemiesCleared());

        Debug.Log($"[ElevetorEvent] {waveName} cleared.");
    }

    private void SpawnWave(GameObject[] wave)
    {
        if (wave == null || wave.Length == 0)
        {
            Debug.LogWarning("[ElevetorEvent] Wave is empty.", this);
            return;
        }

        for (int i = 0; i < wave.Length; i++)
        {
            GameObject enemyPrefab = wave[i];

            if (enemyPrefab == null) continue;

            Transform spawnPoint = GetSpawnPoint(i);

            Vector3 spawnPos = spawnPoint != null? spawnPoint.position : transform.position;

            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // Ensure enemies belong to this streamed zone.
            MoveSpawnedObjectToEventScene(enemyObj);

            EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();

            if (enemy != null)
            {
                enemy.OnSpawn(spawnPoints, safePoint, false, spawnPoint);
            }
            else
            {
                Debug.LogWarning("[ElevetorEvent] Spawned enemy missing EnemyBase.", enemyObj);
            }

            spawnedEnemies.Add(enemyObj);
        }
    }

    private void MoveSpawnedObjectToEventScene(GameObject obj)
    {
        if (obj == null) return;

        Scene eventScene = gameObject.scene;

        if (!eventScene.IsValid() || !eventScene.isLoaded) return;

        if (obj.scene != eventScene)
        {
            SceneManager.MoveGameObjectToScene(obj, eventScene
            );
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

    private void ClearSpawnedEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }

        spawnedEnemies.Clear();
    }

    public void ReStart()
    {
        // Never reset permanently completed progress.
        if (ambushCompleted)  return;

        StopAllCoroutines();

        eventRoutine = null;
        freeMoveRoutine = null;

        eventRunning = false;
        platformMoving = false;

        ClearSpawnedEnemies();

        // Restore the original state for a new attempt.
        ambushCompleted = false;
        isAtTop = true;

        if (plattform != null)
        {
            plattform.localPosition = initialLocalPosition;
        }

        // Do not save completion here.
        // The encounter must be completed successfully.

        Debug.Log("[ElevetorEvent] Event reset.");
    }
}