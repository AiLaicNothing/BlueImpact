using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmbushEvent : MonoBehaviour
{
    [Header("Save System")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Event State")]
    [SerializeField] private bool hasStarted;
    [SerializeField] private bool hasFinished;

    [Header("Enemies")]
    [SerializeField] private List<SpawnEntry> spawnEntries = new();

    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnVfx;
    [SerializeField] private Transform[] vfxPos;

    [Header("Doors")]
    [SerializeField] private Transform[] doors;
    [SerializeField] private float posY = 4f;
    [SerializeField] private float doorSpeed = 2f;

    [Header("Camera Events")]
    [SerializeField] private CameraRequest closeDoorsEvent;
    [SerializeField] private CameraRequest cameraShowSpawn;

    private Vector3[] closedLocalPositions;

    private readonly List<GameObject> enemies = new();

    private PlayerControl playerControl;

    public bool HasStarted => hasStarted;
    public bool HasFinished => hasFinished;

    // =========================
    // INITIALIZATION
    // =========================

    private void Awake()
    {
        // Doors are already positioned CLOSED in the scene.
        // Remember their original local positions.
        closedLocalPositions = new Vector3[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
            {
                closedLocalPositions[i] = doors[i].localPosition;
            }
        }
    }

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += ResetEvent;
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void Start()
    {
        if (playerControl == null)
        {
            playerControl = FindFirstObjectByType<PlayerControl>();
        }

        RestoreEventState();
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= ResetEvent;
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;

        // If the zone unloads during the ambush,
        // cancel the unfinished encounter.
        if (hasStarted)
        {
            ResetEvent();
        }
    }

    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

    // =========================
    // SAVE SYSTEM
    // =========================

    private void RestoreEventState()
    {
        hasStarted = false;
        hasFinished = false;

        if (Event_Manager.Instance != null && eventData != null)
        {
            hasFinished = Event_Manager.Instance.IsCompleted(eventData);
        }

        // Doors are always OPEN when the event is inactive.
        // This does not play any animation or camera event.
        SetDoorsInstant(true);

        Debug.Log($"[AmbushEvent] State restored. Completed: {hasFinished}", this);
    }

    private void SaveEventState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning("[AmbushEvent] Event could not be saved. " + "Missing Event_Manager or WorldEvent_Data.", this);

            return;
        }

        // This is a one-time event.
        // Once completed, it remains completed.
        Event_Manager.Instance.SaveState(eventData, true);

        Debug.Log("[AmbushEvent] Completion saved.");
    }

    // =========================
    // EVENT ACTIVATION
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Cannot activate while running or after completion.
        if (hasStarted || hasFinished) return;

        hasStarted = true;

        StartCoroutine(Ambush());
    }

    // =========================
    // MAIN AMBUSH
    // =========================

    private IEnumerator Ambush()
    {
        Debug.Log("[AmbushEvent] Ambush started.");

        // STEP 1: Enter cutscene mode.
        SetCutsceneMode(true);

        if (closeDoorsEvent != null &&
            CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(closeDoorsEvent);
        }

        // STEP 2: Close doors.
        yield return MoveDoors(false);

        // STEP 3: Spawn enemies and play spawn camera event.
        StartWave();

        // STEP 4: Return control to the player.
        SetCutsceneMode(false);

        // STEP 5: Wait for all enemies to be defeated.
        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null || !enemy.activeInHierarchy);

            yield return null;
        }

        Debug.Log("[AmbushEvent] All enemies defeated.");

        // STEP 6: Enter cutscene mode.
        SetCutsceneMode(true);

        // STEP 7: Open doors.
        yield return MoveDoors(true);

        // STEP 8: Return control to the player.
        SetCutsceneMode(false);

        // STEP 9: Mark the event as completed.
        hasFinished = true;
        hasStarted = false;

        // STEP 10: Save permanent completion.
        SaveEventState();

        Debug.Log("[AmbushEvent] Event completed.");
    }

    // =========================
    // DOOR MOVEMENT
    // =========================

    private IEnumerator MoveDoors(bool open)
    {
        if (doors == null || closedLocalPositions == null) yield break;

        bool moving = true;

        while (moving)
        {
            moving = false;

            for (int i = 0; i < doors.Length; i++)
            {
                Transform door = doors[i];

                if (door == null) continue;

                // Original position is the CLOSED position.
                float targetY = closedLocalPositions[i].y;

                // Add the offset to calculate OPEN position.
                if (open) targetY += posY;

                Vector3 currentPosition = door.localPosition;

                float newY = Mathf.MoveTowards(currentPosition.y, targetY, Mathf.Max(0.01f, doorSpeed) * Time.deltaTime);

                door.localPosition = new Vector3(currentPosition.x, newY,currentPosition.z);

                if (Mathf.Abs(newY - targetY) > 0.001f)
                {
                    moving = true;
                }
            }

            yield return null;
        }

        // Ensure doors reach their exact destinations.
        SetDoorsInstant(open);
    }

    private void SetDoorsInstant(bool open)
    {
        if (doors == null || closedLocalPositions == null) return;

        for (int i = 0; i < doors.Length; i++)
        {
            Transform door = doors[i];

            if (door == null) continue;

            Vector3 position = closedLocalPositions[i];

            if (open) position.y += posY;

            door.localPosition = position;
        }
    }

    // =========================
    // ENEMY SPAWNING
    // =========================

    private void StartWave()
    {
        if (cameraShowSpawn != null && CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(cameraShowSpawn);
        }

        enemies.Clear();
        ResetSpawnEntries();

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            // Spawn visual effect.
            if (spawnVfx != null && vfxPos != null && i < vfxPos.Length && vfxPos[i] != null)
            {
                Transform point = vfxPos[i];

                GameObject vfx = Instantiate(spawnVfx, point.position, point.rotation);

                MoveSpawnedObjectToEventScene(vfx);

                Destroy(vfx, 2.25f);
            }

            Spawn(i);
        }
    }

    public void Spawn(int index)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.enemyPrefab == null || entry.spawnPoint == null)  return;

        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        // Ensure the enemy belongs to this streamed zone.
        MoveSpawnedObjectToEventScene(enemyObj);

        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();

        if (enemy != null)
        {
            enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint);
        }
        else
        {
            Debug.LogWarning("[AmbushEvent] Enemy prefab missing EnemyBase.", enemyObj);
        }

        entry.hasSpawned = true;
        entry.spawnedEnemy = enemy;

        enemies.Add(enemyObj);
    }

    private void MoveSpawnedObjectToEventScene(GameObject obj)
    {
        if (obj == null) return;

        Scene eventScene = gameObject.scene;

        if (!eventScene.IsValid() || !eventScene.isLoaded) return;

        if (obj.scene != eventScene)
        {
            SceneManager.MoveGameObjectToScene(obj, eventScene);
        }
    }

    private void ResetSpawnEntries()
    {
        foreach (SpawnEntry entry in spawnEntries)
        {
            entry.hasSpawned = false;
            entry.spawnedEnemy = null;
        }
    }

    // =========================
    // PLAYER CONTROL
    // =========================

    private void SetCutsceneMode(bool cutscene)
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(cutscene? GameMode.Cutscene : GameMode.Gameplay
            );
        }

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(cutscene);
        }
    }

    // =========================
    // RESET EVENT
    // =========================

    public void ResetEvent()
    {
        // A completed event must never reset.
        if (hasFinished) return;

        // Only reset an encounter that was running.
        if (!hasStarted) return;

        Debug.Log("[AmbushEvent] Resetting unfinished encounter.");

        // Stop ongoing movement and combat sequence.
        StopAllCoroutines();

        // Restore player control.
        SetCutsceneMode(false);

        // Destroy all spawned enemies.
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        enemies.Clear();

        ResetSpawnEntries();

        // Return doors to their OPEN positions.
        SetDoorsInstant(true);

        // Allow the player to retry.
        hasStarted = false;
        hasFinished = false;

        // No SaveState call here.
        // An unfinished event is not saved as completed.
    }
}