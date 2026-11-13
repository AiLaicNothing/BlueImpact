using UnityEngine;
using System.Collections.Generic;

public class GameDatabase : MonoBehaviour
{
    public static GameDatabase Instance { get; private set; }

    [Header("Scriptables")]
    [SerializeField] List<Skill> allSkills;
    [SerializeField] public List<Log_Data> allLogs;
    [SerializeField] public List<Item_Data> allItems;
    [SerializeField] public List<CheckpointData> allCheckpointsData;


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public Skill GetSkillByID(string id) => allSkills.Find(x => x != null && x.skillName == id);
    public Log_Data GetLogByID(int id) => allLogs.Find(x => x != null && x.log_ID == id);
    public Item_Data GetItemByID(int id) => allItems.Find(x => x != null && x.ID == id);
    public CheckpointData GetCheckpointByID(string id) => allCheckpointsData.Find(x => x != null && x.checkpointID == id);

}