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
            // ✅ TELETRANSPORTAR
            player.transform.position = targetSpawn.position;
            player.transform.rotation = targetSpawn.rotation;

            // ✅ RESETEAR isDead
            player.isDead = false;

            // ✅ RESTAURAR TODOS LOS STATS AL MÁXIMO
            var statsManager = player.PlayerStatsManager;
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
}