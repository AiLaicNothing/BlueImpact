using System.Collections.Generic;
using UnityEngine;

public class StatsModificationSession
{
    private PlayerStatsManager playerStats;

    /// <summary>
    /// Cantidad de inversiones hechas desde cada stat durante esta sesión.
    /// Solo estas inversiones habilitan el botón "-".
    /// </summary>
    private Dictionary<StatDefinition, int> pendingInvestments =
        new Dictionary<StatDefinition, int>();

    /// <summary>
    /// Cambios visuales acumulados (+ o -).
    /// Se usan para Actual / Cambio / Final.
    /// </summary>
    private Dictionary<StatDefinition, int> previewChanges =
        new Dictionary<StatDefinition, int>();

    public StatsModificationSession(PlayerStatsManager playerStats)
    {
        this.playerStats = playerStats;

        Debug.Log($"playerStats = {playerStats}");

        Debug.Log($"GetAllStats = {playerStats?.GetAllStats()}");

        foreach (var stat in playerStats.GetAllStats().Values)
        {
            Debug.Log($"runtimeStat = {stat}");

            Debug.Log($"definition = {stat?.definition}");

            pendingInvestments.Add(stat.definition, 0);
            previewChanges.Add(stat.definition, 0);
        }
    }

    public int RemainingPoints =>
        playerStats.UpgradePoints - GetUsedPoints();

    public int GetUsedPoints()
    {
        int total = 0;

        foreach (var value in pendingInvestments.Values)
        {
            total += value;
        }

        return total;
    }

    public int GetCurrentValue(StatDefinition stat)
    {
        return playerStats.GetCurrentValue(stat);
    }

    public int GetChange(StatDefinition stat)
    {
        return previewChanges[stat];
    }

    public int GetFinalValue(StatDefinition stat)
    {
        return GetCurrentValue(stat) + previewChanges[stat];
    }

    public int GetPendingInvestments(StatDefinition stat)
    {
        return pendingInvestments[stat];
    }

    public bool CanUndo(StatDefinition stat)
    {
        return pendingInvestments[stat] > 0;
    }

    public bool CanIncrease(StatDefinition stat, out string reason)
    {
        reason = "";

        // ¿Tiene puntos?
        if (RemainingPoints <= 0)
        {
            reason = "No tienes suficientes puntos de mejora.";
            return false;
        }

        // ¿Se pasa del máximo?
        int finalValue = GetFinalValue(stat);

        if (finalValue + stat.increaseAmount > stat.maxValue)
        {
            reason = $"{stat.statName} ya alcanzó su valor máximo.";
            return false;
        }

        // Revisar penalización
        StatDefinition penaltyTarget = stat.penaltyTarget;

        if (penaltyTarget != null)
        {
            int penalty = GetPenaltyAmount(stat);

            int penaltyFinal =
                GetFinalValue(penaltyTarget) - penalty;

            if (penaltyFinal < penaltyTarget.minValue)
            {
                reason =
                    $"No puedes aumentar {stat.statName} porque {penaltyTarget.statName} alcanzaría su valor mínimo.";

                return false;
            }
        }

        return true;
    }

    public bool IncreaseStat(StatDefinition stat)
    {
        if (!CanIncrease(stat, out _))
            return false;

        pendingInvestments[stat]++;

        previewChanges[stat] += stat.increaseAmount;

        if (stat.penaltyTarget != null)
        {
            previewChanges[stat.penaltyTarget]
                -= GetPenaltyAmount(stat);
        }

        return true;
    }

    public bool UndoIncrease(StatDefinition stat)
    {
        if (!CanUndo(stat))
            return false;

        // Obtener la penalización que se aplicó
        int penalty = GetPenaltyAmountBeforeUndo(stat);

        pendingInvestments[stat]--;

        previewChanges[stat] -= stat.increaseAmount;

        if (stat.penaltyTarget != null)
        {
            previewChanges[stat.penaltyTarget]
                += penalty;
        }

        return true;
    }

    private int GetPenaltyAmount(StatDefinition stat)
    {
        int historicalInvestments =
            playerStats.GetTotalInvestments(stat);

        int pending =
            pendingInvestments[stat];

        int totalInvestments =
            historicalInvestments + pending;

        int penalty = 1;

        foreach (PenaltyTier tier in playerStats.StatsConfig.penaltyTiers)
        {
            if (totalInvestments >= tier.requiredInvestments)
            {
                penalty = tier.penaltyAmount;
            }
        }

        return penalty;
    }

    private int GetPenaltyAmountBeforeUndo(StatDefinition stat)
    {
        int historicalInvestments =
            playerStats.GetTotalInvestments(stat);

        int pending =
            pendingInvestments[stat] - 1;

        int totalInvestments =
            historicalInvestments + pending;

        int penalty = 1;

        foreach (PenaltyTier tier in playerStats.StatsConfig.penaltyTiers)
        {
            if (totalInvestments >= tier.requiredInvestments)
            {
                penalty = tier.penaltyAmount;
            }
        }

        return penalty;
    }

    public void ConfirmChanges()
    {
        int spentPoints = GetUsedPoints();

        if (spentPoints <= 0)
            return;

        Dictionary<StatDefinition, int> finalValues =
            new Dictionary<StatDefinition, int>();

        Dictionary<StatDefinition, int> investments =
            new Dictionary<StatDefinition, int>();

        foreach (var runtimeStat in playerStats.GetAllStats().Values)
        {
            StatDefinition stat = runtimeStat.definition;

            finalValues.Add(
                stat,
                GetFinalValue(stat));

            investments.Add(
                stat,
                pendingInvestments[stat]);
        }

        playerStats.ApplyConfirmedChanges(
            finalValues,
            investments,
            spentPoints);

        ClearSession();
    }

    public void CancelChanges()
    {
        ClearSession();
    }

    private void ClearSession()
    {
        List<StatDefinition> stats =
            new List<StatDefinition>(pendingInvestments.Keys);

        foreach (StatDefinition stat in stats)
        {
            pendingInvestments[stat] = 0;
            previewChanges[stat] = 0;
        }
    }
}