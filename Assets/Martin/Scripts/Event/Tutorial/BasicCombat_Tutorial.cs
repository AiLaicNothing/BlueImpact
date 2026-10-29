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

    [Header("Doors")]
    [SerializeField] private Transform[] doors;
    [SerializeField] private float posY = 4f;
    [SerializeField] private float doorSpeed = 2f;

    [Header("Camera")]
    [SerializeField] private CameraRequest cameraEvent;

    [Header("UI")]
    [SerializeField] private List<PopUpPage> pages = new();
    private UIPopUp ui;

    private Vector3[] closedLocalPositions;

    private readonly List<GameObject> enemies = new();

    private PlayerControl playerControl;

    public bool HasStarted => hasStarted;
    public bool HasFinished => hasFinished;

    private void Awake()
    {
        ui = FindAnyObjectByType<UIPopUp>();

        // Doors are already CLOSED in the Unity Editor.
        // Save each door's original local position.
        closedLocalPositions = new Vector3[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] == null) continue;

            closedLocalPositions[i] = doors[i].localPosition;
        }

        // Doors must start OPEN.
        SetDoorsInstant(true);
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

        if (hasStarted && !hasFinished)
        {
            ResetEvent();
        }
    }

    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

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

        // Always restore the doors OPEN.
        // This doesn't play the movement or camera event.
        SetDoorsInstant(true);

        Debug.Log( $"[BasicCombat_Tutorial] State restored. Completed: {hasFinished}",this);
    }

    private void SaveEventState()
    {
        if (Event_Manager.Instance == null || eventData == null)
        {
            Debug.LogWarning("[BasicCombat_Tutorial] Cannot save completion.", this);
            return;
        }

        Event_Manager.Instance.SaveState(eventData, true);

        Debug.Log("[BasicCombat_Tutorial] Completion saved.");
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Prevent restarting a completed or running event.
        if (hasStarted || hasFinished) return;

        hasStarted = true;

        StartCoroutine(Ambush());
    }

    private IEnumerator Ambush()
    {
        Debug.Log("[BasicCombat_Tutorial] Tutorial started.");

        // STEP 1: Enter cutscene mode.
        SetCutsceneMode(true);

        if (cameraEvent != null &&
            CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.Play(cameraEvent);
        }

        yield return MoveDoors(false);

        yield return StartSpawn();

        if (ui != null)
        {
            ui.ShowPopUp(pages);

            while (GameModeManager.Instance != null &&
                   GameModeManager.Instance.CurrentMode == GameMode.UI)
            {
                yield return null;
            }
        }

        SetCutsceneMode(false);

        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null || !enemy.activeInHierarchy);

            yield return null;
        }

        Debug.Log("[BasicCombat_Tutorial] All enemies defeated.");

        SetCutsceneMode(true);

        yield return MoveDoors(true);

        SetCutsceneMode(false);

        hasFinished = true;
        hasStarted = false;

        SaveEventState();

        Debug.Log("[BasicCombat_Tutorial] Tutorial completed.");
    }

    private IEnumerator MoveDoors(bool open)
    {
        if (doors == null || closedLocalPositions == null)
            yield break;

        bool moving = true;

        while (moving)
        {
            moving = false;

            for (int i = 0; i < doors.Length; i++)
            {
                Transform door = doors[i];

                if (door == null) continue;

                Vector3 targetPosition =closedLocalPositions[i];

                if (open)
                {
                    targetPosition.y += posY;
                }

                Vector3 currentPosition = door.localPosition;

                float newY = Mathf.MoveTowards(currentPosition.y, targetPosition.y, 
                    Mathf.Max(0.01f, doorSpeed) * Time.deltaTime);

                door.localPosition = new Vector3( currentPosition.x,newY,
                    currentPosition.z);

                // Check if this door is still moving.
                if (Mathf.Abs(newY - targetPosition.y) > 0.001f)
                {
                    moving = true;
                }
            }

            yield return null;
        }

        SetDoorsInstant(open);
    }

    private void SetDoorsInstant(bool open)
    {
        if (doors == null || closedLocalPositions == null) return;

        for (int i = 0; i < doors.Length; i++)
        {
            Transform door = doors[i];

            if (door == null) continue;

            Vector3 targetPosition =
                closedLocalPositions[i];

            if (open)
            {
                targetPosition.y += posY;
            }

            door.localPosition = targetPosition;
        }
    }

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

        if (entry.enemyPrefab == null ||
            entry.spawnPoint == null) return;

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
            Debug.LogWarning("[BasicCombat_Tutorial] Enemy prefab missing EnemyBase.", enemyObj);

            Destroy(enemyObj); return;
        }

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint);

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

    private void SetCutsceneMode(bool cutscene)
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SetMode(cutscene? GameMode.Cutscene : GameMode.Gameplay);
        }

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(cutscene);
        }
    }

    public void ResetEvent()
    {
        if (hasFinished) return;

        if (!hasStarted) return;

        Debug.Log("[BasicCombat_Tutorial] Resetting encounter.");

        StopAllCoroutines();

        SetCutsceneMode(false);

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null) Destroy(enemy);
        }

        enemies.Clear();
        ResetSpawnEntries();

        SetDoorsInstant(true);

        hasStarted = false;
        hasFinished = false;
    }
}