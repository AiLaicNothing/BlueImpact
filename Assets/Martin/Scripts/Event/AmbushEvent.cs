using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbushEvent : MonoBehaviour
{
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    [SerializeField] private bool hasStarted;
    [SerializeField] private bool hasFinished;

    [SerializeField] private GameObject spawnVfx;
    [SerializeField] private Transform[] vfxPos;

    [SerializeField] private float doorSpeed;
    [SerializeField] private Transform[] doors;
    [SerializeField] private Transform[] closePos;
    [SerializeField] private Transform[] openPos;

    [SerializeField] private CameraRequest closeDoorsEvent;
    [SerializeField] private CameraRequest cameraShowSpawn;

    private List<GameObject> enemies = new();
    private PlayerControl playerControl;

    private void Awake()
    {
        // Suscribirse a OnPlayerSpawned para obtener referencia al jugador
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
        }
    }

    private void Start()
    {
        doors[0].position = openPos[0].position;
        doors[1].position = openPos[1].position;
    }

    /// <summary>
    /// Se ejecuta cuando el jugador es instanciado (después de seleccionar personaje)
    /// </summary>
    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

    private void OnEnable()
    {
        PlayerControl.OnPlayerDied += ResetEvent;
    }

    private void OnDisable()
    {
        PlayerControl.OnPlayerDied -= ResetEvent;
    }

    private IEnumerator Ambush()
    {
        hasStarted = true;

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[AmbushEvent] Jugador pausado y silenciado");
        }

        if (closeDoorsEvent != null)
        {
            CameraEventRelay.Instance.Play(closeDoorsEvent);
            Debug.Log("CloseDoor");
        }

        yield return MoveDoor(closePos);

        yield return StartWave();

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[AmbushEvent] Jugador reactivado para combate");
        }

        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null);
            yield return null;
        }

        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[AmbushEvent] Jugador pausado después de combate");
        }

        yield return MoveDoor(openPos);

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[AmbushEvent] Jugador reactivado - Evento completado");
        }

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
        if (cameraShowSpawn != null)
        {
            CameraEventRelay.Instance.Play(cameraShowSpawn);
        }

        enemies.Clear();

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            if (spawnVfx != null)
            {
                var desiredPos = vfxPos[i];
                GameObject vfx = Instantiate(spawnVfx, desiredPos.position, desiredPos.rotation);

                Destroy(vfx, 2.25f);
            }

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

        enemy.OnSpawn(entry.patrolPoints, entry.safePoint, entry.hasPatrol, entry.spawnPoint);

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

    public void ResetEvent()
    {
        if (!hasStarted || hasFinished) return;

        StopAllCoroutines();

        // Reactivar jugador si el evento se cancela por muerte
        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        // SetMode(Gameplay) llama automáticamente a UnlockPlayerControl()

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[AmbushEvent] 🔓 Jugador reactivado tras reset");
        }

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null) Destroy(enemy);
        }

        enemies.Clear();

        foreach (SpawnEntry entry in spawnEntries)
        {
            entry.hasSpawned = false;
            entry.spawnedEnemy = null;
        }

        for (int i = 0; i < doors.Length && i < openPos.Length; i++)
        {
            if (doors[i] != null && openPos[i] != null) doors[i].position = openPos[i].position;
        }

        hasStarted = false;
        hasFinished = false;
    }
}