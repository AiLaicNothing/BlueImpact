using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    // Persistent checkpoint reference: no scene Transform is stored.
    private string respawnSceneName;
    private string respawnCheckpointID;

    // Initial spawn fallback, stored as values that survive scene unloads.
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

        Debug.Log($"Initial spawn saved: {spawnPoint.name} in {initialSceneName}");
    }

    // Called when the player activates a checkpoint.
    public void SetRespawn(DiscoveredCheckpoint checkpoint)
    {
        if (checkpoint == null) return;

        respawnSceneName = checkpoint._sceneName;
        respawnCheckpointID = checkpoint._checkpointID;

        Debug.Log($"Respawn checkpoint set: {respawnCheckpointID} in {respawnSceneName}");
    }

    public void Respawn()
    {
        if (isRespawning)
            return;

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

        // Checkpoint takes priority over the initial spawn.
        if (!string.IsNullOrEmpty(respawnSceneName) && !string.IsNullOrEmpty(respawnCheckpointID))
        {
            targetSceneName = respawnSceneName;

            yield return EnsureSceneLoaded(targetSceneName);

            Checkpoint checkpoint = Checkpoints_Manager.Instance.FindCheckpointInScene( targetSceneName, respawnCheckpointID);

            if (checkpoint == null || checkpoint.SpawnPoint == null)
            {
                Debug.LogError($"Could not find checkpoint '{respawnCheckpointID}'or its spawn point in scene '{targetSceneName}'.");

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

        // Set the active scene if it has been loaded.
        Scene loadedScene = SceneManager.GetSceneByName(targetSceneName);

        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        // Teleport the player to the spawn point.
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

//{
//    public static RespawnManager Instance { get; private set; }

//    private Transform respawnPoint;
//    private Transform initialSpawnPoint;  // ✅ GUARDAR SPAWN INICIAL

//    private GameObject player;

//    private void Awake()
//    {
//        Instance = this;
//    }

//    public void SetInitialSpawnPoint(Transform spawnPoint)
//    {
//        initialSpawnPoint = spawnPoint;
//        Debug.Log($"✅ Initial spawn point guardado: {spawnPoint.name}");
//    }

//    public void SetRespawn(Transform spawnPoint)
//    {
//        respawnPoint = spawnPoint;
//        Debug.Log($"✅ Respawn point establecido: {spawnPoint.name}");
//    }

//    public void Respawn()
//    {
//        // ✅ PRIORIDAD: Checkpoint actual, si no -> Initial spawn
//        Transform targetSpawn = respawnPoint != null ? respawnPoint : initialSpawnPoint;

//        if (targetSpawn == null)
//        {
//            Debug.LogError("❌ No hay spawnPoint disponible");
//            return;
//        }

//        var Controlplayer = player.GetComponent<PlayerControl>();
//        if (Controlplayer != null)
//        {
//            // ✅ TELETRANSPORTAR
//            TeleportTo(targetSpawn);

//            // ✅ RESETEAR isDead
//            Controlplayer.isDead = false;

//            // ✅ RESTAURAR TODOS LOS STATS AL MÁXIMO
//            var statsManager = Controlplayer.PlayerStatsManager;
//            if (statsManager != null)
//            {
//                statsManager.RestoreFull(StatType.Vida);
//                statsManager.RestoreFull(StatType.Maná);
//                statsManager.RestoreFull(StatType.Estamina);

//                Debug.Log($"❤️ Vida: {statsManager.GetActualValue(StatType.Vida)}/{statsManager.GetMaxValue(StatType.Vida)}");
//                Debug.Log($"🔵 Maná: {statsManager.GetActualValue(StatType.Maná)}/{statsManager.GetMaxValue(StatType.Maná)}");
//                Debug.Log($"⚡ Estamina: {statsManager.GetActualValue(StatType.Estamina)}/{statsManager.GetMaxValue(StatType.Estamina)}");
//            }

//            Debug.Log($"♻️ Player respawned en: {targetSpawn.name}");
//        }
//    }

//    private void TeleportTo(Transform desiredPos)
//    {
//        if (player == null || desiredPos == null) return;

//        PlayerControl controller = player.GetComponent<PlayerControl>();
//        Rigidbody rb = player.GetComponent<Rigidbody>();

//        if (controller != null) controller.enabled = false;

//        if (rb != null)
//        {
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;
//            rb.position = desiredPos.position;
//        }
//        else
//        {
//            player.transform.position = desiredPos.position;
//        }

//        if (controller != null) controller.enabled = true;
//    }
//    public void SetPlayerTarget(GameObject player)
//    {
//        this.player = player;
//    }
//}