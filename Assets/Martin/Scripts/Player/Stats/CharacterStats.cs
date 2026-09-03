using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private CharacterData characterData;
    [SerializeField] private bool debug;

    private Dictionary<StatsType, int> allocatedPoints = new();
    private Dictionary<StatsType, int> pendingPoints = new();

    private int totalEarnedPoints;

    private int availablePoints;

    public CharacterData CharData => characterData;
    public int TotalEarnedPoints => totalEarnedPoints;
    public int AvailablePoints => availablePoints;

    public event Action OnStatsChanged;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
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
        foreach (StatsType stat in Enum.GetValues(typeof(StatType)))
        {
            pendingPoints[stat] = allocatedPoints[stat];
        }

        RecalculateAvailablePoints();

        OnStatsChanged?.Invoke();
    }
}
