using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Inspection_Manager : MonoBehaviour
{
    public static Inspection_Manager Instance { get; private set; }

    [SerializeField] private List<Log_Data> inspectedObjects = new();
    public IReadOnlyList<Log_Data> InspectedItems => inspectedObjects;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsInspectedObject(Log_Data logData)
    {
        return logData != null && inspectedObjects.Contains(logData);
    }

    public void AddInspectedObject(Log_Data log_Data)
    {
        if (log_Data == null || IsInspectedObject(log_Data)) return;

        inspectedObjects.Add(log_Data);
    }
}
