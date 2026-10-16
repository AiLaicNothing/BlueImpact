using UnityEngine;

[System.Serializable]
public class DiscoveredCheckpoint 
{
    public string _checkpointID;
    public string _sceneName;
    public CheckpointData _data;

    public DiscoveredCheckpoint(Checkpoint checkpoint)
    {
        _checkpointID = checkpoint.Data.checkpointID;
        _sceneName = checkpoint.Data.locationSceneName;
        _data = checkpoint.Data;
    }

    public bool Matches(string sceneName, string checkpointID)
    {
        return _sceneName == sceneName && _checkpointID == checkpointID;
    }
}
