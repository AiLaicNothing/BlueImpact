using System.Collections.Generic;
using UnityEngine;

public class Checkpoints_Manager : MonoBehaviour
{
    public static Checkpoints_Manager Instance { get; private set; } 

    private readonly List<DiscoveredCheckpoint> discoveredCheckpoints = new();

    private DiscoveredCheckpoint activeCheckpoint;

    public DiscoveredCheckpoint ActiveCheckpoint => activeCheckpoint;

    [SerializeField] private bool debug;

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

    public bool DiscoverCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || checkpoint.Data == null) return false;

        var record = new DiscoveredCheckpoint(checkpoint);

        foreach (var discoveredCheckpoint in discoveredCheckpoints)
        {
            if (discoveredCheckpoint.Matches(record._sceneName, record._checkpointID)) return false;
        }

        discoveredCheckpoints.Add(record);

        if (debug) Debug.Log($"[Checkpoint] Discovered {record._checkpointID} in {record._sceneName}");

        return true;
    }

    public void SetActiveCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || checkpoint.Data == null) return;

        //Set new checkpoint
        activeCheckpoint = new DiscoveredCheckpoint(checkpoint);

        if (RespawnManager.Instance != null) RespawnManager.Instance.SetRespawn(activeCheckpoint); ;
    }

    public IReadOnlyList<DiscoveredCheckpoint> GetDiscoveredCheckpoints()
    {
        return discoveredCheckpoints;
    }

    public DiscoveredCheckpoint GetActiveCheckpoint()
    {
        return activeCheckpoint;
    }

    public Checkpoint FindCheckpointInScene(string sceneName, string checkpointID)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);

        if (!scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Checkpoint[] checkpoints = root.GetComponentsInChildren<Checkpoint>();

            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.Data != null && checkpoint.Data.checkpointID == checkpointID) return checkpoint;
            }
        }

        return null;
    }

}
