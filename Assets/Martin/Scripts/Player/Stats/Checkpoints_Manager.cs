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

        if (SaveLoadManager.canLoad) LoadData();
    }
    private void Start()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            if (RespawnManager.Instance != null)
            {
                if (loadedData.activeCheckpoint != null)
                {
                    Debug.Log("go");
                }
                else
                {
                    Debug.Log("tttttttttttttttttttttttttttt");
                }
                RespawnManager.Instance.SetRespawn(activeCheckpoint);
            }
        }
    }
    void LoadData()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            discoveredCheckpoints.Clear();
            for(int i = 0; i < loadedData.discoveredCheckpoints.Count; i++)
            {
                discoveredCheckpoints.Add(loadedData.discoveredCheckpoints[i]);
            }
            activeCheckpoint = loadedData.activeCheckpoint;
            
        }
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
        SaveLoadManager.Instance.SaveGame("discoveredCheckpoints", discoveredCheckpoints);
        if (debug) Debug.Log($"[Checkpoint] Discovered {record._checkpointID} in {record._sceneName}");

        return true;
    }

    public void SetActiveCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || checkpoint.Data == null) return;

        //Set new checkpoint
        activeCheckpoint = new DiscoveredCheckpoint(checkpoint);

        SaveLoadManager.Instance.SaveGame("activeCheckpoint", activeCheckpoint);
        if (RespawnManager.Instance != null) RespawnManager.Instance.SetRespawn(activeCheckpoint); 
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
