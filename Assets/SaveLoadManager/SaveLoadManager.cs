using UnityEngine;
using System.IO;
using System.Collections.Generic;
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }
    string filePath;
    public static bool canLoad;
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        filePath = Application.persistentDataPath + "/savefile.json";
    }
    private void Start()
    {
    }

    public void SaveGame(string fieldName, object value)
    {
        SaveData data = LoadGameRaw();

        switch (fieldName)
        {
            case "currentIndexCharacter":
                data.currentIndexCharacter = (int)value;
                break;
            case "allocatedPoints":
                data.allocatedPoints = (StatsType)value;
                break;
            case "availablePoints":
                data.availablePoints = (int)value;
                break;
            case "discoveredCheckpoints":
                data.discoveredCheckpoints = (List<DiscoveredCheckpoint>)value;
                break;
            case "activeCheckpoint":
                data.activeCheckpoint = (DiscoveredCheckpoint)value;
                break;
            case "unlockedSkills":
                data.unlockedSkills = (List<Skill>)value;
                break;
            case "equipedSkills":
                data.equipedSkills = (Skill[])value;
                break;
            case "gameCreated":
                data.gameCreated = (bool)value;
                break;
            case "checkpoint":
                data.checkpoint = (Checkpoint)value;
                break;
        }
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);

        Debug.Log("Guardado en" + filePath);
    }

    public SaveData LoadGame()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            Debug.Log("Juego Cargado");
            return data;
        }
        else
        {
            Debug.LogWarning("Savegame no encontrado");
            return null;
        }
    }
    private SaveData LoadGameRaw()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return new SaveData();
    }
}
