using UnityEngine;

[System.Serializable]
public class BaseStats 
{
    [Min(0)]
    public int health = 10;

    [Min(0)]
    public int stamina = 10;

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
