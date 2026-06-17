using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    private Transform respawnPoint;
    private Transform initialSpawnPoint;  // ✅ GUARDAR SPAWN INICIAL

    private void Awake()
    {
        Instance = this;
    }

    public void SetInitialSpawnPoint(Transform spawnPoint)
    {
        initialSpawnPoint = spawnPoint;
        Debug.Log($"✅ Initial spawn point guardado: {spawnPoint.name}");
    }

    public void SetRespawn(Transform spawnPoint)
    {
        respawnPoint = spawnPoint;
        Debug.Log($"✅ Respawn point establecido: {spawnPoint.name}");
    }

    public void Respawn()
    {
        // ✅ PRIORIDAD: Checkpoint actual, si no -> Initial spawn
        Transform targetSpawn = respawnPoint != null ? respawnPoint : initialSpawnPoint;

        if (targetSpawn == null)
        {
            Debug.LogError("❌ No hay spawnPoint disponible");
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerControl>();
        if (player != null)
        {
            player.transform.position = targetSpawn.position;
            player.transform.rotation = targetSpawn.rotation;
            Debug.Log($"♻️ Player respawned en: {targetSpawn.name}");
        }
    }
}