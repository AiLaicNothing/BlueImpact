using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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
                List<Log_Data> logs = (List<Log_Data>)value;
                data.discoveredLogsIDs.Clear();
                foreach (Log_Data l in logs)
                {
                    if (l != null) data.discoveredLogsIDs.Add(l.log_ID);
                }
                break;
            case "inspectedObjects":
                List<Log_Data> inspected = (List<Log_Data>)value;
                data.inspectedObjectsIDs.Clear();
                foreach (Log_Data l in inspected)
                {
                    if (l != null) data.inspectedObjectsIDs.Add(l.log_ID);
                }
                break;
            case "inventory":
                List<Item_Data> items = (List<Item_Data>)value;
                data.inventoryIDs.Clear();
                foreach (Item_Data item in items)
                {
                    if (item != null) data.inventoryIDs.Add(item.ID);
                }
                break;
            case "availablePoints":
                data.availablePoints = (int)value;
                break;
            case "totalEarnedPoints":
                data.totalEarnedPoints = (int)value;
                break;
            case "discoveredCheckpoints":
                List<DiscoveredCheckpoint> cps = (List<DiscoveredCheckpoint>)value;
                data.discoveredCheckpoints.Clear();
                foreach (DiscoveredCheckpoint cpe in cps)
                {
                    if (cpe != null) data.discoveredCheckpoints.Add(cpe._checkpointID);
                }
                break;
            case "activeCheckpoint":
                DiscoveredCheckpoint activeCp = (DiscoveredCheckpoint)value;
                data.activeCheckpointID = activeCp != null ? activeCp._checkpointID : "";
                break;
            case "unlockedSkills":
                List<Skill> skills = (List<Skill>)value;
                data.unlockedSkillsIDs.Clear();
                foreach (Skill s in skills)
                {
                    if (s != null) data.unlockedSkillsIDs.Add(s.skillName);
                }
                break;
            case "equipedSkills":
                Skill[] eqSkills = (Skill[])value;
                data.equipedSkillsIDs = new string[eqSkills.Length];
                for (int i = 0; i < eqSkills.Length; i++)
                {
                    data.equipedSkillsIDs[i] = eqSkills[i] != null ? eqSkills[i].skillName : "";
                }
                break;
            case "gameCreated":
                data.gameCreated = (bool)value;
                break;
            case "checkpoint":
                Checkpoint cp = (Checkpoint)value;
                data.currentCheckpointID = cp != null ? cp.Data.checkpointID : "";
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
