using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private readonly List<Checkpoint> discoveredCheckpoints = new();

    private Checkpoint activeCheckpoint;
    private PlayerStatsManager playerStats;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // ✅ SUSCRIBIRSE AL EVENTO DE SPAWN
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        // ✅ DESUSCRIBIRSE
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerControl player)
    {
        // ✅ GUARDAR REFERENCIA CUANDO SE CREA EL PLAYER
        playerStats = player.GetComponent<PlayerStatsManager>();

        if (playerStats == null)
            Debug.LogError("❌ PlayerStatsManager no encontrado en el player spawneado");
    }

    public void Interact(Checkpoint checkpoint)
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStatsManager no inicializado");
            return;
        }

        SetActiveCheckpoint(checkpoint);
        bool firstDiscovery = DiscoverCheckpoint(checkpoint);

        // ✅ PRIMER DESCUBRIMIENTO: Mostrar popup y dar upgrade points
        if (firstDiscovery)
        {
            playerStats.AddUpgradePoints(checkpoint.Data.upgradePointsReward);

            // ✅ NULL CHECK para PopupUI
            if (PopupUI.Instance != null)
            {
                PopupUI.Instance.Show($"¡Checkpoint Descubierto!\n+{checkpoint.Data.upgradePointsReward} Puntos de Mejora");
                Debug.Log($"✅ Popup mostrado para checkpoint: {checkpoint.Data.checkpointName}");
            }
            else
            {
                Debug.LogWarning("⚠️ PopupUI.Instance es null - popup no mostrado");
            }
        }

        // ✅ NULL CHECK para CheckpointMenuUI
        if (CheckpointMenuUI.Instance != null)
        {
            CheckpointMenuUI.Instance.Open(checkpoint);
        }
        else
        {
            Debug.LogError("❌ CheckpointMenuUI.Instance es null");
        }
    }

    private bool DiscoverCheckpoint(Checkpoint checkpoint)
    {
        if (discoveredCheckpoints.Contains(checkpoint))
            return false;

        discoveredCheckpoints.Add(checkpoint);
        return true;
    }

    private void SetActiveCheckpoint(Checkpoint checkpoint)
    {
        activeCheckpoint = checkpoint;

        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.SetRespawn(checkpoint.SpawnPoint);
        }
        else
        {
            Debug.LogError("❌ RespawnManager.Instance es null");
        }
    }

    public IReadOnlyList<Checkpoint> GetDiscoveredCheckpoints()
    {
        return discoveredCheckpoints;
    }

    public Checkpoint GetActiveCheckpoint()
    {
        return activeCheckpoint;
    }
}