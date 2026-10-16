using System.Collections;
using Unity.VisualScripting;
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
    [SerializeField] private int mpRegen = 5;
    [SerializeField] private int currentStamina;
    [SerializeField] private int staminaRegen= 4;

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

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int MaxMp => maxMp;
    public int CurrentMp => currentMp;
    public int MaxStamina => maxStamina;
    public int CurrentStamina => currentStamina;

    private void Awake()
    {
        stats = CharacterStatsManager.Instance;
    }

    private void Start()
    {
        SetStatsValue();
        SetInitialValue();

        // Preserve the original regeneration values.
        baseHpRegen = hpRegen;
        baseMpRegen = mpRegen;
        baseStaminaRegen = staminaRegen;

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
        int previousMaxHp = maxHp;
        int previousMaxMp = maxMp;
        int previousMaxStamina = maxStamina;

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
        if (currentMp >= maxMp || mpRegen <= 0)
            return;

        mpRegenAccumulator += mpRegen * Time.deltaTime;

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
        StartCoroutine(IncreaseRegenRoutine(type, multiplier, duration));
    }

    private IEnumerator IncreaseRegenRoutine(StatsType type, int multiplier, float duration)
    {
        int originalRegen = 0;

        // Get the original regeneration value.
        if (type == StatsType.Health) originalRegen = hpRegen;

        else if (type == StatsType.Mana) originalRegen = mpRegen;

        else if (type == StatsType.Stamina) originalRegen = staminaRegen;

        // Increase regeneration.
        int boostedRegen = originalRegen * multiplier;

        if (type == StatsType.Health) hpRegen = boostedRegen;

        else if (type == StatsType.Mana) mpRegen = boostedRegen;

        else if (type == StatsType.Stamina) staminaRegen = boostedRegen;

        // Wait for the buff duration.
        yield return new WaitForSeconds(duration);

        // Restore the original regeneration.
        if (type == StatsType.Health) hpRegen = originalRegen;

        else if (type == StatsType.Mana) mpRegen = originalRegen;

        else if (type == StatsType.Stamina) staminaRegen = originalRegen;
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
