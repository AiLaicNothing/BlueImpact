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

    [SerializeField] private CameraRequest cameraEvent;
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
        // 📌 SetMode(Cutscene) llama automáticamente a LockPlayerControl()

        // 🔇 Silenciar audio
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[AmbushEvent] 🔒 Jugador pausado y silenciado");
        }

        yield return MoveDoor(closePos);

        yield return StartWave();

        // 🔓 Reactivar jugador para que pueda combatir
        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        // 📌 SetMode(Gameplay) llama automáticamente a UnlockPlayerControl()

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[AmbushEvent] 🔓 Jugador reactivado para combate");
        }

        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null);
            yield return null;
        }

        GameModeManager.Instance.SetMode(GameMode.Cutscene);
        // 📌 SetMode(Cutscene) llama automáticamente a LockPlayerControl()

        // 🔒 Pausar nuevamente al terminar la ola
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
            Debug.Log("[AmbushEvent] 🔒 Jugador pausado después de combate");
        }

        yield return MoveDoor(openPos);

        // 🔓 Reactivar jugador definitivamente
        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        // 📌 SetMode(Gameplay) llama automáticamente a UnlockPlayerControl()

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
            Debug.Log("[AmbushEvent] 🔓 Jugador reactivado - Evento completado");
        }

        hasFinished = true;
    }

    private IEnumerator MoveDoor(Transform[] targets)
    {
        if (targets == null || targets.Length != doors.Length) yield break;

        if (cameraEvent != null)
        {
            // 📌 Asegurar que cameraEvent tenga pausePlayer=true y mutePlayerAudio=true en inspector
            CameraEventRelay.Instance.Play(cameraEvent);
        }

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
            // 📌 Asegurar que cameraShowSpawn tenga pausePlayer=true y mutePlayerAudio=true en inspector
            CameraEventRelay.Instance.Play(cameraShowSpawn);
        }

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

        // 🔓 Reactivar jugador si el evento se cancela por muerte
        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        // 📌 SetMode(Gameplay) llama automáticamente a UnlockPlayerControl()

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