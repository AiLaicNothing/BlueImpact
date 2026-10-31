using System;
using System.Collections.Generic;
using UnityEngine;

public class Log_Manager : MonoBehaviour
{
    public static Log_Manager Instance { get; private set; }

    [Header("Discovered Logs")]
    [SerializeField] private List<Log_Data> discoveredLogs = new();

    public event Action OnDiscoveredLogsChange;

    public IReadOnlyList<Log_Data> DiscoveredLogs => discoveredLogs;

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
        SortDiscoveredLogs();
    }
    void LoadData()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            discoveredLogs.Clear();
            foreach (Log_Data log in loadedData.discoveredLogs)
            {
                discoveredLogs.Add(log);
            }
        }
    }

    public bool IsDiscoveredLog(Log_Data logData)
    {
        return logData != null && discoveredLogs.Contains(logData);
    }

    public void AddLogToDiscovered(Log_Data logData)
    {
        if (logData == null || IsDiscoveredLog(logData)) return;

        discoveredLogs.Add(logData);
        SortDiscoveredLogs();
        OnDiscoveredLogsChange?.Invoke();

        SaveLoadManager.Instance.SaveGame("discoveredLogs", discoveredLogs);
    }

    public void RemoveLogFromDiscovered(Log_Data logData)
    {
        if (logData == null || !discoveredLogs.Remove(logData)) return;

        OnDiscoveredLogsChange?.Invoke();
    }

    private void SortDiscoveredLogs()
    {
        discoveredLogs.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            return a.log_ID.CompareTo(b.log_ID);
        });
    }
}
