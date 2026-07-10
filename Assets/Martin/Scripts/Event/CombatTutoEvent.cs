using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTutoEvent : MonoBehaviour
{
    [SerializeField] private List<SpawnEntry> spawnEntries = new();
    [SerializeField] private GameObject spawnVfx;
    private bool hasActivated;

    [SerializeField] private CameraRequest cameraShowSpawn;

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
        hasActivated = true;

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(true);
        }

        //Show -> TutorialPopUp
        //-> Activate this stop time

        Debug.Log("Change to UI");

        if (ui != null) ui.ShowPopUp(pages);
        Debug.Log("Show pop up");

        while(GameModeManager.Instance.CurrentMode == GameMode.UI)
        {
            yield return null;
        }

        GameModeManager.Instance.SetMode(GameMode.Cutscene);
        Debug.Log("Change to cutscene");

        yield return StartSpawn();

        GameModeManager.Instance.SetMode(GameMode.Gameplay);
        Debug.Log("Change to gameplay");

        if (playerControl != null)
        {
            playerControl.MutePlayerAudio(false);
        }

    }

    private IEnumerator StartSpawn()
    {
        if (cameraShowSpawn != null)
        {
            CameraEventRelay.Instance.Play(cameraShowSpawn);
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
