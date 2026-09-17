using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCombat_Tutorial : MonoBehaviour
{
    [Header("Envioriment event")]
    [SerializeField] private float doorSpeed;
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Transform openPos;
    [SerializeField] private Transform closePos;

    [Header("Enemy spawn")]
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    [SerializeField] private GameObject spawnVfx;
    private bool hasActivated;

    [SerializeField] private CameraRequest cameraShowEvent;

    [Header("UI")]
    [SerializeField] private List<PopUpPage> pages = new List<PopUpPage>();
    private UIPopUp ui;

    private List<GameObject> enemies = new();
    private PlayerControl playerControl;

    private void Awake()
    {
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
        }

        ui = FindAnyObjectByType<UIPopUp>();
    }

    private void OnDestroy()
    {
        if (PlayerSpawn_Manager.Instance != null)
        {
            PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
        }
    }
    private void OnPlayerSpawned(PlayerControl control)
    {
        playerControl = control;
    }

    private IEnumerator RunEvent()
    {
        //Sequence
        hasActivated = true;

        //Mute the player
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
        }
        //->Camara show the enemy
        GameModeManager.Instance.SetMode(GameMode.Cutscene);

        //-> Add camera event
        if (cameraShowEvent != null)
        {
            CameraEventRelay.Instance.Play(cameraShowEvent);
        }

        //->Enemy close the door
        yield return SetDoorPos(closePos);

        //->Block player path so it cant go away or advance, need to be show!!

        //->Show UI
        if (ui != null) ui.ShowPopUp(pages);

        while (GameModeManager.Instance.CurrentMode == GameMode.UI)
        {
            yield return null;
        }

        //->Combat
        GameModeManager.Instance.SetMode(GameMode.Gameplay);

        //Remove mute from the player
        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

        //->Check if enemy still alive
        //->If death, desactve barrier
        //->Opem door
        if (enemies.Count <= 0)
        {
            SetDoorPos(openPos);
        }

        //yield return StartSpawn();
    }

    private IEnumerator SetDoorPos(Transform finalDestination)
    {
        while (doorTransform.position != finalDestination.position)
        {
            doorTransform.position = Vector3.MoveTowards(doorTransform.position, finalDestination.position, doorSpeed * Time.deltaTime);
            yield return null;
        }

        doorTransform.position = finalDestination.position;
    }

    private IEnumerator StartSpawn()
    {
        if (cameraShowEvent != null)
        {
            CameraEventRelay.Instance.Play(cameraShowEvent);
        }

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
        if (hasActivated) return;

        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(RunEvent());
        }
    }
}
