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
    }

    public bool IsDiscoveredLog(Log_Data logData)
    {
        return logData != null && discoveredLogs.Contains(logData);
    }

    public void AddLogToDiscovered(Log_Data logData)
    {
        if (logData == null || IsDiscoveredLog(logData)) return;

        discoveredLogs.Add(logData);
        OnDiscoveredLogsChange?.Invoke();
    }

    public void RemoveLogFromDiscovered(Log_Data logData)
    {
        if (logData == null || !discoveredLogs.Remove(logData)) return;

        OnDiscoveredLogsChange?.Invoke();
    }
}
