using UnityEngine;

[CreateAssetMenu(menuName = "Checkpoint/Checkpoint Data")]
public class CheckpointData : ScriptableObject
{
    public string checkpointID;

    public string checkpointName;

    public Sprite previewImage;

    public int upgradePointsReward = 5;
}