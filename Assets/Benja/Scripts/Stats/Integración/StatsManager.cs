using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    // ==================== SINGLETON ====================
    public static StatsManager Instance { get; private set; }

    private Dictionary<StatType, CharacterStatValue> stats = new();
    private Dictionary<StatType, float> regenTimers = new();
    private Dictionary<StatType, int> currentValues = new();

    [Header("Character Definition")]
    [SerializeField] private CharacterDefinition characterDefinition;

    [Header("Regeneration")]
    [SerializeField] private float staminaRegenRate = 5f;
    [SerializeField] private float staminaRegenDelay = 1f;

    [SerializeField] private float manaRegenRate = 3f;
    [SerializeField] private float manaRegenDelay = 1.5f;

    [SerializeField] private float healthRegenRate = 1f;
    [SerializeField] private float healthRegenDelay = 3f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar con CharacterDefinition asignado
        if (characterDefinition != null)
        {
            Initialize(characterDefinition);
        }
        else
        {
            Debug.LogError("CharacterDefinition no asignado en StatsManager");
        }
    }

    public void Initialize(CharacterDefinition charDef)
    {
        characterDefinition = charDef;
        stats.Clear();
        currentValues.Clear();
        regenTimers.Clear();

        foreach (var statValue in charDef.baseStats)
        {
            StatType statType = GetStatType(statValue.stat.statName);
            stats[statType] = statValue;
            currentValues[statType] = statValue.baseValue;
            regenTimers[statType] = 0f;
        }

        Debug.Log("StatsManager inicializado con: " + charDef.characterName);
    }

    private void Update()
    {
        if (stats == null || stats.Count == 0) return;

        if (stats.ContainsKey(StatType.Stamina))
            UpdateRegeneration(StatType.Stamina, staminaRegenRate, staminaRegenDelay);

        if (stats.ContainsKey(StatType.Mana))
            UpdateRegeneration(StatType.Mana, manaRegenRate, manaRegenDelay);

        if (stats.ContainsKey(StatType.Health))
            UpdateRegeneration(StatType.Health, healthRegenRate, healthRegenDelay);
    }

    private void UpdateRegeneration(StatType statType, float regenRate, float regenDelay)
    {
        if (!stats.TryGetValue(statType, out var statValue)) return;

        regenTimers[statType] += Time.deltaTime;

        if (regenTimers[statType] >= regenDelay)
        {
            int regenAmount = Mathf.CeilToInt(regenRate * Time.deltaTime);
            int maxValue = statValue.stat.maxValue;
            currentValues[statType] = Mathf.Min(currentValues[statType] + regenAmount, maxValue);
        }
    }

    // ==================== VERIFICACIÓN ====================

    public bool CanConsume(StatType statType, int amount)
    {
        if (!currentValues.TryGetValue(statType, out var currentValue))
        {
            return false;
        }
        return currentValue >= amount;
    }

    // ==================== CONSUMO ====================

    public bool TryConsume(StatType statType, int amount)
    {
        if (!CanConsume(statType, amount))
        {
            Debug.Log($"{statType} insuficiente. Necesitas {amount}, tienes {GetCurrentValue(statType)}");
            return false;
        }

        Consume(statType, amount);
        return true;
    }

    public void Consume(StatType statType, int amount)
    {
        if (currentValues.TryGetValue(statType, out var currentValue))
        {
            if (stats.TryGetValue(statType, out var statDef))
            {
                currentValues[statType] = Mathf.Max(currentValue - amount, statDef.stat.minValue);
                regenTimers[statType] = 0f;
            }
        }
    }

    // ==================== GETTERS INDIVIDUALES ====================

    public int GetCurrentValue(StatType statType)
    {
        return currentValues.TryGetValue(statType, out var value) ? value : 0;
    }

    public int GetMaxValue(StatType statType)
    {
        return stats.TryGetValue(statType, out var stat) ? stat.stat.maxValue : 0;
    }

    public float GetPercentage(StatType statType)
    {
        if (stats.TryGetValue(statType, out var stat))
        {
            int max = stat.stat.maxValue;
            int current = GetCurrentValue(statType);
            return max > 0 ? (float)current / max : 0f;
        }
        return 0f;
    }

    // ==================== GETTERS MÚLTIPLES ====================

    public StatInfo GetStatInfo(StatType statType)
    {
        return new StatInfo
        {
            statType = statType,
            currentValue = GetCurrentValue(statType),
            maxValue = GetMaxValue(statType),
            percentage = GetPercentage(statType),
            statDefinition = stats.TryGetValue(statType, out var s) ? s.stat : null
        };
    }

    public Dictionary<StatType, StatInfo> GetAllStats()
    {
        Dictionary<StatType, StatInfo> allStats = new();

        foreach (StatType statType in stats.Keys)
        {
            allStats[statType] = GetStatInfo(statType);
        }

        return allStats;
    }

    public Dictionary<StatType, int> GetAllCurrentValues()
    {
        return new Dictionary<StatType, int>(currentValues);
    }

    public Dictionary<StatType, int> GetAllMaxValues()
    {
        Dictionary<StatType, int> maxValues = new();

        foreach (var kvp in stats)
        {
            maxValues[kvp.Key] = kvp.Value.stat.maxValue;
        }

        return maxValues;
    }

    public Dictionary<StatType, float> GetAllPercentages()
    {
        Dictionary<StatType, float> percentages = new();

        foreach (StatType statType in stats.Keys)
        {
            percentages[statType] = GetPercentage(statType);
        }

        return percentages;
    }

    // ==================== SETTERS ====================

    public void SetValue(StatType statType, int value)
    {
        if (stats.TryGetValue(statType, out var stat))
        {
            var def = stat.stat;
            currentValues[statType] = Mathf.Clamp(value, def.minValue, def.maxValue);
        }
    }

    public void Restore(StatType statType, int amount)
    {
        if (stats.TryGetValue(statType, out var stat))
        {
            int maxValue = stat.stat.maxValue;
            currentValues[statType] = Mathf.Min(GetCurrentValue(statType) + amount, maxValue);
        }
    }

    public void RestoreFull(StatType statType)
    {
        if (stats.TryGetValue(statType, out var stat))
        {
            currentValues[statType] = stat.baseValue;
        }
    }

    // ==================== UTILIDADES ====================

    private StatType GetStatType(string statName)
    {
        return statName switch
        {
            "Health" => StatType.Health,
            "PhysicalDamage" => StatType.PhysicalDamage,
            "MagicalDamage" => StatType.MagicalDamage,
            "Stamina" => StatType.Stamina,
            "Mana" => StatType.Mana,
            _ => StatType.Health
        };
    }

    public bool IsDead()
    {
        return GetCurrentValue(StatType.Health) <= 0;
    }
}

// ==================== STRUCT INFO ====================

public struct StatInfo
{
    public StatType statType;
    public int currentValue;
    public int maxValue;
    public float percentage;
    public StatDefinition statDefinition;

    public override string ToString()
    {
        return $"{statType}: {currentValue}/{maxValue} ({percentage:P0})";
    }
}