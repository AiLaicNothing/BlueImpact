using System.Collections.Generic;
using UnityEngine;

public class Event_Manager : MonoBehaviour
{
    public static Event_Manager Instance { get; private set; }

    private readonly Dictionary<string, WorldEvent_State> eventStates = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (SaveLoadManager.canLoad)
        {
            LoadData();
        }
    }
    void LoadData()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            LoadStates(loadedData.eventStates);
        }
    }

    public void SaveState(WorldEvent_Data eventData,bool completed, int state = 0)
    {
        if (eventData == null || string.IsNullOrWhiteSpace(eventData.Event_ID))
        {
            Debug.LogWarning("Invalid WorldEvent_Data.");
            return;
        }

        eventStates[eventData.Event_ID] = new WorldEvent_State(completed, state);
        SaveLoadManager.Instance.SaveGame("eventStates", eventStates);
    }

    public bool TryGetState(WorldEvent_Data eventData, out WorldEvent_State savedState)
    {
        savedState = null;

        if (eventData == null) return false;

        if (!eventStates.TryGetValue(eventData.Event_ID, out WorldEvent_State storedState))
        {
            return false;
        }

        savedState = new WorldEvent_State(storedState.completed, storedState.state);

        return true;
    }

    public bool IsCompleted(WorldEvent_Data eventData)
    {
        return TryGetState(eventData, out WorldEvent_State state) && state.completed;
    }

    // Future Save_Manager can request a copy of these entries.
    public Dictionary<string, WorldEvent_State> GetAllStates()
    {
        Dictionary<string, WorldEvent_State> copy = new();  

        foreach (var entry in eventStates)
        {
            copy.Add(entry.Key, new WorldEvent_State( entry.Value.completed, entry.Value.state));
        }

        return copy;
    }

    // Called after loading the saved data from disk.
    public void LoadStates(List<EventStateSavedEntry> savedStates)
    {
        eventStates.Clear();

        if (savedStates == null) return;

        foreach (var entry in savedStates)
        {
            if (string.IsNullOrWhiteSpace(entry.eventID) || entry.stateData == null) continue;

            eventStates[entry.eventID] = new WorldEvent_State(entry.stateData.completed, entry.stateData.state);
        }
    }
}
