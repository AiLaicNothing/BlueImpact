using System;
using UnityEngine;

[Serializable]
public class PenaltyTier
{
    [Tooltip("Cantidad mínima de inversiones para activar este tier")]
    public int requiredInvestments;

    [Tooltip("Penalización aplicada")]
    public int penaltyAmount;
}