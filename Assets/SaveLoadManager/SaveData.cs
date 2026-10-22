using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public struct StatSavedEntry
{
    public StatsType statType;
    public int points;
}
[System.Serializable]
public class SaveData
{
    public int currentIndexCharacter;
    public List<StatSavedEntry> savedStats = new();
    public int availablePoints;
    public int totalEarnedPoints;
    public List<DiscoveredCheckpoint> discoveredCheckpoints;
    public DiscoveredCheckpoint activeCheckpoint;
    public Checkpoint checkpoint;
    public List<Skill> unlockedSkills;
    public Skill[] equipedSkills;
    public bool gameCreated;
}
