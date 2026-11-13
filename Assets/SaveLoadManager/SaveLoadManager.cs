using UnityEngine;
using System;
using System.IO;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // Lo fija el menú: true al pulsar "Continuar", false en "Nueva partida".
    public static bool canLoad;

    // Se calcula al usarse (no en Awake): ningún script que guarde antes que este recibe una ruta null.
    private static string FilePath => Path.Combine(Application.persistentDataPath, "savefile.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveGame(string fieldName, object value)
    {
        SaveData data = LoadGameRaw();
        if (data == null) return; // No sobrescribir el archivo si no se pudo leer

        switch (fieldName)
        {
            case "currentIndexCharacter":
                data.currentIndexCharacter = (int)value;
                break;
            case "allocatedPoints":
                var dict = (System.Collections.Generic.Dictionary<StatsType, int>)value;
                data.savedStats.Clear();
                foreach (var kvp in dict)
                {
                    data.savedStats.Add(new StatSavedEntry { statType = kvp.Key, points = kvp.Value });
                }
                break;
            case "collectedPickups":
                var pickupSet = (System.Collections.Generic.HashSet<string>)value;
                data.collectedPickups.Clear();
                data.collectedPickups.AddRange(pickupSet);
                break;
            case "eventStates":
                var eventDict = (System.Collections.Generic.Dictionary<string, WorldEvent_State>)value;
                data.eventStates.Clear();
                foreach (var kvp in eventDict)
                {
                    data.eventStates.Add(new EventStateSavedEntry { eventID = kvp.Key, stateData = kvp.Value });
                }
                break;
            case "discoveredLogs":
                data.discoveredLogs = (System.Collections.Generic.List<Log_Data>)value;
                break;
            case "inspectedObjects":
                data.inspectedObjects = (System.Collections.Generic.List<Log_Data>)value;
                break;
            case "inventory":
                data.inventory = (System.Collections.Generic.List<Item_Data>)value;
                break;
            case "availablePoints":
                data.availablePoints = (int)value;
                break;
            case "totalEarnedPoints":
                data.totalEarnedPoints = (int)value;
                break;
            case "discoveredCheckpoints":
                data.discoveredCheckpoints = (System.Collections.Generic.List<DiscoveredCheckpoint>)value;
                break;
            case "activeCheckpoint":
                data.activeCheckpoint = (DiscoveredCheckpoint)value;
                break;
            case "unlockedSkills":
                data.unlockedSkills = (System.Collections.Generic.List<Skill>)value;
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
            case "levelKills":
                data.levelKills = (System.Collections.Generic.List<LevelKillsSavedEntry>)value;
                break;
            default:
                Debug.LogWarning($"SaveLoadManager: campo desconocido '{fieldName}'. No se guardó.");
                return;
        }

        WriteFile(JsonUtility.ToJson(data, true));
    }

    // Devuelve null si no hay guardado o si no se pudo leer.
    public SaveData LoadGame()
    {
        if (!File.Exists(FilePath)) return null;

        return ReadFile();
    }

    // Para guardar campos sueltos: si no existe archivo, parte de uno vacío.
    // Devuelve null si el archivo existe pero no se pudo leer (así no se pierde progreso).
    private static SaveData LoadGameRaw()
    {
        if (!File.Exists(FilePath)) return new SaveData();

        return ReadFile();
    }

    private static SaveData ReadFile()
    {
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
        }
        catch (IOException e)
        {
            Debug.LogError($"SaveLoadManager: no se pudo leer '{FilePath}': {e.Message}");
            return null;
        }
        catch (ArgumentException)
        {
            // JSON corrupto: se respalda para no perderlo y se empieza de cero.
            Debug.LogWarning("SaveLoadManager: guardado corrupto. Se respalda como .corrupt y se empieza de nuevo.");
            try { File.Copy(FilePath, FilePath + ".corrupt", true); }
            catch (IOException) { }
            return new SaveData();
        }
    }

    private static void WriteFile(string json)
    {
        try
        {
            File.WriteAllText(FilePath, json);
        }
        catch (IOException e)
        {
            Debug.LogError($"SaveLoadManager: no se pudo escribir el guardado: {e.Message}");
        }
    }
}
