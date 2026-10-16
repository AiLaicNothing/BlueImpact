using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

//This Manager would know the stats in point of the player
//Having function related to the modification of this
public class CharacterStatsManager : MonoBehaviour
{
    public static CharacterStatsManager Instance;

    [Header("Debug")]
    [SerializeField] private bool debug;
    private CharacterData characterData;

    //Only to show via inspector how many points are there
    [Header("Current Stats")]
    [SerializeField] private int hp;
    [SerializeField] private int stamina;
    [SerializeField] private int mana;
    [SerializeField] private int attack;
    [SerializeField] private int magic;

    private Dictionary<StatsType, int> allocatedPoints = new();
    private Dictionary<StatsType, int> pendingPoints = new();

    private int totalEarnedPoints;

    private int availablePoints;

    //This not needed now since palyer_Manager exist
    //public CharacterData CharData => characterData;
    public int TotalEarnedPoints => totalEarnedPoints;
    public int AvailablePoints => availablePoints;

    public event Action OnStatsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;    
    }


    //Initialize after player spawn
    public void Initialize()
    {
        if (Player_Manager.Instance == null)
        {
            Debug.LogError("Player_Manager Instance is null");
            return;
        }

        CharacterInfo character = Player_Manager.Instance.GetCharacterData();

        if (character == null || character.data == null)
        {
            Debug.LogError("Character data has not been assigned");
            return;
        } 

        characterData = character.data;

        allocatedPoints.Clear();
        pendingPoints.Clear();

        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            allocatedPoints[stat] = 0;
            pendingPoints[stat] = 0;
        }

        totalEarnedPoints = characterData.startingStatPoints;

        RecalculateAvailablePoints();

        OnStatsChanged?.Invoke();

        if (debug) Debug.Log("[Character stats manager initiliazed correctly]");
    }

    public int GetBaseStat(StatsType statType)
    {
        return characterData.baseStats.GetStat(statType);
    }

    public int GetAllocatedPoints(StatsType statType)
    {
        return allocatedPoints[statType];
    }

    public int GetPendingPoints(StatsType statType)
    {
        return pendingPoints[statType];
    }

    public int GetTotalPendingPoints()
    {
        int total = 0;

        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            total += pendingPoints[stat];
        }

        return total;
    }

    //This show how the change will affect that stat
    public int GetDisplayedStat(StatsType statType)
    {
        return GetBaseStat(statType) + GetPendingPoints(statType);
    }

    //This show the stat with the points already assigned
    public int GetCurrentStat(StatsType statType)
    {
        return GetBaseStat(statType) + GetAllocatedPoints(statType);
    }

    public bool AddPoint(StatsType statsType)
    {
        if (availablePoints <= 0) return false;

        pendingPoints[statsType]++;

        RecalculateAvailablePoints();

        OnStatsChanged?.Invoke();

        return true;
    }

    public bool RemovePoint(StatsType statsType)
    {
        // The player can only remove points that were assigned by them and not the base stats

        if (pendingPoints[statsType] <= allocatedPoints[statsType]) return false;

        pendingPoints[statsType]--;

        RecalculateAvailablePoints();

        OnStatsChanged?.Invoke();

        return true;
    }

    public void ApplyStats()
    {
        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            allocatedPoints[stat] = pendingPoints[stat];
        }

        RecalculateAvailablePoints();

        if (debug) Debug.Log($"{characterData.name} stats applied");

        OnStatsChanged?.Invoke();
    }

    public void CancelChanges()
    {
        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            pendingPoints[stat] = allocatedPoints[stat];
        }

        RecalculateAvailablePoints();

        if (debug) Debug.Log($"{characterData.name} stat changes cancelled");

        OnStatsChanged?.Invoke();
    }

    private void RecalculateAvailablePoints()
    {
        int usedPoint = 0;

        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            usedPoint += pendingPoints[stat];
        }

        availablePoints = totalEarnedPoints - usedPoint;

        if (availablePoints < 0)
        {
            availablePoints = 0;
        }
    }

    // This should be called when the player interact with the structure that give points
    public void AddStatPoints(int amount)
    {
        if (amount <= 0)
        {
            if (debug)
            {
                Debug.Log($"Tried to add an invalid ammount of points {amount}");
            }

            return;
        }

        totalEarnedPoints += amount;

        RecalculateAvailablePoints();

        if (debug)
        {
            Debug.Log(
         $"{characterData.characterName} gained {amount} stat points. " +
         $"Total: {totalEarnedPoints}, " +
         $"Available: {availablePoints}");
        }

        OnStatsChanged?.Invoke();
    }

    public void BeginEdit()
    {
        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            pendingPoints[stat] = allocatedPoints[stat];
        }

        RecalculateAvailablePoints();

        OnStatsChanged?.Invoke();
    }
}
