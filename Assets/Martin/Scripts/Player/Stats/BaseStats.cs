using UnityEngine;

[System.Serializable]
public class BaseStats 
{
    [Tooltip("Hp = points * 10")]
    [Min(0)]
    public int health = 10;

    [Tooltip("Stamina = points * 3")]
    [Min(0)]
    public int stamina = 10;

    [Tooltip("Mp = points * 5")]
    [Min(0)]
    public int mana = 10;

    [Min(0)]
    public int physical = 10;

    [Min(0)]
    public int magical = 10;

    public int GetStat(StatsType type)
    {
        return type switch
        {
            StatsType.Health => health,
            StatsType.Stamina => stamina,
            StatsType.Mana => mana,
            StatsType.Physical_Damage => physical,
            StatsType.Magical_Damage => magical,

            _ => 0
        };
    }
}
