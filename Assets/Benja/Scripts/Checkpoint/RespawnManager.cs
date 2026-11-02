using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    // Checkpoint persistente: no se guarda ningún Transform de escena.
    private string respawnSceneName;
    private string respawnCheckpointID;

    // Spawn inicial de respaldo, guardado como valores que sobreviven a la descarga de escenas.
    private string initialSceneName;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private bool hasInitialSpawn;

    private GameObject player;
    private bool isRespawning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetInitialSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("Initial spawn point is null.");
            return;
        }

        initialSceneName = spawnPoint.gameObject.scene.name;
        initialSpawnPosition = spawnPoint.position;
        initialSpawnRotation = spawnPoint.rotation;
        hasInitialSpawn = true;
    }

    // Se llama cuando el jugador activa un checkpoint.
    public void SetRespawn(DiscoveredCheckpoint checkpoint)
    {
        if (checkpoint == null) return;

        respawnSceneName = checkpoint._sceneName;
        respawnCheckpointID = checkpoint._checkpointID;

        Debug.Log($"Respawn checkpoint set: {respawnCheckpointID} in {respawnSceneName}");
    }

    public void Respawn()
    {
        if (isRespawning) return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (player == null)
        {
            Debug.LogError("Respawn failed: player target has not been assigned.");
            yield break;
        }

        isRespawning = true;

        Vector3 targetPosition;
        Quaternion targetRotation;
        string targetSceneName;

        // El checkpoint tiene prioridad sobre el spawn inicial.
        if (!string.IsNullOrEmpty(respawnSceneName) && !string.IsNullOrEmpty(respawnCheckpointID))
        {
            targetSceneName = respawnSceneName;

            yield return EnsureSceneLoaded(targetSceneName);

            Checkpoint checkpoint = Checkpoints_Manager.Instance != null
                ? Checkpoints_Manager.Instance.FindCheckpointInScene(targetSceneName, respawnCheckpointID)
                : null;

            if (checkpoint == null || checkpoint.SpawnPoint == null)
            {
                Debug.LogError($"Could not find checkpoint '{respawnCheckpointID}' or its spawn point in scene '{targetSceneName}'.");
                isRespawning = false;
                yield break;
            }

            targetPosition = checkpoint.SpawnPoint.position;
            targetRotation = checkpoint.SpawnPoint.rotation;
        }
        else if (hasInitialSpawn)
        {
            targetSceneName = initialSceneName;

            yield return EnsureSceneLoaded(targetSceneName);

            targetPosition = initialSpawnPosition;
            targetRotation = initialSpawnRotation;
        }
        else
        {
            Debug.LogError("Respawn failed: no checkpoint or initial spawn saved.");
            isRespawning = false;
            yield break;
        }

        // Activar la escena si está cargada.
        Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);

        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        // Teletransportar al jugador al punto de spawn.
        TeleportTo(targetPosition, targetRotation);

        PlayerControl playerControl = player.GetComponent<PlayerControl>();

        if (playerControl != null)
        {
            playerControl.isDead = false;

            var statsManager = playerControl.PlayerStatsManager;

            if (statsManager != null)
            {
                statsManager.RestoreFull(StatType.Vida);
                statsManager.RestoreFull(StatType.Maná);
                statsManager.RestoreFull(StatType.Estamina);
            }

            var stats = playerControl._Stats;

            if (stats != null)
            {
                stats.RestoreToMax();
            }
        }

        Debug.Log($"Player respawned in scene: {targetSceneName}");

        isRespawning = false;
    }

    private IEnumerator EnsureSceneLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Respawn scene name is empty.");
            yield break;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (scene.IsValid() && scene.isLoaded)
            yield break;

        if (SceneLoaderManager.Instance == null)
        {
            Debug.LogError("SceneLoaderManager.Instance is missing.");
            yield break;
        }

        yield return SceneLoaderManager.Instance.LoadSceneAndWait(sceneName);
    }

    private void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (player == null) return;

        PlayerControl playerControl = player.GetComponent<PlayerControl>();
        Rigidbody rb = player.GetComponent<Rigidbody>();
        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController != null) characterController.enabled = false;
        if (playerControl != null) playerControl.blockVelocity = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(position, rotation);
        }

        if (characterController != null) characterController.enabled = true;
        if (playerControl != null) playerControl.blockVelocity = false;
    }

    public void SetPlayerTarget(GameObject player)
    {
        this.player = player;
    }
}
