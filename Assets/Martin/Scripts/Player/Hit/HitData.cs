using UnityEngine;

[System.Serializable]
public class HitData 
{
    [Header("Attack Values")]
    // (physical * stat) + (magical * stat) = damage
    public float physicalScale = 1f;
    public float magicalScale = 1f;
    public float damageMultiplier = 1f;
    public float staggerCharge = 10f;

    public ThrowType throwType;
    public float stunDuration;

    public bool keepInAir;
    public float airLiftForce;
}
