using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmbushEvent : MonoBehaviour, ILevelSpawnSource
{
    [System.Serializable]
    public struct Door
    {
        [Tooltip("Door transform. Place it in its CLOSED position in the scene.")]
        public Transform transform;

        [Tooltip("If true, this door stays closed from the beginning and only opens " +
                 "when the event is completed (shortcut reward).")]
        public bool isClosed;
    }

    [Header("Save System")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Event State")]
    [SerializeField] private bool hasStarted;
    [SerializeField] private bool hasFinished;

    [Header("Enemies")]
    [SerializeField] private List<SpawnEntry> spawnEntries = new();

    [Header("Spawn VFX")]
    [SerializeField] private GameObject spawnVfx;
    [SerializeField] private float vfxDuration = 1f;
    [SerializeField] private Vector3 vfxOffset = new Vector3(0, 0.5f, 0);

    [Header("Doors")]
    [SerializeField] private Door[] doors;
    [SerializeField] private float posY = 4f;
    [SerializeField] private float doorSpeed = 2f;

    [Header("Reward")]
    [SerializeField] private bool hasReward;
    [SerializeField] private GameObject reward;
    [Tooltip("How long the cutscene holds while the reward camera plays.")]
    [SerializeField] private float rewardShowDuration = 2f;

    [Header("Camera Events")]
    [Tooltip("Plays from the moment the doors start closing until the last enemy has spawned. " +
             "Its Duration is ignored: the event releases it when the spawn finishes.")]
    [SerializeField] private CameraRequest cameraShowSpawn;
    [Tooltip("Extra seconds the spawn camera stays after the last enemy appears.")]
    [SerializeField] private float spawnCameraHold = 0.5f;
    [SerializeField] private CameraRequest cameraShowReward;

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
        if (doors == null) doors = new Door[0];

        closedLocalPositions = new Vector3[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i].transform != null)
            {
                closedLocalPositions[i] = doors[i].transform.localPosition;
            }
        }

        // The spawn camera lasts as long as doors + spawn take,
        // so it is released manually instead of using its Duration.
        if (cameraShowSpawn != null)
        {
            cameraShowSpawn.holdUntilReleased = true;
        }

        // Reward stays hidden until the event is completed.
        if (reward != null)
        {
            reward.SetActive(false);
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

        // Inactive event:
        // - Normal doors are OPEN.
        // - "isClosed" doors stay CLOSED until the event is completed.
        // Completed event: every door is OPEN.
        // No animation or camera event is played here.
        SetDoorsInstant(true, includeLockedDoors: hasFinished);

        // Completed event: reward is already revealed.
        SetRewardActive(hasFinished);

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

        // STEP 1: Enter cutscene mode and start the spawn camera.
        // It stays active through the door closing AND the enemy spawn.
        SetCutsceneMode(true);

        PlayCamera(cameraShowSpawn);

        // STEP 2: Close all doors ("isClosed" doors are already closed).
        yield return MoveDoors(false, includeLockedDoors: true);

        // STEP 3: Spawn enemies.
        yield return StartCoroutine(StartWave());

        // Let the last spawn be seen, then end the spawn camera.
        if (spawnCameraHold > 0f)
        {
            yield return new WaitForSeconds(spawnCameraHold);
        }

        ReleaseCamera(cameraShowSpawn);

        // STEP 4: Return control to the player.
        SetCutsceneMode(false);

        // STEP 5: Wait for all enemies to be defeated.
        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null || !enemy.activeInHierarchy);

            yield return null;
        }

        Debug.Log("[AmbushEvent] All enemies defeated.");

        // Encuentro completado: sus kills dejan de revertirse.
        LevelObjectiveManager.Instance?.CommitEncounter(LevelObjectiveManager.PathOf(transform));

        // STEP 6: Enter cutscene mode.
        SetCutsceneMode(true);

        // STEP 7: Open ALL doors, including the "isClosed" shortcut doors.
        yield return MoveDoors(true, includeLockedDoors: true);

        // STEP 8: Reveal the reward.
        yield return ShowReward();

        // STEP 9: Return control to the player.
        SetCutsceneMode(false);

        // STEP 10: Mark the event as completed.
        hasFinished = true;
        hasStarted = false;

        // STEP 11: Save permanent completion.
        SaveEventState();

        Debug.Log("[AmbushEvent] Event completed.");
    }

    // =========================
    // CAMERA
    // =========================

    private void PlayCamera(CameraRequest request)
    {
        if (request == null || CameraEventRelay.Instance == null) return;

        CameraEventRelay.Instance.Play(request);
    }

    private void ReleaseCamera(CameraRequest request)
    {
        if (request == null || CameraEventRelay.Instance == null) return;

        CameraEventRelay.Instance.Release(request);
    }

    // =========================
    // REWARD
    // =========================

    private IEnumerator ShowReward()
    {
        if (!hasReward || reward == null) yield break;

        SetRewardActive(true);

        if (cameraShowReward != null && CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(cameraShowReward);

            yield return new WaitForSeconds(rewardShowDuration);
        }

        Debug.Log("[AmbushEvent] Reward revealed.");
    }

    private void SetRewardActive(bool active)
    {
        if (!hasReward || reward == null) return;

        reward.SetActive(active);
    }

    // =========================
    // DOOR MOVEMENT
    // =========================

    // Returns the local Y the door at index i should move to.
    // open = false            -> every door goes CLOSED.
    // open = true             -> normal doors go OPEN.
    // includeLockedDoors=true -> "isClosed" doors also go OPEN.
    private float GetTargetY(int i, bool open, bool includeLockedDoors)
    {
        float targetY = closedLocalPositions[i].y;

        bool shouldOpen = open && (includeLockedDoors || !doors[i].isClosed);

        if (shouldOpen) targetY += posY;

        return targetY;
    }

    private IEnumerator MoveDoors(bool open, bool includeLockedDoors)
    {
        if (doors == null || closedLocalPositions == null) yield break;

        bool moving = true;

        while (moving)
        {
            moving = false;

            for (int i = 0; i < doors.Length; i++)
            {
                Transform door = doors[i].transform;

                if (door == null) continue;

                float targetY = GetTargetY(i, open, includeLockedDoors);

                Vector3 currentPosition = door.localPosition;

                float newY = Mathf.MoveTowards(currentPosition.y, targetY, Mathf.Max(0.01f, doorSpeed) * Time.deltaTime);

                door.localPosition = new Vector3(currentPosition.x, newY, currentPosition.z);

                if (Mathf.Abs(newY - targetY) > 0.001f)
                {
                    moving = true;
                }
            }

            yield return null;
        }

        // Ensure doors reach their exact destinations.
        SetDoorsInstant(open, includeLockedDoors);
    }

    private void SetDoorsInstant(bool open, bool includeLockedDoors)
    {
        if (doors == null || closedLocalPositions == null) return;

        for (int i = 0; i < doors.Length; i++)
        {
            Transform door = doors[i].transform;

            if (door == null) continue;

            Vector3 position = closedLocalPositions[i];
            position.y = GetTargetY(i, open, includeLockedDoors);

            door.localPosition = position;
        }
    }

    // =========================
    // ENEMY SPAWNING
    // =========================

    private IEnumerator StartWave()
    {
        // The spawn camera is already active (started in STEP 1).

        enemies.Clear();
        ResetSpawnEntries();

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            SpawnEntry entry = spawnEntries[i];
            // Spawn visual effect.
            if (spawnVfx != null && entry.enemyPrefab != null && entry.spawnPoint != null)
            {
                GameObject vfx = Instantiate(spawnVfx, entry.spawnPoint.position + vfxOffset, entry.spawnPoint.rotation);

                MoveSpawnedObjectToEventScene(vfx);

                Destroy(vfx, vfxDuration + 2);
            }
            yield return new WaitForSeconds(vfxDuration / 2);
            Spawn(i);
        }
    }

    // Para LevelObjectiveManager: enemigos contables del encuentro.
    public int CountLevelEnemies()
    {
        int n = 0;
        foreach (SpawnEntry e in spawnEntries) if (e.enemyPrefab != null && e.countsForLevel) n++;
        return n;
    }

    public void Spawn(int index)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.enemyPrefab == null || entry.spawnPoint == null) return;

        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        // Ensure the enemy belongs to this streamed zone.
        MoveSpawnedObjectToEventScene(enemyObj);

        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();

        if (enemy != null)
        {
            // Kills de ambush se revierten si el jugador muere antes de completarlo.
            LevelObjectiveManager.AssignObjective(enemy, gameObject.scene,
                LevelObjectiveManager.PathOf(transform), index, entry.countsForLevel, true);

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
            GameModeManager.Instance.SetMode(cutscene ? GameMode.Cutscene : GameMode.Gameplay);
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

        // The spawn camera waits to be released; if the sequence was
        // stopped mid-spawn, release it here so it doesn't stay stuck.
        ReleaseCamera(cameraShowSpawn);

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

        // Normal doors return OPEN, "isClosed" doors stay CLOSED.
        SetDoorsInstant(true, includeLockedDoors: false);

        // Reward stays hidden until completion.
        SetRewardActive(false);

        // Allow the player to retry.
        hasStarted = false;
        hasFinished = false;

        // No SaveState call here.
        // An unfinished event is not saved as completed.
    }
}
