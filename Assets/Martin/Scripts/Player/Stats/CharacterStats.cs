using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This would go in the Player
//This script is in charge of handling live stats like hp, mp and stamina
//The other stats can be adquire via the manager, since does stats are "static"
public class CharacterStats : MonoBehaviour
{
    [Header("Current stats Value")]
    [SerializeField] private int currentHp;
    [SerializeField] private int hpRegen = 3;
    [SerializeField] private int currentMp;
    [SerializeField, Range(0, 100)] private int mpRegen = 5;
    [SerializeField] private int currentStamina;
    [SerializeField] private int staminaRegen = 4;

    private int maxHp;
    private int maxMp;
    private int maxStamina;

    private CharacterStatsManager stats;

    // Fractional regeneration accumulated between frames.
    private float hpRegenAccumulator;
    private float mpRegenAccumulator;
    private float staminaRegenAccumulator;

    // Base regeneration values, before temporary boosts.
    private int baseHpRegen;
    private int baseMpRegen;
    private int baseStaminaRegen;

    // Cuántos buffs de regeneración activos hay por tipo.
    private readonly Dictionary<StatsType, int> activeBuffs = new Dictionary<StatsType, int>();

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int MaxMp => maxMp;
    public int CurrentMp => currentMp;
    public int MaxStamina => maxStamina;
    public int CurrentStamina => currentStamina;

    private void Awake()
    {
        stats = CharacterStatsManager.Instance;

        // Se guardan en Awake (no en Start) para que un buff llamado antes de Start
        // no tome 0 como valor base.
        baseHpRegen = hpRegen;
        baseMpRegen = mpRegen;
        baseStaminaRegen = staminaRegen;
    }

    private void Start()
    {
        SetStatsValue();
        SetInitialValue();

        if (stats != null)
            stats.OnStatsChanged += RefreshStats;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnStatsChanged -= RefreshStats;
    }

    private void Update()
    {
        if (IsDead())
            return;

        RegenHp();
        RegenMp();
        RegenStamina();
    }

    private void SetStatsValue()
    {
        if (stats == null) return;

        maxHp = stats.GetCurrentStat(StatsType.Health) * 10;
        maxMp = stats.GetCurrentStat(StatsType.Mana) * 5;
        maxStamina = stats.GetCurrentStat(StatsType.Stamina) * 5;
    }

    private void SetInitialValue()
    {
        currentHp = maxHp;
        currentMp = maxMp;
        currentStamina = maxStamina;
    }

    private void RefreshStats()
    {
        SetStatsValue();

        // Keep current values within the updated maximums.
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        currentMp = Mathf.Clamp(currentMp, 0, maxMp);
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    private void RegenHp()
    {
        if (currentHp >= maxHp || hpRegen <= 0)
            return;

        hpRegenAccumulator += hpRegen * Time.deltaTime;

        int amount = Mathf.FloorToInt(hpRegenAccumulator);

        if (amount > 0)
        {
            int restored = Mathf.Min(amount, maxHp - currentHp);
            currentHp += restored;
            hpRegenAccumulator -= amount;
        }
    }

    private void RegenMp()
    {
        if (currentMp >= maxMp || mpRegen <= 0) return;

        // mpRegen represents % of maximum MP regenerated per second.
        float regenPerSecond = maxMp * (mpRegen / 100f);

        mpRegenAccumulator += regenPerSecond * Time.deltaTime;

        int amount = Mathf.FloorToInt(mpRegenAccumulator);

        if (amount > 0)
        {
            int restored = Mathf.Min(amount, maxMp - currentMp);
            currentMp += restored;
            mpRegenAccumulator -= amount;
        }
    }

    private void RegenStamina()
    {
        if (currentStamina >= maxStamina || staminaRegen <= 0)
            return;

        staminaRegenAccumulator += staminaRegen * Time.deltaTime;

        int amount = Mathf.FloorToInt(staminaRegenAccumulator);

        if (amount > 0)
        {
            int restored = Mathf.Min(amount, maxStamina - currentStamina);
            currentStamina += restored;
            staminaRegenAccumulator -= amount;
        }
    }

    // Temporarily multiplies the regeneration rate for the selected stat.
    public void IncreaseRegen(StatsType type, int multiplier, float duration)
    {
        if (!IsRegenType(type)) return;

        StartCoroutine(IncreaseRegenRoutine(type, multiplier, duration));
    }

    private IEnumerator IncreaseRegenRoutine(StatsType type, int multiplier, float duration)
    {
        // Siempre se parte del valor base, no del ya aumentado:
        // antes, dos buffs solapados se multiplicaban y el regen quedaba aumentado para siempre.
        activeBuffs[type] = GetActiveBuffCount(type) + 1;
        SetRegen(type, GetBaseRegen(type) * multiplier);

        yield return new WaitForSeconds(duration);

        activeBuffs[type] = GetActiveBuffCount(type) - 1;

        // Solo se vuelve al valor base cuando termina el último buff de ese tipo.
        if (GetActiveBuffCount(type) <= 0)
        {
            activeBuffs[type] = 0;
            SetRegen(type, GetBaseRegen(type));
        }
    }

    private int GetActiveBuffCount(StatsType type)
    {
        return activeBuffs.TryGetValue(type, out int count) ? count : 0;
    }

    private static bool IsRegenType(StatsType type)
    {
        return type == StatsType.Health || type == StatsType.Mana || type == StatsType.Stamina;
    }

    private int GetBaseRegen(StatsType type)
    {
        if (type == StatsType.Health) return baseHpRegen;
        if (type == StatsType.Mana) return baseMpRegen;
        return baseStaminaRegen;
    }

    private void SetRegen(StatsType type, int value)
    {
        if (type == StatsType.Health) hpRegen = value;
        else if (type == StatsType.Mana) mpRegen = value;
        else if (type == StatsType.Stamina) staminaRegen = value;
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }

    public void ConsumeStat(StatsType type, int amount)
    {
        if (amount <= 0)
            return;

        if (type == StatsType.Health)
            currentHp = Mathf.Max(currentHp - amount, 0);
        else if (type == StatsType.Mana)
            currentMp = Mathf.Max(currentMp - amount, 0);
        else if (type == StatsType.Stamina)
            currentStamina = Mathf.Max(currentStamina - amount, 0);
    }

    public bool CanConsume(StatsType type, int amount)
    {
        if (amount < 0)
            return false;

        if (type == StatsType.Health)
            return currentHp >= amount;

        if (type == StatsType.Mana)
            return currentMp >= amount;

        if (type == StatsType.Stamina)
            return currentStamina >= amount;

        return false;
    }

    public void RestoreCurrentStat(StatsType type, int amount)
    {
        if (amount <= 0)
            return;

        if (type == StatsType.Health)
            currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);
        else if (type == StatsType.Mana)
            currentMp = Mathf.Clamp(currentMp + amount, 0, maxMp);
        else if (type == StatsType.Stamina)
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);
    }

    public void RestoreToMax()
    {
        currentHp = maxHp;
        currentMp = maxMp;
        currentStamina = maxStamina;
    }
}
