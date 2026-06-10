using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Stats Config")]
public class StatsConfig : ScriptableObject
{
    [Header("Penalty Scaling")]
    public List<PenaltyTier> penaltyTiers = new();
}