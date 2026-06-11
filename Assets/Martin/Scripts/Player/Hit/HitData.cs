using UnityEngine;

[System.Serializable]
public class HitData 
{
    [Header("Attack Values")]
    public float physicalScale = 1f;
    public float magicalScale = 1f;
    public float damageMultiplier = 1f;
    public float staggerCharge = 10f;

    [Header("Reaction")]
    public ThrowType throwType;
    public float stunDuration = 0.15f;
    public bool keepInAir;
    public float airLiftForce = 8f;
    public float pushForce = 8f;
    public float knockDownForce = 10f;

    [Range(0f, 1f)]
    public float knockDownForwardScale = 0.15f;
}
