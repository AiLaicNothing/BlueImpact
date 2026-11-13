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

    public List<string> discoveredCheckpoints = new();
    public string activeCheckpointID;
    public string currentCheckpointID;

    public List<int> inspectedObjectsIDs = new(); 
    public List<int> inventoryIDs = new();
    public List<int> discoveredLogsIDs = new();
    public List<string> unlockedSkillsIDs = new();
    public string[] equipedSkillsIDs = new string[0];

    public bool gameCreated;
}
