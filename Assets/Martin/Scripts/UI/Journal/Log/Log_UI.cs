using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Log_UI : MonoBehaviour
{
    [Header("Log UI Information")]
    [SerializeField] private TextMeshProUGUI logTitle;
    [SerializeField] private Image logImage;
    [SerializeField] private TextMeshProUGUI logDescription;

    [Header("Scroll Options")]
    [SerializeField] private Transform logsContainer;
    [SerializeField] private GameObject logEntry_Prefab;

    private readonly List<LogEntry> logEntries = new();

    private Log_Manager logManager;
    private Log_Data selectedLog;

    private void OnEnable()
    {
        logManager = Log_Manager.Instance;

        if (logManager == null)
        {
            Debug.LogWarning("Log_UI: No Log_Manager found.");
            return;
        }

        logManager.OnDiscoveredLogsChange += RefreshLogEntries;
        RefreshLogEntries();
    }

    private void OnDisable()
    {
        if (logManager != null) logManager.OnDiscoveredLogsChange -= RefreshLogEntries;

        ClearLogEntries();
        logManager = null;
    }

    private void RefreshLogEntries()
    {
        ClearLogEntries();

        IReadOnlyList<Log_Data> discoveredLogs = logManager.DiscoveredLogs;

        foreach (Log_Data logData in discoveredLogs)
        {
            if (logData == null) continue;

            GameObject instance = Instantiate(logEntry_Prefab, logsContainer);
            UINavigation.DisableNavigation(instance.transform);
            LogEntry entry = instance.GetComponent<LogEntry>();

            if (entry == null)
            {
                Destroy(instance);
                continue;
            }

            entry.Initialize(logData);
            entry.OnSelected += ShowLogInformation;

            logEntries.Add(entry);
        }

        // Keep the current selection if it still exists.
        if (selectedLog != null && logManager.IsDiscoveredLog(selectedLog))
        {
            ShowLogInformation(selectedLog);
        }
        else if (discoveredLogs.Count > 0)
        {
            selectedLog = null;

            foreach (Log_Data logData in discoveredLogs)
            {
                if (logData != null)
                {
                    ShowLogInformation(logData);
                    break;
                }
            }

            if (selectedLog == null) ClearLogInformation();
        }
        else
        {
            selectedLog = null;
            ClearLogInformation();
        }
    }

    private void ClearLogEntries()
    {
        foreach (LogEntry entry in logEntries)
        {
            if (entry == null) continue;

            entry.OnSelected -= ShowLogInformation;
            Destroy(entry.gameObject);
        }

        logEntries.Clear();
    }

    private void ShowLogInformation(Log_Data logData)
    {
        if (logData == null) return;

        selectedLog = logData;

        logTitle.text = logData.logName;
        logDescription.text = logData.logDescription;
        logImage.sprite = logData.logIllustration;
        logImage.enabled = logData.logIllustration != null;
    }

    private void ClearLogInformation()
    {
        logTitle.text = "";
        logDescription.text = "";
        logImage.sprite = null;
        logImage.enabled = false;
    }
}
