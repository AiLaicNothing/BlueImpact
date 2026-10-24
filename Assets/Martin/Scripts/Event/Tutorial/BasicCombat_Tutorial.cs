using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicCombat_Tutorial : MonoBehaviour
{
    [Header("Save System")]
    [SerializeField] private WorldEvent_Data eventData;

    [Header("Enemies")]
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    [SerializeField] private GameObject spawnVfx;

    [Header("Event State")]
    [SerializeField] private bool hasStarted;
    [SerializeField] private bool hasFinished;

    [Header("Door")]
    [SerializeField] private float doorSpeed = 2f;
    [SerializeField] private Transform door;
    [SerializeField] private Transform closePos;
    [SerializeField] private Transform openPos;

    [Header("Camera")]
    [SerializeField] private CameraRequest cameraEvent;

    [Header("UI")]
    [SerializeField] private List<PopUpPage> pages = new();
    private UIPopUp ui;

    private readonly List<GameObject> enemies = new();

    private PlayerControl playerControl;

    public bool HasStarted => hasStarted;
    public bool HasFinished => hasFinished;

    // =========================
    // INITIALIZATION
    // =========================

    private void Awake()
    {
        ui = FindAnyObjectByType<UIPopUp>();

        // The tutorial door starts OPEN.
        SetDoorOpenInstant();
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

        // Cancel unfinished encounters when the
        // scene or this component is disabled.
        if (hasStarted && !hasFinished)
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
        else
        {
            Debug.LogWarning("[BasicCombat_Tutorial] Missing Event_Manager or eventData.", this);
        }

        // Restore the door immediately.
        // No animation, audio, or camera event.
        SetDoorOpenInstant();

        Debug.Log($"[BasicCombat_Tutorial] State restored. Completed: {hasFinished}", this);
    }

    private void SaveEventState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning("[BasicCombat_Tutorial] Cannot save completion.", this);
            return;
        }

        Event_Manager.Instance.SaveState(eventData, true);

        Debug.Log("[BasicCombat_Tutorial] Completion saved.", this);
    }

    // =========================
    // EVENT ACTIVATION
    // =========================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Never trigger again once completed.
        if (hasStarted || hasFinished) return;

        hasStarted = true;

        StartCoroutine(Ambush());
    }

    // =========================
    // MAIN EVENT
    // =========================

    private IEnumerator Ambush()
    {
        Debug.Log("[BasicCombat_Tutorial] Tutorial started.");

        SetCutsceneMode(true);

        if (cameraEvent != null && CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(cameraEvent);
        }

        yield return MoveDoor(closePos);

        yield return StartSpawn();

        // STEP 5: Show the tutorial popup.
        if (ui != null)
        {
            ui.ShowPopUp(pages);

            // Wait until the UI is closed.
            while (GameModeManager.Instance != null &&
                   GameModeManager.Instance.CurrentMode == GameMode.UI)
            {
                yield return null;
            }
        }

        SetCutsceneMode(false);

        while (enemies.Count > 0)
        {
            enemies.RemoveAll(
                enemy => enemy == null || !enemy.activeInHierarchy
            );

            yield return null;
        }

        Debug.Log("[BasicCombat_Tutorial] All enemies defeated.");

        SetCutsceneMode(true);

        yield return MoveDoor(openPos);

        SetCutsceneMode(false);

        hasFinished = true;
        hasStarted = false;

        SaveEventState();

        Debug.Log("[BasicCombat_Tutorial] Tutorial completed.");
    }

    // =========================
    // DOOR MOVEMENT
    // =========================

    private IEnumerator MoveDoor(Transform finalPos)
    {
        if (door == null || finalPos == null)
            yield break;

        while (Vector3.Distance(door.position,finalPos.position) > 0.01f)
        {
            door.position = Vector3.MoveTowards(door.position, finalPos.position, Mathf.Max(0.01f, doorSpeed) * Time.deltaTime);

            yield return null;
        }

        door.position = finalPos.position;
    }

    private void SetDoorOpenInstant()
    {
        if (door == null || openPos == null) return;

        door.position = openPos.position;
    }

    // =========================
    // ENEMY SPAWNING
    // =========================

    private IEnumerator StartSpawn()
    {
        enemies.Clear();
        ResetSpawnEntries();

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            SpawnEnemy(i);
        }

        yield break;
    }

    private void SpawnEnemy(int index)
    {
        if (index < 0 || index >= spawnEntries.Count) return;

        SpawnEntry entry = spawnEntries[index];

        if (entry.enemyPrefab == null || entry.spawnPoint == null) return;

        // Spawn VFX.
        if (spawnVfx != null)
        {
            GameObject vfx = Instantiate(spawnVfx, entry.spawnPoint.position, entry.spawnPoint.rotation);

            MoveSpawnedObjectToEventScene(vfx);

            Destroy(vfx, 1f);
        }

        // Spawn enemy.
        GameObject enemyObj = Instantiate(entry.enemyPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        MoveSpawnedObjectToEventScene(enemyObj);

        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();

        if (enemy == null)
        {
            Debug.LogWarning("[BasicCombat_Tutorial] Enemy prefab is missing EnemyBase.", enemyObj);

            Destroy(enemyObj);
            return;
        }

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint
        );

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
    // EVENT RESET
    // =========================

    public void ResetEvent()
    {
        // Do not reset a completed event.
        if (hasFinished) return;

        // Only reset an encounter that was running.
        if (!hasStarted) return;

        Debug.Log("[BasicCombat_Tutorial] Resetting encounter.");

        StopAllCoroutines();

        // Restore player control and audio.
        SetCutsceneMode(false);

        // Destroy enemies from this encounter.
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null) Destroy(enemy);
        }

        enemies.Clear();
        ResetSpawnEntries();

        // Return the door to its initial OPEN position.
        SetDoorOpenInstant();

        hasStarted = false;
        hasFinished = false;

        // Don't save completion because the encounter
        // wasn't successfully finished.
    }
}