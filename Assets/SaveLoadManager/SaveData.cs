using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public struct StatSavedEntry
{
    public StatsType statType;
    public int points;
}
[System.Serializable]
public struct EventStateSavedEntry
{
    public string eventID;
    public WorldEvent_State stateData;
}
[System.Serializable]
public class SaveData
{
    public int currentIndexCharacter;
    public List<StatSavedEntry> savedStats = new();
    public List<string> collectedPickups = new List<string>();
    public List<EventStateSavedEntry> eventStates = new List<EventStateSavedEntry>();
    public int availablePoints;
    public int totalEarnedPoints;
    public List<DiscoveredCheckpoint> discoveredCheckpoints;
    public DiscoveredCheckpoint activeCheckpoint;
    public Checkpoint checkpoint;
    public List<Skill> unlockedSkills;
    public Skill[] equipedSkills;
    public bool gameCreated;
}
