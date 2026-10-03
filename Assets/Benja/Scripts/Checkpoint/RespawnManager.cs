using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    private Transform respawnPoint;
    private Transform initialSpawnPoint;  // ✅ GUARDAR SPAWN INICIAL

    private GameObject player;

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

        var Controlplayer = player.GetComponent<PlayerControl>();
        if (Controlplayer != null)
        {
            // ✅ TELETRANSPORTAR
            TeleportTo(targetSpawn);

            // ✅ RESETEAR isDead
            Controlplayer.isDead = false;

            // ✅ RESTAURAR TODOS LOS STATS AL MÁXIMO
            var statsManager = Controlplayer.PlayerStatsManager;
            if (statsManager != null)
            {
                statsManager.RestoreFull(StatType.Vida);
                statsManager.RestoreFull(StatType.Maná);
                statsManager.RestoreFull(StatType.Estamina);

                Debug.Log($"❤️ Vida: {statsManager.GetActualValue(StatType.Vida)}/{statsManager.GetMaxValue(StatType.Vida)}");
                Debug.Log($"🔵 Maná: {statsManager.GetActualValue(StatType.Maná)}/{statsManager.GetMaxValue(StatType.Maná)}");
                Debug.Log($"⚡ Estamina: {statsManager.GetActualValue(StatType.Estamina)}/{statsManager.GetMaxValue(StatType.Estamina)}");
            }

            Debug.Log($"♻️ Player respawned en: {targetSpawn.name}");
        }
    }

    private void TeleportTo(Transform desiredPos)
    {
        if (player == null || desiredPos == null) return;

        PlayerControl controller = player.GetComponent<PlayerControl>();
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (controller != null) controller.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = desiredPos.position;
        }
        else
        {
            player.transform.position = desiredPos.position;
        }

        if (controller != null) controller.enabled = true;
    }
    public void SetPlayerTarget(GameObject player)
    {
        this.player = player;
    }
}