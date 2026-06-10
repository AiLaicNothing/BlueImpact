using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterDefinition characterDefinition;

    [SerializeField] private StatsConfig statsConfig;

    [Header("Upgrade Points")]
    [SerializeField] private int upgradePoints;

    private Dictionary<StatDefinition, RuntimeStat> runtimeStats;

    public CharacterDefinition CharacterDefinition => characterDefinition;

    public StatsConfig StatsConfig => statsConfig;

    public int UpgradePoints => upgradePoints;

    private void Awake()
    {
        Debug.Log("PlayerStatsManager Awake");

        EnsureInitialized();
    }
    public void EnsureInitialized()
    {
        if (runtimeStats != null)
            return;

        InitializeStats();
    }
    private void InitializeStats()
    {
        runtimeStats = new Dictionary<StatDefinition, RuntimeStat>();

        foreach (CharacterStatValue statValue in characterDefinition.baseStats)
        {
            RuntimeStat runtimeStat = new RuntimeStat
            {
                definition = statValue.stat,
                currentValue = statValue.baseValue,
                totalInvestments = 0
            };

            runtimeStats.Add(statValue.stat, runtimeStat);
        }
    }

    public int GetCurrentValue(StatDefinition stat)
    {
        EnsureInitialized();

        return runtimeStats[stat].currentValue;
    }

    public int GetTotalInvestments(StatDefinition stat)
    {
        EnsureInitialized();

        return runtimeStats[stat].totalInvestments;
    }

    public RuntimeStat GetRuntimeStat(StatDefinition stat)
    {
        EnsureInitialized();

        return runtimeStats[stat];
    }

    public IReadOnlyDictionary<StatDefinition, RuntimeStat> GetAllStats()
    {
        EnsureInitialized();

        return runtimeStats;
    }

    public void AddUpgradePoints(int amount)
    {
        upgradePoints += amount;
    }

    public bool HasEnoughPoints(int amount)
    {
        return upgradePoints >= amount;
    }

    /// <summary>
    /// Aplicado SOLO al confirmar cambios.
    /// </summary>
    public void ApplyConfirmedChanges(
        Dictionary<StatDefinition, int> finalValues,
        Dictionary<StatDefinition, int> investmentChanges,
        int spentPoints)
    {
        if (spentPoints > upgradePoints)
        {
            Debug.LogError("Intentando gastar más puntos de los disponibles.");
            return;
        }

        foreach (var pair in finalValues)
        {
            runtimeStats[pair.Key].currentValue = pair.Value;
        }

        foreach (var pair in investmentChanges)
        {
            runtimeStats[pair.Key].totalInvestments += pair.Value;
        }

        upgradePoints -= spentPoints;
    }

#if UNITY_EDITOR
    [ContextMenu("Add 20 Upgrade Points")]
    private void DebugAddPoints()
    {
        AddUpgradePoints(20);

        Debug.Log($"Upgrade Points: {upgradePoints}");
    }
#endif
}