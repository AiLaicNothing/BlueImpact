using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterDefinition characterDefinition;
    [SerializeField] private StatsConfig statsConfig;

    [Header("Upgrade Points")]
    [SerializeField] private int upgradePoints;

    [Header("Regeneration")]
    [SerializeField] private float staminaRegenRate = 5f;
    [SerializeField] private float staminaRegenDelay = 1.5f;

    [SerializeField] private float manaRegenRate = 3f;
    [SerializeField] private float manaRegenDelay = 2f;

    [SerializeField] private float healthRegenRate = 1f;
    [SerializeField] private float healthRegenDelay = 5f;

    private Dictionary<StatDefinition, RuntimeStat> runtimeStats;
    private Dictionary<StatDefinition, int> actualValues;        // ✅ Valores actuales para combate
    private Dictionary<StatDefinition, float> regenTimers;

    public CharacterDefinition CharacterDefinition => characterDefinition;
    public StatsConfig StatsConfig => statsConfig;
    public int UpgradePoints => upgradePoints;

    private void Awake()
    {
        if (statsConfig == null)
        {
            Debug.LogError($"{nameof(PlayerStatsManager)}: StatsConfig no asignado.", this);
        }

        EnsureInitialized();
    }

    private void Update()
    {
        if (runtimeStats == null) return;

        // ✅ Actualizar regeneración
        UpdateRegeneration(StatType.Stamina, staminaRegenRate, staminaRegenDelay);
        UpdateRegeneration(StatType.Mana, manaRegenRate, manaRegenDelay);
        UpdateRegeneration(StatType.Health, healthRegenRate, healthRegenDelay);
    }

    public void EnsureInitialized()
    {
        if (runtimeStats != null)
            return;

        InitializeStats();
    }

    private void InitializeStats()
    {
        if (characterDefinition == null)
        {
            Debug.LogError($"{nameof(PlayerStatsManager)}: CharacterDefinition no asignado.", this);
            return;
        }

        runtimeStats = new Dictionary<StatDefinition, RuntimeStat>();
        actualValues = new Dictionary<StatDefinition, int>();
        regenTimers = new Dictionary<StatDefinition, float>();

        foreach (CharacterStatValue statValue in characterDefinition.baseStats)
        {
            RuntimeStat runtimeStat = new RuntimeStat
            {
                definition = statValue.stat,
                currentValue = statValue.baseValue,  // ✅ Máximo actual
                totalInvestments = 0
            };

            runtimeStats.Add(statValue.stat, runtimeStat);

            // ✅ Inicializar valor actual igual al máximo (lleno)
            actualValues.Add(statValue.stat, statValue.baseValue);
            regenTimers.Add(statValue.stat, 0f);
        }
    }

    // ==================== REGENERACIÓN ✅ ====================

    private void UpdateRegeneration(StatType statType, float regenRate, float regenDelay)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return;

        if (!runtimeStats.ContainsKey(statDef)) return;

        // Incrementar timer
        regenTimers[statDef] += Time.deltaTime;

        // Solo regenerar si pasó el delay
        if (regenTimers[statDef] >= regenDelay)
        {
            int regenAmount = Mathf.CeilToInt(regenRate * Time.deltaTime);
            int maxValue = runtimeStats[statDef].currentValue;  // ✅ El máximo actual de la stat
            int currentActualValue = actualValues[statDef];

            // No exceder el máximo actual
            actualValues[statDef] = Mathf.Min(currentActualValue + regenAmount, maxValue);
        }
    }

    // ==================== CONSUMO DE STATS ✅ ====================

    /// <summary>
    /// Obtiene la StatDefinition por StatType
    /// </summary>
    private StatDefinition GetStatDefinition(StatType statType)
    {
        EnsureInitialized();

        foreach (var kvp in runtimeStats)
        {
            if (kvp.Key.statName == statType.ToString())
            {
                return kvp.Key;
            }
        }

        Debug.LogWarning($"StatDefinition no encontrada para {statType}");
        return null;
    }

    /// <summary>
    /// Verifica si hay suficiente valor actual para consumir
    /// </summary>
    public bool CanConsume(StatType statType, int amount)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return false;

        return actualValues[statDef] >= amount;
    }

    /// <summary>
    /// Consume el valor actual (no el máximo) y resetea el timer
    /// </summary>
    public bool Consume(StatType statType, int amount)
    {
        if (!CanConsume(statType, amount))
        {
            Debug.Log($"{statType} insuficiente. Necesitas {amount}, tienes {actualValues[GetStatDefinition(statType)]}");
            return false;
        }

        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return false;

        actualValues[statDef] = Mathf.Max(
            actualValues[statDef] - amount,
            runtimeStats[statDef].definition.minValue
        );

        // ✅ Resetear timer de regeneración al consumir
        regenTimers[statDef] = 0f;

        return true;
    }

    /// <summary>
    /// Restaura valor actual (para pociones, etc)
    /// </summary>
    public void Restore(StatType statType, int amount)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return;

        // ✅ No puede exceder el máximo actual (currentValue)
        int maxActual = runtimeStats[statDef].currentValue;
        actualValues[statDef] = Mathf.Min(
            actualValues[statDef] + amount,
            maxActual
        );
    }

    /// <summary>
    /// Restaura completamente al máximo actual
    /// </summary>
    public void RestoreFull(StatType statType)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return;

        actualValues[statDef] = runtimeStats[statDef].currentValue;
    }

    // ==================== GETTERS PARA COMBATE ✅ ====================

    /// <summary>
    /// Obtiene el valor ACTUAL (lo que se consume en combate)
    /// </summary>
    public int GetActualValue(StatType statType)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return 0;

        return actualValues[statDef];
    }

    /// <summary>
    /// Obtiene el MÁXIMO ACTUAL (el currentValue que se puede cambiar en checkpoints)
    /// </summary>
    public int GetMaxValue(StatType statType)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return 0;

        return runtimeStats[statDef].currentValue;
    }

    /// <summary>
    /// Obtiene el CAP máximo (maxValue de StatDefinition)
    /// </summary>
    public int GetCapValue(StatType statType)
    {
        StatDefinition statDef = GetStatDefinition(statType);
        if (statDef == null) return 0;

        return statDef.maxValue;
    }

    /// <summary>
    /// Porcentaje de uso actual vs máximo actual
    /// </summary>
    public float GetPercentage(StatType statType)
    {
        int actual = GetActualValue(statType);
        int max = GetMaxValue(statType);

        return max > 0 ? (float)actual / max : 0f;
    }

    /// <summary>
    /// Verifica si está muerto (Health actual <= 0)
    /// </summary>
    public bool IsDead()
    {
        StatDefinition healthDef = GetStatDefinition(StatType.Health);
        if (healthDef == null) return false;

        return actualValues[healthDef] <= 0;
    }

    // ==================== GETTERS ORIGINALES (para checkpoints) ====================

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

    // ==================== CUANDO CONFIRMAS CAMBIOS EN CHECKPOINT ✅ ====================

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
            StatDefinition statDef = pair.Key;
            int newMaxValue = pair.Value;

            // ✅ Actualizar el máximo
            runtimeStats[statDef].currentValue = newMaxValue;

            // ✅ Si aumentó el máximo, también aumenta el actual
            if (actualValues[statDef] < newMaxValue)
            {
                actualValues[statDef] = newMaxValue;  // Llena hasta el nuevo máximo
            }
        }

        foreach (var pair in investmentChanges)
        {
            runtimeStats[pair.Key].totalInvestments += pair.Value;
        }

        upgradePoints -= spentPoints;
    }

    // ==================== UPGRADE POINTS ====================

    public void AddUpgradePoints(int amount)
    {
        upgradePoints += amount;
        Debug.Log($"Upgrade Points: {upgradePoints}");
    }

    public bool HasEnoughPoints(int amount)
    {
        return upgradePoints >= amount;
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