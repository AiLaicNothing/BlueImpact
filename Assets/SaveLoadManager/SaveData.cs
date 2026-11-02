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
    public List<string> collectedPickups = new();
    public List<EventStateSavedEntry> eventStates = new();
    public int availablePoints;
    public int totalEarnedPoints;
    public List<DiscoveredCheckpoint> discoveredCheckpoints = new();
    public List<Log_Data> discoveredLogs = new();
    public List<Log_Data> inspectedObjects = new();
    public List<Item_Data> inventory = new();
    public DiscoveredCheckpoint activeCheckpoint;
    public Checkpoint checkpoint;
    public List<Skill> unlockedSkills = new();
    public Skill[] equipedSkills = new Skill[0];
    public bool gameCreated;
}
