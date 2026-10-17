using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class SaveData
{
    public int currentIndexCharacter;
    public StatsType allocatedPoints;
    public int availablePoints;
    public List<DiscoveredCheckpoint> discoveredCheckpoints;
    public DiscoveredCheckpoint activeCheckpoint;
    public Checkpoint checkpoint;
    public List<Skill> unlockedSkills;
    public Skill[] equipedSkills;
    public bool gameCreated;
}
