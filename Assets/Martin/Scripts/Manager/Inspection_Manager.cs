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
            inspectedObjects.Clear();
            foreach (Log_Data log in loadedData.inspectedObjects)
            {
                inspectedObjects.Add(log);
            }
        }
    }

    public bool IsInspectedObject(Log_Data logData)
    {
        return logData != null && inspectedObjects.Contains(logData);
    }

    public void AddInspectedObject(Log_Data log_Data)
    {
        if (log_Data == null || IsInspectedObject(log_Data)) return;

        inspectedObjects.Add(log_Data);
        SaveLoadManager.Instance.SaveGame("inspectedObjects", inspectedObjects);
    }
}
