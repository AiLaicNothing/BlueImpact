using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    [Header("Info")]
    public string statName;
    public Sprite icon;

    [Header("Limits")]
    public int minValue = 1;
    public int maxValue = 999;

    [Header("Upgrade")]
    public int increaseAmount = 5;

    [Header("Penalty")]
    public StatDefinition penaltyTarget;
}   