using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCombat_Tutorial : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    [SerializeField] private GameObject spawnVfx;
    private bool hasStarted;
    private bool hasFinished;

    [Header("Door")]
    [SerializeField] private float doorSpeed;
    [SerializeField] private Transform door;
    [SerializeField] private Transform closePos;
    [SerializeField] private Transform openPos;

    [Header("Camera")]
    [SerializeField] private CameraRequest cameraEvent;

    [Header("UI")]
    [SerializeField] private List<PopUpPage> pages = new List<PopUpPage>();
    private UIPopUp ui;

    private List<GameObject> enemies = new();
    private PlayerControl playerControl;

    private void Awake()
    {
        // Suscribirse a OnPlayerSpawned para obtener referencia al jugador
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
        }

        ui = FindAnyObjectByType<UIPopUp>();

        door.position = openPos.position;
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
        //Open door
        hasStarted = true;

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
        }

        //Start Cutscene
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (cameraEvent != null)
        {
            CameraEventRelay.Instance.Play(cameraEvent);
        }

        //Close Door
        yield return MoveDoor(closePos);

        //Spawn enemy
        yield return StartSpawn();

        //Show UI
        if (ui != null) ui.ShowPopUp(pages);

        while (GameModeManager.Instance.CurrentMode == GameMode.UI)
        {
            yield return null;
        }

        //Start gameplay
        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

        //Check enemy death
        while (enemies.Count > 0)
        {
            enemies.RemoveAll(enemy => enemy == null);
            yield return null;
        }

        //Start Cutscene
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
        }

        //Open Door
        yield return MoveDoor(openPos);

        //Start gameplay
        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

        hasFinished = true;
    }

    private IEnumerator MoveDoor(Transform finalPos)
    {
        if (finalPos == null) yield break;

        while (Vector3.Distance(door.position, finalPos.position) > 0.01f)
        {
            door.position = Vector3.MoveTowards(door.position, finalPos.position, doorSpeed * Time.deltaTime);
            yield return null;
        }

        door.transform.position = finalPos.position;
    }

    private IEnumerator StartSpawn()
    {
        enemies.Clear();

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

        if (spawnVfx != null)
        {
            GameObject vfx = Instantiate(spawnVfx, entry.spawnPoint.position, entry.spawnPoint.rotation);

            Destroy(vfx, 1f);
        }

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

        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
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

        door.position = openPos.position;

        hasStarted = false;
        hasFinished = false;
    }
}