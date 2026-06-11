using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private readonly List<Checkpoint> discoveredCheckpoints =
        new();

    private Checkpoint activeCheckpoint;

    private PlayerStatsManager playerStats;

    private void Awake()
    {
        Instance = this;

        playerStats =
            FindFirstObjectByType<PlayerStatsManager>();
    }

    public void Interact(Checkpoint checkpoint)
    {
        SetActiveCheckpoint(checkpoint);

        bool firstDiscovery =
            DiscoverCheckpoint(checkpoint);

        if (firstDiscovery)
        {
            playerStats.AddUpgradePoints(
                checkpoint.Data.upgradePointsReward);

            PopupUI.Instance.Show(
                $"+{checkpoint.Data.upgradePointsReward} puntos por descubrir\n{checkpoint.Data.checkpointName}");
        }

        CheckpointMenuUI.Instance.Open(checkpoint);
    }

    private bool DiscoverCheckpoint(
        Checkpoint checkpoint)
    {
        if (discoveredCheckpoints.Contains(checkpoint))
            return false;

        discoveredCheckpoints.Add(checkpoint);

        return true;
    }

    private void SetActiveCheckpoint(
        Checkpoint checkpoint)
    {
        activeCheckpoint = checkpoint;

        RespawnManager.Instance.SetRespawn(
            checkpoint.SpawnPoint);
    }

    public IReadOnlyList<Checkpoint>
        GetDiscoveredCheckpoints()
    {
        return discoveredCheckpoints;
    }

    public Checkpoint GetActiveCheckpoint()
    {
        return activeCheckpoint;
    }
}